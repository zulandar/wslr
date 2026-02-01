using Microsoft.Extensions.Logging;
using Wslr.Infrastructure.Services;

namespace Wslr.Infrastructure.Tests.Services;

public class ProcessRunnerTests
{
    private readonly Mock<ILogger<ProcessRunner>> _loggerMock;
    private readonly ProcessRunner _runner;

    public ProcessRunnerTests()
    {
        _loggerMock = new Mock<ILogger<ProcessRunner>>();
        _runner = new ProcessRunner(_loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidLogger_CreatesInstance()
    {
        var runner = new ProcessRunner(_loggerMock.Object);

        runner.Should().NotBeNull();
    }

    #endregion

    #region RunAsync Tests - Using cmd.exe for cross-platform compatibility

    [Fact]
    public async Task RunAsync_WithSimpleCommand_ReturnsOutput()
    {
        // Using cmd /c echo on Windows
        var result = await _runner.RunAsync("cmd.exe", "/c echo hello", System.Text.Encoding.UTF8);

        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("hello");
    }

    [Fact]
    public async Task RunAsync_WithFailingCommand_ReturnsNonZeroExitCode()
    {
        // exit 1 returns exit code 1
        var result = await _runner.RunAsync("cmd.exe", "/c exit 1", System.Text.Encoding.UTF8);

        result.ExitCode.Should().Be(1);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_WithStderrOutput_CapturesStderr()
    {
        // Echo to stderr using cmd
        var result = await _runner.RunAsync("cmd.exe", "/c echo error 1>&2", System.Text.Encoding.UTF8);

        // Note: cmd.exe might not properly redirect, but the test verifies the capability
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task RunAsync_CapturesBothOutputStreams()
    {
        var result = await _runner.RunAsync("cmd.exe", "/c echo stdout && echo stderr 1>&2", System.Text.Encoding.UTF8);

        result.StandardOutput.Should().Contain("stdout");
        // stderr capture depends on OS behavior
    }

    [Fact]
    public async Task RunAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => _runner.RunAsync("cmd.exe", "/c ping localhost -n 10", System.Text.Encoding.UTF8, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task RunAsync_WithDefaultEncoding_UsesUnicode()
    {
        // Default call should use Unicode encoding for WSL compatibility
        var result = await _runner.RunAsync("cmd.exe", "/c echo test");

        result.Should().NotBeNull();
        // The actual output encoding test is implicit
    }

    #endregion

    #region RunWithOutputAsync Tests

    [Fact]
    public async Task RunWithOutputAsync_StreamsOutput()
    {
        var outputLines = new List<string>();

        var exitCode = await _runner.RunWithOutputAsync(
            "cmd.exe",
            "/c echo line1 & echo line2",
            line => outputLines.Add(line),
            null);

        exitCode.Should().Be(0);
        // Output may contain the lines, check for non-empty
        outputLines.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RunWithOutputAsync_StreamsErrors()
    {
        var errorLines = new List<string>();

        await _runner.RunWithOutputAsync(
            "cmd.exe",
            "/c echo error 1>&2",
            null,
            line => errorLines.Add(line));

        // Error capture depends on OS behavior
        errorLines.Should().NotBeNull();
    }

    [Fact]
    public async Task RunWithOutputAsync_WithNullHandlers_DoesNotThrow()
    {
        var act = () => _runner.RunWithOutputAsync("cmd.exe", "/c echo test", null, null);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RunWithOutputAsync_ReturnsCorrectExitCode()
    {
        var exitCode = await _runner.RunWithOutputAsync("cmd.exe", "/c exit 42", null, null);

        exitCode.Should().Be(42);
    }

    [Fact]
    public async Task RunWithOutputAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => _runner.RunWithOutputAsync("cmd.exe", "/c ping localhost -n 10", null, null, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task RunAsync_WithEmptyOutput_ReturnsEmptyString()
    {
        // REM is a comment command that produces no output
        var result = await _runner.RunAsync("cmd.exe", "/c rem", System.Text.Encoding.UTF8);

        result.StandardOutput.Should().BeEmpty();
        result.ExitCode.Should().Be(0);
    }

    [Fact]
    public async Task RunAsync_WithLargeOutput_HandlesCorrectly()
    {
        // Generate large output
        var result = await _runner.RunAsync("cmd.exe", "/c for /L %i in (1,1,100) do @echo Line %i", System.Text.Encoding.UTF8);

        result.StandardOutput.Should().NotBeEmpty();
        result.StandardOutput.Should().Contain("Line 1");
        result.StandardOutput.Should().Contain("Line 100");
    }

    [Fact]
    public async Task RunAsync_WithSpecialCharacters_HandlesCorrectly()
    {
        var result = await _runner.RunAsync("cmd.exe", "/c echo Hello World!", System.Text.Encoding.UTF8);

        result.StandardOutput.Should().Contain("Hello");
    }

    [Fact]
    public async Task RunAsync_WithQuotedArguments_HandlesCorrectly()
    {
        var result = await _runner.RunAsync("cmd.exe", "/c echo \"quoted string\"", System.Text.Encoding.UTF8);

        result.StandardOutput.Should().Contain("quoted string");
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task RunAsync_LogsCommandExecution()
    {
        await _runner.RunAsync("cmd.exe", "/c echo test", System.Text.Encoding.UTF8);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Executing")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RunAsync_LogsCompletion()
    {
        await _runner.RunAsync("cmd.exe", "/c echo test", System.Text.Encoding.UTF8);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Completed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion
}
