using System.Text;
using Wslr.Core.Exceptions;
using Wslr.Core.Interfaces;
using Wslr.Core.Models;
using Wslr.Infrastructure.Services;

namespace Wslr.Infrastructure.Tests.Services;

public class WslServiceTests
{
    private readonly Mock<IProcessRunner> _processRunnerMock;
    private readonly WslService _sut;

    public WslServiceTests()
    {
        _processRunnerMock = new Mock<IProcessRunner>();
        _sut = new WslService(_processRunnerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullProcessRunner_ThrowsArgumentNullException()
    {
        var act = () => new WslService(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("processRunner");
    }

    #endregion

    #region GetDistributionsAsync Tests

    [Fact]
    public async Task GetDistributionsAsync_CallsProcessRunnerWithCorrectArguments()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("wsl.exe", "--list --verbose", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult { ExitCode = 0, StandardOutput = "", StandardError = "" });

        await _sut.GetDistributionsAsync();

        _processRunnerMock.Verify(
            x => x.RunAsync("wsl.exe", "--list --verbose", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetDistributionsAsync_WithValidOutput_ReturnsDistributions()
    {
        var output = """
              NAME      STATE           VERSION
            * Ubuntu    Running         2
              Debian    Stopped         2
            """;

        _processRunnerMock
            .Setup(x => x.RunAsync("wsl.exe", "--list --verbose", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult { ExitCode = 0, StandardOutput = output, StandardError = "" });

        var result = await _sut.GetDistributionsAsync();

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Ubuntu");
        result[1].Name.Should().Be("Debian");
    }

    [Fact]
    public async Task GetDistributionsAsync_WithNoDistributionsError_ReturnsEmptyList()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("wsl.exe", "--list --verbose", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult
            {
                ExitCode = 1,
                StandardOutput = "",
                StandardError = "Windows Subsystem for Linux has no installed distributions."
            });

        var result = await _sut.GetDistributionsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDistributionsAsync_WithOtherError_ThrowsWslException()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("wsl.exe", "--list --verbose", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult
            {
                ExitCode = 1,
                StandardOutput = "",
                StandardError = "Some unexpected error"
            });

        var act = async () => await _sut.GetDistributionsAsync();

        await act.Should().ThrowAsync<WslException>()
            .Where(e => e.ExitCode == 1);
    }

    #endregion

    #region GetOnlineDistributionsAsync Tests

    [Fact]
    public async Task GetOnlineDistributionsAsync_CallsProcessRunnerWithCorrectArguments()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("wsl.exe", "--list --online", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult { ExitCode = 0, StandardOutput = "", StandardError = "" });

        await _sut.GetOnlineDistributionsAsync();

        _processRunnerMock.Verify(
            x => x.RunAsync("wsl.exe", "--list --online", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOnlineDistributionsAsync_WithError_ThrowsWslException()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("wsl.exe", "--list --online", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult { ExitCode = 1, StandardOutput = "", StandardError = "Network error" });

        var act = async () => await _sut.GetOnlineDistributionsAsync();

        await act.Should().ThrowAsync<WslException>();
    }

    #endregion

    #region InstallDistributionAsync Tests

    [Fact]
    public async Task InstallDistributionAsync_WithNullName_ThrowsArgumentException()
    {
        var act = async () => await _sut.InstallDistributionAsync(null!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task InstallDistributionAsync_WithEmptyName_ThrowsArgumentException()
    {
        var act = async () => await _sut.InstallDistributionAsync("");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task InstallDistributionAsync_CallsProcessRunnerWithCorrectArguments()
    {
        _processRunnerMock
            .Setup(x => x.RunWithOutputAsync(
                "wsl.exe",
                "--install -d Ubuntu",
                It.IsAny<Action<string>?>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await _sut.InstallDistributionAsync("Ubuntu");

        _processRunnerMock.Verify(
            x => x.RunWithOutputAsync(
                "wsl.exe",
                "--install -d Ubuntu",
                It.IsAny<Action<string>?>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InstallDistributionAsync_WithFailure_ThrowsWslException()
    {
        _processRunnerMock
            .Setup(x => x.RunWithOutputAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var act = async () => await _sut.InstallDistributionAsync("Ubuntu");

        await act.Should().ThrowAsync<WslException>();
    }

    [Fact]
    public async Task InstallDistributionAsync_ReportsProgress()
    {
        var progressMessages = new List<string>();
        var progress = new Progress<string>(msg => progressMessages.Add(msg));

        _processRunnerMock
            .Setup(x => x.RunWithOutputAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await _sut.InstallDistributionAsync("Ubuntu", progress);

        // Allow time for Progress<T> to process
        await Task.Delay(50);

        progressMessages.Should().Contain(m => m.Contains("Installing Ubuntu"));
    }

    #endregion

    #region UnregisterDistributionAsync Tests

    [Fact]
    public async Task UnregisterDistributionAsync_WithNullName_ThrowsArgumentException()
    {
        var act = async () => await _sut.UnregisterDistributionAsync(null!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UnregisterDistributionAsync_CallsProcessRunnerWithCorrectArguments()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("wsl.exe", "--unregister Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult { ExitCode = 0, StandardOutput = "", StandardError = "" });

        await _sut.UnregisterDistributionAsync("Ubuntu");

        _processRunnerMock.Verify(
            x => x.RunAsync("wsl.exe", "--unregister Ubuntu", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UnregisterDistributionAsync_WithFailure_ThrowsWslException()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult { ExitCode = 1, StandardOutput = "", StandardError = "Not found" });

        var act = async () => await _sut.UnregisterDistributionAsync("Ubuntu");

        await act.Should().ThrowAsync<WslException>();
    }

    #endregion

    #region ExportDistributionAsync Tests

    [Fact]
    public async Task ExportDistributionAsync_WithNullName_ThrowsArgumentException()
    {
        var act = async () => await _sut.ExportDistributionAsync(null!, "C:\\export.tar");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExportDistributionAsync_WithNullPath_ThrowsArgumentException()
    {
        var act = async () => await _sut.ExportDistributionAsync("Ubuntu", null!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExportDistributionAsync_CallsProcessRunnerWithCorrectArguments()
    {
        _processRunnerMock
            .Setup(x => x.RunWithOutputAsync(
                "wsl.exe",
                "--export Ubuntu \"C:\\backup\\ubuntu.tar\"",
                It.IsAny<Action<string>?>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await _sut.ExportDistributionAsync("Ubuntu", "C:\\backup\\ubuntu.tar");

        _processRunnerMock.Verify(
            x => x.RunWithOutputAsync(
                "wsl.exe",
                "--export Ubuntu \"C:\\backup\\ubuntu.tar\"",
                It.IsAny<Action<string>?>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ImportDistributionAsync Tests

    [Fact]
    public async Task ImportDistributionAsync_WithNullName_ThrowsArgumentException()
    {
        var act = async () => await _sut.ImportDistributionAsync(null!, "C:\\wsl", "C:\\backup.tar");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ImportDistributionAsync_WithInvalidVersion_ThrowsArgumentOutOfRangeException()
    {
        var act = async () => await _sut.ImportDistributionAsync("Ubuntu", "C:\\wsl", "C:\\backup.tar", version: 3);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("version");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task ImportDistributionAsync_WithValidVersion_CallsProcessRunnerWithCorrectArguments(int version)
    {
        _processRunnerMock
            .Setup(x => x.RunWithOutputAsync(
                "wsl.exe",
                $"--import MyDistro \"C:\\wsl\\mydistro\" \"C:\\backup.tar\" --version {version}",
                It.IsAny<Action<string>?>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await _sut.ImportDistributionAsync("MyDistro", "C:\\wsl\\mydistro", "C:\\backup.tar", version);

        _processRunnerMock.Verify(
            x => x.RunWithOutputAsync(
                "wsl.exe",
                $"--import MyDistro \"C:\\wsl\\mydistro\" \"C:\\backup.tar\" --version {version}",
                It.IsAny<Action<string>?>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region StartDistributionAsync Tests

    [Fact]
    public async Task StartDistributionAsync_WithNullName_ThrowsArgumentException()
    {
        var act = async () => await _sut.StartDistributionAsync(null!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task StartDistributionAsync_CallsProcessRunnerWithCorrectArguments()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("wsl.exe", "-d Ubuntu --exec exit", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult { ExitCode = 0, StandardOutput = "", StandardError = "" });

        await _sut.StartDistributionAsync("Ubuntu");

        _processRunnerMock.Verify(
            x => x.RunAsync("wsl.exe", "-d Ubuntu --exec exit", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region TerminateDistributionAsync Tests

    [Fact]
    public async Task TerminateDistributionAsync_WithNullName_ThrowsArgumentException()
    {
        var act = async () => await _sut.TerminateDistributionAsync(null!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task TerminateDistributionAsync_CallsProcessRunnerWithCorrectArguments()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("wsl.exe", "--terminate Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult { ExitCode = 0, StandardOutput = "", StandardError = "" });

        await _sut.TerminateDistributionAsync("Ubuntu");

        _processRunnerMock.Verify(
            x => x.RunAsync("wsl.exe", "--terminate Ubuntu", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ShutdownAsync Tests

    [Fact]
    public async Task ShutdownAsync_CallsProcessRunnerWithCorrectArguments()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("wsl.exe", "--shutdown", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult { ExitCode = 0, StandardOutput = "", StandardError = "" });

        await _sut.ShutdownAsync();

        _processRunnerMock.Verify(
            x => x.RunAsync("wsl.exe", "--shutdown", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ShutdownAsync_WithFailure_ThrowsWslException()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult { ExitCode = 1, StandardOutput = "", StandardError = "Error" });

        var act = async () => await _sut.ShutdownAsync();

        await act.Should().ThrowAsync<WslException>();
    }

    #endregion

    #region SetDefaultDistributionAsync Tests

    [Fact]
    public async Task SetDefaultDistributionAsync_WithNullName_ThrowsArgumentException()
    {
        var act = async () => await _sut.SetDefaultDistributionAsync(null!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SetDefaultDistributionAsync_CallsProcessRunnerWithCorrectArguments()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("wsl.exe", "--set-default Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult { ExitCode = 0, StandardOutput = "", StandardError = "" });

        await _sut.SetDefaultDistributionAsync("Ubuntu");

        _processRunnerMock.Verify(
            x => x.RunAsync("wsl.exe", "--set-default Ubuntu", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ExecuteCommandAsync Tests

    [Fact]
    public async Task ExecuteCommandAsync_WithNullDistributionName_ThrowsArgumentException()
    {
        var act = async () => await _sut.ExecuteCommandAsync(null!, "ls");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithNullCommand_ThrowsArgumentException()
    {
        var act = async () => await _sut.ExecuteCommandAsync("Ubuntu", null!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteCommandAsync_CallsProcessRunnerWithCorrectArguments()
    {
        var expectedResult = new ProcessResult { ExitCode = 0, StandardOutput = "output", StandardError = "" };

        _processRunnerMock
            .Setup(x => x.RunAsync("wsl.exe", "-d Ubuntu -- ls -la", Encoding.UTF8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await _sut.ExecuteCommandAsync("Ubuntu", "ls -la");

        result.Should().Be(expectedResult);
        _processRunnerMock.Verify(
            x => x.RunAsync("wsl.exe", "-d Ubuntu -- ls -la", Encoding.UTF8, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteCommandAsync_ReturnsProcessResult_EvenOnFailure()
    {
        var expectedResult = new ProcessResult { ExitCode = 1, StandardOutput = "", StandardError = "command not found" };

        _processRunnerMock
            .Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<string>(), Encoding.UTF8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await _sut.ExecuteCommandAsync("Ubuntu", "invalid-command");

        result.Should().Be(expectedResult);
        result.IsSuccess.Should().BeFalse();
    }

    #endregion
}
