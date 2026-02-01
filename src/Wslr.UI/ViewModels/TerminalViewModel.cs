using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wslr.Core.Interfaces;

namespace Wslr.UI.ViewModels;

/// <summary>
/// ViewModel for the terminal page with multi-tab support.
/// </summary>
public partial class TerminalViewModel : ObservableObject, IAsyncDisposable
{
    private readonly ITerminalSessionService _sessionService;

    /// <summary>
    /// Gets the collection of terminal tabs.
    /// </summary>
    public ObservableCollection<TerminalTabViewModel> Tabs { get; } = new();

    [ObservableProperty]
    private TerminalTabViewModel? _activeTab;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Gets whether there are any tabs open.
    /// </summary>
    public bool HasTabs => Tabs.Count > 0;

    /// <summary>
    /// Raised when a tab is added.
    /// </summary>
    public event Action<TerminalTabViewModel>? TabAdded;

    /// <summary>
    /// Raised when a tab is removed.
    /// </summary>
    public event Action<TerminalTabViewModel>? TabRemoved;

    /// <summary>
    /// Raised when the active tab changes.
    /// </summary>
    public event Action<TerminalTabViewModel?>? ActiveTabChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalViewModel"/> class.
    /// </summary>
    /// <param name="sessionService">The terminal session service.</param>
    public TerminalViewModel(ITerminalSessionService sessionService)
    {
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        Tabs.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasTabs));
    }

    /// <summary>
    /// Opens a new terminal tab for the specified distribution.
    /// </summary>
    /// <param name="distributionName">The name of the distribution to connect to.</param>
    [RelayCommand]
    public async Task OpenTabAsync(string distributionName)
    {
        if (string.IsNullOrWhiteSpace(distributionName))
        {
            ErrorMessage = "Distribution name is required.";
            return;
        }

        ErrorMessage = null;

        try
        {
            var tab = new TerminalTabViewModel(distributionName);
            tab.CloseRequested += OnTabCloseRequested;
            tab.ActivateRequested += OnTabActivateRequested;

            Tabs.Add(tab);
            TabAdded?.Invoke(tab);

            // Activate the new tab
            ActivateTab(tab);

            // Connect the session
            await tab.ConnectAsync(_sessionService);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to open terminal: {ex.Message}";
            Debug.WriteLine($"Terminal open error: {ex}");
        }
    }

    /// <summary>
    /// Closes the specified tab.
    /// </summary>
    /// <param name="tab">The tab to close.</param>
    [RelayCommand]
    public async Task CloseTabAsync(TerminalTabViewModel? tab)
    {
        if (tab == null || !Tabs.Contains(tab))
        {
            return;
        }

        try
        {
            tab.CloseRequested -= OnTabCloseRequested;
            tab.ActivateRequested -= OnTabActivateRequested;

            var index = Tabs.IndexOf(tab);
            Tabs.Remove(tab);
            TabRemoved?.Invoke(tab);

            await tab.DisposeAsync();

            // If this was the active tab, activate an adjacent tab
            if (ActiveTab == tab)
            {
                if (Tabs.Count > 0)
                {
                    var newIndex = Math.Min(index, Tabs.Count - 1);
                    ActivateTab(Tabs[newIndex]);
                }
                else
                {
                    ActiveTab = null;
                    ActiveTabChanged?.Invoke(null);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error closing tab: {ex.Message}");
        }
    }

    /// <summary>
    /// Activates the specified tab.
    /// </summary>
    /// <param name="tab">The tab to activate.</param>
    public void ActivateTab(TerminalTabViewModel? tab)
    {
        if (tab == ActiveTab)
        {
            return;
        }

        // Deactivate old tab
        if (ActiveTab != null)
        {
            ActiveTab.IsActive = false;
        }

        // Activate new tab
        ActiveTab = tab;
        if (ActiveTab != null)
        {
            ActiveTab.IsActive = true;
        }

        ActiveTabChanged?.Invoke(ActiveTab);
    }

    /// <summary>
    /// Activates the next tab (wraps around).
    /// </summary>
    [RelayCommand]
    private void NextTab()
    {
        if (Tabs.Count <= 1 || ActiveTab == null)
        {
            return;
        }

        var index = Tabs.IndexOf(ActiveTab);
        var nextIndex = (index + 1) % Tabs.Count;
        ActivateTab(Tabs[nextIndex]);
    }

    /// <summary>
    /// Activates the previous tab (wraps around).
    /// </summary>
    [RelayCommand]
    private void PreviousTab()
    {
        if (Tabs.Count <= 1 || ActiveTab == null)
        {
            return;
        }

        var index = Tabs.IndexOf(ActiveTab);
        var prevIndex = (index - 1 + Tabs.Count) % Tabs.Count;
        ActivateTab(Tabs[prevIndex]);
    }

    /// <summary>
    /// Activates a tab by index (1-based for keyboard shortcuts).
    /// </summary>
    /// <param name="index">The 1-based tab index.</param>
    [RelayCommand]
    private void ActivateTabByIndex(int index)
    {
        if (index < 1 || index > Tabs.Count)
        {
            return;
        }

        ActivateTab(Tabs[index - 1]);
    }

    /// <summary>
    /// Closes all tabs.
    /// </summary>
    public async Task CloseAllTabsAsync()
    {
        var tabsToClose = Tabs.ToList();
        foreach (var tab in tabsToClose)
        {
            await CloseTabAsync(tab);
        }
    }

    private void OnTabCloseRequested(TerminalTabViewModel tab)
    {
        _ = CloseTabAsync(tab);
    }

    private void OnTabActivateRequested(TerminalTabViewModel tab)
    {
        ActivateTab(tab);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await CloseAllTabsAsync();
    }
}
