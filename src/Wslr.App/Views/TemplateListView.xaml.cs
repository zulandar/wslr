using System.Windows;
using System.Windows.Controls;
using Wslr.UI.ViewModels;

namespace Wslr.App.Views;

/// <summary>
/// Interaction logic for TemplateListView.xaml
/// </summary>
public partial class TemplateListView : UserControl
{
    public TemplateListView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is TemplateListViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }
}
