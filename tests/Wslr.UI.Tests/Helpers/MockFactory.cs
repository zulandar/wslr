using Wslr.Core.Interfaces;
using Wslr.Core.Models;
using Wslr.UI.Services;

namespace Wslr.UI.Tests.Helpers;

/// <summary>
/// Factory for creating test data and mock objects for UI tests.
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

    #region Service Mocks

    /// <summary>
    /// Creates a mock IWslService with default setup.
    /// </summary>
    public static Mock<IWslService> CreateWslServiceMock()
    {
        var mock = new Mock<IWslService>();

        mock.Setup(x => x.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDistributionList());

        mock.Setup(x => x.ExecuteCommandAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult());

        return mock;
    }

    /// <summary>
    /// Creates a mock IDialogService with default setup (confirms all dialogs).
    /// </summary>
    public static Mock<IDialogService> CreateDialogServiceMock(bool confirmResult = true)
    {
        var mock = new Mock<IDialogService>();

        mock.Setup(x => x.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(confirmResult);

        mock.Setup(x => x.ShowErrorAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        mock.Setup(x => x.ShowInfoAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        return mock;
    }

    /// <summary>
    /// Creates a mock IDialogService configured for file dialogs.
    /// </summary>
    public static Mock<IDialogService> CreateFileDialogServiceMock(
        string? saveFilePath = @"C:\test\export.tar",
        string? openFilePath = @"C:\test\import.tar",
        string? folderPath = @"C:\test\folder")
    {
        var mock = CreateDialogServiceMock();

        mock.Setup(x => x.ShowSaveFileDialogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(saveFilePath);

        mock.Setup(x => x.ShowOpenFileDialogAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(openFilePath);

        mock.Setup(x => x.ShowFolderBrowserDialogAsync(It.IsAny<string>()))
            .ReturnsAsync(folderPath);

        return mock;
    }

    /// <summary>
    /// Creates a mock INavigationService.
    /// </summary>
    public static Mock<INavigationService> CreateNavigationServiceMock(bool canGoBack = false)
    {
        var mock = new Mock<INavigationService>();

        mock.SetupGet(x => x.CanGoBack).Returns(canGoBack);

        return mock;
    }

    /// <summary>
    /// Creates a mock INotificationService.
    /// </summary>
    public static Mock<INotificationService> CreateNotificationServiceMock()
    {
        var mock = new Mock<INotificationService>();
        return mock;
    }

    /// <summary>
    /// Creates a mock ITrayIconService.
    /// </summary>
    public static Mock<ITrayIconService> CreateTrayIconServiceMock()
    {
        var mock = new Mock<ITrayIconService>();
        return mock;
    }

    #endregion
}
