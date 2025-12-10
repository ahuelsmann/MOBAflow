// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WebApp.Service;

using SharedUI.Interface;

/// <summary>
/// Blazor Server implementation of IUiDispatcher.
/// Uses the current SynchronizationContext to marshal calls to the Blazor dispatcher thread.
/// Unlike WinUI (DispatcherQueue) or MAUI (MainThread), Blazor relies on SynchronizationContext.
/// </summary>
public class BlazorUiDispatcher : IUiDispatcher
{
    public void InvokeOnUi(Action action)
    {
        var syncContext = SynchronizationContext.Current;
        
        if (syncContext != null)
        {
            // Post to Blazor's synchronization context
            syncContext.Post(_ => action(), null);
        }
        else
        {
            // Fallback: execute directly (e.g., during initialization or testing)
            action();
        }
    }
}
