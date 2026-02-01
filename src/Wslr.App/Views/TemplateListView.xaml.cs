using System.Windows;
using System.Windows.Controls;
using Wslr.UI.ViewModels;

namespace Wslr.App.Views;

/// <summary>
/// Interaction logic for TemplateListView.xaml
/// </summary>
public partial class TemplateListView : UserControl
{
    private bool _hasLoaded;

    public TemplateListView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await TryLoadAsync();
    }

    private async void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        await TryLoadAsync();
    }

    private async Task TryLoadAsync()
    {
        if (_hasLoaded)
        {
            return;
        }

        if (DataContext is TemplateListViewModel viewModel)
        {
            _hasLoaded = true;
            await viewModel.LoadAsync();
        }
    }
}
