using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Data.Sqlite;
using StackExchange.Redis;

namespace Backend;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly SqliteConnection _connection;

    public DatabaseHealthCheck(SqliteConnection connection)
    {
        _connection = connection;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync(cancellationToken);
            }

            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("Database is responsive");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database check failed", ex);
        }
    }
}

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.PingAsync();
            return HealthCheckResult.Healthy("Redis is responsive");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Redis check failed", ex);
        }
    }
}

public class MemoryHealthCheck : IHealthCheck
{
    private readonly long _thresholdBytes = 1024L * 1024 * 1024; // 1 GB

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var allocated = GC.GetTotalMemory(forceFullCollection: false);
        var data = new Dictionary<string, object>
        {
            { "AllocatedMB", allocated / 1024 / 1024 },
            { "Gen0Collections", GC.CollectionCount(0) },
            { "Gen1Collections", GC.CollectionCount(1) },
            { "Gen2Collections", GC.CollectionCount(2) }
        };

        var status = allocated < _thresholdBytes 
            ? HealthCheckResult.Healthy("Memory usage is normal", data)
            : HealthCheckResult.Degraded("Memory usage is high", null, data);

        return Task.FromResult(status);
    }
}
