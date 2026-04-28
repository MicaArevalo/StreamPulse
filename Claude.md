# CLAUDE.md — StreamPulse

> Este archivo guía a Claude Code en la construcción completa del proyecto StreamPulse.
> Ejecutar las tareas en orden. Cada tarea debe compilar sin errores antes de continuar.

---

## Contexto del proyecto

**StreamPulse** es un pipeline de analytics fintech en tiempo real construido con .NET 8 y Kafka.
Es el **Proyecto 1 (P1)** de un portfolio profesional de backend .NET senior.

### Qué hace
- Un **Producer** simula transacciones financieras (transferencias, pagos, depósitos) y las publica en Kafka
- Un **Processor** consume esos eventos, los agrega en ventanas de tiempo, detecta anomalías y guarda métricas en Redis
- Una **Api** expone los datos via REST y pushea actualizaciones en tiempo real al dashboard via SignalR

### Por qué importa en el portfolio
- Demuestra manejo de Kafka (producers, consumers, consumer groups, DLQ)
- Muestra backpressure handling con Channel bounded buffer
- Dashboard en tiempo real con SignalR — demo visual impactante en entrevistas
- Testcontainers para tests de integración con Kafka real (no mocks)
- Se conecta con PaymentHub (P3): los eventos PaymentCompleted/PaymentFailed alimentan StreamPulse

---

## Entorno

- OS: Windows
- Shell: PowerShell
- Ruta del proyecto: `C:\Users\areva\Repositorios Mica\StreamPulse`
- .NET SDK: 8.0
- Docker Desktop con WSL2 backend

---

## Stack técnico

| Componente | Tecnología |
|---|---|
| Runtime | .NET 8 |
| Kafka client | Confluent.Kafka |
| Mensajería | Apache Kafka + Zookeeper |
| Cache / estado | Redis |
| Real-time push | SignalR (ASP.NET Core) |
| Dashboard | Blazor Server |
| Tests | xUnit + Testcontainers |
| CI/CD | GitHub Actions |
| Contenedores | Docker + Docker Compose |

---

## Arquitectura — src/ layout
StreamPulse/
├── CLAUDE.md
├── StreamPulse.sln
├── .gitignore
├── docker-compose.yml
├── docker-compose.override.yml
├── README.md
├── src/
│   ├── StreamPulse.Producer/
│   │   ├── StreamPulse.Producer.csproj
│   │   ├── Program.cs
│   │   ├── Workers/
│   │   │   └── TransactionSimulatorWorker.cs
│   │   ├── Models/
│   │   │   └── TransactionEvent.cs
│   │   ├── Services/
│   │   │   └── KafkaProducerService.cs
│   │   └── appsettings.json
│   ├── StreamPulse.Processor/
│   │   ├── StreamPulse.Processor.csproj
│   │   ├── Program.cs
│   │   ├── Workers/
│   │   │   └── TransactionConsumerWorker.cs
│   │   ├── Aggregators/
│   │   │   └── TransactionAggregator.cs
│   │   ├── Anomaly/
│   │   │   └── AnomalyDetector.cs
│   │   ├── Services/
│   │   │   ├── RedisMetricsService.cs
│   │   │   └── KafkaConsumerService.cs
│   │   └── appsettings.json
│   └── StreamPulse.Api/
│       ├── StreamPulse.Api.csproj
│       ├── Program.cs
│       ├── Hubs/
│       │   └── MetricsHub.cs
│       ├── Controllers/
│       │   └── MetricsController.cs
│       ├── Services/
│       │   └── MetricsBroadcastService.cs
│       └── appsettings.json
└── tests/
└── StreamPulse.Tests/
├── StreamPulse.Tests.csproj
├── Producer/
│   └── TransactionSimulatorTests.cs
└── Processor/
└── AnomalyDetectorTests.cs

---

## Eventos de dominio

### TransactionEvent
```json
{
  "transactionId": "uuid",
  "accountId": "ACC-001",
  "amount": 1500.00,
  "currency": "ARS",
  "type": "TRANSFER | PAYMENT | DEPOSIT | WITHDRAWAL",
  "channel": "ONLINE | ATM | POS",
  "status": "COMPLETED | FAILED | PENDING",
  "failureReason": "INSUFFICIENT_FUNDS | TIMEOUT | FRAUD_DETECTED | null",
  "processingTimeMs": 142,
  "timestamp": "2026-04-24T20:00:00Z"
}
```

### Topics Kafka
| Topic | Descripción | Particiones |
|---|---|---|
| `transactions.raw` | Todos los eventos sin procesar | 3 |
| `transactions.completed` | Solo completadas | 1 |
| `transactions.failed` | Solo fallidas | 1 |
| `anomalies.alerts` | Anomalías detectadas | 1 |
| `transactions.dlq` | Dead Letter Queue | 1 |

### Distribución simulada del Producer
- 85% COMPLETED
- 12% FAILED
- 3% anomalías (monto > 3 desvíos estándar o velocidad > umbral)

---

## Métricas en tiempo real (dashboard)

| Métrica | Descripción |
|---|---|
| Transacciones / minuto | Volumen actual |
| Tasa de éxito % | Completadas vs total |
| Volumen $ último minuto | Suma de montos |
| Latencia promedio (ms) | ProcessingTimeMs promedio |
| Anomalías activas | Detectadas en últimos 5 min |
| Consumer lag (ms) | Salud del pipeline Kafka |

---

## Tareas de construcción

### TAREA 1 — Solución y estructura base

```powershell
cd "C:\Users\areva\Repositorios Mica"
New-Item -ItemType Directory -Name "StreamPulse"
cd StreamPulse

dotnet new sln -n StreamPulse

New-Item -ItemType Directory -Path "src"
New-Item -ItemType Directory -Path "tests"

dotnet new worker -n StreamPulse.Producer -o src/StreamPulse.Producer
dotnet new worker -n StreamPulse.Processor -o src/StreamPulse.Processor
dotnet new webapi -n StreamPulse.Api -o src/StreamPulse.Api --no-openapi
dotnet new xunit -n StreamPulse.Tests -o tests/StreamPulse.Tests

dotnet sln add src/StreamPulse.Producer/StreamPulse.Producer.csproj
dotnet sln add src/StreamPulse.Processor/StreamPulse.Processor.csproj
dotnet sln add src/StreamPulse.Api/StreamPulse.Api.csproj
dotnet sln add tests/StreamPulse.Tests/StreamPulse.Tests.csproj

dotnet new gitignore
```

Verificar: `dotnet build` debe completar sin errores.

---

### TAREA 2 — Paquetes NuGet

**StreamPulse.Producer:**
```powershell
cd src/StreamPulse.Producer
dotnet add package Confluent.Kafka --version 2.3.0
dotnet add package Microsoft.Extensions.Hosting
dotnet add package Microsoft.Extensions.Logging.Console
cd ../..
```

**StreamPulse.Processor:**
```powershell
cd src/StreamPulse.Processor
dotnet add package Confluent.Kafka --version 2.3.0
dotnet add package StackExchange.Redis --version 2.7.33
dotnet add package Microsoft.Extensions.Hosting
dotnet add package Microsoft.Extensions.Logging.Console
cd ../..
```

**StreamPulse.Api:**
```powershell
cd src/StreamPulse.Api
dotnet add package StackExchange.Redis --version 2.7.33
dotnet add package Microsoft.AspNetCore.SignalR
cd ../..
```

**StreamPulse.Tests:**
```powershell
cd tests/StreamPulse.Tests
dotnet add package Testcontainers.Kafka --version 3.8.0
dotnet add package Testcontainers --version 3.8.0
dotnet add package Confluent.Kafka --version 2.3.0
dotnet add package FluentAssertions --version 6.12.0
dotnet add reference ../../src/StreamPulse.Producer/StreamPulse.Producer.csproj
dotnet add reference ../../src/StreamPulse.Processor/StreamPulse.Processor.csproj
cd ../..
```

Verificar: `dotnet restore && dotnet build`

---

### TAREA 3 — Modelos compartidos

Crear `src/StreamPulse.Producer/Models/TransactionEvent.cs`:

```csharp
namespace StreamPulse.Producer.Models;

public record TransactionEvent
{
    public Guid TransactionId { get; init; } = Guid.NewGuid();
    public string AccountId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "ARS";
    public TransactionType Type { get; init; }
    public TransactionChannel Channel { get; init; }
    public TransactionStatus Status { get; init; }
    public string? FailureReason { get; init; }
    public int ProcessingTimeMs { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public enum TransactionType { TRANSFER, PAYMENT, DEPOSIT, WITHDRAWAL }
public enum TransactionChannel { ONLINE, ATM, POS }
public enum TransactionStatus { COMPLETED, FAILED, PENDING }
```

---

### TAREA 4 — Producer: KafkaProducerService

Crear `src/StreamPulse.Producer/Services/KafkaProducerService.cs`:

```csharp
using Confluent.Kafka;
using StreamPulse.Producer.Models;
using System.Text.Json;

namespace StreamPulse.Producer.Services;

public sealed class KafkaProducerService : IAsyncDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private const string TopicRaw = "transactions.raw";
    private const string TopicCompleted = "transactions.completed";
    private const string TopicFailed = "transactions.failed";

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
        var json = JsonSerializer.Serialize(transaction);
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
```

---

### TAREA 5 — Producer: TransactionSimulatorWorker

Crear `src/StreamPulse.Producer/Workers/TransactionSimulatorWorker.cs`:

```csharp
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
```

---

### TAREA 6 — Producer: Program.cs y appsettings

Reemplazar `src/StreamPulse.Producer/Program.cs`:

```csharp
using StreamPulse.Producer.Services;
using StreamPulse.Producer.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<KafkaProducerService>();
builder.Services.AddHostedService<TransactionSimulatorWorker>();

var host = builder.Build();
host.Run();
```

Reemplazar `src/StreamPulse.Producer/appsettings.json`:

```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  },
  "Simulator": {
    "EventsPerSecond": 10
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

---

### TAREA 7 — Processor: Aggregators y Anomaly

Crear `src/StreamPulse.Processor/Aggregators/TransactionAggregator.cs`:

```csharp
namespace StreamPulse.Processor.Aggregators;

public sealed class WindowMetrics
{
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Failed { get; set; }
    public decimal TotalAmount { get; set; }
    public double TotalProcessingMs { get; set; }
    public int AnomalyCount { get; set; }
    public DateTimeOffset WindowStart { get; set; } = DateTimeOffset.UtcNow;

    public double SuccessRate => Total == 0 ? 0 : (double)Completed / Total * 100;
    public double AvgProcessingMs => Total == 0 ? 0 : TotalProcessingMs / Total;
    public decimal AvgAmount => Total == 0 ? 0 : TotalAmount / Total;
}

public sealed class TransactionAggregator
{
    private WindowMetrics _current = new();
    private readonly List<decimal> _amounts = new();
    private readonly Lock _lock = new();

    public void Record(string status, decimal amount, int processingMs, bool isAnomaly)
    {
        lock (_lock)
        {
            _current.Total++;
            _current.TotalAmount += amount;
            _current.TotalProcessingMs += processingMs;
            _amounts.Add(amount);

            if (status == "COMPLETED") _current.Completed++;
            else if (status == "FAILED") _current.Failed++;
            if (isAnomaly) _current.AnomalyCount++;
        }
    }

    public WindowMetrics Flush()
    {
        lock (_lock)
        {
            var snapshot = _current;
            _current = new WindowMetrics();
            _amounts.Clear();
            return snapshot;
        }
    }

    public (double Mean, double StdDev) GetAmountStats()
    {
        lock (_lock)
        {
            if (_amounts.Count == 0) return (0, 0);
            var mean = _amounts.Average(a => (double)a);
            var variance = _amounts.Average(a => Math.Pow((double)a - mean, 2));
            return (mean, Math.Sqrt(variance));
        }
    }
}
```

Crear `src/StreamPulse.Processor/Anomaly/AnomalyDetector.cs`:

```csharp
namespace StreamPulse.Processor.Anomaly;

public sealed class AnomalyDetector
{
    private const double StdDevThreshold = 3.0;
    private const int VelocityWindowSeconds = 60;
    private const int VelocityThreshold = 5;

    private readonly Dictionary<string, Queue<DateTimeOffset>> _accountActivity = new();
    private readonly Lock _lock = new();

    public bool IsAnomaly(string accountId, decimal amount, double mean, double stdDev)
    {
        if (stdDev > 0 && Math.Abs((double)amount - mean) > StdDevThreshold * stdDev)
            return true;

        return IsHighVelocity(accountId);
    }

    private bool IsHighVelocity(string accountId)
    {
        lock (_lock)
        {
            if (!_accountActivity.TryGetValue(accountId, out var queue))
            {
                queue = new Queue<DateTimeOffset>();
                _accountActivity[accountId] = queue;
            }

            var cutoff = DateTimeOffset.UtcNow.AddSeconds(-VelocityWindowSeconds);
            while (queue.Count > 0 && queue.Peek() < cutoff)
                queue.Dequeue();

            queue.Enqueue(DateTimeOffset.UtcNow);
            return queue.Count > VelocityThreshold;
        }
    }
}
```

---

### TAREA 8 — Processor: TransactionConsumerWorker

Crear `src/StreamPulse.Processor/Workers/TransactionConsumerWorker.cs`:

```csharp
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
                var status       = root.GetProperty("Status").GetString() ?? "";
                var processingMs = root.GetProperty("ProcessingTimeMs").GetInt32();

                var (mean, stdDev) = _aggregator.GetAmountStats();
                var isAnomaly = _anomalyDetector.IsAnomaly(accountId, amount, mean, stdDev);

                _aggregator.Record(status, amount, processingMs, isAnomaly);
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
```

---

### TAREA 9 — Processor: RedisMetricsService y Program.cs

Crear `src/StreamPulse.Processor/Services/RedisMetricsService.cs`:

```csharp
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
            metrics.AnomalyCount,
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
```

Reemplazar `src/StreamPulse.Processor/Program.cs`:

```csharp
using StackExchange.Redis;
using StreamPulse.Processor.Aggregators;
using StreamPulse.Processor.Anomaly;
using StreamPulse.Processor.Services;
using StreamPulse.Processor.Workers;

var builder = Host.CreateApplicationBuilder(args);

var redisConn = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConn));

builder.Services.AddSingleton<TransactionAggregator>();
builder.Services.AddSingleton<AnomalyDetector>();
builder.Services.AddSingleton<RedisMetricsService>();
builder.Services.AddHostedService<TransactionConsumerWorker>();

var host = builder.Build();
host.Run();
```

Reemplazar `src/StreamPulse.Processor/appsettings.json`:

```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

---

### TAREA 10 — Api: SignalR Hub, Controller y Program.cs

Crear `src/StreamPulse.Api/Hubs/MetricsHub.cs`:

```csharp
using Microsoft.AspNetCore.SignalR;

namespace StreamPulse.Api.Hubs;

public sealed class MetricsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "metrics");
        await base.OnConnectedAsync();
    }
}
```

Crear `src/StreamPulse.Api/Controllers/MetricsController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json;

namespace StreamPulse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MetricsController : ControllerBase
{
    private readonly IDatabase _redis;

    public MetricsController(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest()
    {
        var value = await _redis.StringGetAsync("streampulse:metrics:latest");
        if (value.IsNullOrEmpty) return NotFound();
        return Content(value!, "application/json");
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int count = 10)
    {
        var results = await _redis.ListRangeAsync("streampulse:metrics:history", 0, count - 1);
        var items = results.Select(r => JsonDocument.Parse(r.ToString()).RootElement);
        return Ok(items);
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok", timestamp = DateTimeOffset.UtcNow });
}
```

Crear `src/StreamPulse.Api/Services/MetricsBroadcastService.cs`:

```csharp
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using StreamPulse.Api.Hubs;

namespace StreamPulse.Api.Services;

public sealed class MetricsBroadcastService : BackgroundService
{
    private readonly IHubContext<MetricsHub> _hub;
    private readonly IDatabase _redis;
    private readonly ILogger<MetricsBroadcastService> _logger;

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
                if (!value.IsNullOrEmpty)
                    await _hub.Clients.Group("metrics").SendAsync("MetricsUpdate", value.ToString(), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error broadcasting metrics");
            }
        }
    }
}
```

Reemplazar `src/StreamPulse.Api/Program.cs`:

```csharp
using StackExchange.Redis;
using StreamPulse.Api.Hubs;
using StreamPulse.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var redisConn = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConn));

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddHostedService<MetricsBroadcastService>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseCors();
app.MapControllers();
app.MapHub<MetricsHub>("/hubs/metrics");
app.MapGet("/", () => "StreamPulse API is running");

app.Run();
```

Reemplazar `src/StreamPulse.Api/appsettings.json`:

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Urls": "http://localhost:5000"
}
```

---

### TAREA 11 — docker-compose.yml

Crear `docker-compose.yml` en la raíz:

```yaml
version: '3.8'

services:
  zookeeper:
    image: confluentinc/cp-zookeeper:7.6.0
    container_name: streampulse-zookeeper
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
      ZOOKEEPER_TICK_TIME: 2000
    ports:
      - "2181:2181"

  kafka:
    image: confluentinc/cp-kafka:7.6.0
    container_name: streampulse-kafka
    depends_on:
      - zookeeper
    ports:
      - "9092:9092"
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
      KAFKA_AUTO_CREATE_TOPICS_ENABLE: "true"

  kafka-ui:
    image: provectuslabs/kafka-ui:latest
    container_name: streampulse-kafka-ui
    depends_on:
      - kafka
    ports:
      - "8080:8080"
    environment:
      KAFKA_CLUSTERS_0_NAME: local
      KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS: kafka:9092

  redis:
    image: redis:7-alpine
    container_name: streampulse-redis
    ports:
      - "6379:6379"
    command: redis-server --save 20 1 --loglevel warning
```

Crear `docker-compose.override.yml`:

```yaml
version: '3.8'

services:
  producer:
    build:
      context: .
      dockerfile: src/StreamPulse.Producer/Dockerfile
    container_name: streampulse-producer
    depends_on:
      - kafka
    environment:
      Kafka__BootstrapServers: kafka:9092
      Simulator__EventsPerSecond: 20

  processor:
    build:
      context: .
      dockerfile: src/StreamPulse.Processor/Dockerfile
    container_name: streampulse-processor
    depends_on:
      - kafka
      - redis
    environment:
      Kafka__BootstrapServers: kafka:9092
      Redis__ConnectionString: redis:6379

  api:
    build:
      context: .
      dockerfile: src/StreamPulse.Api/Dockerfile
    container_name: streampulse-api
    depends_on:
      - redis
    ports:
      - "5000:5000"
    environment:
      Redis__ConnectionString: redis:6379
```

---

### TAREA 12 — Tests con Testcontainers

Crear `tests/StreamPulse.Tests/Producer/TransactionSimulatorTests.cs`:

```csharp
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Testcontainers.Kafka;
using StreamPulse.Producer.Models;
using FluentAssertions;

namespace StreamPulse.Tests.Producer;

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
```

Crear `tests/StreamPulse.Tests/Processor/AnomalyDetectorTests.cs`:

```csharp
using StreamPulse.Processor.Anomaly;
using FluentAssertions;

namespace StreamPulse.Tests.Processor;

public sealed class AnomalyDetectorTests
{
    private readonly AnomalyDetector _detector = new();

    [Fact]
    public void IsAnomaly_WhenAmountExceedsThreeSigma_ReturnsTrue()
    {
        var result = _detector.IsAnomaly("ACC-001", 3000m, 1000.0, 500.0);
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAnomaly_WhenAmountIsNormal_ReturnsFalse()
    {
        var result = _detector.IsAnomaly("ACC-NEW", 1200m, 1000.0, 500.0);
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAnomaly_WhenHighVelocityForSameAccount_ReturnsTrue()
    {
        for (int i = 0; i < 6; i++)
            _detector.IsAnomaly("ACC-FAST", 100m, 1000.0, 500.0);

        var result = _detector.IsAnomaly("ACC-FAST", 100m, 1000.0, 500.0);
        result.Should().BeTrue();
    }
}
```

---

### TAREA 13 — README.md

Crear `README.md` en la raíz:

```markdown
# StreamPulse

Pipeline de analytics fintech en tiempo real construido con .NET 8 y Apache Kafka.

## Arquitectura

Producer → Kafka → Processor → Redis → Api → SignalR → Dashboard

## Stack

- .NET 8 · ASP.NET Core · C#
- Apache Kafka (Confluent SDK)
- Redis (StackExchange.Redis)
- SignalR · Testcontainers · xUnit
- Docker Compose · GitHub Actions

## Levantar el entorno

docker-compose up -d zookeeper kafka kafka-ui redis
dotnet run --project src/StreamPulse.Producer
dotnet run --project src/StreamPulse.Processor
dotnet run --project src/StreamPulse.Api

## Correr los tests

dotnet test

## Endpoints

GET  /api/metrics/latest   — última ventana de métricas
GET  /api/metrics/history  — historial de ventanas
GET  /api/metrics/health   — estado del servicio
WS   /hubs/metrics         — SignalR hub live
```

---

### TAREA 14 — Verificación final

```powershell
dotnet restore
dotnet build
docker-compose up -d zookeeper kafka kafka-ui redis
dotnet test
```

---

## Notas para Claude Code

- Ejecutar las tareas en orden numérico
- Verificar `dotnet build` sin errores después de cada tarea
- Si hay error de compilación, corregirlo antes de continuar con la siguiente tarea
- Los namespaces deben coincidir exactamente con los nombres de los proyectos
- No modificar archivos fuera de la estructura definida en este CLAUDE.md