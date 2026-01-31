using CommunityToolkit.Mvvm.ComponentModel;

namespace Wslr.UI.ViewModels;

/// <summary>
/// A placeholder ViewModel for views that are not yet implemented.
/// </summary>
public partial class PlaceholderViewModel : ObservableObject
{
    /// <summary>
    /// Gets the title of the placeholder page.
    /// </summary>
    [ObservableProperty]
    private string _title;

    /// <summary>
    /// Gets the description message for the placeholder page.
    /// </summary>
    [ObservableProperty]
    private string _message;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaceholderViewModel"/> class.
    /// </summary>
    /// <param name="title">The title of the placeholder page.</param>
    /// <param name="message">The description message.</param>
    public PlaceholderViewModel(string title, string message)
    {
        _title = title;
        _message = message;
    }
}
