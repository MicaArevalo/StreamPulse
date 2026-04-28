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
