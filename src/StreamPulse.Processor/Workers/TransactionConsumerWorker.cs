using Confluent.Kafka;
using StreamPulse.Processor.Aggregators;
using StreamPulse.Processor.Anomaly;
using StreamPulse.Processor.Services;
using System.Text.Json;
using System.Threading.Channels;

namespace StreamPulse.Processor.Workers;

public sealed class TransactionConsumerWorker : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly TransactionAggregator _aggregator;
    private readonly AnomalyDetector _anomalyDetector;
    private readonly RedisMetricsService _redis;
    private readonly ILogger<TransactionConsumerWorker> _logger;
    private readonly Channel<string> _buffer;

    public TransactionConsumerWorker(
        IConfiguration config,
        TransactionAggregator aggregator,
        AnomalyDetector anomalyDetector,
        RedisMetricsService redis,
        ILogger<TransactionConsumerWorker> logger)
    {
        _config = config;
        _aggregator = aggregator;
        _anomalyDetector = anomalyDetector;
        _redis = redis;
        _logger = logger;
        _buffer = Channel.CreateBounded<string>(new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumerTask = Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
        var processorTask = Task.Run(() => ProcessLoop(stoppingToken), stoppingToken);
        var flushTask = Task.Run(() => FlushLoop(stoppingToken), stoppingToken);

        await Task.WhenAll(consumerTask, processorTask, flushTask);
    }

    private async Task ConsumeLoop(CancellationToken ct)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "streampulse-processor",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe("transactions.raw");

        _logger.LogInformation("Processor consumer started");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromMilliseconds(100));
                if (result?.Message?.Value is null) continue;

                await _buffer.Writer.WriteAsync(result.Message.Value, ct);
                consumer.Commit(result);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consuming from Kafka");
            }
        }

        consumer.Close();
    }

    private async Task ProcessLoop(CancellationToken ct)
    {
        await foreach (var json in _buffer.Reader.ReadAllAsync(ct))
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var accountId    = root.GetProperty("AccountId").GetString() ?? "";
                var amount       = root.GetProperty("Amount").GetDecimal();
                var statusElement = root.GetProperty("Status");
                var status = statusElement.ValueKind == JsonValueKind.Number
                    ? statusElement.GetInt32() switch { 0 => "COMPLETED", 1 => "FAILED", _ => "PENDING" }
                    : statusElement.GetString() ?? "";
                var processingMs = root.GetProperty("ProcessingTimeMs").GetInt32();
                var failureReason = root.TryGetProperty("FailureReason", out var frEl) && frEl.ValueKind != JsonValueKind.Null
                    ? frEl.GetString()
                    : null;

                var (mean, stdDev) = _aggregator.GetAmountStats();
                var isAnomaly = _anomalyDetector.IsAnomaly(accountId, amount, mean, stdDev);

                _aggregator.Record(status, amount, processingMs, isAnomaly, failureReason);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process message");
            }
        }
    }

    private async Task FlushLoop(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
        while (await timer.WaitForNextTickAsync(ct))
        {
            var metrics = _aggregator.Flush();
            await _redis.SaveMetricsAsync(metrics);
            _logger.LogInformation(
                "Window flushed — Total:{Total} Success:{Rate:F1}% Anomalies:{A}",
                metrics.Total, metrics.SuccessRate, metrics.AnomalyCount);
        }
    }
}
