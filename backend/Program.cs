using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Data.Sqlite;
using StackExchange.Redis;
using AspNetCoreRateLimit;
using Serilog;
using Backend;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/ai-mate-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Add OpenTelemetry instrumentation
builder.Services.AddOpenTelemetryInstrumentation(builder.Configuration);

// Allow Blazor dev host and configurable production origins
var corsPolicy = "blazor-dev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            var originsEnv = Environment.GetEnvironmentVariable("FRONTEND_ORIGINS");
            var origins = new List<string> {
                "http://localhost:5173",
                "http://127.0.0.1:5173"
            };
            if (!string.IsNullOrWhiteSpace(originsEnv))
            {
                origins.AddRange(originsEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
            policy.WithOrigins(origins.ToArray())
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
});

// Application Insights (optional)
var appInsightsKey = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(appInsightsKey))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

// Redis Configuration (optional, falls back to in-memory)
var redisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? builder.Configuration["Redis:Connection"];
IConnectionMultiplexer? redis = null;
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    try
    {
        redis = ConnectionMultiplexer.Connect(redisConnection);
        builder.Services.AddSingleton(redis);
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "AIMate_";
        });
        Log.Information("Redis connected: {Connection}", redisConnection);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Redis connection failed, falling back to in-memory cache");
        builder.Services.AddDistributedMemoryCache();
    }
}
else
{
    Log.Information("Redis not configured, using in-memory cache");
    builder.Services.AddDistributedMemoryCache();
}

// Register cache service
builder.Services.AddSingleton<ICacheService, CacheService>();

// Rate Limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Period = "1m",
            Limit = 100 // 100 requests per minute
        },
        new RateLimitRule
        {
            Endpoint = "*",
            Period = "1h",
            Limit = 1000 // 1000 requests per hour
        }
    };
});
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

// Health Checks
var healthChecks = builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<MemoryHealthCheck>("memory");

// Only add Redis health check if Redis is configured
if (redis != null)
{
    healthChecks.AddCheck<RedisHealthCheck>("redis");
}

// Metrics collection
builder.Services.AddSingleton<MetricsCollector>();

// Database migrations
builder.Services.AddSingleton<DatabaseMigrations>();

// Initialize SQLCipher (requires SQLitePCLRaw.bundle_e_sqlcipher package)
SQLitePCL.Batteries_V2.Init();

// Database configuration
var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
Directory.CreateDirectory(dataDir);
var dbPassword = Environment.GetEnvironmentVariable("AIMATE_DB_PASSWORD");
if (string.IsNullOrWhiteSpace(dbPassword))
{
    dbPassword = "dev-only-password"; // fallback for local development
    Console.Error.WriteLine("WARN: AIMATE_DB_PASSWORD is not set. Using a DEV-ONLY default encryption key. Set AIMATE_DB_PASSWORD in production.");
}
var isEncrypted = !string.IsNullOrWhiteSpace(dbPassword);
var dbFileName = isEncrypted ? "app.enc.db" : "app.db"; // separate files to avoid format mismatch
var dbPath = Path.Combine(dataDir, dbFileName);
string connString;
if (isEncrypted)
{
    connString = new SqliteConnectionStringBuilder
    {
        DataSource = dbPath,
        Mode = SqliteOpenMode.ReadWriteCreate,
        Password = dbPassword
    }.ToString();
}
else
{
    // This branch should not occur due to fallback above; kept for completeness.
    connString = new SqliteConnectionStringBuilder
    {
        DataSource = dbPath,
        Mode = SqliteOpenMode.ReadWriteCreate
    }.ToString();
}

builder.Services.AddSingleton(new SqliteConnection(connString));
// Register repository with recovery if unencrypted DB is corrupted from a prior encrypted run
builder.Services.AddSingleton<InvoiceRepository>(sp =>
{
    try
    {
        return new InvoiceRepository(sp.GetRequiredService<SqliteConnection>());
    }
    catch (SqliteException ex) when (!isEncrypted && ex.SqliteErrorCode == 26)
    {
        // "file is not a database" — likely opened an encrypted DB without key or corrupted file
        try
        {
            if (File.Exists(dbPath))
            {
                Console.Error.WriteLine($"WARN: Removing invalid SQLite file at {dbPath} due to error 26. A fresh DB will be created.");
                File.Delete(dbPath);
            }
        }
        catch { }
        return new InvoiceRepository(sp.GetRequiredService<SqliteConnection>());
    }
});

var app = builder.Build();

// Run database migrations
using (var scope = app.Services.CreateScope())
{
    var migrations = scope.ServiceProvider.GetRequiredService<DatabaseMigrations>();
    try
    {
        await migrations.MigrateAsync();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to run database migrations");
    }
}

// OpenTelemetry API metrics middleware
app.UseMiddleware<ApiMetricsMiddleware>();

// Performance metrics middleware
app.UseMiddleware<PerformanceMetricsMiddleware>();

// Rate limiting middleware
app.UseIpRateLimiting();

app.UseCors(corsPolicy);

// Enhanced health checks endpoint
app.MapHealthChecks("/api/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            ok = report.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy,
            status = report.Status.ToString(),
            time = DateTimeOffset.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            })
        });
        await context.Response.WriteAsync(result);
    }
}).WithName("Health");

// Simple health endpoint for quick checks
app.MapGet("/health", () => Results.Ok(new { ok = true }))
   .WithName("HealthSimple");

// Metrics endpoint (performance monitoring)
app.MapGet("/api/metrics", (MetricsCollector metrics, HttpRequest req) =>
{
    var windowMinutes = int.TryParse(req.Query["window"], out var w) ? w : 5;
    var summary = metrics.GetSummary(TimeSpan.FromMinutes(windowMinutes));
    return Results.Ok(summary);
}).WithName("Metrics");

// (records moved to the end of file to avoid mixing type declarations with top-level statements)

var enrollFile = Path.Combine(dataDir, "enrolled.json");
var jsonOpts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
List<string> enrolled;
try { enrolled = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(enrollFile), jsonOpts) ?? new(); }
catch { enrolled = new List<string>(); }
void SaveEnrolled(){ try { File.WriteAllText(enrollFile, JsonSerializer.Serialize(enrolled, jsonOpts)); } catch { } }

// Azure Speaker Verification placeholders (for future integration)
var azureKey = builder.Configuration["AZURE_SPEECH_KEY"];
var azureRegion = builder.Configuration["AZURE_SPEECH_REGION"];
var azureProfileId = builder.Configuration["AZURE_SPEAKER_PROFILE_ID"]; // text-dependent/text-independent profile

app.MapPost("/voice/enroll", async (VoiceData payload) =>
{
    if (string.IsNullOrWhiteSpace(payload.DataUrl)) return Results.BadRequest(new { ok = false, error = "missing dataUrl" });
    // If Azure is configured, this is where you'd send payload to create/enroll a profile
    // For now, persist locally
    enrolled.Add(payload.DataUrl);
    if (enrolled.Count > 20) enrolled.RemoveAt(0);
    SaveEnrolled();
    await Task.CompletedTask;
    return Results.Ok(new { ok = true, count = enrolled.Count });
});

app.MapPost("/voice/verify", (VoiceData payload) =>
{
    if (string.IsNullOrWhiteSpace(payload.DataUrl)) return Results.BadRequest(new { ok = false, error = "missing dataUrl" });
    if (enrolled.Count == 0) return Results.Ok(new VerifyResponse(false, 0));

    // If Azure configured, this is where you'd send payload and return provider score
    // Dev placeholder: naive length-proximity
    var len = payload.DataUrl.Length;
    var best = enrolled.Select(s => Math.Abs(s.Length - len)).DefaultIfEmpty(int.MaxValue).Min();
    var score = Math.Clamp(1.0 - Math.Min(1.0, best / 20000.0), 0, 1);
    var ok = score > 0.6;
    return Results.Ok(new VerifyResponse(ok, score));
});

// Seed database (dev convenience)
using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<InvoiceRepository>();
    try { await repo.SeedIfEmptyAsync(); } catch { }
}

// Ensure Jobs schema exists
{
    var conn = app.Services.GetRequiredService<SqliteConnection>();
    using var con = new SqliteConnection(conn.ConnectionString);
    await con.OpenAsync();
    using var cmd = con.CreateCommand();
    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS jobs(
  id TEXT PRIMARY KEY,
  title TEXT NOT NULL,
  status TEXT,
  quotedPrice REAL
);";
    await cmd.ExecuteNonQueryAsync();
}

// Seed Jobs (dev convenience)
{
    var conn = app.Services.GetRequiredService<SqliteConnection>();
    using var con = new SqliteConnection(conn.ConnectionString);
    await con.OpenAsync();
    using (var check = con.CreateCommand())
    {
        check.CommandText = "SELECT COUNT(1) FROM jobs";
        var count = Convert.ToInt32(await check.ExecuteScalarAsync());
        if (count == 0)
        {
            var rng = new Random();
            using var tx = con.BeginTransaction();
            string[] verbs = new[] { "Install", "Repair", "Inspect", "Configure", "Upgrade", "Replace", "Calibrate", "Assemble" };
            string[] nouns = new[] { "HVAC", "Wiring", "Roof", "Plumbing", "Door", "Window", "Network", "Server", "Panel", "Lighting" };
            string[] statuses = new[] { "Upcoming", "In Progress", "Completed", "On Hold", "Cancelled" };
            int jmin = 80, jmax = 200;
            if (int.TryParse(Environment.GetEnvironmentVariable("JOB_SEED_MIN"), out var envJMin) && envJMin > 0) jmin = envJMin;
            if (int.TryParse(Environment.GetEnvironmentVariable("JOB_SEED_MAX"), out var envJMax) && envJMax >= jmin) jmax = envJMax;
            int total = rng.Next(jmin, jmax + 1); // configurable  
            for (int i = 1; i <= total; i++)
            {
                var id = Guid.NewGuid().ToString("N");
                var title = $"{verbs[rng.Next(verbs.Length)]} {nouns[rng.Next(nouns.Length)]} - #{i:000}";

                // Weighted status: In Progress/Upcoming dominate
                var r = rng.NextDouble();
                string status = r < 0.35 ? "In Progress"
                                : r < 0.65 ? "Upcoming"
                                : r < 0.85 ? "Completed"
                                : r < 0.95 ? "On Hold"
                                : "Cancelled";

                // Skewed price tiers: small(60%), mid(35%), large(5%)
                decimal price;
                var pt = rng.NextDouble();
                if (pt < 0.60) price = Math.Round((decimal)rng.Next(50, 500) + (decimal)rng.NextDouble(), 2);
                else if (pt < 0.95) price = Math.Round((decimal)rng.Next(500, 5000) + (decimal)rng.NextDouble(), 2);
                else price = Math.Round((decimal)rng.Next(5000, 30000) + (decimal)rng.NextDouble(), 2);

                using var ins = con.CreateCommand();
                ins.CommandText = "INSERT INTO jobs(id, title, status, quotedPrice) VALUES($id,$title,$status,$price)";
                ins.Parameters.AddWithValue("$id", id);
                ins.Parameters.AddWithValue("$title", title);
                ins.Parameters.AddWithValue("$status", status);
                ins.Parameters.AddWithValue("$price", price);
                await ins.ExecuteNonQueryAsync();
            }
            tx.Commit();
        }
    }
}

// Jobs API (list/create)
app.MapGet("/api/jobs", async (SqliteConnection conn) =>
{
    using var con = new SqliteConnection(conn.ConnectionString);
    await con.OpenAsync();
    using var cmd = con.CreateCommand();
    cmd.CommandText = "SELECT id, title, status, quotedPrice FROM jobs ORDER BY rowid DESC LIMIT 500";
    var list = new List<object>();
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var row = new
        {
            id = reader.GetString(0),
            title = reader.GetString(1),
            status = reader.IsDBNull(2) ? null : reader.GetString(2),
            quotedPrice = reader.IsDBNull(3) ? (decimal?)null : Convert.ToDecimal(reader.GetDouble(3))
        };
        list.Add(row);
    }
    return Results.Ok(list);
});

app.MapPost("/api/jobs", async (HttpRequest req, SqliteConnection conn) =>
{
    using var activity = OpenTelemetryConfig.StartActivity("CreateJob");
    
    var payload = await req.ReadFromJsonAsync<Dictionary<string, object?>>();
    var title = Convert.ToString(payload?.GetValueOrDefault("title"));
    if (string.IsNullOrWhiteSpace(title)) return Results.BadRequest(new { ok = false, error = "title is required" });
    var status = Convert.ToString(payload?.GetValueOrDefault("status"));
    if (string.IsNullOrWhiteSpace(status)) status = "Upcoming";
    decimal? quoted = null;
    var qv = payload?.GetValueOrDefault("quotedPrice");
    if (qv is not null)
    {
        try { quoted = Convert.ToDecimal(qv); } catch { quoted = null; }
    }
    var id = Guid.NewGuid().ToString("N");
    
    activity?.SetTag("job.title", title);
    activity?.SetTag("job.status", status);
    if (quoted.HasValue) activity?.SetTag("job.quoted_price", quoted.Value);
    
    using var con = new SqliteConnection(conn.ConnectionString);
    await con.OpenAsync();
    using var ins = con.CreateCommand();
    ins.CommandText = "INSERT INTO jobs(id, title, status, quotedPrice) VALUES($id,$title,$status,$price)";
    ins.Parameters.AddWithValue("$id", id);
    ins.Parameters.AddWithValue("$title", title);
    ins.Parameters.AddWithValue("$status", status);
    ins.Parameters.AddWithValue("$price", (object?)quoted ?? DBNull.Value);
    await ins.ExecuteNonQueryAsync();
    
    OpenTelemetryConfig.JobCreated.Add(1);
    if (quoted.HasValue) OpenTelemetryConfig.JobQuoteAmount.Record(quoted.Value);
    
    return Results.Ok(new { id, title, status, quotedPrice = quoted });
});

// Dashboard (real data)
app.MapGet("/api/dashboard", async (HttpRequest req, InvoiceRepository repo) =>
{
    using var activity = OpenTelemetryConfig.StartActivity("GetDashboard");
    OpenTelemetryConfig.DashboardViewed.Add(1);
    
    int weeks = 8;
    if (int.TryParse(req.Query["weeks"], out var w)) weeks = Math.Clamp(w, 4, 12);
    var today = DateOnly.FromDateTime(DateTime.Today);

    var invoices = await repo.GetUnpaidInvoicesAsync();
    var outstanding = invoices.Where(i => i.DueDate >= today).Sum(i => i.Amount);
    var overdue = invoices.Where(i => i.DueDate < today).Sum(i => i.Amount);
    var dueSoon = invoices.Where(i => i.DueDate >= today && i.DueDate <= today.AddDays(7)).Sum(i => i.Amount);
    var projected = new List<DashboardCashPoint>();
    for (int wi = 0; wi < weeks; wi++)
    {
        var start = today.AddDays(wi * 7);
        var end = start.AddDays(6);
        var sum = invoices.Where(i => i.DueDate >= start && i.DueDate <= end).Sum(i => i.Amount);
        projected.Add(new DashboardCashPoint(end.ToDateTime(TimeOnly.MinValue), sum));
    }
    var paidLast30 = await repo.GetPaidLastDaysAsync(30);

    var invDto = invoices.Select(i => new DashboardInvoice(i.Id, i.Customer, i.Amount, i.DueDate.ToDateTime(TimeOnly.MinValue), i.DueDate < today ? "overdue" : (i.DueDate <= today.AddDays(7) ? "due_soon" : "outstanding"))).ToList();
    
    activity?.SetTag("invoice.count", invoices.Count);
    activity?.SetTag("outstanding.total", outstanding);
    activity?.SetTag("overdue.total", overdue);
    
    return Results.Ok(new DashboardResponse(outstanding, overdue, dueSoon, paidLast30, invDto, projected));
});

// Invoices list (optional filter: status)
app.MapGet("/api/invoices", async (HttpRequest req, InvoiceRepository repo) =>
{
    string? status = req.Query["status"];
    var list = await repo.GetInvoicesAsync(string.IsNullOrWhiteSpace(status) ? null : status);
    return Results.Ok(list);
});

// Mark invoice paid
app.MapPost("/api/invoices/{id}/mark-paid", async (string id, HttpRequest req, InvoiceRepository repo) =>
{
    using var activity = OpenTelemetryConfig.StartActivity("MarkInvoicePaid");
    activity?.SetTag("invoice.id", id);
    
    var payload = await req.ReadFromJsonAsync<MarkPaidPayload>();
    var date = payload?.paidDate ?? DateTime.Today;
    var ok = await repo.MarkInvoicePaidAsync(id, DateOnly.FromDateTime(date));
    
    if (ok)
    {
        OpenTelemetryConfig.InvoicePaid.Add(1);
        activity?.SetTag("result", "success");
    }
    else
    {
        activity?.SetTag("result", "not_found");
    }
    
    return ok ? Results.Ok(new { ok = true }) : Results.NotFound();
});

// Add payment
app.MapPost("/api/payments", async (HttpRequest req, InvoiceRepository repo) =>
{
    var payload = await req.ReadFromJsonAsync<AddPaymentPayload>();
    if (payload is null || string.IsNullOrWhiteSpace(payload.invoiceId)) return Results.BadRequest(new { ok = false, error = "invalid payload" });
    var ok = await repo.AddPaymentAsync(payload.invoiceId!, (decimal)payload.amount, DateOnly.FromDateTime(payload.paidAt ?? DateTime.Today));
    return ok ? Results.Ok(new { ok = true }) : Results.BadRequest(new { ok = false });
});

// Invoice detail
app.MapGet("/api/invoices/{id}", async (string id, InvoiceRepository repo) =>
{
    var inv = await repo.GetInvoiceAsync(id);
    return inv is null ? Results.NotFound() : Results.Ok(inv);
});

// Import invoices (JSON array of InvoiceDto)
app.MapPost("/api/import/invoices", async (HttpRequest req, InvoiceRepository repo) =>
{
    try
    {
        var invoices = await req.ReadFromJsonAsync<List<InvoiceRepository.InvoiceDto>>();
        if (invoices is null) return Results.BadRequest(new { ok = false, error = "invalid payload" });
        await repo.ImportInvoicesAsync(invoices);
        return Results.Ok(new { ok = true, count = invoices.Count });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { ok = false, error = ex.Message });
    }
});

// Payments query (optionally filter by from/to date)
app.MapGet("/api/payments", async (HttpRequest req, SqliteConnection conn) =>
{
    string? fromS = req.Query["from"]; string? toS = req.Query["to"]; 
    using var con = new SqliteConnection(conn.ConnectionString);
    await con.OpenAsync();
    using var cmd = con.CreateCommand();
    if (!string.IsNullOrWhiteSpace(fromS) && !string.IsNullOrWhiteSpace(toS))
    {
        cmd.CommandText = "SELECT id, invoiceId, amount, paidAt FROM payments WHERE paidAt BETWEEN $from AND $to ORDER BY paidAt DESC";
        cmd.Parameters.AddWithValue("$from", fromS!);
        cmd.Parameters.AddWithValue("$to", toS!);
    }
    else
    {
        cmd.CommandText = "SELECT id, invoiceId, amount, paidAt FROM payments ORDER BY paidAt DESC LIMIT 200";
    }
    var list = new List<object>();
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var row = new {
            id = reader.GetString(0),
            invoiceId = reader.GetString(1),
            amount = reader.GetDouble(2),
            paidAt = reader.GetString(3)
        };
        list.Add(row);
    }
    return Results.Ok(list);
});

// Dev reseed endpoint (dangerous in prod). Clears data and reseeds.
app.MapPost("/api/dev/reseed", async (SqliteConnection conn, InvoiceRepository repo) =>
{
    var enable = Environment.GetEnvironmentVariable("ENABLE_DEV_RESEED");
    if (!string.Equals(enable, "true", StringComparison.OrdinalIgnoreCase))
    {
        return Results.NotFound();
    }
    using var con = new SqliteConnection(conn.ConnectionString);
    await con.OpenAsync();
    using (var tx = con.BeginTransaction())
    {
        using (var del = con.CreateCommand()) { del.CommandText = "DELETE FROM payments"; await del.ExecuteNonQueryAsync(); }
        using (var del2 = con.CreateCommand()) { del2.CommandText = "DELETE FROM invoices"; await del2.ExecuteNonQueryAsync(); }
        using (var del3 = con.CreateCommand()) { del3.CommandText = "DELETE FROM jobs"; await del3.ExecuteNonQueryAsync(); }
        tx.Commit();
    }
    try { await repo.SeedIfEmptyAsync(); } catch { }

    // Reseed jobs
    using (var con2 = new SqliteConnection(conn.ConnectionString))
    {
        await con2.OpenAsync();
        var rng = new Random();
        using var tx2 = con2.BeginTransaction();
        string[] verbs = new[] { "Install", "Repair", "Inspect", "Configure", "Upgrade", "Replace", "Calibrate", "Assemble" };
        string[] nouns = new[] { "HVAC", "Wiring", "Roof", "Plumbing", "Door", "Window", "Network", "Server", "Panel", "Lighting" };
        string[] statuses = new[] { "Upcoming", "In Progress", "Completed", "On Hold", "Cancelled" };
        int jmin = 80, jmax = 200;
        if (int.TryParse(Environment.GetEnvironmentVariable("JOB_SEED_MIN"), out var envJMin) && envJMin > 0) jmin = envJMin;
        if (int.TryParse(Environment.GetEnvironmentVariable("JOB_SEED_MAX"), out var envJMax) && envJMax >= jmin) jmax = envJMax;
        int total = rng.Next(jmin, jmax + 1);
        for (int i = 1; i <= total; i++)
        {
            var id = Guid.NewGuid().ToString("N");
            var title = $"{verbs[rng.Next(verbs.Length)]} {nouns[rng.Next(nouns.Length)]} - #{i:000}";

            var r = rng.NextDouble();
            string status = r < 0.35 ? "In Progress"
                            : r < 0.65 ? "Upcoming"
                            : r < 0.85 ? "Completed"
                            : r < 0.95 ? "On Hold"
                            : "Cancelled";

            decimal price;
            var pt = rng.NextDouble();
            if (pt < 0.60) price = Math.Round((decimal)rng.Next(50, 500) + (decimal)rng.NextDouble(), 2);
            else if (pt < 0.95) price = Math.Round((decimal)rng.Next(500, 5000) + (decimal)rng.NextDouble(), 2);
            else price = Math.Round((decimal)rng.Next(5000, 30000) + (decimal)rng.NextDouble(), 2);

            using var ins = con2.CreateCommand();
            ins.CommandText = "INSERT INTO jobs(id, title, status, quotedPrice) VALUES($id,$title,$status,$price)";
            ins.Parameters.AddWithValue("$id", id);
            ins.Parameters.AddWithValue("$title", title);
            ins.Parameters.AddWithValue("$status", status);
            ins.Parameters.AddWithValue("$price", price);
            await ins.ExecuteNonQueryAsync();
        }
        tx2.Commit();
    }
    return Results.Ok(new { ok = true });
}).WithDisplayName("Dev-ReSeed");

app.Run();
