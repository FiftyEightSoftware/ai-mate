using System.Diagnostics;

namespace Backend;

/// <summary>
/// Performance metrics middleware and monitoring
/// </summary>
public class PerformanceMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMetricsMiddleware> _logger;

    public PerformanceMetricsMiddleware(RequestDelegate next, ILogger<PerformanceMetricsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? "";
        
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var elapsed = sw.ElapsedMilliseconds;
            
            // Log slow requests (>1 second)
            if (elapsed > 1000)
            {
                _logger.LogWarning(
                    "Slow request: {Method} {Path} took {Duration}ms, Status: {StatusCode}",
                    context.Request.Method,
                    path,
                    elapsed,
                    context.Response.StatusCode
                );
            }
            
            // Add performance header
            context.Response.Headers["X-Response-Time-Ms"] = elapsed.ToString();
        }
    }
}

/// <summary>
/// In-memory metrics collector for dashboard
/// </summary>
public class MetricsCollector
{
    private readonly List<RequestMetric> _recentRequests = new();
    private readonly object _lock = new();
    private const int MaxMetrics = 1000;

    public void RecordRequest(string method, string path, int statusCode, long durationMs)
    {
        lock (_lock)
        {
            _recentRequests.Add(new RequestMetric
            {
                Timestamp = DateTimeOffset.UtcNow,
                Method = method,
                Path = path,
                StatusCode = statusCode,
                DurationMs = durationMs
            });

            // Keep only recent metrics
            if (_recentRequests.Count > MaxMetrics)
            {
                _recentRequests.RemoveAt(0);
            }
        }
    }

    public MetricsSummary GetSummary(TimeSpan window)
    {
        lock (_lock)
        {
            var cutoff = DateTimeOffset.UtcNow - window;
            var recent = _recentRequests.Where(r => r.Timestamp >= cutoff).ToList();

            if (recent.Count == 0)
            {
                return new MetricsSummary
                {
                    TotalRequests = 0,
                    AverageDurationMs = 0,
                    ErrorRate = 0,
                    RequestsPerSecond = 0
                };
            }

            var errors = recent.Count(r => r.StatusCode >= 400);
            var duration = (recent.Max(r => r.Timestamp) - recent.Min(r => r.Timestamp)).TotalSeconds;
            
            return new MetricsSummary
            {
                TotalRequests = recent.Count,
                AverageDurationMs = recent.Average(r => r.DurationMs),
                P95DurationMs = Percentile(recent.Select(r => r.DurationMs).ToList(), 0.95),
                P99DurationMs = Percentile(recent.Select(r => r.DurationMs).ToList(), 0.99),
                ErrorRate = errors / (double)recent.Count,
                RequestsPerSecond = duration > 0 ? recent.Count / duration : 0,
                TopEndpoints = recent
                    .GroupBy(r => r.Path)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => new EndpointStats
                    {
                        Path = g.Key,
                        Count = g.Count(),
                        AverageDurationMs = g.Average(r => r.DurationMs)
                    })
                    .ToList()
            };
        }
    }

    private static double Percentile(List<long> values, double percentile)
    {
        if (values.Count == 0) return 0;
        
        values.Sort();
        var index = (int)Math.Ceiling(percentile * values.Count) - 1;
        return values[Math.Max(0, Math.Min(index, values.Count - 1))];
    }

    private class RequestMetric
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public long DurationMs { get; set; }
    }
}

public class MetricsSummary
{
    public int TotalRequests { get; set; }
    public double AverageDurationMs { get; set; }
    public double P95DurationMs { get; set; }
    public double P99DurationMs { get; set; }
    public double ErrorRate { get; set; }
    public double RequestsPerSecond { get; set; }
    public List<EndpointStats> TopEndpoints { get; set; } = new();
}

public class EndpointStats
{
    public string Path { get; set; } = string.Empty;
    public int Count { get; set; }
    public double AverageDurationMs { get; set; }
}
