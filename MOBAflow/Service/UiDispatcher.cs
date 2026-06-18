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
    private DispatcherQueue? _dispatcherQueue;

    /// <summary>
    /// Resolves the UI-thread dispatcher lazily. The singleton may be created before
    /// <see cref="DispatcherQueue"/> is available (e.g. during early DI in App constructor).
    /// </summary>
    private DispatcherQueue? DispatcherQueue => _dispatcherQueue ??= Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

    public void InvokeOnUi(Action action)
    {
        InvokeOnUi(action, DispatcherQueuePriority.Normal);
    }

    public void InvokeOnUiHighPriority(Action action)
    {
        InvokeOnUi(action, DispatcherQueuePriority.High);
    }

    public void InvokeOnUiLowPriority(Action action)
    {
        InvokeOnUi(action, DispatcherQueuePriority.Low);
    }

    private void InvokeOnUi(Action action, DispatcherQueuePriority priority)
    {
        ArgumentNullException.ThrowIfNull(action);

        var queue = DispatcherQueue;
        if (queue?.HasThreadAccess == true)
        {
            action();
            return;
        }

        if (queue is null)
        {
            action();
            return;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!queue.TryEnqueue(priority, () => InvokeActionOnUiThread(action, tcs)))
        {
            action();
            return;
        }

        tcs.Task.GetAwaiter().GetResult();
    }

    private static void InvokeActionOnUiThread(Action action, TaskCompletionSource tcs)
    {
        try
        {
            action();
            tcs.TrySetResult();
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }
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
        ArgumentNullException.ThrowIfNull(asyncFunc);

        var queue = DispatcherQueue;
        if (queue?.HasThreadAccess == true)
        {
            return await asyncFunc();
        }

        if (queue is null)
        {
            return await asyncFunc();
        }

        var tcs = new TaskCompletionSource<T>();
        var dispatcherPriority = MapToDispatcherPriority(priority);
        if (!queue.TryEnqueue(dispatcherPriority, () => _ = InvokeAsyncInternal(asyncFunc, tcs)))
        {
            return await asyncFunc();
        }

        return await tcs.Task;
    }

    public async Task InvokeOnUiAsync(Func<Task> asyncAction, UiPriority priority)
    {
        ArgumentNullException.ThrowIfNull(asyncAction);

        var queue = DispatcherQueue;
        if (queue?.HasThreadAccess == true)
        {
            await asyncAction();
            return;
        }

        if (queue is null)
        {
            await asyncAction();
            return;
        }

        var tcs = new TaskCompletionSource();
        var dispatcherPriority = MapToDispatcherPriority(priority);
        if (!queue.TryEnqueue(dispatcherPriority, () => _ = InvokeAsyncInternal(asyncAction, tcs)))
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