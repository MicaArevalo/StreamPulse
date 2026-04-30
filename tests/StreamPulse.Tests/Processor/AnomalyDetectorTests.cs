using StreamPulse.Processor.Anomaly;
using FluentAssertions;

namespace StreamPulse.Tests.Processor;

public sealed class AnomalyDetectorTests
{
    private readonly AnomalyDetector _detector = new();

    [Fact]
    public void IsAnomaly_WhenAmountExceedsThreeSigma_ReturnsTrue()
    {
        // 3000 - 1000 = 2000 > 3 * 500 = 1500
        var result = _detector.IsAnomaly("ACC-001", 3000m, 1000.0, 500.0, sampleCount: 30);
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAnomaly_WhenAmountIsNormal_ReturnsFalse()
    {
        // 1200 - 1000 = 200 < 3 * 500 = 1500
        var result = _detector.IsAnomaly("ACC-NEW", 1200m, 1000.0, 500.0, sampleCount: 30);
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAnomaly_WhenHighVelocityForSameAccount_ReturnsTrue()
    {
        for (int i = 0; i < 30; i++)
            _detector.IsAnomaly("ACC-FAST", 100m, 1000.0, 500.0, sampleCount: 30);

        var result = _detector.IsAnomaly("ACC-FAST", 100m, 1000.0, 500.0, sampleCount: 30);
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAnomaly_WhenBelowMinSamples_SkipsThreeSigmaCheck()
    {
        // Amount would trigger 3σ rule but sampleCount < 30 → warmup period
        var result = _detector.IsAnomaly("ACC-WARMUP", 3000m, 1000.0, 500.0, sampleCount: 5);
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAnomaly_WhenStdDevIsZero_DoesNotTriggerDivisionError()
    {
        // stdDev = 0 should never flag as 3σ anomaly regardless of amount
        var result = _detector.IsAnomaly("ACC-ZERO", 999999m, 1000.0, 0.0, sampleCount: 30);
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAnomaly_DifferentAccountsAreTrackedIndependently()
    {
        for (int i = 0; i < 31; i++)
            _detector.IsAnomaly("ACC-HEAVY", 100m, 1000.0, 500.0, sampleCount: 30);

        // ACC-OTHER has zero activity — should not be flagged for velocity
        var result = _detector.IsAnomaly("ACC-OTHER", 100m, 1000.0, 500.0, sampleCount: 30);
        result.Should().BeFalse();
    }
}
