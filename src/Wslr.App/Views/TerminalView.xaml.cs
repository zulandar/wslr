using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wslr.App.Controls;
using Wslr.UI.ViewModels;

namespace Wslr.App.Views;

/// <summary>
/// Interaction logic for TerminalView.xaml
/// </summary>
public partial class TerminalView : UserControl
{
    private TerminalViewModel? _viewModel;
    private readonly Dictionary<string, TerminalControl> _terminalControls = new();
    private TerminalControl? _activeTerminal;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalView"/> class.
    /// </summary>
    public TerminalView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // Unsubscribe from old ViewModel
        if (_viewModel != null)
        {
            _viewModel.TabAdded -= OnTabAdded;
            _viewModel.TabRemoved -= OnTabRemoved;
            _viewModel.ActiveTabChanged -= OnActiveTabChanged;
        }

        // Subscribe to new ViewModel
        _viewModel = e.NewValue as TerminalViewModel;
        if (_viewModel != null)
        {
            _viewModel.TabAdded += OnTabAdded;
            _viewModel.TabRemoved += OnTabRemoved;
            _viewModel.ActiveTabChanged += OnActiveTabChanged;

            // Create controls for any existing tabs
            foreach (var tab in _viewModel.Tabs)
            {
                CreateTerminalControl(tab);
            }

            // Activate the active tab
            if (_viewModel.ActiveTab != null)
            {
                ActivateTerminalControl(_viewModel.ActiveTab);
            }
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Set up keyboard shortcuts
        var window = Window.GetWindow(this);
        if (window != null)
        {
            window.PreviewKeyDown += OnWindowPreviewKeyDown;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // Remove keyboard shortcuts
        var window = Window.GetWindow(this);
        if (window != null)
        {
            window.PreviewKeyDown -= OnWindowPreviewKeyDown;
        }

        // Clean up all terminal controls
        foreach (var control in _terminalControls.Values)
        {
            TerminalContainer.Children.Remove(control);
        }
        _terminalControls.Clear();
        _activeTerminal = null;
    }

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_viewModel == null)
        {
            return;
        }

        // Ctrl+W: Close current tab
        if (e.Key == Key.W && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (_viewModel.ActiveTab != null)
            {
                _ = _viewModel.CloseTabAsync(_viewModel.ActiveTab);
                e.Handled = true;
            }
        }
        // Ctrl+Tab: Next tab
        else if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _viewModel.NextTabCommand.Execute(null);
            e.Handled = true;
        }
        // Ctrl+Shift+Tab: Previous tab
        else if (e.Key == Key.Tab && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            _viewModel.PreviousTabCommand.Execute(null);
            e.Handled = true;
        }
        // Ctrl+1-9: Switch to tab by index
        else if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            var tabIndex = e.Key switch
            {
                Key.D1 => 1,
                Key.D2 => 2,
                Key.D3 => 3,
                Key.D4 => 4,
                Key.D5 => 5,
                Key.D6 => 6,
                Key.D7 => 7,
                Key.D8 => 8,
                Key.D9 => 9,
                _ => 0
            };

            if (tabIndex > 0 && tabIndex <= _viewModel.Tabs.Count)
            {
                _viewModel.ActivateTabByIndexCommand.Execute(tabIndex);
                e.Handled = true;
            }
        }
    }

    private void OnTabAdded(TerminalTabViewModel tab)
    {
        System.Diagnostics.Debug.WriteLine($"[Terminal] OnTabAdded: {tab.Id}");
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                CreateTerminalControl(tab);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Terminal] ERROR in OnTabAdded: {ex}");
            }
        });
    }

    private void OnTabRemoved(TerminalTabViewModel tab)
    {
        System.Diagnostics.Debug.WriteLine($"[Terminal] OnTabRemoved: {tab.Id}");
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                RemoveTerminalControl(tab);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Terminal] ERROR in OnTabRemoved: {ex}");
            }
        });
    }

    private void OnActiveTabChanged(TerminalTabViewModel? tab)
    {
        System.Diagnostics.Debug.WriteLine($"[Terminal] OnActiveTabChanged: {tab?.Id ?? "null"}");
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                if (tab != null)
                {
                    ActivateTerminalControl(tab);
                }
                else
                {
                    // No active tab - hide all terminals
                    foreach (var control in _terminalControls.Values)
                    {
                        control.Visibility = Visibility.Collapsed;
                    }
                    _activeTerminal = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Terminal] ERROR in OnActiveTabChanged: {ex}");
            }
        });
    }

    private void CreateTerminalControl(TerminalTabViewModel tab)
    {
        try
        {
            if (_terminalControls.ContainsKey(tab.Id))
            {
                System.Diagnostics.Debug.WriteLine($"[Terminal] Control already exists for tab {tab.Id}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[Terminal] Creating control for tab {tab.Id} ({tab.DistributionName})");

            var terminal = new TerminalControl
            {
                Visibility = Visibility.Collapsed
            };

            // Wire up events
            terminal.InputReceived += (_, input) => OnTerminalInput(tab, input);
            terminal.Resized += (_, e) => OnTerminalResized(tab, e);
            terminal.Ready += (_, _) => OnTerminalReady(tab, terminal);

            // Subscribe to tab output
            tab.OutputReceived += output => OnTabOutput(tab, terminal, output);
            tab.SessionExited += exitCode => OnTabSessionExited(tab, terminal, exitCode);

            // Subscribe to IsConnected changes to ensure terminal is visible when connected
            tab.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TerminalTabViewModel.IsConnected) && tab.IsConnected)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        System.Diagnostics.Debug.WriteLine($"[Terminal] Tab {tab.Id} connected, ensuring visibility");
                        if (_viewModel?.ActiveTab == tab && _terminalControls.TryGetValue(tab.Id, out var ctrl))
                        {
                            ctrl.Visibility = Visibility.Visible;
                        }
                    });
                }
            };

            _terminalControls[tab.Id] = terminal;
            TerminalContainer.Children.Add(terminal);
            System.Diagnostics.Debug.WriteLine($"[Terminal] Control created and added for tab {tab.Id}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Terminal] ERROR creating control: {ex}");
            throw;
        }
    }

    private void RemoveTerminalControl(TerminalTabViewModel tab)
    {
        if (!_terminalControls.TryGetValue(tab.Id, out var terminal))
        {
            return;
        }

        TerminalContainer.Children.Remove(terminal);
        _terminalControls.Remove(tab.Id);

        if (_activeTerminal == terminal)
        {
            _activeTerminal = null;
        }
    }

    private void ActivateTerminalControl(TerminalTabViewModel tab)
    {
        if (!_terminalControls.TryGetValue(tab.Id, out var terminal))
        {
            return;
        }

        // Hide all terminals
        foreach (var control in _terminalControls.Values)
        {
            control.Visibility = Visibility.Collapsed;
        }

        // Show and focus the active terminal
        terminal.Visibility = Visibility.Visible;
        _activeTerminal = terminal;

        if (terminal.IsReady)
        {
            terminal.Focus();
        }
    }

    private void OnTerminalInput(TerminalTabViewModel tab, string input)
    {
        _ = tab.SendInputAsync(input);
    }

    private void OnTerminalResized(TerminalTabViewModel tab, TerminalSizeEventArgs e)
    {
        _ = tab.ResizeAsync(e.Columns, e.Rows);
    }

    private void OnTerminalReady(TerminalTabViewModel tab, TerminalControl terminal)
    {
        // If this is the active tab, focus the terminal
        if (_viewModel?.ActiveTab == tab)
        {
            terminal.Focus();
        }
    }

    private void OnTabOutput(TerminalTabViewModel tab, TerminalControl terminal, string output)
    {
        Dispatcher.BeginInvoke(() =>
        {
            terminal.WriteOutput(output);
        });
    }

    private void OnTabSessionExited(TerminalTabViewModel tab, TerminalControl terminal, int exitCode)
    {
        Dispatcher.BeginInvoke(() =>
        {
            terminal.WriteOutput($"\r\n[Session exited with code {exitCode}]\r\n");
        });
    }
}
