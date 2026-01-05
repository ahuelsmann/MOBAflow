// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Microsoft.UI.Dispatching;
using SharedUI.Interface;

public class UiDispatcher : IUiDispatcher
{
    private readonly DispatcherQueue? _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public void InvokeOnUi(Action action)
    {
        if (_dispatcherQueue?.HasThreadAccess == true)
            action();
        else if (_dispatcherQueue is not null)
            _dispatcherQueue.TryEnqueue(() => action());
        else
            action();
    }

    public async Task InvokeOnUiAsync(Func<Task> asyncAction)
    {
        if (_dispatcherQueue?.HasThreadAccess == true)
        {
            await asyncAction();
        }
        else if (_dispatcherQueue is not null)
        {
            var tcs = new TaskCompletionSource();
            _dispatcherQueue.TryEnqueue(async void () =>
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
            });
            await tcs.Task;
        }
        else
        {
            await asyncAction();
        }
    }

    public async Task<T> InvokeOnUiAsync<T>(Func<Task<T>> asyncFunc)
    {
        if (_dispatcherQueue?.HasThreadAccess == true)
        {
            return await asyncFunc();
        }

        if (_dispatcherQueue is not null)
        {
            var tcs = new TaskCompletionSource<T>();
            _dispatcherQueue.TryEnqueue(async void () =>
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
            });
            return await tcs.Task;
        }

        return await asyncFunc();
    }
}