using Microsoft.Extensions.Logging;
using Wslr.Core.Interfaces;
using Wslr.Core.Models;
using Wslr.Infrastructure.Services;

namespace Wslr.Infrastructure.Tests.Services;

public class ScriptExecutionServiceTests
{
    private readonly Mock<IProcessRunner> _processRunnerMock;
    private readonly Mock<ILogger<ScriptExecutionService>> _loggerMock;
    private readonly ScriptExecutionService _service;

    public ScriptExecutionServiceTests()
    {
        _processRunnerMock = new Mock<IProcessRunner>();
        _loggerMock = new Mock<ILogger<ScriptExecutionService>>();
        _service = new ScriptExecutionService(_processRunnerMock.Object, _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullProcessRunner_ThrowsArgumentNullException()
    {
        var act = () => new ScriptExecutionService(null!, _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("processRunner");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ScriptExecutionService(_processRunnerMock.Object, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region ExecuteScriptAsync Input Validation Tests

    [Fact]
    public async Task ExecuteScriptAsync_WithNullDistribution_ThrowsArgumentException()
    {
        var act = () => _service.ExecuteScriptAsync(null!, "echo test");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteScriptAsync_WithEmptyDistribution_ThrowsArgumentException()
    {
        var act = () => _service.ExecuteScriptAsync("", "echo test");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteScriptAsync_WithNullScript_ThrowsArgumentException()
    {
        var act = () => _service.ExecuteScriptAsync("Ubuntu", null!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteScriptAsync_WithEmptyScript_ThrowsArgumentException()
    {
        var act = () => _service.ExecuteScriptAsync("Ubuntu", "");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region ExecuteScriptAsync Execution Tests

    [Fact]
    public async Task ExecuteScriptAsync_WithSuccessfulExecution_ReturnsSuccessResult()
    {
        _processRunnerMock.Setup(p => p.RunWithOutputAsync(
                "wsl.exe",
                It.IsAny<string>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _service.ExecuteScriptAsync("Ubuntu", "echo test");

        result.ExitCode.Should().Be(0);
        result.IsSuccess.Should().BeTrue();
        result.WasCancelled.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteScriptAsync_WithFailedExecution_ReturnsFailureResult()
    {
        _processRunnerMock.Setup(p => p.RunWithOutputAsync(
                "wsl.exe",
                It.IsAny<string>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _service.ExecuteScriptAsync("Ubuntu", "exit 1");

        result.ExitCode.Should().Be(1);
        result.IsSuccess.Should().BeFalse();
        result.WasCancelled.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteScriptAsync_CapturesOutput()
    {
        _processRunnerMock.Setup(p => p.RunWithOutputAsync(
                "wsl.exe",
                It.IsAny<string>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Action<string>?, Action<string>?, CancellationToken>(
                (_, _, output, _, _) =>
                {
                    output?.Invoke("line 1");
                    output?.Invoke("line 2");
                })
            .ReturnsAsync(0);

        var result = await _service.ExecuteScriptAsync("Ubuntu", "echo test");

        result.StandardOutput.Should().Contain("line 1");
        result.StandardOutput.Should().Contain("line 2");
    }

    [Fact]
    public async Task ExecuteScriptAsync_CapturesStderr()
    {
        _processRunnerMock.Setup(p => p.RunWithOutputAsync(
                "wsl.exe",
                It.IsAny<string>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Action<string>?, Action<string>?, CancellationToken>(
                (_, _, _, error, _) =>
                {
                    error?.Invoke("error message");
                })
            .ReturnsAsync(1);

        var result = await _service.ExecuteScriptAsync("Ubuntu", "bad command");

        result.StandardError.Should().Contain("error message");
    }

    [Fact]
    public async Task ExecuteScriptAsync_ReportsDuration()
    {
        _processRunnerMock.Setup(p => p.RunWithOutputAsync(
                "wsl.exe",
                It.IsAny<string>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _service.ExecuteScriptAsync("Ubuntu", "echo test");

        result.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public async Task ExecuteScriptAsync_WithVariables_SubstitutesValues()
    {
        string? capturedCommand = null;
        _processRunnerMock.Setup(p => p.RunWithOutputAsync(
                "wsl.exe",
                It.IsAny<string>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Action<string>?, Action<string>?, CancellationToken>(
                (_, cmd, _, _, _) => capturedCommand = cmd)
            .ReturnsAsync(0);

        var variables = new Dictionary<string, string>
        {
            ["USERNAME"] = "testuser",
            ["HOME"] = "/home/testuser"
        };

        await _service.ExecuteScriptAsync(
            "Ubuntu",
            "echo ${USERNAME} lives at ${HOME}",
            variables);

        capturedCommand.Should().Contain("testuser");
        capturedCommand.Should().Contain("/home/testuser");
    }

    [Fact]
    public async Task ExecuteScriptAsync_WithProgress_ReportsProgress()
    {
        var progressReports = new List<string>();
        var progress = new Progress<string>(s => progressReports.Add(s));

        _processRunnerMock.Setup(p => p.RunWithOutputAsync(
                "wsl.exe",
                It.IsAny<string>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Action<string>?, Action<string>?, CancellationToken>(
                (_, _, output, _, _) =>
                {
                    output?.Invoke("progress 1");
                    output?.Invoke("progress 2");
                })
            .ReturnsAsync(0);

        await _service.ExecuteScriptAsync("Ubuntu", "echo test", progress: progress);

        // Allow async progress to complete
        await Task.Delay(100);

        progressReports.Should().Contain("progress 1");
        progressReports.Should().Contain("progress 2");
    }

    [Fact]
    public async Task ExecuteScriptAsync_WhenCancelled_ReturnsCancelledResult()
    {
        _processRunnerMock.Setup(p => p.RunWithOutputAsync(
                "wsl.exe",
                It.IsAny<string>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var result = await _service.ExecuteScriptAsync("Ubuntu", "sleep 100");

        result.WasCancelled.Should().BeTrue();
        result.ExitCode.Should().Be(-1);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteScriptAsync_EscapesSingleQuotes()
    {
        string? capturedCommand = null;
        _processRunnerMock.Setup(p => p.RunWithOutputAsync(
                "wsl.exe",
                It.IsAny<string>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Action<string>?, Action<string>?, CancellationToken>(
                (_, cmd, _, _, _) => capturedCommand = cmd)
            .ReturnsAsync(0);

        await _service.ExecuteScriptAsync("Ubuntu", "echo 'hello'");

        capturedCommand.Should().Contain(@"'\''");
    }

    #endregion

    #region ValidateScriptAsync Tests

    [Fact]
    public async Task ValidateScriptAsync_WithNullDistribution_ThrowsArgumentException()
    {
        var act = () => _service.ValidateScriptAsync(null!, "echo test");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ValidateScriptAsync_WithValidScript_ReturnsSuccess()
    {
        _processRunnerMock.Setup(p => p.RunAsync(
                "wsl.exe",
                It.IsAny<string>(),
                It.IsAny<System.Text.Encoding?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult
            {
                ExitCode = 0,
                StandardOutput = "",
                StandardError = ""
            });

        var result = await _service.ValidateScriptAsync("Ubuntu", "echo test");

        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.ErrorLine.Should().BeNull();
    }

    [Fact]
    public async Task ValidateScriptAsync_WithSyntaxError_ReturnsFailure()
    {
        _processRunnerMock.Setup(p => p.RunAsync(
                "wsl.exe",
                It.IsAny<string>(),
                It.IsAny<System.Text.Encoding?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult
            {
                ExitCode = 2,
                StandardOutput = "",
                StandardError = "bash: -c: line 5: syntax error near unexpected token `}'"
            });

        var result = await _service.ValidateScriptAsync("Ubuntu", "if true; }");

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("syntax error");
        result.ErrorLine.Should().Be(5);
    }

    [Fact]
    public async Task ValidateScriptAsync_WithErrorWithoutLineNumber_ReturnsErrorWithoutLine()
    {
        _processRunnerMock.Setup(p => p.RunAsync(
                "wsl.exe",
                It.IsAny<string>(),
                It.IsAny<System.Text.Encoding?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult
            {
                ExitCode = 1,
                StandardOutput = "",
                StandardError = "unexpected error"
            });

        var result = await _service.ValidateScriptAsync("Ubuntu", "invalid");

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("unexpected error");
        result.ErrorLine.Should().BeNull();
    }

    [Fact]
    public async Task ValidateScriptAsync_WhenProcessRunnerThrows_ReturnsFailure()
    {
        _processRunnerMock.Setup(p => p.RunAsync(
                "wsl.exe",
                It.IsAny<string>(),
                It.IsAny<System.Text.Encoding?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("WSL not available"));

        var result = await _service.ValidateScriptAsync("Ubuntu", "echo test");

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("WSL not available");
    }

    #endregion

    #region ExecuteTemplateAsync Tests

    [Fact]
    public async Task ExecuteTemplateAsync_WithNullTemplate_ThrowsArgumentNullException()
    {
        var act = () => _service.ExecuteTemplateAsync("Ubuntu", null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteTemplateAsync_MergesVariables()
    {
        string? capturedCommand = null;
        _processRunnerMock.Setup(p => p.RunWithOutputAsync(
                "wsl.exe",
                It.IsAny<string>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Action<string>?, Action<string>?, CancellationToken>(
                (_, cmd, _, _, _) => capturedCommand = cmd)
            .ReturnsAsync(0);

        var template = new ScriptTemplate
        {
            Name = "Test",
            ScriptContent = "echo ${USER} and ${HOME}",
            Variables = new Dictionary<string, string>
            {
                ["USER"] = "default_user",
                ["HOME"] = "/home/default"
            }
        };

        var overrides = new Dictionary<string, string>
        {
            ["USER"] = "override_user"
        };

        await _service.ExecuteTemplateAsync("Ubuntu", template, overrides);

        // Override should take precedence
        capturedCommand.Should().Contain("override_user");
        // Default should be used when no override
        capturedCommand.Should().Contain("/home/default");
    }

    [Fact]
    public async Task ExecuteTemplateAsync_WithNoVariables_ExecutesSuccessfully()
    {
        _processRunnerMock.Setup(p => p.RunWithOutputAsync(
                "wsl.exe",
                It.IsAny<string>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var template = new ScriptTemplate
        {
            Name = "Test",
            ScriptContent = "echo hello"
        };

        var result = await _service.ExecuteTemplateAsync("Ubuntu", template);

        result.IsSuccess.Should().BeTrue();
    }

    #endregion
}
