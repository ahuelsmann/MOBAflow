// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using SharedUI.Interface;

public class UiDispatcher : IUiDispatcher
{
    public void InvokeOnUi(Action action)
    {
#if ANDROID || IOS || MACCATALYST
        MainThread.BeginInvokeOnMainThread(action);
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
        InvokeOnUi(action);
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