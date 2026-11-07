// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Helpers;

using Moba.SharedUI.Interface;

/// <summary>A UI dispatcher for tests that runs every action on the calling thread.</summary>
internal sealed class ImmediateUiDispatcher : IUiDispatcher
{
    public static ImmediateUiDispatcher Instance { get; } = new();

    public void InvokeOnUi(Action action) => action();

    public Task InvokeOnUiAsync(Func<Task> asyncAction) => asyncAction();

    public Task<T> InvokeOnUiAsync<T>(Func<Task<T>> asyncFunc) => asyncFunc();

    public void InvokeOnUiHighPriority(Action action) => action();

    public void InvokeOnUiLowPriority(Action action) => action();

    public Task InvokeOnUiAsync(Func<Task> asyncAction, UiPriority priority)
    {
        _ = priority;
        return asyncAction();
    }
}
