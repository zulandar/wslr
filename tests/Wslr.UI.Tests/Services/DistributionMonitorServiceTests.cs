using Wslr.Core.Interfaces;
using Wslr.Core.Models;
using Wslr.UI.Services;

namespace Wslr.UI.Tests.Services;

public class DistributionMonitorServiceTests
{
    private readonly Mock<IWslService> _wslServiceMock;
    private readonly DistributionMonitorService _sut;

    public DistributionMonitorServiceTests()
    {
        _wslServiceMock = new Mock<IWslService>();
        _sut = new DistributionMonitorService(_wslServiceMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullWslService_ThrowsArgumentNullException()
    {
        var act = () => new DistributionMonitorService(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("wslService");
    }

    [Fact]
    public void Constructor_InitializesWithEmptyDistributions()
    {
        _sut.Distributions.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_InitializesWithMonitoringDisabled()
    {
        _sut.IsMonitoring.Should().BeFalse();
    }

    #endregion

    #region RefreshIntervalSeconds Tests

    [Fact]
    public void RefreshIntervalSeconds_DefaultsToFive()
    {
        _sut.RefreshIntervalSeconds.Should().Be(5);
    }

    [Fact]
    public void RefreshIntervalSeconds_CanBeChanged()
    {
        _sut.RefreshIntervalSeconds = 10;

        _sut.RefreshIntervalSeconds.Should().Be(10);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void RefreshIntervalSeconds_WithInvalidValue_ThrowsArgumentOutOfRangeException(int invalidInterval)
    {
        var act = () => _sut.RefreshIntervalSeconds = invalidInterval;

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region RefreshAsync Tests

    [Fact]
    public async Task RefreshAsync_CallsWslServiceGetDistributions()
    {
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>());

        await _sut.RefreshAsync();

        _wslServiceMock.Verify(
            x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_PopulatesDistributionsCollection()
    {
        var distributions = new List<WslDistribution>
        {
            new() { Name = "Ubuntu", State = DistributionState.Running, Version = 2, IsDefault = true },
            new() { Name = "Debian", State = DistributionState.Stopped, Version = 2, IsDefault = false }
        };

        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(distributions);

        await _sut.RefreshAsync();

        _sut.Distributions.Should().HaveCount(2);
        _sut.Distributions.Should().Contain(d => d.Name == "Ubuntu");
        _sut.Distributions.Should().Contain(d => d.Name == "Debian");
    }

    [Fact]
    public async Task RefreshAsync_RaisesDistributionsRefreshedEvent()
    {
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>());

        var eventRaised = false;
        _sut.DistributionsRefreshed += (_, _) => eventRaised = true;

        await _sut.RefreshAsync();

        eventRaised.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAsync_WithError_RaisesRefreshErrorEvent()
    {
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test error"));

        string? errorMessage = null;
        _sut.RefreshError += (_, msg) => errorMessage = msg;

        await _sut.RefreshAsync();

        errorMessage.Should().Be("Test error");
    }

    [Fact]
    public async Task RefreshAsync_UpdatesExistingDistributions()
    {
        // First refresh
        var initialDistributions = new List<WslDistribution>
        {
            new() { Name = "Ubuntu", State = DistributionState.Stopped, Version = 2, IsDefault = true }
        };

        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(initialDistributions);

        await _sut.RefreshAsync();

        // Second refresh with updated state
        var updatedDistributions = new List<WslDistribution>
        {
            new() { Name = "Ubuntu", State = DistributionState.Running, Version = 2, IsDefault = true }
        };

        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDistributions);

        await _sut.RefreshAsync();

        _sut.Distributions.Should().HaveCount(1);
        _sut.Distributions[0].State.Should().Be(DistributionState.Running);
    }

    [Fact]
    public async Task RefreshAsync_RemovesDeletedDistributions()
    {
        // First refresh with two distributions
        var initialDistributions = new List<WslDistribution>
        {
            new() { Name = "Ubuntu", State = DistributionState.Running, Version = 2, IsDefault = true },
            new() { Name = "Debian", State = DistributionState.Stopped, Version = 2, IsDefault = false }
        };

        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(initialDistributions);

        await _sut.RefreshAsync();

        // Second refresh with one distribution removed
        var updatedDistributions = new List<WslDistribution>
        {
            new() { Name = "Ubuntu", State = DistributionState.Running, Version = 2, IsDefault = true }
        };

        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDistributions);

        await _sut.RefreshAsync();

        _sut.Distributions.Should().HaveCount(1);
        _sut.Distributions.Should().NotContain(d => d.Name == "Debian");
    }

    #endregion

    #region State Change Detection Tests

    [Fact]
    public async Task RefreshAsync_WhenStateChanges_RaisesDistributionStateChangedEvent()
    {
        // First refresh
        var initialDistributions = new List<WslDistribution>
        {
            new() { Name = "Ubuntu", State = DistributionState.Stopped, Version = 2, IsDefault = true }
        };

        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(initialDistributions);

        await _sut.RefreshAsync();

        // Set up event handler
        DistributionStateChangedEventArgs? eventArgs = null;
        _sut.DistributionStateChanged += (_, args) => eventArgs = args;

        // Second refresh with changed state
        var updatedDistributions = new List<WslDistribution>
        {
            new() { Name = "Ubuntu", State = DistributionState.Running, Version = 2, IsDefault = true }
        };

        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDistributions);

        await _sut.RefreshAsync();

        eventArgs.Should().NotBeNull();
        eventArgs!.DistributionName.Should().Be("Ubuntu");
        eventArgs.OldState.Should().Be(DistributionState.Stopped);
        eventArgs.NewState.Should().Be(DistributionState.Running);
    }

    [Fact]
    public async Task RefreshAsync_WhenDistributionAdded_RaisesStateChangedWithNullOldState()
    {
        // First refresh with empty list
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>());

        await _sut.RefreshAsync();

        // Set up event handler
        DistributionStateChangedEventArgs? eventArgs = null;
        _sut.DistributionStateChanged += (_, args) => eventArgs = args;

        // Second refresh with new distribution
        var newDistributions = new List<WslDistribution>
        {
            new() { Name = "Ubuntu", State = DistributionState.Stopped, Version = 2, IsDefault = true }
        };

        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(newDistributions);

        await _sut.RefreshAsync();

        eventArgs.Should().NotBeNull();
        eventArgs!.DistributionName.Should().Be("Ubuntu");
        eventArgs.OldState.Should().BeNull();
        eventArgs.NewState.Should().Be(DistributionState.Stopped);
        eventArgs.WasAdded.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAsync_WhenDistributionRemoved_RaisesStateChangedWithNullNewState()
    {
        // First refresh with distribution
        var initialDistributions = new List<WslDistribution>
        {
            new() { Name = "Ubuntu", State = DistributionState.Stopped, Version = 2, IsDefault = true }
        };

        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(initialDistributions);

        await _sut.RefreshAsync();

        // Set up event handler
        DistributionStateChangedEventArgs? eventArgs = null;
        _sut.DistributionStateChanged += (_, args) => eventArgs = args;

        // Second refresh with empty list (distribution removed)
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>());

        await _sut.RefreshAsync();

        eventArgs.Should().NotBeNull();
        eventArgs!.DistributionName.Should().Be("Ubuntu");
        eventArgs.OldState.Should().Be(DistributionState.Stopped);
        eventArgs.NewState.Should().BeNull();
        eventArgs.WasRemoved.Should().BeTrue();
    }

    #endregion

    #region StartMonitoring/StopMonitoring Tests

    [Fact]
    public void StartMonitoring_SetsIsMonitoringToTrue()
    {
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>());

        _sut.StartMonitoring();

        _sut.IsMonitoring.Should().BeTrue();

        _sut.StopMonitoring(); // Cleanup
    }

    [Fact]
    public void StopMonitoring_SetsIsMonitoringToFalse()
    {
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>());

        _sut.StartMonitoring();
        _sut.StopMonitoring();

        _sut.IsMonitoring.Should().BeFalse();
    }

    [Fact]
    public void StartMonitoring_CalledTwice_DoesNotThrow()
    {
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>());

        _sut.StartMonitoring();
        var act = () => _sut.StartMonitoring();

        act.Should().NotThrow();

        _sut.StopMonitoring(); // Cleanup
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_StopsMonitoring()
    {
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>());

        _sut.StartMonitoring();
        _sut.Dispose();

        _sut.IsMonitoring.Should().BeFalse();
    }

    [Fact]
    public async Task Dispose_ClearsDistributions()
    {
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>
            {
                new() { Name = "Ubuntu", State = DistributionState.Running, Version = 2, IsDefault = true }
            });

        await _sut.RefreshAsync();
        _sut.Dispose();

        _sut.Distributions.Should().BeEmpty();
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        _sut.Dispose();
        var act = () => _sut.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void StartMonitoring_AfterDispose_ThrowsObjectDisposedException()
    {
        _sut.Dispose();

        var act = () => _sut.StartMonitoring();

        act.Should().Throw<ObjectDisposedException>();
    }

    #endregion

    #region Event History Tests

    [Fact]
    public void GetEventHistory_InitiallyEmpty()
    {
        var history = _sut.GetEventHistory();

        history.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEventHistory_AfterRefresh_ContainsRefreshEvent()
    {
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>());

        await _sut.RefreshAsync();

        var history = _sut.GetEventHistory();

        history.Should().HaveCount(1);
        history[0].EventType.Should().Be(MonitoringEventType.ManualRefresh);
    }

    [Fact]
    public async Task GetEventHistory_AfterStateChange_ContainsStateChangedEvent()
    {
        // First refresh
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>
            {
                new() { Name = "Ubuntu", State = DistributionState.Stopped, Version = 2, IsDefault = true }
            });

        await _sut.RefreshAsync();

        // Second refresh with state change
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>
            {
                new() { Name = "Ubuntu", State = DistributionState.Running, Version = 2, IsDefault = true }
            });

        await _sut.RefreshAsync();

        var history = _sut.GetEventHistory();

        history.Should().Contain(e => e.EventType == MonitoringEventType.StateChanged);
        var stateChangeEvent = history.First(e => e.EventType == MonitoringEventType.StateChanged);
        stateChangeEvent.DistributionName.Should().Be("Ubuntu");
        stateChangeEvent.OldState.Should().Be(DistributionState.Stopped);
        stateChangeEvent.NewState.Should().Be(DistributionState.Running);
    }

    [Fact]
    public async Task GetEventHistory_AfterDistributionAdded_ContainsAddedEvent()
    {
        // First refresh - empty
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>());

        await _sut.RefreshAsync();

        // Second refresh with new distribution
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>
            {
                new() { Name = "Ubuntu", State = DistributionState.Stopped, Version = 2, IsDefault = true }
            });

        await _sut.RefreshAsync();

        var history = _sut.GetEventHistory();

        history.Should().Contain(e => e.EventType == MonitoringEventType.DistributionAdded);
        var addedEvent = history.First(e => e.EventType == MonitoringEventType.DistributionAdded);
        addedEvent.DistributionName.Should().Be("Ubuntu");
    }

    [Fact]
    public async Task GetEventHistory_AfterDistributionRemoved_ContainsRemovedEvent()
    {
        // First refresh with distribution
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>
            {
                new() { Name = "Ubuntu", State = DistributionState.Stopped, Version = 2, IsDefault = true }
            });

        await _sut.RefreshAsync();

        // Second refresh - distribution removed
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>());

        await _sut.RefreshAsync();

        var history = _sut.GetEventHistory();

        history.Should().Contain(e => e.EventType == MonitoringEventType.DistributionRemoved);
        var removedEvent = history.First(e => e.EventType == MonitoringEventType.DistributionRemoved);
        removedEvent.DistributionName.Should().Be("Ubuntu");
    }

    [Fact]
    public async Task GetEventHistory_WithMaxCount_ReturnsLimitedResults()
    {
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>());

        // Generate multiple events
        for (var i = 0; i < 10; i++)
        {
            await _sut.RefreshAsync();
        }

        var history = _sut.GetEventHistory(maxCount: 3);

        history.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetEventHistory_ReturnsMostRecentFirst()
    {
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>());

        await _sut.RefreshAsync();
        await _sut.RefreshAsync();

        var history = _sut.GetEventHistory();

        // Most recent should be first
        history[0].Timestamp.Should().BeOnOrAfter(history[1].Timestamp);
    }

    [Fact]
    public async Task ClearEventHistory_RemovesAllEvents()
    {
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>());

        await _sut.RefreshAsync();
        await _sut.RefreshAsync();

        _sut.ClearEventHistory();

        var history = _sut.GetEventHistory();
        history.Should().BeEmpty();
    }

    [Fact]
    public void StartMonitoring_LogsMonitoringStartedEvent()
    {
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>());

        _sut.StartMonitoring();

        // Wait for initial refresh
        Thread.Sleep(100);

        var history = _sut.GetEventHistory();

        history.Should().Contain(e => e.EventType == MonitoringEventType.MonitoringStarted);

        _sut.StopMonitoring();
    }

    [Fact]
    public void StopMonitoring_LogsMonitoringStoppedEvent()
    {
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>());

        _sut.StartMonitoring();
        Thread.Sleep(100);
        _sut.StopMonitoring();

        var history = _sut.GetEventHistory();

        history.Should().Contain(e => e.EventType == MonitoringEventType.MonitoringStopped);
    }

    [Fact]
    public async Task GetEventHistory_AfterError_ContainsErrorEvent()
    {
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test error"));

        await _sut.RefreshAsync();

        var history = _sut.GetEventHistory();

        history.Should().Contain(e => e.EventType == MonitoringEventType.Error);
        var errorEvent = history.First(e => e.EventType == MonitoringEventType.Error);
        errorEvent.Details.Should().Be("Test error");
    }

    [Fact]
    public async Task EventHistory_TrimmedToMaxSize()
    {
        _wslServiceMock
            .Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>());

        // Generate more than max events (100)
        for (var i = 0; i < 120; i++)
        {
            await _sut.RefreshAsync();
        }

        var history = _sut.GetEventHistory();

        history.Should().HaveCount(100);
    }

    #endregion
}
