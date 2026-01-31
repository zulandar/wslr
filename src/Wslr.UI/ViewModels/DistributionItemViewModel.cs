using CommunityToolkit.Mvvm.ComponentModel;
using Wslr.Core.Models;

namespace Wslr.UI.ViewModels;

/// <summary>
/// ViewModel for a single WSL distribution item.
/// </summary>
public partial class DistributionItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private DistributionState _state;

    [ObservableProperty]
    private int _version;

    [ObservableProperty]
    private bool _isDefault;

    /// <summary>
    /// Gets a value indicating whether the distribution is currently running.
    /// </summary>
    public bool IsRunning => State == DistributionState.Running;

    /// <summary>
    /// Gets the display text for the state.
    /// </summary>
    public string StateText => State switch
    {
        DistributionState.Running => "Running",
        DistributionState.Stopped => "Stopped",
        DistributionState.Installing => "Installing",
        _ => "Unknown"
    };

    /// <summary>
    /// Creates a new instance from a domain model.
    /// </summary>
    /// <param name="distribution">The domain model.</param>
    /// <returns>A new ViewModel instance.</returns>
    public static DistributionItemViewModel FromModel(WslDistribution distribution)
    {
        return new DistributionItemViewModel
        {
            Name = distribution.Name,
            State = distribution.State,
            Version = distribution.Version,
            IsDefault = distribution.IsDefault
        };
    }

    /// <summary>
    /// Updates this ViewModel from a domain model.
    /// </summary>
    /// <param name="distribution">The domain model.</param>
    public void UpdateFromModel(WslDistribution distribution)
    {
        Name = distribution.Name;
        State = distribution.State;
        Version = distribution.Version;
        IsDefault = distribution.IsDefault;

        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(StateText));
    }
}
