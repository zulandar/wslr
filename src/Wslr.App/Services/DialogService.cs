using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;
using Wslr.App.Dialogs;
using Wslr.UI.Services;

namespace Wslr.App.Services;

/// <summary>
/// Implementation of <see cref="IDialogService"/> using WPF dialogs.
/// </summary>
public class DialogService : IDialogService
{
    /// <inheritdoc />
    public Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var result = MessageBox.Show(
            message,
            title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        return Task.FromResult(result == MessageBoxResult.Yes);
    }

    /// <inheritdoc />
    public Task ShowErrorAsync(string title, string message)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShowInfoAsync(string title, string message)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> ShowSaveFileDialogAsync(string title, string defaultFileName, string filter)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            FileName = defaultFileName,
            Filter = filter
        };

        var result = dialog.ShowDialog();

        return Task.FromResult(result == true ? dialog.FileName : null);
    }

    /// <inheritdoc />
    public Task<string?> ShowOpenFileDialogAsync(string title, string filter)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter
        };

        var result = dialog.ShowDialog();

        return Task.FromResult(result == true ? dialog.FileName : null);
    }

    /// <inheritdoc />
    public Task<string?> ShowFolderBrowserDialogAsync(string title)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title
        };

        var result = dialog.ShowDialog();

        return Task.FromResult(result == true ? dialog.FolderName : null);
    }

    /// <inheritdoc />
    public Task<int> ShowSelectionDialogAsync(string title, string message, IReadOnlyList<string> options)
    {
        var owner = Application.Current.MainWindow;
        var result = SelectionDialog.Show(owner, title, message, options);
        return Task.FromResult(result);
    }
}
