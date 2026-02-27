// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

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
    /// Executes the action on the UI thread. If the call is already on the UI thread,
    /// runs synchronously; otherwise switches to the UI thread.
    /// </summary>
    void InvokeOnUi(Action action);

    /// <summary>
    /// Executes an async action on the UI thread and waits for completion.
    /// </summary>
    Task InvokeOnUiAsync(Func<Task> asyncAction);

    /// <summary>
    /// Executes an async function on the UI thread and returns the result.
    /// </summary>
    Task<T> InvokeOnUiAsync<T>(Func<Task<T>> asyncFunc);
}