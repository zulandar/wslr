namespace Wslr.UI.Services;

/// <summary>
/// Service for navigating between views.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigates to the specified view.
    /// </summary>
    /// <typeparam name="TViewModel">The type of the ViewModel to navigate to.</typeparam>
    void NavigateTo<TViewModel>() where TViewModel : class;

    /// <summary>
    /// Navigates to the specified view with a parameter.
    /// </summary>
    /// <typeparam name="TViewModel">The type of the ViewModel to navigate to.</typeparam>
    /// <param name="parameter">The navigation parameter.</param>
    void NavigateTo<TViewModel>(object parameter) where TViewModel : class;

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    void GoBack();

    /// <summary>
    /// Gets a value indicating whether navigation back is possible.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Shows the main window.
    /// </summary>
    void ShowMainWindow();

    /// <summary>
    /// Hides the main window.
    /// </summary>
    void HideMainWindow();

    /// <summary>
    /// Exits the application.
    /// </summary>
    void ExitApplication();
}
