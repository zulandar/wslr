using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wslr.Core.Interfaces;

namespace Wslr.UI.ViewModels;

/// <summary>
/// ViewModel for a single terminal tab.
/// </summary>
public partial class TerminalTabViewModel : ObservableObject, IAsyncDisposable
{
    private ITerminalSession? _session;
    private readonly Queue<string> _pendingOutput = new();
    private bool _hasSubscribers;

    /// <summary>
    /// Gets the unique identifier for this tab.
    /// </summary>
    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Gets the distribution name for this tab.
    /// </summary>
    public string DistributionName { get; }

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isConnecting;

    [ObservableProperty]
    private string? _errorMessage;

    private Action<string>? _outputReceived;

    /// <summary>
    /// Raised when output is received from the terminal.
    /// Buffered output is flushed when the first subscriber attaches.
    /// </summary>
    public event Action<string>? OutputReceived
    {
        add
        {
            _outputReceived += value;
            if (value != null && !_hasSubscribers)
            {
                _hasSubscribers = true;
                FlushPendingOutput();
            }
        }
        remove
        {
            _outputReceived -= value;
        }
    }

    /// <summary>
    /// Raised when the terminal session exits.
    /// </summary>
    public event Action<int>? SessionExited;

    /// <summary>
    /// Raised when the tab requests to be closed.
    /// </summary>
    public event Action<TerminalTabViewModel>? CloseRequested;

    /// <summary>
    /// Raised when the tab requests to be activated.
    /// </summary>
    public event Action<TerminalTabViewModel>? ActivateRequested;

    /// <summary>
    /// Gets the current session ID, or null if not connected.
    /// </summary>
    public string? SessionId => _session?.Id;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalTabViewModel"/> class.
    /// </summary>
    /// <param name="distributionName">The distribution name.</param>
    public TerminalTabViewModel(string distributionName)
    {
        DistributionName = distributionName;
        _title = distributionName;
    }

    /// <summary>
    /// Connects to the WSL distribution.
    /// </summary>
    /// <param name="sessionService">The session service to use.</param>
    public async Task ConnectAsync(ITerminalSessionService sessionService)
    {
        if (_session != null)
        {
            return;
        }

        IsConnecting = true;
        ErrorMessage = null;

        try
        {
            _session = await sessionService.CreateSessionAsync(DistributionName);
            _session.OutputReceived += OnOutputReceived;
            _session.Exited += OnSessionExited;
            IsConnected = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to connect: {ex.Message}";
        }
        finally
        {
            IsConnecting = false;
        }
    }

    /// <summary>
    /// Disconnects the terminal session.
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_session == null)
        {
            return;
        }

        try
        {
            _session.OutputReceived -= OnOutputReceived;
            _session.Exited -= OnSessionExited;
            _session.Terminate();
            await _session.DisposeAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error disconnecting terminal: {ex.Message}");
        }
        finally
        {
            _session = null;
            IsConnected = false;
        }
    }

    /// <summary>
    /// Sends input to the terminal.
    /// </summary>
    /// <param name="input">The input to send.</param>
    public async Task SendInputAsync(string input)
    {
        if (_session == null || !IsConnected)
        {
            return;
        }

        try
        {
            await _session.WriteAsync(input);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error sending terminal input: {ex.Message}");
        }
    }

    /// <summary>
    /// Notifies the terminal of a resize.
    /// </summary>
    /// <param name="columns">The new column count.</param>
    /// <param name="rows">The new row count.</param>
    public async Task ResizeAsync(int columns, int rows)
    {
        if (_session == null)
        {
            return;
        }

        try
        {
            await _session.ResizeAsync(columns, rows);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error resizing terminal: {ex.Message}");
        }
    }

    /// <summary>
    /// Opens this distribution in Windows Terminal.
    /// </summary>
    [RelayCommand]
    private void OpenInWindowsTerminal()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "wt.exe",
                Arguments = $"-d \\\\wsl$\\{DistributionName}",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to open Windows Terminal: {ex.Message}";
        }
    }

    /// <summary>
    /// Activates this tab.
    /// </summary>
    [RelayCommand]
    private void Activate()
    {
        ActivateRequested?.Invoke(this);
    }

    /// <summary>
    /// Requests to close this tab.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke(this);
    }

    private void OnOutputReceived(string output)
    {
        if (_hasSubscribers)
        {
            _outputReceived?.Invoke(output);
        }
        else
        {
            // Buffer output until a subscriber attaches
            _pendingOutput.Enqueue(output);
        }
    }

    private void FlushPendingOutput()
    {
        while (_pendingOutput.Count > 0)
        {
            var output = _pendingOutput.Dequeue();
            _outputReceived?.Invoke(output);
        }
    }

    private void OnSessionExited(int exitCode)
    {
        IsConnected = false;
        SessionExited?.Invoke(exitCode);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
