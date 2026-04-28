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
    p.WithOrigins("http://localhost:3000")
     .AllowAnyMethod()
     .AllowAnyHeader()
     .AllowCredentials()));

var app = builder.Build();

app.UseCors();
app.MapControllers();
app.MapHub<MetricsHub>("/hubs/metrics");
app.MapGet("/", () => "StreamPulse API is running");

app.Run();
