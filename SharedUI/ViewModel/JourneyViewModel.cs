namespace Moba.SharedUI.ViewModel;

using Backend.Model;
using Backend.Model.Enum;

using CommunityToolkit.Mvvm.ComponentModel;

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Moba.SharedUI.Service;

public partial class JourneyViewModel : ObservableObject
{
    [ObservableProperty]
    private Journey model;

    private readonly IUiDispatcher? _dispatcher;

    public JourneyViewModel(Journey model, IUiDispatcher? dispatcher = null)
    {
        Model = model;
        Stations = new ObservableCollection<Station>(model.Stations);
        _dispatcher = dispatcher;

        // ✅ Subscribe to StateChanged event ONLY if dispatcher is available
        // Platform-specific derived classes (WinUI, MAUI) will provide dispatcher
        // Base class fallback: Direct property changes (for WebApp/Tests without dispatcher)
        if (_dispatcher != null)
        {
            Model.StateChanged += (_, _) =>
            {
                _dispatcher.InvokeOnUi(() => OnModelStateChanged());
            };
        }
        else
        {
            // Fallback for tests or WebApp (no dispatcher)
            Model.StateChanged += (_, _) => OnModelStateChanged();
        }
    }

    /// <summary>
    /// Called when the underlying Journey model's state changes.
    /// Platform-specific derived classes can override this for additional logic.
    /// </summary>
    protected virtual void OnModelStateChanged()
    {
        OnPropertyChanged(nameof(CurrentCounter));
        OnPropertyChanged(nameof(CurrentPos));
    }

    public uint InPort
    {
        get => Model.InPort;
        set => SetProperty(Model.InPort, value, Model, (m, v) => m.InPort = v);
    }

    public bool IsUsingTimerToIgnoreFeedbacks
    {
        get => Model.IsUsingTimerToIgnoreFeedbacks;
        set => SetProperty(Model.IsUsingTimerToIgnoreFeedbacks, value, Model, (m, v) => m.IsUsingTimerToIgnoreFeedbacks = v);
    }

    public double IntervalForTimerToIgnoreFeedbacks
    {
        get => Model.IntervalForTimerToIgnoreFeedbacks;
        set => SetProperty(Model.IntervalForTimerToIgnoreFeedbacks, value, Model, (m, v) => m.IntervalForTimerToIgnoreFeedbacks = v);
    }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    [Display(Name = "Text-to-speech template")]
    public string? Text
    {
        get => Model.Text;
        set => SetProperty(Model.Text, value, Model, (m, v) => m.Text = v);
    }

    public Train? Train
    {
        get => Model.Train;
        set => SetProperty(Model.Train, value, Model, (m, v) => m.Train = v);
    }

    public ObservableCollection<Station> Stations { get; }

    public uint CurrentCounter
    {
        get => Model.CurrentCounter;
        set => SetProperty(Model.CurrentCounter, value, Model, (m, v) => m.CurrentCounter = v);
    }

    public uint CurrentPos
    {
        get => Model.CurrentPos;
        set => SetProperty(Model.CurrentPos, value, Model, (m, v) => m.CurrentPos = v);
    }

    public BehaviorOnLastStop OnLastStop
    {
        get => Model.OnLastStop;
        set => SetProperty(Model.OnLastStop, value, Model, (m, v) => m.OnLastStop = v);
    }

    public string? NextJourney
    {
        get => Model.NextJourney;
        set => SetProperty(Model.NextJourney, value, Model, (m, v) => m.NextJourney = v);
    }

    public uint FirstPos
    {
        get => Model.FirstPos;
        set => SetProperty(Model.FirstPos, value, Model, (m, v) => m.FirstPos = v);
    }
}
