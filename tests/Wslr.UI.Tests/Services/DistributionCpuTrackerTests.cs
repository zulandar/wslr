using Wslr.Core.Parsing;
using Wslr.UI.Services;

namespace Wslr.UI.Tests.Services;

public class DistributionCpuTrackerTests
{
    private readonly DistributionCpuTracker _sut;

    public DistributionCpuTrackerTests()
    {
        _sut = new DistributionCpuTracker();
    }

    #region CalculateCpuPercent Tests

    [Fact]
    public void CalculateCpuPercent_FirstSample_ReturnsNull()
    {
        var stat = CreateCpuStat(user: 1000, idle: 9000);

        var result = _sut.CalculateCpuPercent("Ubuntu", stat);

        result.Should().BeNull();
    }

    [Fact]
    public void CalculateCpuPercent_SecondSample_ReturnsPercentage()
    {
        var first = CreateCpuStat(user: 1000, idle: 9000);
        var second = CreateCpuStat(user: 1100, idle: 9900);

        _sut.CalculateCpuPercent("Ubuntu", first);
        var result = _sut.CalculateCpuPercent("Ubuntu", second);

        result.Should().NotBeNull();
    }

    [Fact]
    public void CalculateCpuPercent_With50PercentUsage_Returns50()
    {
        // First: total=10000 (user=1000, idle=9000)
        // Second: total=11000 (user=1500, idle=9500)
        // Delta: total=1000, idle=500, active=500
        // Percent = 500/1000 * 100 = 50%
        var first = CreateCpuStat(user: 1000, idle: 9000);
        var second = CreateCpuStat(user: 1500, idle: 9500);

        _sut.CalculateCpuPercent("Ubuntu", first);
        var result = _sut.CalculateCpuPercent("Ubuntu", second);

        result.Should().BeApproximately(50.0, 0.01);
    }

    [Fact]
    public void CalculateCpuPercent_With100PercentUsage_Returns100()
    {
        // First: total=10000 (user=1000, idle=9000)
        // Second: total=11000 (user=2000, idle=9000) - all delta went to user
        // Delta: total=1000, idle=0, active=1000
        // Percent = 1000/1000 * 100 = 100%
        var first = CreateCpuStat(user: 1000, idle: 9000);
        var second = CreateCpuStat(user: 2000, idle: 9000);

        _sut.CalculateCpuPercent("Ubuntu", first);
        var result = _sut.CalculateCpuPercent("Ubuntu", second);

        result.Should().BeApproximately(100.0, 0.01);
    }

    [Fact]
    public void CalculateCpuPercent_With0PercentUsage_Returns0()
    {
        // First: total=10000 (user=1000, idle=9000)
        // Second: total=11000 (user=1000, idle=10000) - all delta went to idle
        // Delta: total=1000, idle=1000, active=0
        // Percent = 0/1000 * 100 = 0%
        var first = CreateCpuStat(user: 1000, idle: 9000);
        var second = CreateCpuStat(user: 1000, idle: 10000);

        _sut.CalculateCpuPercent("Ubuntu", first);
        var result = _sut.CalculateCpuPercent("Ubuntu", second);

        result.Should().BeApproximately(0.0, 0.01);
    }

    [Fact]
    public void CalculateCpuPercent_WithZeroDelta_Returns0()
    {
        var first = CreateCpuStat(user: 1000, idle: 9000);
        var second = CreateCpuStat(user: 1000, idle: 9000); // Same values

        _sut.CalculateCpuPercent("Ubuntu", first);
        var result = _sut.CalculateCpuPercent("Ubuntu", second);

        result.Should().Be(0);
    }

    [Fact]
    public void CalculateCpuPercent_IncludesIoWaitInIdleTime()
    {
        // IoWait is considered idle time
        // First: user=1000, idle=8000, iowait=1000, total=10000
        // Second: user=1500, idle=8000, iowait=1500, total=11000
        // Delta: total=1000, idle+iowait=500, active=500
        // Percent = 500/1000 * 100 = 50%
        var first = CreateCpuStat(user: 1000, idle: 8000, ioWait: 1000);
        var second = CreateCpuStat(user: 1500, idle: 8000, ioWait: 1500);

        _sut.CalculateCpuPercent("Ubuntu", first);
        var result = _sut.CalculateCpuPercent("Ubuntu", second);

        result.Should().BeApproximately(50.0, 0.01);
    }

    [Fact]
    public void CalculateCpuPercent_TracksSeparateDistributions()
    {
        var ubuntuFirst = CreateCpuStat(user: 1000, idle: 9000);
        var ubuntuSecond = CreateCpuStat(user: 1500, idle: 9500); // 50% usage
        var debianFirst = CreateCpuStat(user: 2000, idle: 8000);
        var debianSecond = CreateCpuStat(user: 2800, idle: 8200); // 80% usage

        _sut.CalculateCpuPercent("Ubuntu", ubuntuFirst);
        _sut.CalculateCpuPercent("Debian", debianFirst);
        var ubuntuResult = _sut.CalculateCpuPercent("Ubuntu", ubuntuSecond);
        var debianResult = _sut.CalculateCpuPercent("Debian", debianSecond);

        ubuntuResult.Should().BeApproximately(50.0, 0.01);
        debianResult.Should().BeApproximately(80.0, 0.01);
    }

    [Fact]
    public void CalculateCpuPercent_IsCaseInsensitive()
    {
        var first = CreateCpuStat(user: 1000, idle: 9000);
        var second = CreateCpuStat(user: 1500, idle: 9500);

        _sut.CalculateCpuPercent("Ubuntu", first);
        var result = _sut.CalculateCpuPercent("UBUNTU", second);

        result.Should().NotBeNull();
        result.Should().BeApproximately(50.0, 0.01);
    }

    [Fact]
    public void CalculateCpuPercent_WithNullDistributionName_ThrowsArgumentNullException()
    {
        var stat = CreateCpuStat(user: 1000, idle: 9000);

        var act = () => _sut.CalculateCpuPercent(null!, stat);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CalculateCpuPercent_WithNullStat_ThrowsArgumentNullException()
    {
        var act = () => _sut.CalculateCpuPercent("Ubuntu", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Distribution Restart Tests

    [Fact]
    public void CalculateCpuPercent_WhenDistributionRestarted_ReturnsNull()
    {
        // Simulate restart: second sample has lower values than first
        var first = CreateCpuStat(user: 5000, idle: 5000);
        var second = CreateCpuStat(user: 100, idle: 900); // After restart

        _sut.CalculateCpuPercent("Ubuntu", first);
        var result = _sut.CalculateCpuPercent("Ubuntu", second);

        result.Should().BeNull();
    }

    [Fact]
    public void CalculateCpuPercent_AfterRestartDetected_ThirdSampleWorksNormally()
    {
        var first = CreateCpuStat(user: 5000, idle: 5000);
        var afterRestart = CreateCpuStat(user: 100, idle: 900);
        var third = CreateCpuStat(user: 600, idle: 1400); // 50% usage

        _sut.CalculateCpuPercent("Ubuntu", first);
        _sut.CalculateCpuPercent("Ubuntu", afterRestart); // Returns null, resets state
        var result = _sut.CalculateCpuPercent("Ubuntu", third);

        result.Should().BeApproximately(50.0, 0.01);
    }

    #endregion

    #region ClearDistribution Tests

    [Fact]
    public void ClearDistribution_RemovesPreviousSample()
    {
        var stat = CreateCpuStat(user: 1000, idle: 9000);
        _sut.CalculateCpuPercent("Ubuntu", stat);

        _sut.ClearDistribution("Ubuntu");

        _sut.HasPreviousSample("Ubuntu").Should().BeFalse();
    }

    [Fact]
    public void ClearDistribution_AfterClear_NextSampleReturnsNull()
    {
        var first = CreateCpuStat(user: 1000, idle: 9000);
        var second = CreateCpuStat(user: 1500, idle: 9500);
        _sut.CalculateCpuPercent("Ubuntu", first);
        _sut.ClearDistribution("Ubuntu");

        var result = _sut.CalculateCpuPercent("Ubuntu", second);

        result.Should().BeNull();
    }

    [Fact]
    public void ClearDistribution_DoesNotAffectOtherDistributions()
    {
        var first = CreateCpuStat(user: 1000, idle: 9000);
        _sut.CalculateCpuPercent("Ubuntu", first);
        _sut.CalculateCpuPercent("Debian", first);

        _sut.ClearDistribution("Ubuntu");

        _sut.HasPreviousSample("Ubuntu").Should().BeFalse();
        _sut.HasPreviousSample("Debian").Should().BeTrue();
    }

    [Fact]
    public void ClearDistribution_IsCaseInsensitive()
    {
        var stat = CreateCpuStat(user: 1000, idle: 9000);
        _sut.CalculateCpuPercent("Ubuntu", stat);

        _sut.ClearDistribution("UBUNTU");

        _sut.HasPreviousSample("Ubuntu").Should().BeFalse();
    }

    [Fact]
    public void ClearDistribution_WithNonExistentDistribution_DoesNotThrow()
    {
        var act = () => _sut.ClearDistribution("NonExistent");

        act.Should().NotThrow();
    }

    [Fact]
    public void ClearDistribution_WithNullName_ThrowsArgumentNullException()
    {
        var act = () => _sut.ClearDistribution(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ClearAll Tests

    [Fact]
    public void ClearAll_RemovesAllDistributions()
    {
        var stat = CreateCpuStat(user: 1000, idle: 9000);
        _sut.CalculateCpuPercent("Ubuntu", stat);
        _sut.CalculateCpuPercent("Debian", stat);
        _sut.CalculateCpuPercent("Fedora", stat);

        _sut.ClearAll();

        _sut.TrackedDistributionCount.Should().Be(0);
    }

    [Fact]
    public void ClearAll_WhenEmpty_DoesNotThrow()
    {
        var act = () => _sut.ClearAll();

        act.Should().NotThrow();
    }

    #endregion

    #region HasPreviousSample Tests

    [Fact]
    public void HasPreviousSample_BeforeAnySample_ReturnsFalse()
    {
        var result = _sut.HasPreviousSample("Ubuntu");

        result.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousSample_AfterFirstSample_ReturnsTrue()
    {
        var stat = CreateCpuStat(user: 1000, idle: 9000);
        _sut.CalculateCpuPercent("Ubuntu", stat);

        var result = _sut.HasPreviousSample("Ubuntu");

        result.Should().BeTrue();
    }

    [Fact]
    public void HasPreviousSample_WithNullName_ThrowsArgumentNullException()
    {
        var act = () => _sut.HasPreviousSample(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region TrackedDistributionCount Tests

    [Fact]
    public void TrackedDistributionCount_Initially_IsZero()
    {
        _sut.TrackedDistributionCount.Should().Be(0);
    }

    [Fact]
    public void TrackedDistributionCount_AfterAddingSamples_ReflectsCount()
    {
        var stat = CreateCpuStat(user: 1000, idle: 9000);
        _sut.CalculateCpuPercent("Ubuntu", stat);
        _sut.CalculateCpuPercent("Debian", stat);

        _sut.TrackedDistributionCount.Should().Be(2);
    }

    [Fact]
    public void TrackedDistributionCount_SameDistributionMultipleTimes_CountsOnce()
    {
        var first = CreateCpuStat(user: 1000, idle: 9000);
        var second = CreateCpuStat(user: 1500, idle: 9500);
        _sut.CalculateCpuPercent("Ubuntu", first);
        _sut.CalculateCpuPercent("Ubuntu", second);

        _sut.TrackedDistributionCount.Should().Be(1);
    }

    #endregion

    #region Edge Cases and Clamping Tests

    [Fact]
    public void CalculateCpuPercent_ClampsToMaximum100()
    {
        // Create a scenario that might exceed 100% due to timing issues
        // This shouldn't happen in practice but we should handle it gracefully
        var first = CreateCpuStat(user: 1000, nice: 0, system: 0, idle: 9000);
        // Simulate more active time than total delta (timing anomaly)
        var second = new LinuxCpuStat
        {
            User = 2000,
            Nice = 100,
            System = 100,
            Idle = 9000, // Same idle
            IoWait = 0,
            Irq = 0,
            SoftIrq = 0,
            Steal = 0
        };

        _sut.CalculateCpuPercent("Ubuntu", first);
        var result = _sut.CalculateCpuPercent("Ubuntu", second);

        result.Should().BeLessThanOrEqualTo(100.0);
    }

    [Fact]
    public void CalculateCpuPercent_ClampsToMinimum0()
    {
        var first = CreateCpuStat(user: 1000, idle: 9000);
        var second = CreateCpuStat(user: 1000, idle: 9000); // No change

        _sut.CalculateCpuPercent("Ubuntu", first);
        var result = _sut.CalculateCpuPercent("Ubuntu", second);

        result.Should().BeGreaterThanOrEqualTo(0.0);
    }

    #endregion

    #region Helper Methods

    private static LinuxCpuStat CreateCpuStat(
        long user,
        long idle,
        long nice = 0,
        long system = 0,
        long ioWait = 0,
        long irq = 0,
        long softIrq = 0,
        long steal = 0)
    {
        return new LinuxCpuStat
        {
            User = user,
            Nice = nice,
            System = system,
            Idle = idle,
            IoWait = ioWait,
            Irq = irq,
            SoftIrq = softIrq,
            Steal = steal
        };
    }

    #endregion
}
