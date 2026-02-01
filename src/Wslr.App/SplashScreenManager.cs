using System.Windows.Threading;

namespace Wslr.App;

/// <summary>
/// Manages a splash screen on a separate UI thread to ensure smooth animations
/// regardless of main thread activity.
/// </summary>
public sealed class SplashScreenManager : IDisposable
{
    private Thread? _splashThread;
    private SplashScreen? _splashScreen;
    private Dispatcher? _splashDispatcher;
    private readonly ManualResetEventSlim _splashReady = new(false);
    private bool _disposed;

    /// <summary>
    /// Shows the splash screen on a dedicated thread.
    /// </summary>
    public void Show()
    {
        _splashThread = new Thread(SplashThreadStart)
        {
            Name = "SplashScreen",
            IsBackground = true
        };
        _splashThread.SetApartmentState(ApartmentState.STA);
        _splashThread.Start();

        // Wait for splash to be ready
        _splashReady.Wait();
    }

    private void SplashThreadStart()
    {
        _splashScreen = new SplashScreen();
        _splashDispatcher = Dispatcher.CurrentDispatcher;

        _splashScreen.Show();
        _splashReady.Set();

        // Run dispatcher until shutdown
        Dispatcher.Run();
    }

    /// <summary>
    /// Updates the status text on the splash screen.
    /// </summary>
    public void UpdateStatus(string status)
    {
        _splashDispatcher?.BeginInvoke(() =>
        {
            _splashScreen?.UpdateStatus(status);
        });
    }

    /// <summary>
    /// Closes the splash screen with a fade animation.
    /// </summary>
    public async Task CloseAsync()
    {
        if (_splashDispatcher is null || _splashScreen is null)
        {
            return;
        }

        var tcs = new TaskCompletionSource<bool>();

        _ = _splashDispatcher.BeginInvoke(async () =>
        {
            try
            {
                await _splashScreen.FadeOutAndCloseAsync();
            }
            finally
            {
                _splashDispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
                tcs.TrySetResult(true);
            }
        });

        await tcs.Task;
    }

    /// <summary>
    /// Immediately closes the splash screen without animation.
    /// </summary>
    public void Close()
    {
        _splashDispatcher?.BeginInvoke(() =>
        {
            _splashScreen?.Close();
            _splashDispatcher?.BeginInvokeShutdown(DispatcherPriority.Normal);
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Close();
        _splashReady.Dispose();
    }
}
