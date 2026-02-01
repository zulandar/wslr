using System.Windows;
using System.Windows.Controls;
using Wslr.UI.ViewModels;

namespace Wslr.App.Views;

/// <summary>
/// Interaction logic for GlobalWslSettingsView.xaml
/// </summary>
public partial class GlobalWslSettingsView : UserControl
{
    public GlobalWslSettingsView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is GlobalWslSettingsViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }
}
