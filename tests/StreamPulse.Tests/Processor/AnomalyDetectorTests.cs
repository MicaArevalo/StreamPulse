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
        for (int i = 0; i < 30; i++)
            _detector.IsAnomaly("ACC-FAST", 100m, 1000.0, 500.0);

        var result = _detector.IsAnomaly("ACC-FAST", 100m, 1000.0, 500.0);
        result.Should().BeTrue();
    }
}
