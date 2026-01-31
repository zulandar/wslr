using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Wslr.App.Helpers;
using Wslr.UI.Services;
using Wslr.UI.ViewModels;

namespace Wslr.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;
    private readonly ISettingsService _settingsService;
    private readonly MainWindowViewModel _viewModel;
    private bool _isExiting;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The view model.</param>
    /// <param name="navigationService">The navigation service.</param>
    /// <param name="settingsService">The settings service.</param>
    public MainWindow(MainWindowViewModel viewModel, INavigationService navigationService, ISettingsService settingsService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _navigationService = navigationService;
        _settingsService = settingsService;

        // Set application icon for taskbar
        Icon = IconHelper.CreateAppIcon();

        // Update maximize icon when window state changes
        StateChanged += MainWindow_StateChanged;

        // Restore window state on load
        Loaded += MainWindow_Loaded;

        // Handle keyboard navigation
        PreviewKeyDown += MainWindow_PreviewKeyDown;
    }

    /// <summary>
    /// Marks the window for exit (not just minimize to tray).
    /// </summary>
    public void MarkForExit()
    {
        _isExiting = true;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        RestoreWindowState();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        // Always save window state before closing/hiding
        SaveWindowState();

        if (!_isExiting)
        {
            // Minimize to tray instead of closing
            e.Cancel = true;
            _navigationService.HideMainWindow();
        }
    }

    private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // Handle arrow key navigation for the nav rail
        if (e.Key == Key.Up || e.Key == Key.Down)
        {
            // Check if focus is on a navigation item or nearby
            var focused = Keyboard.FocusedElement as FrameworkElement;
            if (focused == null) return;

            // Only handle if we're in the navigation area
            var parent = focused;
            bool isInNavArea = false;
            while (parent != null)
            {
                if (parent is System.Windows.Controls.RadioButton rb && rb.Style == FindResource("NavItemStyle"))
                {
                    isInNavArea = true;
                    break;
                }
                parent = parent.Parent as FrameworkElement;
            }

            if (isInNavArea)
            {
                int currentIndex = _viewModel.SelectedNavigationIndex;
                int maxIndex = 2; // 0=Distributions, 1=Terminal, 2=Install

                if (e.Key == Key.Up && currentIndex > 0)
                {
                    NavigateToIndex(currentIndex - 1);
                    e.Handled = true;
                }
                else if (e.Key == Key.Down && currentIndex < maxIndex)
                {
                    NavigateToIndex(currentIndex + 1);
                    e.Handled = true;
                }
            }
        }
    }

    private void NavigateToIndex(int index)
    {
        switch (index)
        {
            case 0:
                _viewModel.NavigateToDistributionsCommand.Execute(null);
                break;
            case 1:
                _viewModel.NavigateToTerminalCommand.Execute(null);
                break;
            case 2:
                _viewModel.NavigateToInstallCommand.Execute(null);
                break;
        }
    }

    private void SaveWindowState()
    {
        // Don't save if minimized - save the restored size instead
        if (WindowState == WindowState.Minimized)
            return;

        // Save the restore bounds if maximized, otherwise current bounds
        if (WindowState == WindowState.Maximized)
        {
            _settingsService.Set(SettingKeys.WindowWidth, RestoreBounds.Width);
            _settingsService.Set(SettingKeys.WindowHeight, RestoreBounds.Height);
            _settingsService.Set(SettingKeys.WindowLeft, RestoreBounds.Left);
            _settingsService.Set(SettingKeys.WindowTop, RestoreBounds.Top);
        }
        else
        {
            _settingsService.Set(SettingKeys.WindowWidth, Width);
            _settingsService.Set(SettingKeys.WindowHeight, Height);
            _settingsService.Set(SettingKeys.WindowLeft, Left);
            _settingsService.Set(SettingKeys.WindowTop, Top);
        }

        _settingsService.Set(SettingKeys.WindowState, (int)WindowState);
        _settingsService.Save();
    }

    private void RestoreWindowState()
    {
        var savedWidth = _settingsService.Get(SettingKeys.WindowWidth, 0.0);
        var savedHeight = _settingsService.Get(SettingKeys.WindowHeight, 0.0);
        var savedLeft = _settingsService.Get(SettingKeys.WindowLeft, double.NaN);
        var savedTop = _settingsService.Get(SettingKeys.WindowTop, double.NaN);
        var savedState = _settingsService.Get(SettingKeys.WindowState, (int)WindowState.Normal);

        // Only restore if we have valid saved values
        if (savedWidth > 0 && savedHeight > 0)
        {
            Width = savedWidth;
            Height = savedHeight;
        }

        // Restore position if saved, validating against current monitor setup
        if (!double.IsNaN(savedLeft) && !double.IsNaN(savedTop))
        {
            // Validate that the window will be visible on at least one monitor
            if (IsWindowPositionValid(savedLeft, savedTop, Width, Height))
            {
                Left = savedLeft;
                Top = savedTop;
            }
            else
            {
                // Position is invalid (monitor may have been disconnected), center on primary
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        // Restore window state (normal/maximized)
        var state = (WindowState)savedState;
        if (state == WindowState.Maximized)
        {
            WindowState = WindowState.Maximized;
        }
        // Don't restore minimized state - always show normal or maximized
    }

    private bool IsWindowPositionValid(double left, double top, double width, double height)
    {
        // Use Win32 API to check if the window position is on any monitor
        var point = new POINT { X = (int)(left + width / 2), Y = (int)(top + 16) }; // Center-top of title bar
        var monitor = MonitorFromPoint(point, MONITOR_DEFAULTTONULL);
        return monitor != IntPtr.Zero;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    private const uint MONITOR_DEFAULTTONULL = 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        // Update the maximize/restore icon based on window state
        // E922 = Maximize, E923 = Restore
        MaximizeIcon.Text = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
        MaximizeButton.ToolTip = WindowState == WindowState.Maximized ? "Restore" : "Maximize";
    }
}
