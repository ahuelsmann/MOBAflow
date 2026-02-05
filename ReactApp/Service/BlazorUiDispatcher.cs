// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.ReactApp.Service;

using SharedUI.Interface;

/// <summary>
/// Blazor Server implementation of IUiDispatcher.
/// In Blazor Server, we execute actions directly since:
/// - The ViewModel raises PropertyChanged events
/// - Components subscribe to PropertyChanged and call InvokeAsync(StateHasChanged)
/// - SignalR pushes updates to the client
/// </summary>
public class BlazorUiDispatcher : IUiDispatcher
{
    public void InvokeOnUi(Action action)
    {
        action();
    }

    public void EnqueueOnUi(Action action)
    {
        // In Blazor, synchronous execution is acceptable since we're already on the correct thread
        action();
    }

    public async Task InvokeOnUiAsync(Func<Task> asyncAction)
    {
        await asyncAction().ConfigureAwait(false);
    }

    public async Task<T> InvokeOnUiAsync<T>(Func<Task<T>> asyncFunc)
    {
        return await asyncFunc().ConfigureAwait(false);
    }
}
