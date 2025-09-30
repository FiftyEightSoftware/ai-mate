using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// CORS: allow local frontend
const string CorsPolicy = "LocalCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy => policy
        .WithOrigins(
            "http://localhost:5210",
            "http://localhost:5211",
            "http://127.0.0.1:5210",
            "http://127.0.0.1:5211"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
    );
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var app = builder.Build();

app.UseCors(CorsPolicy);

// Health
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow }))
   .WithName("Health");

// In-memory data
var jobs = new List<Job>();
var clients = new List<Client>();

app.MapGet("/api/jobs", () => Results.Ok(jobs));
app.MapPost("/api/jobs", (Job job) => { job.Id = Guid.NewGuid(); jobs.Add(job); return Results.Created($"/api/jobs/{job.Id}", job); });
app.MapGet("/api/jobs/{id}", (Guid id) => jobs.FirstOrDefault(j => j.Id == id) is { } j ? Results.Ok(j) : Results.NotFound());

app.MapGet("/api/clients", () => Results.Ok(clients));
app.MapPost("/api/clients", (Client c) => { c.Id = Guid.NewGuid(); clients.Add(c); return Results.Created($"/api/clients/{c.Id}", c); });

app.MapGet("/", () => Results.Redirect("/health"));

app.Run();

record Job
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Status { get; set; }
    public decimal? QuotedPrice { get; set; }
}

record Client
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
}

// Expose Program for WebApplicationFactory in tests
public partial class Program { }
