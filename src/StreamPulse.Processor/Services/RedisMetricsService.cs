using StackExchange.Redis;
using StreamPulse.Processor.Aggregators;
using System.Text.Json;

namespace StreamPulse.Processor.Services;

public sealed class RedisMetricsService
{
    private readonly IDatabase _db;
    private const string MetricsKey = "streampulse:metrics:latest";
    private const string HistoryKey = "streampulse:metrics:history";

    public RedisMetricsService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task SaveMetricsAsync(WindowMetrics metrics)
    {
        var json = JsonSerializer.Serialize(new
        {
            metrics.Total,
            metrics.Completed,
            metrics.Failed,
            metrics.SuccessRate,
            TotalAmount = metrics.TotalAmount,
            AvgAmount = metrics.AvgAmount,
            metrics.AvgProcessingMs,
            metrics.P90Ms,
            metrics.P95Ms,
            metrics.P99Ms,
            metrics.AnomalyCount,
            metrics.FailureReasons,
            metrics.WindowStart,
            SavedAt = DateTimeOffset.UtcNow
        });

        await _db.StringSetAsync(MetricsKey, json, TimeSpan.FromMinutes(5));
        await _db.ListLeftPushAsync(HistoryKey, json);
        await _db.ListTrimAsync(HistoryKey, 0, 59);
    }

    public async Task<string?> GetLatestAsync()
        => await _db.StringGetAsync(MetricsKey);

    public async Task<IEnumerable<string>> GetHistoryAsync(int count = 10)
    {
        var results = await _db.ListRangeAsync(HistoryKey, 0, count - 1);
        return results.Select(r => r.ToString());
    }
}
