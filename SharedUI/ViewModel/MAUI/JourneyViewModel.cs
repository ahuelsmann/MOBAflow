namespace Moba.SharedUI.ViewModel.MAUI;

using System;
using Backend.Model;
using Moba.SharedUI.Service;

/// <summary>
/// MAUI-specific JourneyViewModel adapter that dispatches property updates via IUiDispatcher.
/// </summary>
public class JourneyViewModel : ViewModel.JourneyViewModel
{
    private readonly IUiDispatcher _dispatcher;

    public JourneyViewModel(Journey model, IUiDispatcher dispatcher) : base(model)
    {
        _dispatcher = dispatcher;
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
        _dispatcher.InvokeOnUi(action);
    }
}
