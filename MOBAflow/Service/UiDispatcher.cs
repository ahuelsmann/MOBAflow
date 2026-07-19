// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Microsoft.UI.Dispatching;

using Moba.SharedUI.Interface;

/// <summary>
/// WinUI implementation of IUiDispatcher. Uses the DispatcherQueue of the thread
/// on which the instance is created (the UI thread during application startup).
/// Supports priorities for critical vs. background updates with Windows App SDK 2.0.
/// </summary>
internal sealed class UiDispatcher : IUiDispatcher
{
    private readonly IWinUiDispatcherQueue _dispatcherQueue;

    public UiDispatcher()
        : this(WinUiDispatcherQueue.CreateForCurrentThread())
    {
    }

    internal UiDispatcher(IWinUiDispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
    }

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

        if (_dispatcherQueue.HasThreadAccess)
        {
            action();
            return;
        }

        if (!_dispatcherQueue.TryEnqueue(priority, action))
        {
            throw new InvalidOperationException("The UI dispatcher queue is shutting down.");
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

        if (_dispatcherQueue.HasThreadAccess)
        {
            return await asyncFunc();
        }

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatcherPriority = MapToDispatcherPriority(priority);
        if (!_dispatcherQueue.TryEnqueue(dispatcherPriority, () => _ = InvokeAsyncInternal(asyncFunc, tcs)))
        {
            throw new InvalidOperationException("The UI dispatcher queue is shutting down.");
        }

        return await tcs.Task;
    }

    public async Task InvokeOnUiAsync(Func<Task> asyncAction, UiPriority priority)
    {
        ArgumentNullException.ThrowIfNull(asyncAction);

        if (_dispatcherQueue.HasThreadAccess)
        {
            await asyncAction();
            return;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatcherPriority = MapToDispatcherPriority(priority);
        if (!_dispatcherQueue.TryEnqueue(dispatcherPriority, () => _ = InvokeAsyncInternal(asyncAction, tcs)))
        {
            throw new InvalidOperationException("The UI dispatcher queue is shutting down.");
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

internal interface IWinUiDispatcherQueue
{
    bool HasThreadAccess { get; }

    bool TryEnqueue(DispatcherQueuePriority priority, Action action);
}

internal sealed class WinUiDispatcherQueue : IWinUiDispatcherQueue
{
    private readonly DispatcherQueue? _dispatcherQueue;

    public WinUiDispatcherQueue(DispatcherQueue? dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
    }

    public static WinUiDispatcherQueue CreateForCurrentThread()
    {
        try
        {
            return new WinUiDispatcherQueue(DispatcherQueue.GetForCurrentThread());
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // Headless test hosts do not activate the Windows App SDK runtime.
            return new WinUiDispatcherQueue(dispatcherQueue: null);
        }
    }

    public bool HasThreadAccess => _dispatcherQueue?.HasThreadAccess ?? true;

    public bool TryEnqueue(DispatcherQueuePriority priority, Action action)
    {
        return _dispatcherQueue?.TryEnqueue(priority, () => action()) ?? false;
    }
}
