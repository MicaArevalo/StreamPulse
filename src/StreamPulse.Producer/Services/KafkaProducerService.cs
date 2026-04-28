using Confluent.Kafka;
using StreamPulse.Producer.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StreamPulse.Producer.Services;

public sealed class KafkaProducerService : IAsyncDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private const string TopicRaw = "transactions.raw";
    private const string TopicCompleted = "transactions.completed";
    private const string TopicFailed = "transactions.failed";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public KafkaProducerService(IConfiguration config, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092",
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageSendMaxRetries = 3
        };
        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    public async Task PublishAsync(TransactionEvent transaction, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(transaction, JsonOptions);
        var message = new Message<string, string>
        {
            Key = transaction.AccountId,
            Value = json
        };

        await _producer.ProduceAsync(TopicRaw, message, ct);

        var derivedTopic = transaction.Status switch
        {
            TransactionStatus.COMPLETED => TopicCompleted,
            TransactionStatus.FAILED    => TopicFailed,
            _                           => null
        };

        if (derivedTopic is not null)
            await _producer.ProduceAsync(derivedTopic, message, ct);

        _logger.LogDebug("Published {Status} transaction {Id} amount {Amount}",
            transaction.Status, transaction.TransactionId, transaction.Amount);
    }

    public ValueTask DisposeAsync()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
        return ValueTask.CompletedTask;
    }
}
