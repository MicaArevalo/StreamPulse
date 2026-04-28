using StreamPulse.Producer.Services;
using StreamPulse.Producer.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<KafkaProducerService>();
builder.Services.AddHostedService<TransactionSimulatorWorker>();

var host = builder.Build();
host.Run();
