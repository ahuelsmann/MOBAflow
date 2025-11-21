// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Factory;

using Moba.Backend.Model;
using Moba.SharedUI.Service;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;

/// <summary>
/// WinUI-specific factory for creating LocomotiveViewModel instances with UI dispatcher
/// </summary>
public class WinUILocomotiveViewModelFactory : ILocomotiveViewModelFactory
{
    private readonly IUiDispatcher _dispatcher;

    public WinUILocomotiveViewModelFactory(IUiDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public LocomotiveViewModel Create(Locomotive model)
        => new LocomotiveViewModel(model, _dispatcher);
}
