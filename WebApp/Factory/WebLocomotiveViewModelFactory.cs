// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WebApp.Factory;

using Moba.Backend.Model;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Blazor-specific factory for creating LocomotiveViewModel instances
/// </summary>
public class WebLocomotiveViewModelFactory : ILocomotiveViewModelFactory
{
    public LocomotiveViewModel Create(Locomotive model)
        => new LocomotiveViewModel(model, dispatcher: null);
}
