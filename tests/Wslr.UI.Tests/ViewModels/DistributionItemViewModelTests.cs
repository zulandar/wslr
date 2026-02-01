using Wslr.Core.Models;
using Wslr.UI.ViewModels;
using MockFactoryHelper = Wslr.UI.Tests.Helpers.MockFactory;

namespace Wslr.UI.Tests.ViewModels;

public class DistributionItemViewModelTests
{
    #region Property Tests

    [Fact]
    public void IsRunning_WhenStateIsRunning_ReturnsTrue()
    {
        var vm = new DistributionItemViewModel { State = DistributionState.Running };

        vm.IsRunning.Should().BeTrue();
    }

    [Theory]
    [InlineData(DistributionState.Stopped)]
    [InlineData(DistributionState.Installing)]
    public void IsRunning_WhenStateIsNotRunning_ReturnsFalse(DistributionState state)
    {
        var vm = new DistributionItemViewModel { State = state };

        vm.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void IsInstalling_WhenStateIsInstalling_ReturnsTrue()
    {
        var vm = new DistributionItemViewModel { State = DistributionState.Installing };

        vm.IsInstalling.Should().BeTrue();
    }

    [Theory]
    [InlineData(DistributionState.Running, "Running")]
    [InlineData(DistributionState.Stopped, "Stopped")]
    [InlineData(DistributionState.Installing, "Installing")]
    public void StateText_ReturnsExpectedText(DistributionState state, string expected)
    {
        var vm = new DistributionItemViewModel { State = state };

        vm.StateText.Should().Be(expected);
    }

    [Fact]
    public void HasMemoryUsage_WhenMemoryUsageGbIsSet_ReturnsTrue()
    {
        var vm = new DistributionItemViewModel { MemoryUsageGb = 2.5 };

        vm.HasMemoryUsage.Should().BeTrue();
    }

    [Fact]
    public void HasMemoryUsage_WhenMemoryUsageGbIsNull_ReturnsFalse()
    {
        var vm = new DistributionItemViewModel { MemoryUsageGb = null };

        vm.HasMemoryUsage.Should().BeFalse();
    }

    [Fact]
    public void HasCpuUsage_WhenCpuUsagePercentIsSet_ReturnsTrue()
    {
        var vm = new DistributionItemViewModel { CpuUsagePercent = 50.0 };

        vm.HasCpuUsage.Should().BeTrue();
    }

    [Fact]
    public void HasDiskUsage_WhenDiskUsageGbIsSet_ReturnsTrue()
    {
        var vm = new DistributionItemViewModel { DiskUsageGb = 10.0 };

        vm.HasDiskUsage.Should().BeTrue();
    }

    #endregion

    #region FromModel Tests

    [Fact]
    public void FromModel_CopiesAllProperties()
    {
        var distribution = new WslDistribution
        {
            Name = "Ubuntu",
            State = DistributionState.Running,
            Version = 2,
            IsDefault = true
        };

        var vm = DistributionItemViewModel.FromModel(distribution, isPinned: true);

        vm.Name.Should().Be("Ubuntu");
        vm.State.Should().Be(DistributionState.Running);
        vm.Version.Should().Be(2);
        vm.IsDefault.Should().BeTrue();
        vm.IsPinned.Should().BeTrue();
    }

    [Fact]
    public void FromModel_DefaultsPinnedToFalse()
    {
        var distribution = MockFactoryHelper.CreateDistribution("Debian");

        var vm = DistributionItemViewModel.FromModel(distribution);

        vm.IsPinned.Should().BeFalse();
    }

    #endregion

    #region UpdateFromModel Tests

    [Fact]
    public void UpdateFromModel_UpdatesAllProperties()
    {
        var vm = new DistributionItemViewModel
        {
            Name = "OldName",
            State = DistributionState.Stopped,
            Version = 1,
            IsDefault = false
        };

        var distribution = new WslDistribution
        {
            Name = "Ubuntu",
            State = DistributionState.Running,
            Version = 2,
            IsDefault = true
        };

        vm.UpdateFromModel(distribution);

        vm.Name.Should().Be("Ubuntu");
        vm.State.Should().Be(DistributionState.Running);
        vm.Version.Should().Be(2);
        vm.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void UpdateFromModel_ClearsResourceUsage_WhenNotRunning()
    {
        var vm = new DistributionItemViewModel
        {
            State = DistributionState.Running,
            MemoryUsageGb = 2.5,
            CpuUsagePercent = 50.0,
            DiskUsageGb = 10.0
        };

        var distribution = MockFactoryHelper.CreateDistribution("Ubuntu", DistributionState.Stopped);

        vm.UpdateFromModel(distribution);

        vm.MemoryUsageGb.Should().BeNull();
        vm.CpuUsagePercent.Should().BeNull();
        vm.DiskUsageGb.Should().BeNull();
    }

    [Fact]
    public void UpdateFromModel_PreservesResourceUsage_WhenRunning()
    {
        var vm = new DistributionItemViewModel
        {
            State = DistributionState.Running,
            MemoryUsageGb = 2.5,
            CpuUsagePercent = 50.0,
            DiskUsageGb = 10.0
        };

        var distribution = MockFactoryHelper.CreateDistribution("Ubuntu", DistributionState.Running);

        vm.UpdateFromModel(distribution);

        vm.MemoryUsageGb.Should().Be(2.5);
        vm.CpuUsagePercent.Should().Be(50.0);
        vm.DiskUsageGb.Should().Be(10.0);
    }

    #endregion

    #region PropertyChanged Tests

    [Fact]
    public void OnStateChanged_RaisesPropertyChangedForDependentProperties()
    {
        var vm = new DistributionItemViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        vm.State = DistributionState.Running;

        changedProperties.Should().Contain("IsRunning");
        changedProperties.Should().Contain("IsInstalling");
        changedProperties.Should().Contain("StateText");
    }

    [Fact]
    public void OnMemoryUsageGbChanged_RaisesPropertyChangedForHasMemoryUsage()
    {
        var vm = new DistributionItemViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        vm.MemoryUsageGb = 2.5;

        changedProperties.Should().Contain("HasMemoryUsage");
    }

    [Fact]
    public void OnCpuUsagePercentChanged_RaisesPropertyChangedForHasCpuUsage()
    {
        var vm = new DistributionItemViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        vm.CpuUsagePercent = 50.0;

        changedProperties.Should().Contain("HasCpuUsage");
    }

    [Fact]
    public void OnDiskUsageGbChanged_RaisesPropertyChangedForHasDiskUsage()
    {
        var vm = new DistributionItemViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        vm.DiskUsageGb = 10.0;

        changedProperties.Should().Contain("HasDiskUsage");
    }

    #endregion
}
