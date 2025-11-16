namespace Moba.SharedUI.ViewModel.MAUI;

using System;
using Backend.Model;

/// <summary>
/// MAUI-specific JourneyViewModel adapter that dispatches property updates to the MAUI MainThread.
/// This adapter inherits from the Shared base and only deals with UI-thread dispatching.
/// </summary>
public class JourneyViewModel : Moba.SharedUI.ViewModel.JourneyViewModel
{
    public JourneyViewModel(Journey model) : base(model)
    {
        // Subscribe to model changes and dispatch property updates via MainThread
        Model.StateChanged += (_, _) =>
        {
            Dispatch(() =>
            {
                OnPropertyChanged(nameof(CurrentCounter));
                OnPropertyChanged(nameof(CurrentPos));
            });
        };
    }

    protected virtual void Dispatch(Action action)
    {
#if ANDROID || IOS || MACCATALYST
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(action);
#else
        action();
#endif
    }
}
