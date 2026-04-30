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
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(allowedOrigins)
     .AllowAnyMethod()
     .AllowAnyHeader()
     .AllowCredentials()));

var app = builder.Build();

app.UseCors();
app.MapControllers();
app.MapHub<MetricsHub>("/hubs/metrics");
app.MapGet("/", () => "StreamPulse API is running");

app.Run();
