// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WebApp.Service;

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
        // Execute directly - Blazor components handle their own UI updates
        // via PropertyChanged subscription and InvokeAsync(StateHasChanged)
        action();
    }
}