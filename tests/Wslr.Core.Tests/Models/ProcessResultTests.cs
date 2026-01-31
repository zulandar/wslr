using Wslr.Core.Models;

namespace Wslr.Core.Tests.Models;

public class ProcessResultTests
{
    [Fact]
    public void IsSuccess_ShouldReturnTrue_WhenExitCodeIsZero()
    {
        var result = new ProcessResult
        {
            ExitCode = 0,
            StandardOutput = "output",
            StandardError = ""
        };

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void IsSuccess_ShouldReturnFalse_WhenExitCodeIsNonZero()
    {
        var result = new ProcessResult
        {
            ExitCode = 1,
            StandardOutput = "",
            StandardError = "error"
        };

        result.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(255)]
    public void IsSuccess_ShouldReturnFalse_ForVariousNonZeroExitCodes(int exitCode)
    {
        var result = new ProcessResult
        {
            ExitCode = exitCode,
            StandardOutput = "",
            StandardError = ""
        };

        result.IsSuccess.Should().BeFalse();
    }
}
