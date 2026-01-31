using Wslr.Core.Models;

namespace Wslr.UI.Services;

/// <summary>
/// Types of monitoring events that can be logged.
/// </summary>
public enum MonitoringEventType
{
    /// <summary>
    /// A distribution's state changed (e.g., Started, Stopped).
    /// </summary>
    StateChanged,

    /// <summary>
    /// A new distribution was added/installed.
    /// </summary>
    DistributionAdded,

    /// <summary>
    /// A distribution was removed/unregistered.
    /// </summary>
    DistributionRemoved,

    /// <summary>
    /// An automatic refresh occurred.
    /// </summary>
    AutoRefresh,

    /// <summary>
    /// A manual refresh was triggered.
    /// </summary>
    ManualRefresh,

    /// <summary>
    /// Monitoring was started.
    /// </summary>
    MonitoringStarted,

    /// <summary>
    /// Monitoring was stopped.
    /// </summary>
    MonitoringStopped,

    /// <summary>
    /// An error occurred during monitoring.
    /// </summary>
    Error
}

/// <summary>
/// Represents a monitoring event for audit/logging purposes.
/// </summary>
public sealed record MonitoringEvent
{
    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the type of event.
    /// </summary>
    public required MonitoringEventType EventType { get; init; }

    /// <summary>
    /// Gets the name of the distribution involved, if applicable.
    /// </summary>
    public string? DistributionName { get; init; }

    /// <summary>
    /// Gets the previous state of the distribution, if applicable.
    /// </summary>
    public DistributionState? OldState { get; init; }

    /// <summary>
    /// Gets the new state of the distribution, if applicable.
    /// </summary>
    public DistributionState? NewState { get; init; }

    /// <summary>
    /// Gets additional details or error message, if applicable.
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// Creates a state changed event.
    /// </summary>
    public static MonitoringEvent StateChanged(string distributionName, DistributionState oldState, DistributionState newState)
    {
        return new MonitoringEvent
        {
            Timestamp = DateTime.Now,
            EventType = MonitoringEventType.StateChanged,
            DistributionName = distributionName,
            OldState = oldState,
            NewState = newState
        };
    }

    /// <summary>
    /// Creates a distribution added event.
    /// </summary>
    public static MonitoringEvent DistributionAdded(string distributionName, DistributionState initialState)
    {
        return new MonitoringEvent
        {
            Timestamp = DateTime.Now,
            EventType = MonitoringEventType.DistributionAdded,
            DistributionName = distributionName,
            NewState = initialState
        };
    }

    /// <summary>
    /// Creates a distribution removed event.
    /// </summary>
    public static MonitoringEvent DistributionRemoved(string distributionName, DistributionState lastState)
    {
        return new MonitoringEvent
        {
            Timestamp = DateTime.Now,
            EventType = MonitoringEventType.DistributionRemoved,
            DistributionName = distributionName,
            OldState = lastState
        };
    }

    /// <summary>
    /// Creates an auto refresh event.
    /// </summary>
    public static MonitoringEvent AutoRefresh(int distributionCount)
    {
        return new MonitoringEvent
        {
            Timestamp = DateTime.Now,
            EventType = MonitoringEventType.AutoRefresh,
            Details = $"Refreshed {distributionCount} distribution(s)"
        };
    }

    /// <summary>
    /// Creates a manual refresh event.
    /// </summary>
    public static MonitoringEvent ManualRefresh(int distributionCount)
    {
        return new MonitoringEvent
        {
            Timestamp = DateTime.Now,
            EventType = MonitoringEventType.ManualRefresh,
            Details = $"Refreshed {distributionCount} distribution(s)"
        };
    }

    /// <summary>
    /// Creates a monitoring started event.
    /// </summary>
    public static MonitoringEvent MonitoringStarted(int intervalSeconds)
    {
        return new MonitoringEvent
        {
            Timestamp = DateTime.Now,
            EventType = MonitoringEventType.MonitoringStarted,
            Details = $"Interval: {intervalSeconds}s"
        };
    }

    /// <summary>
    /// Creates a monitoring stopped event.
    /// </summary>
    public static MonitoringEvent MonitoringStopped()
    {
        return new MonitoringEvent
        {
            Timestamp = DateTime.Now,
            EventType = MonitoringEventType.MonitoringStopped
        };
    }

    /// <summary>
    /// Creates an error event.
    /// </summary>
    public static MonitoringEvent Error(string errorMessage)
    {
        return new MonitoringEvent
        {
            Timestamp = DateTime.Now,
            EventType = MonitoringEventType.Error,
            Details = errorMessage
        };
    }
}
