using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Wslr.UI.Services;
using Wslr.UI.ViewModels;

namespace Wslr.App.Services;

/// <summary>
/// Implementation of <see cref="INavigationService"/> for WPF navigation.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationService"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public bool CanGoBack => false; // Simple navigation, no back stack

    /// <inheritdoc />
    public void NavigateTo<TViewModel>() where TViewModel : class
    {
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();

        if (mainWindow.DataContext is MainWindowViewModel mainViewModel)
        {
            mainViewModel.CurrentViewModel = viewModel;
        }
    }

    /// <inheritdoc />
    public void NavigateTo<TViewModel>(object parameter) where TViewModel : class
    {
        // For now, just navigate without parameter
        NavigateTo<TViewModel>();
    }

    /// <inheritdoc />
    public void GoBack()
    {
        // Simple navigation, navigate to main view
        NavigateTo<DistributionListViewModel>();
    }

    /// <inheritdoc />
    public void ShowMainWindow()
    {
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
        mainWindow.WindowState = WindowState.Normal;
        mainWindow.Activate();
    }

    /// <inheritdoc />
    public void HideMainWindow()
    {
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Hide();
    }

    /// <inheritdoc />
    public void ExitApplication()
    {
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.MarkForExit();
        Application.Current.Shutdown();
    }
}
