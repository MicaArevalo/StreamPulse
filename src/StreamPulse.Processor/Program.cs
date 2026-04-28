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
