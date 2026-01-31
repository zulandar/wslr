using Wslr.UI.Services;

namespace Wslr.UI.Tests.Services;

public class ResourceMonitorServiceTests : IDisposable
{
    private readonly ResourceMonitorService _sut;

    public ResourceMonitorServiceTests()
    {
        _sut = new ResourceMonitorService();
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesWithEmptyCurrentUsage()
    {
        _sut.CurrentUsage.Should().NotBeNull();
        _sut.CurrentUsage.IsWslRunning.Should().BeFalse();
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
    public async Task RefreshAsync_ReturnsResourceUsage()
    {
        var result = await _sut.RefreshAsync();

        result.Should().NotBeNull();
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RefreshAsync_UpdatesCurrentUsage()
    {
        var result = await _sut.RefreshAsync();

        _sut.CurrentUsage.Should().Be(result);
    }

    [Fact]
    public async Task RefreshAsync_RaisesResourceUsageUpdatedEvent()
    {
        ResourceUsage? receivedUsage = null;
        _sut.ResourceUsageUpdated += (_, usage) => receivedUsage = usage;

        await _sut.RefreshAsync();

        receivedUsage.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await _sut.RefreshAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task RefreshAsync_ReturnsNonNegativeCpuUsage()
    {
        var result = await _sut.RefreshAsync();

        result.CpuUsagePercent.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsNonNegativeMemoryUsage()
    {
        var result = await _sut.RefreshAsync();

        result.MemoryUsageGb.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsNonNegativeDiskUsage()
    {
        var result = await _sut.RefreshAsync();

        result.TotalDiskUsageGb.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsDiskUsageByDistributionDictionary()
    {
        var result = await _sut.RefreshAsync();

        result.DiskUsageByDistribution.Should().NotBeNull();
    }

    #endregion

    #region StartMonitoring/StopMonitoring Tests

    [Fact]
    public void StartMonitoring_SetsIsMonitoringToTrue()
    {
        _sut.StartMonitoring();

        _sut.IsMonitoring.Should().BeTrue();

        _sut.StopMonitoring(); // Cleanup
    }

    [Fact]
    public void StopMonitoring_SetsIsMonitoringToFalse()
    {
        _sut.StartMonitoring();
        _sut.StopMonitoring();

        _sut.IsMonitoring.Should().BeFalse();
    }

    [Fact]
    public void StartMonitoring_CalledTwice_DoesNotThrow()
    {
        _sut.StartMonitoring();
        var act = () => _sut.StartMonitoring();

        act.Should().NotThrow();

        _sut.StopMonitoring(); // Cleanup
    }

    [Fact]
    public void StopMonitoring_WhenNotMonitoring_DoesNotThrow()
    {
        var act = () => _sut.StopMonitoring();

        act.Should().NotThrow();
    }

    #endregion

    #region GetDistributionDiskUsage Tests

    [Fact]
    public void GetDistributionDiskUsage_WhenNotRefreshed_ReturnsNull()
    {
        var result = _sut.GetDistributionDiskUsage("NonExistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDistributionDiskUsage_AfterRefresh_ForNonExistentDistribution_ReturnsNull()
    {
        await _sut.RefreshAsync();

        var result = _sut.GetDistributionDiskUsage("NonExistentDistribution12345");

        result.Should().BeNull();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_StopsMonitoring()
    {
        _sut.StartMonitoring();
        _sut.Dispose();

        _sut.IsMonitoring.Should().BeFalse();
    }

    [Fact]
    public void Dispose_ClearsCurrentUsage()
    {
        _sut.Dispose();

        _sut.CurrentUsage.IsWslRunning.Should().BeFalse();
        _sut.CurrentUsage.CpuUsagePercent.Should().Be(0);
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

    [Fact]
    public async Task RefreshAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        _sut.Dispose();

        var act = async () => await _sut.RefreshAsync();

        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region ResourceUsage Model Tests

    [Fact]
    public void ResourceUsage_Empty_ReturnsExpectedDefaults()
    {
        var empty = ResourceUsage.Empty;

        empty.CpuUsagePercent.Should().Be(0);
        empty.MemoryUsageGb.Should().Be(0);
        empty.TotalDiskUsageGb.Should().Be(0);
        empty.IsWslRunning.Should().BeFalse();
        empty.DiskUsageByDistribution.Should().BeEmpty();
    }

    #endregion
}
