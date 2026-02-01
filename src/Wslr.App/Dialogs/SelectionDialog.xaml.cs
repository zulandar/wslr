using System.Collections.Generic;
using System.Windows;

namespace Wslr.App.Dialogs;

/// <summary>
/// Dialog for selecting an item from a list of options.
/// </summary>
public partial class SelectionDialog : Window
{
    /// <summary>
    /// Gets or sets the dialog title.
    /// </summary>
    public new string Title { get => base.Title; set => base.Title = value; }

    /// <summary>
    /// Gets or sets the dialog message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of options.
    /// </summary>
    public IReadOnlyList<string> Options { get; set; } = [];

    /// <summary>
    /// Gets or sets the selected index.
    /// </summary>
    public int SelectedIndex { get; set; } = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="SelectionDialog"/> class.
    /// </summary>
    public SelectionDialog()
    {
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>
    /// Shows the dialog and returns the selected index.
    /// </summary>
    /// <param name="owner">The owner window.</param>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The dialog message.</param>
    /// <param name="options">The list of options.</param>
    /// <returns>The selected index, or -1 if cancelled.</returns>
    public static int Show(Window? owner, string title, string message, IReadOnlyList<string> options)
    {
        var dialog = new SelectionDialog
        {
            Owner = owner,
            Title = title,
            Message = message,
            Options = options,
            SelectedIndex = options.Count > 0 ? 0 : -1
        };

        var result = dialog.ShowDialog();
        return result == true ? dialog.SelectedIndex : -1;
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedIndex >= 0)
        {
            DialogResult = true;
            Close();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
