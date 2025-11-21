// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WebApp.Factory;

using Moba.Backend.Model;
using Moba.SharedUI.Service;
using Moba.SharedUI.Interface;

/// <summary>
/// Blazor-specific factory for creating JourneyViewModel instances
/// Note: Blazor uses synchronization context for UI updates, no explicit dispatcher needed in most cases
/// </summary>
public class WebJourneyViewModelFactory : IJourneyViewModelFactory
{
    private readonly IUiDispatcher? _dispatcher;

    public WebJourneyViewModelFactory(IUiDispatcher? dispatcher = null)
    {
        _dispatcher = dispatcher;
    }

    public SharedUI.ViewModel.JourneyViewModel Create(Journey model)
        => new SharedUI.ViewModel.JourneyViewModel(model, _dispatcher);
}
