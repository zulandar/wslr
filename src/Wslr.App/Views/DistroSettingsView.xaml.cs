using System.Windows;
using System.Windows.Controls;
using Wslr.UI.ViewModels;

namespace Wslr.App.Views;

/// <summary>
/// Interaction logic for DistroSettingsView.xaml
/// </summary>
public partial class DistroSettingsView : UserControl
{
    public DistroSettingsView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is DistroSettingsViewModel viewModel)
        {
            await viewModel.LoadDistributionsAsync();
        }
    }
}
