// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Microsoft.UI.Dispatching;

using Moba.SharedUI.Interface;

/// <summary>
/// WinUI implementation of IUiDispatcher. Uses the DispatcherQueue of the thread
/// on which the instance was created (typically UI thread on first DI resolution).
/// Supports priorities for critical vs. background updates with Windows App SDK 2.0.
/// </summary>
internal class UiDispatcher : IUiDispatcher
{
    private readonly DispatcherQueue? _dispatcherQueue;

    public UiDispatcher()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    public void InvokeOnUi(Action action)
    {
        if (_dispatcherQueue?.HasThreadAccess == true)
        {
            action();
            return;
        }

        // Normal-Priorität für Standard-Updates
        if (_dispatcherQueue is not null && _dispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Normal, () => action()))
            return;

        action();
    }

    public void InvokeOnUiHighPriority(Action action)
    {
        if (_dispatcherQueue?.HasThreadAccess == true)
        {
            action();
            return;
        }

        if (_dispatcherQueue is not null && _dispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.High, () => action()))
            return;

        action();
    }

    public void InvokeOnUiLowPriority(Action action)
    {
        if (_dispatcherQueue?.HasThreadAccess == true)
        {
            action();
            return;
        }

        if (_dispatcherQueue is not null && _dispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low, () => action()))
            return;

        action();
    }

    public async Task InvokeOnUiAsync(Func<Task> asyncAction)
    {
        await InvokeOnUiAsync(asyncAction, UiPriority.Normal);
    }

    public async Task<T> InvokeOnUiAsync<T>(Func<Task<T>> asyncFunc)
    {
        return await InvokeOnUiAsync(asyncFunc, UiPriority.Normal);
    }

    public async Task<T> InvokeOnUiAsync<T>(Func<Task<T>> asyncFunc, UiPriority priority)
    {
        if (_dispatcherQueue?.HasThreadAccess == true)
        {
            return await asyncFunc();
        }

        if (_dispatcherQueue is null)
        {
            return await asyncFunc();
        }

        var tcs = new TaskCompletionSource<T>();
        var dispatcherPriority = MapToDispatcherPriority(priority);
        if (!_dispatcherQueue.TryEnqueue(dispatcherPriority, () => _ = InvokeAsyncInternal(asyncFunc, tcs)))
        {
            return await asyncFunc();
        }

        return await tcs.Task;
    }

    public async Task InvokeOnUiAsync(Func<Task> asyncAction, UiPriority priority)
    {
        if (_dispatcherQueue?.HasThreadAccess == true)
        {
            await asyncAction();
            return;
        }

        if (_dispatcherQueue is null)
        {
            await asyncAction();
            return;
        }

        var tcs = new TaskCompletionSource();
        var dispatcherPriority = MapToDispatcherPriority(priority);
        if (!_dispatcherQueue.TryEnqueue(dispatcherPriority, () => _ = InvokeAsyncInternal(asyncAction, tcs)))
        {
            await asyncAction();
            return;
        }

        await tcs.Task;
    }

    private static DispatcherQueuePriority MapToDispatcherPriority(UiPriority priority)
    {
        return priority switch
        {
            UiPriority.Low => DispatcherQueuePriority.Low,
            UiPriority.High => DispatcherQueuePriority.High,
            _ => DispatcherQueuePriority.Normal
        };
    }

    private static async Task InvokeAsyncInternal(Func<Task> asyncAction, TaskCompletionSource tcs)
    {
        try
        {
            await asyncAction();
            tcs.SetResult();
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
        }
    }

    private static async Task InvokeAsyncInternal<T>(Func<Task<T>> asyncFunc, TaskCompletionSource<T> tcs)
    {
        try
        {
            var result = await asyncFunc();
            tcs.SetResult(result);
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
        }
    }
}