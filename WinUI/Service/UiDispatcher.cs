// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Microsoft.UI.Dispatching;
using SharedUI.Interface;

/// <summary>
/// WinUI-Implementierung von IUiDispatcher. Nutzt die DispatcherQueue des Threads,
/// auf dem die Instanz erstellt wurde (typischerweise UI-Thread bei erster DI-Auflösung).
/// </summary>
public class UiDispatcher : IUiDispatcher
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

        if (_dispatcherQueue is not null && _dispatcherQueue.TryEnqueue(() => action()))
            return;

        action();
    }

    public async Task InvokeOnUiAsync(Func<Task> asyncAction)
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
        if (!_dispatcherQueue.TryEnqueue(() => _ = InvokeAsyncInternal(asyncAction, tcs)))
        {
            await asyncAction();
            return;
        }

        await tcs.Task;
    }

    public async Task<T> InvokeOnUiAsync<T>(Func<Task<T>> asyncFunc)
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
        if (!_dispatcherQueue.TryEnqueue(() => _ = InvokeAsyncInternal(asyncFunc, tcs)))
        {
            return await asyncFunc();
        }

        return await tcs.Task;
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