using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Backend;

/// <summary>
/// OpenTelemetry instrumentation for AI Mate
/// Captures metrics, traces, and events for complete observability
/// </summary>
public static class OpenTelemetryConfig
{
    public const string ServiceName = "ai-mate-api";
    public const string ServiceVersion = "1.0.0";

    // Custom meter for business metrics
    public static readonly Meter AppMeter = new(ServiceName, ServiceVersion);
    
    // Custom activity source for tracing
    public static readonly ActivitySource AppActivitySource = new(ServiceName, ServiceVersion);

    // Business Metrics
    public static readonly Counter<long> InvoiceCreated = AppMeter.CreateCounter<long>(
        "ai_mate.invoice.created",
        description: "Number of invoices created");

    public static readonly Counter<long> InvoicePaid = AppMeter.CreateCounter<long>(
        "ai_mate.invoice.paid",
        description: "Number of invoices marked as paid");

    public static readonly Counter<long> JobCreated = AppMeter.CreateCounter<long>(
        "ai_mate.job.created",
        description: "Number of jobs created");

    public static readonly Counter<long> DashboardViewed = AppMeter.CreateCounter<long>(
        "ai_mate.dashboard.viewed",
        description: "Number of dashboard views");

    public static readonly Histogram<double> InvoiceAmount = AppMeter.CreateHistogram<double>(
        "ai_mate.invoice.amount",
        unit: "GBP",
        description: "Invoice amounts");

    public static readonly Histogram<double> JobQuoteAmount = AppMeter.CreateHistogram<double>(
        "ai_mate.job.quote_amount",
        unit: "GBP",
        description: "Job quoted amounts");

    public static readonly Counter<long> DatabaseQuery = AppMeter.CreateCounter<long>(
        "ai_mate.database.query",
        description: "Number of database queries");

    public static readonly Counter<long> CacheHit = AppMeter.CreateCounter<long>(
        "ai_mate.cache.hit",
        description: "Number of cache hits");

    public static readonly Counter<long> CacheMiss = AppMeter.CreateCounter<long>(
        "ai_mate.cache.miss",
        description: "Number of cache misses");

    public static readonly Histogram<double> ApiResponseTime = AppMeter.CreateHistogram<double>(
        "ai_mate.api.response_time",
        unit: "ms",
        description: "API response times");

    public static readonly Counter<long> ApiError = AppMeter.CreateCounter<long>(
        "ai_mate.api.error",
        description: "Number of API errors");

    /// <summary>
    /// Configure OpenTelemetry with metrics, traces, and resource attributes
    /// </summary>
    public static IServiceCollection AddOpenTelemetryInstrumentation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "";
        var useConsoleExporter = configuration.GetValue<bool>("OTEL_USE_CONSOLE", true);

        // Configure resource attributes
        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(
                serviceName: ServiceName,
                serviceVersion: ServiceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development",
                ["service.namespace"] = "ai-mate",
                ["service.instance.id"] = Environment.MachineName
            });

        // Add OpenTelemetry
        services.AddOpenTelemetry()
            .ConfigureResource(r => r = resourceBuilder)
            .WithMetrics(metrics =>
            {
                metrics
                    // ASP.NET Core instrumentation
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            activity.SetTag("http.request.user_agent", request.Headers.UserAgent.ToString());
                            activity.SetTag("http.request.content_length", request.ContentLength);
                        };
                        options.EnrichWithHttpResponse = (activity, response) =>
                        {
                            activity.SetTag("http.response.content_length", response.ContentLength);
                        };
                    })
                    // HTTP client instrumentation
                    .AddHttpClientInstrumentation()
                    // Runtime metrics
                    .AddRuntimeInstrumentation()
                    // Process metrics
                    .AddProcessInstrumentation()
                    // Custom business metrics
                    .AddMeter(ServiceName);

                // Console exporter (for development)
                if (useConsoleExporter)
                {
                    metrics.AddConsoleExporter((exporterOptions, metricReaderOptions) =>
                    {
                        metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 30000;
                    });
                }

                // OTLP exporter (for production - Jaeger, Prometheus, etc.)
                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            })
            .WithTracing(tracing =>
            {
                tracing
                    // ASP.NET Core tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            activity.SetTag("http.request.path", request.Path);
                            activity.SetTag("http.request.method", request.Method);
                        };
                    })
                    // HTTP client tracing
                    .AddHttpClientInstrumentation()
                    // SQL client tracing (for database queries)
                    .AddSqlClientInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                        options.RecordException = true;
                        options.EnableConnectionLevelAttributes = true;
                    })
                    // Custom activity source
                    .AddSource(ServiceName);

                // Console exporter (for development)
                if (useConsoleExporter)
                {
                    tracing.AddConsoleExporter();
                }

                // OTLP exporter (for production)
                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            });

        return services;
    }

    /// <summary>
    /// Record a business event with custom attributes
    /// </summary>
    public static void RecordEvent(string eventName, Dictionary<string, object>? attributes = null)
    {
        using var activity = AppActivitySource.StartActivity(eventName, ActivityKind.Internal);
        
        if (activity != null && attributes != null)
        {
            foreach (var attr in attributes)
            {
                activity.SetTag(attr.Key, attr.Value);
            }
        }
    }

    /// <summary>
    /// Start a traced operation
    /// </summary>
    public static Activity? StartActivity(string operationName, ActivityKind kind = ActivityKind.Internal)
    {
        return AppActivitySource.StartActivity(operationName, kind);
    }
}

/// <summary>
/// Middleware to capture API response times
/// </summary>
public class ApiMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiMetricsMiddleware> _logger;

    public ApiMetricsMiddleware(RequestDelegate next, ILogger<ApiMetricsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? "unknown";

        try
        {
            await _next(context);
            
            stopwatch.Stop();
            
            // Record response time
            OpenTelemetryConfig.ApiResponseTime.Record(
                stopwatch.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("http.route", path),
                new KeyValuePair<string, object?>("http.method", context.Request.Method),
                new KeyValuePair<string, object?>("http.status_code", context.Response.StatusCode));

            // Record errors
            if (context.Response.StatusCode >= 400)
            {
                OpenTelemetryConfig.ApiError.Add(1,
                    new KeyValuePair<string, object?>("http.route", path),
                    new KeyValuePair<string, object?>("http.status_code", context.Response.StatusCode));
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            OpenTelemetryConfig.ApiError.Add(1,
                new KeyValuePair<string, object?>("http.route", path),
                new KeyValuePair<string, object?>("error.type", ex.GetType().Name));

            _logger.LogError(ex, "Unhandled exception in {Path}", path);
            throw;
        }
    }
}
