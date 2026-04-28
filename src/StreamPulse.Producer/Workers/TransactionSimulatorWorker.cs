using StreamPulse.Producer.Models;
using StreamPulse.Producer.Services;

namespace StreamPulse.Producer.Workers;

public sealed class TransactionSimulatorWorker : BackgroundService
{
    private readonly KafkaProducerService _kafka;
    private readonly ILogger<TransactionSimulatorWorker> _logger;
    private readonly int _eventsPerSecond;
    private readonly Random _rng = new();

    private static readonly string[] Accounts =
        Enumerable.Range(1, 50).Select(i => $"ACC-{i:D3}").ToArray();

    public TransactionSimulatorWorker(
        KafkaProducerService kafka,
        IConfiguration config,
        ILogger<TransactionSimulatorWorker> logger)
    {
        _kafka = kafka;
        _logger = logger;
        _eventsPerSecond = config.GetValue<int>("Simulator:EventsPerSecond", 10);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TransactionSimulator started — {Eps} events/sec", _eventsPerSecond);
        var delay = TimeSpan.FromMilliseconds(1000.0 / _eventsPerSecond);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var tx = GenerateTransaction();
                await _kafka.PublishAsync(tx, stoppingToken);
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing transaction");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }

    private TransactionEvent GenerateTransaction()
    {
        var roll = _rng.NextDouble();
        var status = roll switch
        {
            < 0.85 => TransactionStatus.COMPLETED,
            < 0.97 => TransactionStatus.FAILED,
            _      => TransactionStatus.COMPLETED
        };

        var isAnomaly = roll >= 0.97;
        var baseAmount = (decimal)(_rng.NextDouble() * 50_000 + 100);
        var amount = isAnomaly ? baseAmount * 10 : baseAmount;

        return new TransactionEvent
        {
            AccountId        = Accounts[_rng.Next(Accounts.Length)],
            Amount           = Math.Round(amount, 2),
            Currency         = "ARS",
            Type             = (TransactionType)_rng.Next(4),
            Channel          = (TransactionChannel)_rng.Next(3),
            Status           = status,
            FailureReason    = status == TransactionStatus.FAILED
                                ? new[] { "INSUFFICIENT_FUNDS", "TIMEOUT", "FRAUD_DETECTED" }[_rng.Next(3)]
                                : null,
            ProcessingTimeMs = _rng.Next(50, 500)
        };
    }
}
