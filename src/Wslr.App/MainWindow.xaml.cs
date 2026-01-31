using System.ComponentModel;
using System.Windows;
using Wslr.UI.Services;
using Wslr.UI.ViewModels;

namespace Wslr.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;
    private bool _isExiting;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The view model.</param>
    /// <param name="navigationService">The navigation service.</param>
    public MainWindow(MainWindowViewModel viewModel, INavigationService navigationService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _navigationService = navigationService;
    }

    /// <summary>
    /// Marks the window for exit (not just minimize to tray).
    /// </summary>
    public void MarkForExit()
    {
        _isExiting = true;
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (!_isExiting)
        {
            // Minimize to tray instead of closing
            e.Cancel = true;
            _navigationService.HideMainWindow();
        }
    }
}
