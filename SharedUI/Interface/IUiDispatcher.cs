// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

/// <summary>
/// Dispatcher for executing actions on the UI thread.
/// Supports both synchronous and asynchronous operations with optional return values.
/// </summary>
public interface IUiDispatcher
{
    /// <summary>
    /// Executes a synchronous action on the UI thread.
    /// ⚠️ WARNING: Runs SYNCHRONOUSLY if already on UI thread!
    /// Use EnqueueOnUi() if you need guaranteed async execution to avoid re-entrancy issues.
    /// </summary>
    void InvokeOnUi(Action action);

    /// <summary>
    /// ALWAYS enqueues action to UI thread dispatcher queue, even if already on UI thread.
    /// Use this to break out of PropertyChanged notification chains and prevent COMException from WinUI collection views.
    /// Critical for collection modifications during property change events.
    /// </summary>
    void EnqueueOnUi(Action action);

    /// <summary>
    /// Executes an asynchronous action on the UI thread.
    /// </summary>
    Task InvokeOnUiAsync(Func<Task> asyncAction);

    /// <summary>
    /// Executes an asynchronous function on the UI thread and returns a result.
    /// </summary>
    Task<T> InvokeOnUiAsync<T>(Func<Task<T>> asyncFunc);
}