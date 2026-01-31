using System.Windows;
using System.Windows.Media.Animation;

namespace Wslr.App;

/// <summary>
/// Splash screen window displayed during application startup.
/// </summary>
public partial class SplashScreen : Window
{
    private readonly Storyboard _pulseAnimation;
    private readonly Storyboard _spinAnimation;
    private readonly Storyboard _fadeInAnimation;
    private readonly Storyboard _fadeOutAnimation;
    private TaskCompletionSource<bool>? _closeCompletionSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="SplashScreen"/> class.
    /// </summary>
    public SplashScreen()
    {
        InitializeComponent();

        _pulseAnimation = (Storyboard)FindResource("PulseAnimation");
        _spinAnimation = (Storyboard)FindResource("SpinAnimation");
        _fadeInAnimation = (Storyboard)FindResource("FadeInAnimation");
        _fadeOutAnimation = (Storyboard)FindResource("FadeOutAnimation");

        Loaded += SplashScreen_Loaded;
    }

    private void SplashScreen_Loaded(object sender, RoutedEventArgs e)
    {
        // Start animations
        _fadeInAnimation.Begin();
        _pulseAnimation.Begin();
        _spinAnimation.Begin();
    }

    /// <summary>
    /// Updates the status text displayed on the splash screen.
    /// </summary>
    /// <param name="status">The status message to display.</param>
    public void UpdateStatus(string status)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = status;
        });
    }

    /// <summary>
    /// Begins the fade out animation and closes the window when complete.
    /// </summary>
    /// <returns>A task that completes when the window is closed.</returns>
    public Task FadeOutAndCloseAsync()
    {
        _closeCompletionSource = new TaskCompletionSource<bool>();

        Dispatcher.Invoke(() =>
        {
            _pulseAnimation.Stop();
            _spinAnimation.Stop();
            _fadeOutAnimation.Begin();
        });

        return _closeCompletionSource.Task;
    }

    private void FadeOutAnimation_Completed(object? sender, EventArgs e)
    {
        Close();
        _closeCompletionSource?.TrySetResult(true);
    }
}
