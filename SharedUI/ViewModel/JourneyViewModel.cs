// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Manager;
using Backend.Service;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;

using Interface;

using Moba.Domain.Enum;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

public partial class JourneyViewModel : ObservableObject, IViewModelWrapper<Journey>
{
    #region Fields
    // Model
    private readonly Journey _journey;
    private readonly Project _project;

    // Services
    private readonly IUiDispatcher? _dispatcher;
    private readonly JourneyManager? _journeyManager;

    // Runtime State
    private readonly JourneySessionState _state;
    private ObservableCollection<StationViewModel>? _stations;
    #endregion

    /// <summary>
    /// Event fired when the Journey model is modified and should be saved.
    /// </summary>
    public event EventHandler? ModelChanged;

    /// <summary>
    /// Full constructor with SessionState support (for runtime journey execution).
    /// </summary>
    public JourneyViewModel(
        Journey journey,
        Project project,
        JourneySessionState state,
        JourneyManager? journeyManager = null,
        IUiDispatcher? dispatcher = null)
    {
        _journey = journey;
        _project = project;
        _state = state;
        _journeyManager = journeyManager;
        _dispatcher = dispatcher;

        // Initialize Stations collection
        RefreshStations();

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
    public JourneyViewModel(Journey journey, Project project, IUiDispatcher? dispatcher = null)
        : this(journey, project, new JourneySessionState { JourneyId = journey.Id }, null, dispatcher)
    {
    }

    private void OnStationChanged(object? sender, StationChangedEventArgs e)
    {
        if (e.JourneyId != _journey.Id) return; // Only react to THIS journey
        
        _dispatcher?.InvokeOnUi(() =>
        {
            OnPropertyChanged(nameof(CurrentStation));
            OnPropertyChanged(nameof(CurrentCounter));
            OnPropertyChanged(nameof(CurrentPos));
        });
    }

    /// <summary>
    /// Gets the unique identifier of the journey.
    /// </summary>
    public Guid Id => _journey.Id;

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

    /// <summary>
    /// Stations collection resolved from Project.Stations using StationIds.
    /// Cached for UI binding performance.
    /// </summary>
    public ObservableCollection<StationViewModel> Stations
    {
        get
        {
            if (_stations == null)
            {
                RefreshStations();
            }
            return _stations!;
        }
    }

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

    /// <summary>
    /// Updates the local SessionState from the JourneyManager's state and notifies UI.
    /// Called by MainWindowViewModel when JourneyManager.StationChanged fires.
    /// </summary>
    /// <param name="state">The updated SessionState from JourneyManager</param>
    public void UpdateFromSessionState(JourneySessionState state)
    {
        _state.Counter = state.Counter;
        _state.CurrentPos = state.CurrentPos;
        _state.CurrentStationName = state.CurrentStationName;
        _state.LastFeedbackTime = state.LastFeedbackTime;
        _state.IsActive = state.IsActive;

        // Notify UI about property changes
        OnPropertyChanged(nameof(CurrentStation));
        OnPropertyChanged(nameof(CurrentCounter));
        OnPropertyChanged(nameof(CurrentPos));
    }

    /// <summary>
    /// Resets the journey to its initial state.
    /// Clears counter, position, and station highlighting.
    /// </summary>
    [RelayCommand]
    private void Reset()
    {
        // Reset the session state
        _state.Reset((int)_journey.FirstPos);

        // Reset IsCurrentStation for all stations
        foreach (var stationVM in Stations)
        {
            stationVM.IsCurrentStation = false;
        }

        // Notify UI about property changes
        OnPropertyChanged(nameof(CurrentStation));
        OnPropertyChanged(nameof(CurrentCounter));
        OnPropertyChanged(nameof(CurrentPos));

        System.Diagnostics.Debug.WriteLine($"🔄 Journey '{Name}' reset to initial state");
    }

    public BehaviorOnLastStop BehaviorOnLastStop
    {
        get => _journey.BehaviorOnLastStop;
        set => SetProperty(_journey.BehaviorOnLastStop, value, _journey, (m, v) => m.BehaviorOnLastStop = v);
    }

    /// <summary>
    /// Next Journey ID reference (resolved at runtime).
    /// </summary>
    public Guid? NextJourneyId
    {
        get => _journey.NextJourneyId;
        set => SetProperty(_journey.NextJourneyId, value, _journey, (m, v) => m.NextJourneyId = v);
    }

    /// <summary>
    /// Next Journey resolved from Project (for UI display).
    /// </summary>
    public Journey? NextJourney => 
        _journey.NextJourneyId.HasValue 
            ? _project.Journeys.FirstOrDefault(j => j.Id == _journey.NextJourneyId.Value)
            : null;

    public uint FirstPos
    {
        get => _journey.FirstPos;
        set => SetProperty(_journey.FirstPos, value, _journey, (m, v) => m.FirstPos = v);
    }

    /// <summary>
    /// Expose domain object for serialization and other operations.
    /// </summary>
    public Journey Model => _journey;

    [RelayCommand]
    private void AddStation()
    {
        // Note: AddStation creates a generic station.
        // In practice, stations should be added from City Library (drag & drop).
        // This is mainly for testing or quick prototyping.
        var newStation = new Station 
        { 
            Name = "New Station",
            NumberOfLapsToStop = 2,
            IsExitOnLeft = false
        };
        
        // Add Station to Journey
        _journey.Stations.Add(newStation);

        // Refresh collection
        RefreshStations();

        // Notify that model changed
        ModelChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void DeleteStation(StationViewModel stationVM)
    {
        if (stationVM == null) return;

        // Find and remove Station by Id
        var station = _journey.Stations.FirstOrDefault(s => s.Id == stationVM.Model.Id);
        if (station != null)
        {
            _journey.Stations.Remove(station);
        }

        // Refresh collection
        RefreshStations();

        // Notify that model changed
        ModelChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Refreshes the Stations collection after external changes.
    /// Call this after adding/removing stations programmatically.
    /// </summary>
    public void RefreshStations()
    {
        System.Diagnostics.Debug.WriteLine($"🔄 RefreshStations() called for Journey '{_journey.Name}'");
        System.Diagnostics.Debug.WriteLine($"   - Stations count: {_journey.Stations.Count}");

        // Create or clear the collection
        if (_stations == null)
        {
            _stations = new ObservableCollection<StationViewModel>();
        }
        else
        {
            _stations.Clear();
        }

        // Rebuild from Stations (direct list - no lookup needed!)
        var index = 0;
        foreach (var station in _journey.Stations)
        {
            System.Diagnostics.Debug.WriteLine($"   - Station: {station.Name}");
            
            var vm = new StationViewModel(station, _project);
            vm.Position = index + 1;  // 1-based position
            
            // Mark current station based on SessionState
            vm.IsCurrentStation = (index == _state.CurrentPos);
            
            _stations.Add(vm);
            index++;
        }

        System.Diagnostics.Debug.WriteLine($"✅ RefreshStations() complete: {_stations.Count} stations loaded");

        // Notify UI
        OnPropertyChanged(nameof(Stations));
    }

    /// <summary>
    /// Handles station reordering after drag & drop.
    /// Call this method when stations are reordered in the UI.
    /// </summary>
    [RelayCommand]
    public void StationsReordered()
    {
        // Get current UI order
        var currentStations = Stations.ToList();
        
        // Rebuild Stations list to match ViewModel order
        var reorderedStations = new List<Station>();
        foreach (var stationVM in currentStations)
        {
            var station = _journey.Stations.FirstOrDefault(s => s.Id == stationVM.Model.Id);
            if (station != null)
            {
                reorderedStations.Add(station);
            }
        }
        
        // Replace Stations with reordered list
        _journey.Stations.Clear();
        foreach (var s in reorderedStations)
        {
            _journey.Stations.Add(s);
        }

        // Refresh positions (no need to rebuild entire collection)
        for (int i = 0; i < Stations.Count; i++)
        {
            Stations[i].Position = i + 1;
        }

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
