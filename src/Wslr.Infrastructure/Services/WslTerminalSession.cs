using System.Diagnostics;
using System.Text;
using Wslr.Core.Interfaces;

namespace Wslr.Infrastructure.Services;

/// <summary>
/// A terminal session that connects to a WSL distribution via wsl.exe process.
/// </summary>
public sealed class WslTerminalSession : ITerminalSession
{
    private readonly Process _process;
    private readonly StreamWriter _stdin;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _outputTask;
    private readonly Task _errorTask;
    private bool _disposed;
    private int _columns = 80;
    private int _rows = 24;

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string DistributionName { get; }

    /// <inheritdoc />
    public bool IsRunning => !_process.HasExited;

    /// <inheritdoc />
    public event Action<string>? OutputReceived;

    /// <inheritdoc />
    public event Action<int>? Exited;

    /// <summary>
    /// Initializes a new instance of the <see cref="WslTerminalSession"/> class.
    /// </summary>
    /// <param name="distributionName">The name of the WSL distribution to connect to.</param>
    public WslTerminalSession(string distributionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distributionName);

        Id = Guid.NewGuid().ToString("N")[..8];
        DistributionName = distributionName;

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = $"-d {distributionName}",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            },
            EnableRaisingEvents = true,
        };

        _process.Exited += OnProcessExited;

        _process.Start();
        _stdin = _process.StandardInput;
        _stdin.AutoFlush = true;

        // Start reading output asynchronously
        _outputTask = ReadOutputAsync(_process.StandardOutput, _cts.Token);
        _errorTask = ReadOutputAsync(_process.StandardError, _cts.Token);
    }

    private async Task ReadOutputAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        var buffer = new char[4096];

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await reader.ReadAsync(buffer, cancellationToken);

                if (bytesRead == 0)
                {
                    // End of stream
                    break;
                }

                var output = new string(buffer, 0, bytesRead);
                OutputReceived?.Invoke(output);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading terminal output: {ex.Message}");
        }
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        Exited?.Invoke(_process.ExitCode);
    }

    /// <inheritdoc />
    public async Task WriteAsync(string input, CancellationToken cancellationToken = default)
    {
        if (_disposed || _process.HasExited)
        {
            return;
        }

        try
        {
            await _stdin.WriteAsync(input.AsMemory(), cancellationToken);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error writing to terminal: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Task ResizeAsync(int columns, int rows, CancellationToken cancellationToken = default)
    {
        // Store dimensions for reference
        _columns = columns;
        _rows = rows;

        // Note: With the simple wsl.exe process approach, we cannot properly
        // send SIGWINCH to the terminal. This would require using ConPTY.
        // For now, we just store the dimensions. Applications that query
        // terminal size directly (like vim) may not resize properly.
        //
        // A workaround is to set COLUMNS and LINES environment variables
        // or use the 'stty' command, but these have limitations.
        //
        // TODO: Consider implementing ConPTY for proper resize support.

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Terminate()
    {
        if (_disposed || _process.HasExited)
        {
            return;
        }

        try
        {
            // Try to close stdin first to signal the process to exit
            _stdin.Close();

            // Give the process a moment to exit gracefully
            if (!_process.WaitForExit(1000))
            {
                // Force kill if it doesn't exit
                _process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error terminating terminal: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Cancel the read tasks
        await _cts.CancelAsync();

        // Terminate the process if still running
        if (!_process.HasExited)
        {
            Terminate();
        }

        // Wait for read tasks to complete
        try
        {
            await Task.WhenAll(_outputTask, _errorTask).WaitAsync(TimeSpan.FromSeconds(2));
        }
        catch (TimeoutException)
        {
            Debug.WriteLine("Timeout waiting for terminal read tasks to complete");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error waiting for terminal read tasks: {ex.Message}");
        }

        // Clean up
        _process.Exited -= OnProcessExited;
        _stdin.Dispose();
        _process.Dispose();
        _cts.Dispose();
    }
}
