using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Testcontainers.Kafka;
using StreamPulse.Producer.Models;
using FluentAssertions;

namespace StreamPulse.Tests.Producer;

[Trait("Category", "Integration")]
public sealed class TransactionSimulatorTests : IAsyncLifetime
{
    private readonly KafkaContainer _kafka = new KafkaBuilder()
        .WithImage("confluentinc/cp-kafka:7.6.0")
        .Build();

    public async Task InitializeAsync() => await _kafka.StartAsync();
    public async Task DisposeAsync() => await _kafka.DisposeAsync();

    [Fact]
    public async Task Producer_ShouldPublishTransactionToKafka()
    {
        var bootstrapServers = _kafka.GetBootstrapAddress();
        const string topic = "test.transactions";

        using var adminClient = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = bootstrapServers }).Build();

        await adminClient.CreateTopicsAsync(new[]
        {
            new TopicSpecification { Name = topic, NumPartitions = 1, ReplicationFactor = 1 }
        });

        var producerConfig = new ProducerConfig { BootstrapServers = bootstrapServers };
        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        var tx = new TransactionEvent
        {
            AccountId = "ACC-001",
            Amount = 1500m,
            Currency = "ARS",
            Type = TransactionType.PAYMENT,
            Channel = TransactionChannel.ONLINE,
            Status = TransactionStatus.COMPLETED,
            ProcessingTimeMs = 120
        };

        var json = System.Text.Json.JsonSerializer.Serialize(tx);
        var result = await producer.ProduceAsync(topic,
            new Message<string, string> { Key = tx.AccountId, Value = json });

        result.Status.Should().Be(PersistenceStatus.Persisted);
        result.Topic.Should().Be(topic);
    }
}
