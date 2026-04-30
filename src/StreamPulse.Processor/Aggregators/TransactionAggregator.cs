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
    public double P90Ms { get; set; }
    public double P95Ms { get; set; }
    public double P99Ms { get; set; }
    public Dictionary<string, int> FailureReasons { get; set; } = new();

    public double SuccessRate => Total == 0 ? 0 : (double)Completed / Total * 100;
    public double AvgProcessingMs => Total == 0 ? 0 : TotalProcessingMs / Total;
    public decimal AvgAmount => Total == 0 ? 0 : TotalAmount / Total;
}

public sealed class TransactionAggregator
{
    private WindowMetrics _current = new();
    private readonly List<decimal> _amounts = new();
    private readonly List<int> _processingTimes = new();
    private readonly Dictionary<string, int> _failureReasons = new();
    private readonly object _lock = new();

    public void Record(string status, decimal amount, int processingMs, bool isAnomaly, string? failureReason)
    {
        lock (_lock)
        {
            _current.Total++;
            _current.TotalAmount += amount;
            _current.TotalProcessingMs += processingMs;
            _amounts.Add(amount);
            _processingTimes.Add(processingMs);

            if (status == "COMPLETED") _current.Completed++;
            else if (status == "FAILED") _current.Failed++;
            if (isAnomaly) _current.AnomalyCount++;

            if (!string.IsNullOrEmpty(failureReason))
            {
                _failureReasons.TryGetValue(failureReason, out var count);
                _failureReasons[failureReason] = count + 1;
            }
        }
    }

    public WindowMetrics Flush()
    {
        lock (_lock)
        {
            var snapshot = _current;

            if (_processingTimes.Count > 0)
            {
                var sorted = _processingTimes.Order().ToList();
                snapshot.P90Ms = Percentile(sorted, 90);
                snapshot.P95Ms = Percentile(sorted, 95);
                snapshot.P99Ms = Percentile(sorted, 99);
            }

            snapshot.FailureReasons = new Dictionary<string, int>(_failureReasons);

            _current = new WindowMetrics();
            _amounts.Clear();
            _processingTimes.Clear();
            _failureReasons.Clear();
            return snapshot;
        }
    }

    public (double Mean, double StdDev, int Count) GetAmountStats()
    {
        lock (_lock)
        {
            if (_amounts.Count == 0) return (0, 0, 0);
            var mean = _amounts.Average(a => (double)a);
            var variance = _amounts.Average(a => Math.Pow((double)a - mean, 2));
            return (mean, Math.Sqrt(variance), _amounts.Count);
        }
    }

    private static double Percentile(List<int> sorted, double percentile)
    {
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Count - 1)];
    }
}
