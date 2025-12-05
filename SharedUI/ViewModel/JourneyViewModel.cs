// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Moba.Backend.Manager;
using Moba.Backend.Services;
using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Enum;
using Moba.SharedUI.Interface;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

public partial class JourneyViewModel : ObservableObject, IViewModelWrapper<Journey>
{
    private readonly Journey _journey;
    private readonly JourneySessionState _state;
    private readonly JourneyManager? _journeyManager;
    private readonly IUiDispatcher? _dispatcher;

    /// <summary>
    /// Event fired when the Journey model is modified and should be saved.
    /// </summary>
    public event EventHandler? ModelChanged;

    /// <summary>
    /// Full constructor with SessionState support (for runtime journey execution).
    /// </summary>
    public JourneyViewModel(
        Journey journey, 
        JourneySessionState state,
        JourneyManager? journeyManager = null,
        IUiDispatcher? dispatcher = null)
    {
        _journey = journey;
        _state = state;
        _journeyManager = journeyManager;
        _dispatcher = dispatcher;

        // Note: Station.Number removed in Clean Architecture - stations are ordered by list position
        // No sorting needed as list order is the source of truth

        // Wrap existing stations in ViewModels
        Stations = new ObservableCollection<StationViewModel>(
            journey.Stations.Select(s => new StationViewModel(s, dispatcher))
        );

        // Subscribe to JourneyManager.StationChanged event
        if (_journeyManager != null && _dispatcher != null)
        {
            _journeyManager.StationChanged += OnStationChanged;
        }
    }

    /// <summary>
    /// Simplified constructor (for UI-only scenarios like TreeView).
    /// Creates a dummy SessionState that won't receive updates.
    /// </summary>
    public JourneyViewModel(Journey journey, IUiDispatcher? dispatcher = null)
        : this(journey, new JourneySessionState { JourneyId = journey.Id }, null, dispatcher)
    {
    }

    private void OnStationChanged(object? sender, Backend.Manager.StationChangedEventArgs e)
    {
        if (e.JourneyId != _journey.Id) return; // Only react to THIS journey
        
        _dispatcher?.InvokeOnUi(() =>
        {
            OnPropertyChanged(nameof(CurrentStation));
            OnPropertyChanged(nameof(CurrentCounter));
            OnPropertyChanged(nameof(CurrentPos));
        });
    }

    public uint InPort
    {
        get => _journey.InPort;
        set => SetProperty(_journey.InPort, value, _journey, (m, v) => m.InPort = v);
    }

    public bool IsUsingTimerToIgnoreFeedbacks
    {
        get => _journey.IsUsingTimerToIgnoreFeedbacks;
        set => SetProperty(_journey.IsUsingTimerToIgnoreFeedbacks, value, _journey, (m, v) => m.IsUsingTimerToIgnoreFeedbacks = v);
    }

    public double IntervalForTimerToIgnoreFeedbacks
    {
        get => _journey.IntervalForTimerToIgnoreFeedbacks;
        set => SetProperty(_journey.IntervalForTimerToIgnoreFeedbacks, value, _journey, (m, v) => m.IntervalForTimerToIgnoreFeedbacks = v);
    }

    public string Name
    {
        get => _journey.Name;
        set => SetProperty(_journey.Name, value, _journey, (m, v) => m.Name = v);
    }

    public string Description
    {
        get => _journey.Description ?? string.Empty;
        set => SetProperty(_journey.Description, value, _journey, (m, v) => m.Description = v);
    }

    [Display(Name = "Text-to-speech template")]
    public string? Text
    {
        get => _journey.Text;
        set => SetProperty(_journey.Text, value, _journey, (m, v) => m.Text = v);
    }

    // Note: Train property removed - Journey in Domain model no longer has direct Train reference
    // Train-Journey relationship is now managed at Solution/Project level

    public ObservableCollection<StationViewModel> Stations { get; }

    // Enum values for ComboBox binding
    public IEnumerable<BehaviorOnLastStop> BehaviorOnLastStopValues =>
        Enum.GetValues<BehaviorOnLastStop>();

    /// <summary>
    /// Current station name (runtime state).
    /// </summary>
    public string CurrentStation => _state.CurrentStationName ?? string.Empty;

    /// <summary>
    /// Current counter value (runtime state).
    /// Read-only from ViewModel perspective - managed by JourneyManager.
    /// </summary>
    public int CurrentCounter => _state.Counter;

    /// <summary>
    /// Current position in journey (runtime state).
    /// Read-only from ViewModel perspective - managed by JourneyManager.
    /// </summary>
    public int CurrentPos => _state.CurrentPos;

    public BehaviorOnLastStop BehaviorOnLastStop
    {
        get => _journey.BehaviorOnLastStop;
        set => SetProperty(_journey.BehaviorOnLastStop, value, _journey, (m, v) => m.BehaviorOnLastStop = v);
    }

    public Journey? NextJourney
    {
        get => _journey.NextJourney;
        set => SetProperty(_journey.NextJourney, value, _journey, (m, v) => m.NextJourney = v);
    }

    public uint FirstPos
    {
        get => _journey.FirstPos;
        set => SetProperty(_journey.FirstPos, value, _journey, (m, v) => m.FirstPos = v);
    }

    /// <summary>
    /// Expose domain object for serialization and other operations.
    /// </summary>
    public Journey Model => _journey;

    public MobaType EntityType => MobaType.Journey;

    [RelayCommand]
    private void AddStation()
    {
        var newStation = new Station { Name = "New Station" };
        _journey.Stations.Add(newStation);

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

        _journey.Stations.Remove(stationVM.Model);
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
        // Note: Station.Number property removed in Clean Architecture refactoring
        // Stations are now identified by their position in the Stations list
        // This method is kept for API compatibility but does nothing
    }

    /// <summary>
    /// Handles station reordering after drag & drop.
    /// Call this method when stations are reordered in the UI.
    /// </summary>
    [RelayCommand]
    public void StationsReordered()
    {
        // Update _journey.Stations to match ViewModel order
        _journey.Stations.Clear();
        foreach (var stationVM in Stations)
        {
            _journey.Stations.Add(stationVM.Model);
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
    
    /// <summary>
    /// Simple immediate dispatcher for tests and non-UI contexts.
    /// Executes actions synchronously on the current thread without platform-specific dispatching.
    /// Used as fallback when no IUiDispatcher is provided.
    /// </summary>
    private sealed class ImmediateDispatcher : IUiDispatcher
    {
        public void InvokeOnUi(System.Action action) => action();
    }
}

