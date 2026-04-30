using StreamPulse.Processor.Aggregators;
using FluentAssertions;

namespace StreamPulse.Tests.Processor;

public sealed class TransactionAggregatorTests
{
    private readonly TransactionAggregator _aggregator = new();

    [Fact]
    public void Record_AccumulatesCountsCorrectly()
    {
        _aggregator.Record("COMPLETED", 1000m, 100, false, null);
        _aggregator.Record("COMPLETED", 2000m, 200, false, null);
        _aggregator.Record("FAILED",    500m,  300, false, "TIMEOUT");

        var metrics = _aggregator.Flush();

        metrics.Total.Should().Be(3);
        metrics.Completed.Should().Be(2);
        metrics.Failed.Should().Be(1);
    }

    [Fact]
    public void Flush_ResetsStateAfterCall()
    {
        _aggregator.Record("COMPLETED", 1000m, 100, false, null);
        _aggregator.Flush();

        var second = _aggregator.Flush();

        second.Total.Should().Be(0);
        second.Completed.Should().Be(0);
    }

    [Fact]
    public void Flush_CalculatesSuccessRateCorrectly()
    {
        _aggregator.Record("COMPLETED", 100m, 50, false, null);
        _aggregator.Record("COMPLETED", 100m, 50, false, null);
        _aggregator.Record("FAILED",    100m, 50, false, "TIMEOUT");
        _aggregator.Record("FAILED",    100m, 50, false, "TIMEOUT");

        var metrics = _aggregator.Flush();

        metrics.SuccessRate.Should().BeApproximately(50.0, precision: 0.01);
    }

    [Fact]
    public void Flush_CalculatesPercentilesCorrectly()
    {
        // Insert 100 values: 1ms, 2ms, ..., 100ms
        for (int i = 1; i <= 100; i++)
            _aggregator.Record("COMPLETED", 100m, i, false, null);

        var metrics = _aggregator.Flush();

        metrics.P90Ms.Should().Be(90);
        metrics.P95Ms.Should().Be(95);
        metrics.P99Ms.Should().Be(99);
    }

    [Fact]
    public void Flush_AggregatesFailureReasonsCorrectly()
    {
        _aggregator.Record("FAILED", 100m, 50, false, "TIMEOUT");
        _aggregator.Record("FAILED", 100m, 50, false, "TIMEOUT");
        _aggregator.Record("FAILED", 100m, 50, false, "INSUFFICIENT_FUNDS");

        var metrics = _aggregator.Flush();

        metrics.FailureReasons["TIMEOUT"].Should().Be(2);
        metrics.FailureReasons["INSUFFICIENT_FUNDS"].Should().Be(1);
    }

    [Fact]
    public void Flush_WithNoData_ReturnsZeroMetrics()
    {
        var metrics = _aggregator.Flush();

        metrics.Total.Should().Be(0);
        metrics.SuccessRate.Should().Be(0);
        metrics.P90Ms.Should().Be(0);
    }

    [Fact]
    public void GetAmountStats_ReturnsCorrectMeanAndStdDev()
    {
        // Values: 1, 2, 3, 4, 5 → mean = 3, population stddev ≈ 1.414
        foreach (var v in new[] { 1m, 2m, 3m, 4m, 5m })
            _aggregator.Record("COMPLETED", v, 100, false, null);

        var (mean, stdDev, count) = _aggregator.GetAmountStats();

        mean.Should().BeApproximately(3.0, precision: 0.001);
        stdDev.Should().BeApproximately(1.414, precision: 0.001);
        count.Should().Be(5);
    }

    [Fact]
    public void GetAmountStats_WhenEmpty_ReturnsZeros()
    {
        var (mean, stdDev, count) = _aggregator.GetAmountStats();

        mean.Should().Be(0);
        stdDev.Should().Be(0);
        count.Should().Be(0);
    }

    [Fact]
    public void Flush_CountsAnomaliesCorrectly()
    {
        _aggregator.Record("COMPLETED", 100m, 50, isAnomaly: true,  failureReason: null);
        _aggregator.Record("COMPLETED", 100m, 50, isAnomaly: false, failureReason: null);
        _aggregator.Record("FAILED",    100m, 50, isAnomaly: true,  failureReason: "FRAUD_DETECTED");

        var metrics = _aggregator.Flush();

        metrics.AnomalyCount.Should().Be(2);
    }
}
