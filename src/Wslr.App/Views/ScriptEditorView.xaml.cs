using System.Windows;
using System.Windows.Controls;
using Wslr.UI.ViewModels;

namespace Wslr.App.Views;

/// <summary>
/// Interaction logic for ScriptEditorView.xaml
/// </summary>
public partial class ScriptEditorView : UserControl
{
    public ScriptEditorView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ScriptEditorViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }
}
