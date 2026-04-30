using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using StreamPulse.Api.Hubs;

namespace StreamPulse.Api.Services;

public sealed class MetricsBroadcastService : BackgroundService
{
    private readonly IHubContext<MetricsHub> _hub;
    private readonly IDatabase _redis;
    private readonly ILogger<MetricsBroadcastService> _logger;
    private string? _lastBroadcastValue;

    public MetricsBroadcastService(
        IHubContext<MetricsHub> hub,
        IConnectionMultiplexer redis,
        ILogger<MetricsBroadcastService> logger)
    {
        _hub = hub;
        _redis = redis.GetDatabase();
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var value = await _redis.StringGetAsync("streampulse:metrics:latest");
                var current = value.IsNullOrEmpty ? null : value.ToString();
                if (current != null && current != _lastBroadcastValue)
                {
                    _lastBroadcastValue = current;
                    await _hub.Clients.Group("metrics").SendAsync("MetricsUpdate", current, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error broadcasting metrics");
            }
        }
    }
}
