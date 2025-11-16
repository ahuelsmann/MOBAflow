namespace Moba.SharedUI.ViewModel.WinUI;

using Backend.Model;
using System;

/// <summary>
/// Platform-agnostic adapter for WinUI-specific behavior. This lives in SharedUI but does not reference
/// WinUI assemblies. Override `Dispatch` in platform project to dispatch to UI thread.
/// </summary>
public class JourneyViewModel : Moba.SharedUI.ViewModel.JourneyViewModel
{
    public JourneyViewModel(Journey model) : base(model)
    {
        // Subscribe to model changes and dispatch property updates via Dispatch.
        Model.StateChanged += (_, _) =>
        {
            Dispatch(() =>
            {
                OnPropertyChanged(nameof(CurrentCounter));
                OnPropertyChanged(nameof(CurrentPos));
            });
        };
    }

    /// <summary>
    /// Dispatches an action to the UI thread. Default implementation invokes directly.
    /// Platform-specific subclass (e.g., in WinUI project) should override to use DispatcherQueue.
    /// </summary>
    /// <param name="action">Action to execute</param>
    protected virtual void Dispatch(Action action)
    {
        action();
    }
}
