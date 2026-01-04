// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

/// <summary>
/// Dispatcher for executing actions on the UI thread.
/// Supports both synchronous and asynchronous operations with optional return values.
/// </summary>
public interface IUiDispatcher
{
    /// <summary>
    /// Executes a synchronous action on the UI thread.
    /// </summary>
    void InvokeOnUi(Action action);

    /// <summary>
    /// Executes an asynchronous action on the UI thread.
    /// </summary>
    Task InvokeOnUiAsync(Func<Task> asyncAction);

    /// <summary>
    /// Executes an asynchronous function on the UI thread and returns a result.
    /// </summary>
    Task<T> InvokeOnUiAsync<T>(Func<Task<T>> asyncFunc);
}