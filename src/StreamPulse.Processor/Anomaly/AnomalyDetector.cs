namespace StreamPulse.Processor.Anomaly;

public sealed class AnomalyDetector
{
    private const double StdDevThreshold = 3.0;
    private const int VelocityWindowSeconds = 60;
    private const int VelocityThreshold = 30;

    private readonly Dictionary<string, Queue<DateTimeOffset>> _accountActivity = new();
    private readonly object _lock = new();

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
