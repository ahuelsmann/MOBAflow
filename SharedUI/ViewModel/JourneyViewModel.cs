// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Manager;
using Backend.Service;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;
using Domain.Enum;

using Interface;

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

/// <summary>
/// ViewModel wrapper for <see cref="Journey"/> that exposes configuration and runtime state
/// for a train journey and provides commands for managing its stations.
/// </summary>
public sealed partial class JourneyViewModel : ObservableObject, IViewModelWrapper<Journey>
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
    /// Initializes a new instance of the <see cref="JourneyViewModel"/> class with a runtime session state.
    /// Use this constructor when the journey is actively executed by a <see cref="JourneyManager"/>.
    /// </summary>
    public JourneyViewModel(
        Journey journey,
        Project project,
        JourneySessionState state,
        JourneyManager? journeyManager = null,
        IUiDispatcher? dispatcher = null)
    {
        ArgumentNullException.ThrowIfNull(journey);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(state);
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
    /// Initializes a new instance of the <see cref="JourneyViewModel"/> class for UI-only scenarios.
    /// A dummy session state is created that does not receive runtime updates.
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

    /// <summary>
    /// Gets or sets the feedback input port that starts the journey.
    /// </summary>
    public uint InPort
    {
        get => _journey.InPort;
        set => SetProperty(_journey.InPort, value, _journey, (m, v) => m.InPort = v);
    }

    /// <summary>
    /// Gets or sets a value indicating whether a timer-based window is used to ignore feedback events.
    /// </summary>
    public bool IsUsingTimerToIgnoreFeedbacks
    {
        get => _journey.IsUsingTimerToIgnoreFeedbacks;
        set => SetProperty(_journey.IsUsingTimerToIgnoreFeedbacks, value, _journey, (m, v) => m.IsUsingTimerToIgnoreFeedbacks = v);
    }

    /// <summary>
    /// Gets or sets the duration in seconds of the timer window used to ignore feedback events.
    /// </summary>
    public double IntervalForTimerToIgnoreFeedbacks
    {
        get => _journey.IntervalForTimerToIgnoreFeedbacks;
        set => SetProperty(_journey.IntervalForTimerToIgnoreFeedbacks, value, _journey, (m, v) => m.IntervalForTimerToIgnoreFeedbacks = v);
    }

    /// <summary>
    /// Gets or sets the display name of the journey.
    /// </summary>
    public string Name
    {
        get => _journey.Name;
        set => SetProperty(_journey.Name, value, _journey, (m, v) => m.Name = v);
    }

    /// <summary>
    /// Gets or sets the description of the journey.
    /// </summary>
    public string Description
    {
        get => _journey.Description;
        set => SetProperty(_journey.Description, value, _journey, (m, v) => m.Description = v);
    }

    [Display(Name = "Text-to-speech template")]
    public string Text
    {
        get => _journey.Text;
        set => SetProperty(_journey.Text, value, _journey, (m, v) => m.Text = v);
    }

    /// <summary>
    /// Gets the collection of station ViewModels for this journey.
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

    /// <summary>
    /// Gets the possible values for <see cref="BehaviorOnLastStop"/> for ComboBox binding.
    /// </summary>
    public IEnumerable<BehaviorOnLastStop> BehaviorOnLastStopValues =>
        Enum.GetValues<BehaviorOnLastStop>();

    /// <summary>
    /// Gets the current station name from the runtime session state.
    /// </summary>
    public string CurrentStation => _state.CurrentStationName;

    /// <summary>
    /// Gets the current lap or repetition counter from the runtime session state.
    /// Read-only from the ViewModel perspective – managed by <see cref="JourneyManager"/>.
    /// </summary>
    public int CurrentCounter => _state.Counter;

    /// <summary>
    /// Gets the current station index within the journey from the runtime session state.
    /// Read-only from the ViewModel perspective – managed by <see cref="JourneyManager"/>.
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

        // Update station highlighting based on CurrentPos
        for (int i = 0; i < Stations.Count; i++)
        {
            Stations[i].IsCurrentStation = i == state.CurrentPos;
        }

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
        foreach (var stationVm in Stations)
        {
            stationVm.IsCurrentStation = false;
        }

        // Notify UI about property changes
        OnPropertyChanged(nameof(CurrentStation));
        OnPropertyChanged(nameof(CurrentCounter));
        OnPropertyChanged(nameof(CurrentPos));

        Debug.WriteLine($"🔄 Journey '{Name}' reset to initial state");
    }

    /// <summary>
    /// Gets or sets the behavior when the journey reaches the last stop.
    /// </summary>
    public BehaviorOnLastStop BehaviorOnLastStop
    {
        get => _journey.BehaviorOnLastStop;
        set => SetProperty(_journey.BehaviorOnLastStop, value, _journey, (m, v) => m.BehaviorOnLastStop = v);
    }

    /// <summary>
    /// Gets or sets the identifier of the next journey to start after this one finishes.
    /// </summary>
    public Guid? NextJourneyId
    {
        get => _journey.NextJourneyId;
        set => SetProperty(_journey.NextJourneyId, value, _journey, (m, v) => m.NextJourneyId = v);
    }

    /// <summary>
    /// Gets the next journey instance resolved from the project for UI display.
    /// </summary>
    public Journey? NextJourney =>
        _journey.NextJourneyId.HasValue
            ? _project.Journeys.FirstOrDefault(j => j.Id == _journey.NextJourneyId.Value)
            : null;

    /// <summary>
    /// Gets or sets the initial position index used when resetting the journey.
    /// </summary>
    public uint FirstPos
    {
        get => _journey.FirstPos;
        set => SetProperty(_journey.FirstPos, value, _journey, (m, v) => m.FirstPos = v);
    }

    /// <summary>
    /// Gets the underlying journey domain model (for serialization and other operations).
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
        // PropertyChanged fires automatically via Stations property
    }

    [RelayCommand]
    private void DeleteStation(StationViewModel stationVm)
    {
        // Find and remove Station by Id
        var station = _journey.Stations.FirstOrDefault(s => s.Id == stationVm.Model.Id);
        if (station != null)
        {
            _journey.Stations.Remove(station);
        }

        // Refresh collection
        RefreshStations();
        // PropertyChanged fires automatically via Stations property
    }

    /// <summary>
    /// Refreshes the Stations collection after external changes.
    /// Call this after adding/removing stations programmatically.
    /// </summary>
    public void RefreshStations()
    {
        Debug.WriteLine($"🔄 RefreshStations() called for Journey '{_journey.Name}'");
        Debug.WriteLine($"   - Stations count: {_journey.Stations.Count}");

        // Create or clear the collection
        if (_stations == null)
        {
            _stations = [];
        }
        else
        {
            _stations.Clear();
        }

        // Rebuild from Stations (direct list - no lookup needed!)
        var index = 0;
        foreach (var station in _journey.Stations)
        {
            Debug.WriteLine($"   - Station: {station.Name}");

            var vm = new StationViewModel(station, _project)
            {
                Position = index + 1,  // 1-based position

                // Mark current station based on SessionState
                IsCurrentStation = index == _state.CurrentPos
            };

            _stations.Add(vm);
            index++;
        }

        Debug.WriteLine($"✅ RefreshStations() complete: {_stations.Count} stations loaded");

        // Notify UI
        OnPropertyChanged(nameof(Stations));
    }

    /// <summary>
    /// Handles station reordering after drag and drop.
    /// Call this method when stations are reordered in the UI.
    /// </summary>
    [RelayCommand]
    public void StationsReordered()
    {
        // Get current UI order
        var currentStations = Stations.ToList();

        // Rebuild Stations list to match ViewModel order
        var reorderedStations = new List<Station>();
        foreach (var stationVm in currentStations)
        {
            var station = _journey.Stations.FirstOrDefault(s => s.Id == stationVm.Model.Id);
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

        // PropertyChanged fires automatically via Stations property
    }

    /// <summary>
    /// Moves a station up in the journey (decreases its position by 1).
    /// </summary>
    /// <param name="station">The station to move up</param>
    [RelayCommand]
    private void MoveStationUp(StationViewModel station)
    {
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
