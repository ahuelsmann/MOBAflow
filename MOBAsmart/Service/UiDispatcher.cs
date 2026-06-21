// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using SharedUI.Interface;

public class UiDispatcher : IUiDispatcher
{
    public void InvokeOnUi(Action action)
    {
#if ANDROID || IOS || MACCATALYST
        if (MainThread.IsMainThread)
        {
            action();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(action);
        }
#else
        action();
#endif
    }

    public void InvokeOnUiHighPriority(Action action)
    {
        InvokeOnUi(action);
    }

    public void InvokeOnUiLowPriority(Action action)
    {
#if ANDROID || IOS || MACCATALYST
        // Always queue so connect/snapshot bursts cannot block the current UI frame (critical under debugger).
        MainThread.BeginInvokeOnMainThread(action);
#else
        action();
#endif
    }

    public async Task InvokeOnUiAsync(Func<Task> asyncAction)
    {
        await InvokeOnUiAsync(asyncAction, UiPriority.Normal);
    }

    public async Task InvokeOnUiAsync(Func<Task> asyncAction, UiPriority priority)
    {
        _ = priority;
#if ANDROID || IOS || MACCATALYST
        await MainThread.InvokeOnMainThreadAsync(asyncAction);
#else
        await asyncAction();
#endif
    }

    public async Task<T> InvokeOnUiAsync<T>(Func<Task<T>> asyncFunc)
    {
        return await InvokeOnUiAsync(asyncFunc, UiPriority.Normal);
    }

    public async Task<T> InvokeOnUiAsync<T>(Func<Task<T>> asyncFunc, UiPriority priority)
    {
        _ = priority;
#if ANDROID || IOS || MACCATALYST
        return await MainThread.InvokeOnMainThreadAsync(asyncFunc);
#else
        return await asyncFunc();
#endif
    }
}