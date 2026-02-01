using Wslr.Core.Models;

namespace Wslr.Core.Tests.Models;

public class ScriptExecutionResultTests
{
    #region IsSuccess Computed Property Tests

    [Fact]
    public void IsSuccess_WhenExitCodeIsZero_ReturnsTrue()
    {
        var result = new ScriptExecutionResult
        {
            ExitCode = 0,
            StandardOutput = "success",
            StandardError = "",
            Duration = TimeSpan.FromSeconds(1)
        };

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void IsSuccess_WhenExitCodeIsOne_ReturnsFalse()
    {
        var result = new ScriptExecutionResult
        {
            ExitCode = 1,
            StandardOutput = "",
            StandardError = "error",
            Duration = TimeSpan.FromSeconds(1)
        };

        result.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(127)]
    [InlineData(255)]
    public void IsSuccess_WithVariousNonZeroExitCodes_ReturnsFalse(int exitCode)
    {
        var result = new ScriptExecutionResult
        {
            ExitCode = exitCode,
            StandardOutput = "",
            StandardError = "",
            Duration = TimeSpan.Zero
        };

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region Required Property Tests

    [Fact]
    public void ExitCode_IsRequired_CanBeSet()
    {
        var result = new ScriptExecutionResult
        {
            ExitCode = 42,
            StandardOutput = "",
            StandardError = "",
            Duration = TimeSpan.Zero
        };

        result.ExitCode.Should().Be(42);
    }

    [Fact]
    public void StandardOutput_IsRequired_CanBeSet()
    {
        var result = new ScriptExecutionResult
        {
            ExitCode = 0,
            StandardOutput = "Hello World\nLine 2",
            StandardError = "",
            Duration = TimeSpan.Zero
        };

        result.StandardOutput.Should().Be("Hello World\nLine 2");
    }

    [Fact]
    public void StandardError_IsRequired_CanBeSet()
    {
        var result = new ScriptExecutionResult
        {
            ExitCode = 1,
            StandardOutput = "",
            StandardError = "Error: command not found",
            Duration = TimeSpan.Zero
        };

        result.StandardError.Should().Be("Error: command not found");
    }

    [Fact]
    public void Duration_IsRequired_CanBeSet()
    {
        var duration = TimeSpan.FromMilliseconds(1234.567);

        var result = new ScriptExecutionResult
        {
            ExitCode = 0,
            StandardOutput = "",
            StandardError = "",
            Duration = duration
        };

        result.Duration.Should().Be(duration);
    }

    #endregion

    #region WasCancelled Property Tests

    [Fact]
    public void WasCancelled_DefaultValue_IsFalse()
    {
        var result = new ScriptExecutionResult
        {
            ExitCode = 0,
            StandardOutput = "",
            StandardError = "",
            Duration = TimeSpan.Zero
        };

        result.WasCancelled.Should().BeFalse();
    }

    [Fact]
    public void WasCancelled_WhenSet_ReturnsTrue()
    {
        var result = new ScriptExecutionResult
        {
            ExitCode = -1,
            StandardOutput = "",
            StandardError = "",
            Duration = TimeSpan.FromSeconds(5),
            WasCancelled = true
        };

        result.WasCancelled.Should().BeTrue();
    }

    [Fact]
    public void WasCancelled_WithNonZeroExitCode_IsIndependent()
    {
        // A cancelled script still has IsSuccess = false due to exit code
        var result = new ScriptExecutionResult
        {
            ExitCode = -1,
            StandardOutput = "partial output",
            StandardError = "",
            Duration = TimeSpan.FromSeconds(10),
            WasCancelled = true
        };

        result.WasCancelled.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void EmptyOutput_IsValid()
    {
        var result = new ScriptExecutionResult
        {
            ExitCode = 0,
            StandardOutput = "",
            StandardError = "",
            Duration = TimeSpan.Zero
        };

        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Should().BeEmpty();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void LargeOutput_IsSupported()
    {
        var largeOutput = new string('x', 100000);

        var result = new ScriptExecutionResult
        {
            ExitCode = 0,
            StandardOutput = largeOutput,
            StandardError = "",
            Duration = TimeSpan.Zero
        };

        result.StandardOutput.Should().HaveLength(100000);
    }

    [Fact]
    public void ZeroDuration_IsValid()
    {
        var result = new ScriptExecutionResult
        {
            ExitCode = 0,
            StandardOutput = "",
            StandardError = "",
            Duration = TimeSpan.Zero
        };

        result.Duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void LongDuration_IsSupported()
    {
        var duration = TimeSpan.FromHours(1);

        var result = new ScriptExecutionResult
        {
            ExitCode = 0,
            StandardOutput = "",
            StandardError = "",
            Duration = duration
        };

        result.Duration.Should().Be(duration);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        var result1 = new ScriptExecutionResult
        {
            ExitCode = 0,
            StandardOutput = "output",
            StandardError = "",
            Duration = TimeSpan.FromSeconds(1)
        };
        var result2 = new ScriptExecutionResult
        {
            ExitCode = 0,
            StandardOutput = "output",
            StandardError = "",
            Duration = TimeSpan.FromSeconds(1)
        };

        result1.Should().Be(result2);
    }

    [Fact]
    public void Equals_WithDifferentExitCode_ReturnsFalse()
    {
        var result1 = new ScriptExecutionResult
        {
            ExitCode = 0,
            StandardOutput = "output",
            StandardError = "",
            Duration = TimeSpan.FromSeconds(1)
        };
        var result2 = new ScriptExecutionResult
        {
            ExitCode = 1,
            StandardOutput = "output",
            StandardError = "",
            Duration = TimeSpan.FromSeconds(1)
        };

        result1.Should().NotBe(result2);
    }

    #endregion
}
