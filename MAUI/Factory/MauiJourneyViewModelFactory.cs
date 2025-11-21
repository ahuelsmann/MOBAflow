// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Factory;

using Moba.Backend.Model;
using Moba.SharedUI.Service;
using Moba.SharedUI.Interface;

/// <summary>
/// MAUI-specific factory for creating JourneyViewModel instances with UI dispatcher
/// </summary>
public class MauiJourneyViewModelFactory : IJourneyViewModelFactory
{
    private readonly IUiDispatcher _dispatcher;

    public MauiJourneyViewModelFactory(IUiDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public SharedUI.ViewModel.JourneyViewModel Create(Journey model)
        => new SharedUI.ViewModel.MAUI.JourneyViewModel(model, _dispatcher);
}
