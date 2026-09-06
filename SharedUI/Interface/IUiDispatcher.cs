// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

/// <summary>
/// Priority levels for UI thread dispatching (cross-platform abstraction).
/// Maps to platform-specific priority systems (e.g., DispatcherQueuePriority on WinUI).
/// </summary>
public enum UiPriority
{
    /// <summary>Low priority - for background updates that can be deferred.</summary>
    Low,
    /// <summary>Normal priority - standard UI updates.</summary>
    Normal,
    /// <summary>High priority - for critical UI updates that should be processed immediately.</summary>
    High
}

/// <summary>
/// Ensures execution of actions on the UI thread (thread marshalling).
/// Used by ViewModels when e.g. events or background services run on a non-UI thread
/// and properties/collections need to be updated.
/// </summary>
/// <remarks>
/// Best practice: Resolve collection updates during PropertyChanged chains (e.g. on project change)
/// by replacing the collection (assign new ObservableCollection), not by Clear/Add in place –
/// avoids reentrancy and COMException in WinUI bindings.
/// </remarks>
public interface IUiDispatcher
{
    /// <summary>
    /// Executes the action immediately when called on the UI thread. Otherwise, queues the
    /// action on the UI thread and returns without waiting for completion.
    /// </summary>
    /// <remarks>Use <see cref="InvokeOnUiAsync(Func{Task})"/> when completion or exception propagation is required.</remarks>
    void InvokeOnUi(Action action);

    /// <summary>
    /// Executes an async action on the UI thread and waits for completion.
    /// </summary>
    Task InvokeOnUiAsync(Func<Task> asyncAction);

    /// <summary>
    /// Executes an async function on the UI thread and returns the result.
    /// </summary>
    Task<T> InvokeOnUiAsync<T>(Func<Task<T>> asyncFunc);

    /// <summary>
    /// Executes or queues the action on the UI thread with high priority.
    /// Use for critical UI updates that should be processed immediately.
    /// </summary>
    void InvokeOnUiHighPriority(Action action);

    /// <summary>
    /// Executes or queues the action on the UI thread with low priority.
    /// Use for background UI updates that can be deferred.
    /// </summary>
    void InvokeOnUiLowPriority(Action action);

    /// <summary>
    /// Executes an async action on the UI thread with specified priority.
    /// </summary>
    Task InvokeOnUiAsync(Func<Task> asyncAction, UiPriority priority);
}
