using Wslr.Core.Interfaces;
using Wslr.Core.Models;

namespace Wslr.Core.Tests.Helpers;

/// <summary>
/// Factory for creating test data and mock objects.
/// </summary>
public static class MockFactory
{
    #region Model Builders

    /// <summary>
    /// Creates a default WslDistribution for testing.
    /// </summary>
    public static WslDistribution CreateDistribution(
        string name = "Ubuntu",
        DistributionState state = DistributionState.Stopped,
        int version = 2,
        bool isDefault = false)
    {
        return new WslDistribution
        {
            Name = name,
            State = state,
            Version = version,
            IsDefault = isDefault
        };
    }

    /// <summary>
    /// Creates a running WslDistribution for testing.
    /// </summary>
    public static WslDistribution CreateRunningDistribution(string name = "Ubuntu", bool isDefault = false)
    {
        return CreateDistribution(name, DistributionState.Running, 2, isDefault);
    }

    /// <summary>
    /// Creates a stopped WslDistribution for testing.
    /// </summary>
    public static WslDistribution CreateStoppedDistribution(string name = "Ubuntu", bool isDefault = false)
    {
        return CreateDistribution(name, DistributionState.Stopped, 2, isDefault);
    }

    /// <summary>
    /// Creates a list of sample distributions for testing.
    /// </summary>
    public static IReadOnlyList<WslDistribution> CreateDistributionList()
    {
        return new List<WslDistribution>
        {
            CreateDistribution("Ubuntu", DistributionState.Running, 2, true),
            CreateDistribution("Debian", DistributionState.Stopped, 2, false),
            CreateDistribution("Alpine", DistributionState.Stopped, 2, false)
        };
    }

    /// <summary>
    /// Creates a default OnlineDistribution for testing.
    /// </summary>
    public static OnlineDistribution CreateOnlineDistribution(
        string name = "Ubuntu",
        string friendlyName = "Ubuntu 22.04 LTS")
    {
        return new OnlineDistribution
        {
            Name = name,
            FriendlyName = friendlyName
        };
    }

    /// <summary>
    /// Creates a list of sample online distributions for testing.
    /// </summary>
    public static IReadOnlyList<OnlineDistribution> CreateOnlineDistributionList()
    {
        return new List<OnlineDistribution>
        {
            CreateOnlineDistribution("Ubuntu", "Ubuntu 22.04 LTS"),
            CreateOnlineDistribution("Debian", "Debian GNU/Linux"),
            CreateOnlineDistribution("kali-linux", "Kali Linux Rolling"),
            CreateOnlineDistribution("Alpine", "Alpine WSL")
        };
    }

    /// <summary>
    /// Creates a successful ProcessResult for testing.
    /// </summary>
    public static ProcessResult CreateSuccessResult(string output = "")
    {
        return new ProcessResult
        {
            ExitCode = 0,
            StandardOutput = output,
            StandardError = ""
        };
    }

    /// <summary>
    /// Creates a failed ProcessResult for testing.
    /// </summary>
    public static ProcessResult CreateFailureResult(string error = "An error occurred", int exitCode = 1)
    {
        return new ProcessResult
        {
            ExitCode = exitCode,
            StandardOutput = "",
            StandardError = error
        };
    }

    #endregion

    #region Mock Builders

    /// <summary>
    /// Creates a mock IWslService with default setup.
    /// </summary>
    public static Mock<IWslService> CreateWslServiceMock()
    {
        var mock = new Mock<IWslService>();

        mock.Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDistributionList());

        mock.Setup(x => x.GetOnlineDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOnlineDistributionList());

        mock.Setup(x => x.ExecuteCommandAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult());

        return mock;
    }

    /// <summary>
    /// Creates a mock IWslService that returns an empty distribution list.
    /// </summary>
    public static Mock<IWslService> CreateEmptyWslServiceMock()
    {
        var mock = new Mock<IWslService>();

        mock.Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>());

        mock.Setup(x => x.GetOnlineDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OnlineDistribution>());

        return mock;
    }

    /// <summary>
    /// Creates a mock IProcessRunner with default setup.
    /// </summary>
    public static Mock<IProcessRunner> CreateProcessRunnerMock()
    {
        var mock = new Mock<IProcessRunner>();

        mock.Setup(x => x.RunAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult());

        mock.Setup(x => x.RunWithOutputAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        return mock;
    }

    /// <summary>
    /// Creates a mock IProcessRunner that returns specific output.
    /// </summary>
    public static Mock<IProcessRunner> CreateProcessRunnerMock(string output)
    {
        var mock = new Mock<IProcessRunner>();

        mock.Setup(x => x.RunAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(output));

        return mock;
    }

    /// <summary>
    /// Creates a mock IProcessRunner that returns a failure.
    /// </summary>
    public static Mock<IProcessRunner> CreateFailingProcessRunnerMock(string error = "Command failed", int exitCode = 1)
    {
        var mock = new Mock<IProcessRunner>();

        mock.Setup(x => x.RunAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult(error, exitCode));

        mock.Setup(x => x.RunWithOutputAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<Action<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exitCode);

        return mock;
    }

    #endregion
}
