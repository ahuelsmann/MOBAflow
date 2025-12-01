// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel.WinUI;

using System;
using Moba.Domain;
using Moba.SharedUI.Service;

/// <summary>
/// WinUI-specific adapter hosted in SharedUI; dispatching provided via IUiDispatcher.
/// No additional event subscription needed - base class handles it.
/// </summary>
public class JourneyViewModel : ViewModel.JourneyViewModel
{
    public JourneyViewModel(Journey model, IUiDispatcher dispatcher) : base(model, dispatcher)
    {
        // Base class handles StateChanged event subscription with dispatcher
    }

    // Convenience ctor for tests and fallback scenarios where no platform dispatcher is available
    public JourneyViewModel(Journey model) : this(model, new ImmediateDispatcher()) { }

    // Internal simple dispatcher that executes immediately on the current thread.
    private sealed class ImmediateDispatcher : IUiDispatcher
    {
        public void InvokeOnUi(Action action) => action();
    }
}