// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Moba.Domain;
using Moba.Domain.Enum;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Moba.SharedUI.Service;

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

public partial class JourneyViewModel : ObservableObject
{
    [ObservableProperty]
    private Journey model;

    private readonly IUiDispatcher? _dispatcher;

    /// <summary>
    /// Event fired when the Journey model is modified and should be saved.
    /// </summary>
    public event EventHandler? ModelChanged;

    public JourneyViewModel(Journey model, IUiDispatcher? dispatcher = null)
    {
        Model = model;
        _dispatcher = dispatcher;

        // Sort stations by Number before wrapping in ViewModels
        var sortedStations = model.Stations.OrderBy(s => s.Number).ToList();
        model.Stations.Clear();
        foreach (var station in sortedStations)
        {
            model.Stations.Add(station);
        }

        // Wrap existing stations in ViewModels
        Stations = new ObservableCollection<StationViewModel>(
            model.Stations.Select(s => new StationViewModel(s, dispatcher))
        );

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

    public string Description
    {
        get => Model.Description ?? string.Empty;
        set => SetProperty(Model.Description, value, Model, (m, v) => m.Description = v);
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

    public ObservableCollection<StationViewModel> Stations { get; }

    // Enum values for ComboBox binding
    public IEnumerable<BehaviorOnLastStop> BehaviorOnLastStopValues =>
        Enum.GetValues<BehaviorOnLastStop>();

    public BehaviorOnLastStop BehaviorOnLastStop
    {
        get => Model.OnLastStop;
        set => SetProperty(Model.OnLastStop, value, Model, (m, v) => m.OnLastStop = v);
    }

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

    [RelayCommand]
    private void AddStation()
    {
        var newStation = new Station { Name = "New Station" };
        Model.Stations.Add(newStation);

        var stationVM = new StationViewModel(newStation, _dispatcher);
        Stations.Add(stationVM);

        // Renumber all stations
        RenumberStations();

        // Notify that model changed
        ModelChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void DeleteStation(StationViewModel stationVM)
    {
        if (stationVM == null) return;

        Model.Stations.Remove(stationVM.Model);
        Stations.Remove(stationVM);

        // Renumber remaining stations
        RenumberStations();

        // Notify that model changed
        ModelChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Renumbers all stations in the journey sequentially starting from 1.
    /// Should be called after adding, deleting, or reordering stations.
    /// </summary>
    public void RenumberStations()
    {
        for (int i = 0; i < Stations.Count; i++)
        {
            Stations[i].Number = (uint)(i + 1);
        }
    }

    /// <summary>
    /// Handles station reordering after drag & drop.
    /// Call this method when stations are reordered in the UI.
    /// </summary>
    [RelayCommand]
    public void StationsReordered()
    {
        // Update Model.Stations to match ViewModel order
        Model.Stations.Clear();
        foreach (var stationVM in Stations)
        {
            Model.Stations.Add(stationVM.Model);
        }

        // Renumber based on new order
        RenumberStations();

        // Notify that model changed
        ModelChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Moves a station up in the journey (decreases its position by 1).
    /// </summary>
    /// <param name="station">The station to move up</param>
    [RelayCommand]
    private void MoveStationUp(StationViewModel station)
    {
        if (station == null) return;

        var currentIndex = Stations.IndexOf(station);
        if (currentIndex > 0)
        {
            // Move in ViewModel collection
            Stations.Move(currentIndex, currentIndex - 1);

            // Trigger reorder logic (updates Model + renumbers)
            StationsReorderedCommand.Execute(null);
        }
    }

    /// <summary>
    /// Moves a station down in the journey (increases its position by 1).
    /// </summary>
    /// <param name="station">The station to move down</param>
    [RelayCommand]
    private void MoveStationDown(StationViewModel station)
    {
        if (station == null) return;

        var currentIndex = Stations.IndexOf(station);
        if (currentIndex < Stations.Count - 1)
        {
            // Move in ViewModel collection
            Stations.Move(currentIndex, currentIndex + 1);

            // Trigger reorder logic (updates Model + renumbers)
            StationsReorderedCommand.Execute(null);
        }
    }
}
