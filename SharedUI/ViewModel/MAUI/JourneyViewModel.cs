// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel.MAUI;

using System;
using Moba.Domain;
using Moba.SharedUI.Service;

/// <summary>
/// MAUI-specific JourneyViewModel adapter that dispatches property updates via IUiDispatcher.
/// No additional event subscription needed - base class handles it.
/// </summary>
public class JourneyViewModel : ViewModel.JourneyViewModel
{
    public JourneyViewModel(Journey model, IUiDispatcher dispatcher) : base(model, dispatcher)
    {
        // Base class handles StateChanged event subscription with dispatcher
    }

    // Convenience ctor for tests and non-UI execution contexts
    public JourneyViewModel(Journey model)
        : this(model, new ImmediateDispatcher())
    {
    }

    private sealed class ImmediateDispatcher : IUiDispatcher
    {
        public void InvokeOnUi(Action action) => action();
    }
}
