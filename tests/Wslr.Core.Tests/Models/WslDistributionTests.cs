using Wslr.Core.Models;

namespace Wslr.Core.Tests.Models;

public class WslDistributionTests
{
    [Fact]
    public void WslDistribution_ShouldBeEqualWithSameValues()
    {
        var dist1 = new WslDistribution
        {
            Name = "Ubuntu",
            State = DistributionState.Running,
            Version = 2,
            IsDefault = true
        };

        var dist2 = new WslDistribution
        {
            Name = "Ubuntu",
            State = DistributionState.Running,
            Version = 2,
            IsDefault = true
        };

        dist1.Should().Be(dist2);
    }

    [Fact]
    public void WslDistribution_ShouldNotBeEqualWithDifferentName()
    {
        var dist1 = new WslDistribution
        {
            Name = "Ubuntu",
            State = DistributionState.Running,
            Version = 2,
            IsDefault = true
        };

        var dist2 = new WslDistribution
        {
            Name = "Debian",
            State = DistributionState.Running,
            Version = 2,
            IsDefault = true
        };

        dist1.Should().NotBe(dist2);
    }

    [Fact]
    public void WslDistribution_ShouldNotBeEqualWithDifferentState()
    {
        var dist1 = new WslDistribution
        {
            Name = "Ubuntu",
            State = DistributionState.Running,
            Version = 2,
            IsDefault = true
        };

        var dist2 = new WslDistribution
        {
            Name = "Ubuntu",
            State = DistributionState.Stopped,
            Version = 2,
            IsDefault = true
        };

        dist1.Should().NotBe(dist2);
    }
}
