// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Service;

using Common.Runtime;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;
using Domain.Enum;

using Interface;

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

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

    // Runtime State
    private readonly JourneySessionState _state;
    private ObservableCollection<StationViewModel>? _stations;
    private ObservableCollection<JourneyFeedbackStepViewModel>? _feedbackSteps;
    private StationListViewMode _stationListViewMode = StationListViewMode.StopsOnly;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="JourneyViewModel"/> class with a runtime session state.
    /// </summary>
    public JourneyViewModel(
        Journey journey,
        Project project,
        JourneySessionState state,
        IUiDispatcher? dispatcher = null)
    {
        ArgumentNullException.ThrowIfNull(journey);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(state);
        _journey = journey;
        _project = project;
        _state = state;
        _dispatcher = dispatcher;

        // Initialize Stations collection
        RefreshStations();
        RefreshFeedbackSteps();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JourneyViewModel"/> class for UI-only scenarios.
    /// A dummy session state is created that does not receive runtime updates.
    /// </summary>
    public JourneyViewModel(Journey journey, Project project, IUiDispatcher? dispatcher = null)
        : this(journey, project, new JourneySessionState { JourneyId = journey.Id }, dispatcher)
    {
    }

    /// <summary>
    /// Gets the unique identifier of the journey.
    /// </summary>
    public Guid Id => _journey.Id;

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

    /// <summary>
    /// Gets or sets the text-to-speech template used when generating announcements for this journey.
    /// </summary>
    [Display(Name = "Text-to-speech template")]
    public string Text
    {
        get => _journey.Text;
        set => SetProperty(_journey.Text, value, _journey, (m, v) => m.Text = v);
    }

    /// <summary>
    /// Gets or sets the search text used to filter stations by name.
    /// </summary>
    public string StationSearchText
    {
        get => field;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(FilteredStations));
            }
        }
    } = string.Empty;

    /// <summary>
    /// Gets or sets how stations are displayed in the Journeys page list.
    /// </summary>
    public StationListViewMode StationListViewMode
    {
        get => _stationListViewMode;
        set
        {
            if (_stationListViewMode == value)
            {
                return;
            }

            _stationListViewMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsTimelineView));
            OnPropertyChanged(nameof(FilteredStations));
            UpdateStationHighlights();
        }
    }

    /// <summary>
    /// Indicates whether the full journey timeline (including events) is shown.
    /// </summary>
    public bool IsTimelineView => StationListViewMode == StationListViewMode.FullTimeline;

    /// <summary>
    /// Gets the filtered stations based on search text and view mode.
    /// </summary>
    public List<StationViewModel> FilteredStations
    {
        get
        {
            var stations = string.IsNullOrWhiteSpace(StationSearchText)
                ? Stations
                : Stations.Where(s => s.Name.Contains(StationSearchText, StringComparison.OrdinalIgnoreCase));

            if (StationListViewMode == StationListViewMode.StopsOnly)
            {
                stations = stations.Where(s => s.IsRealStation);
            }

            return [.. stations];
        }
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

    /// <summary>Gets the ordered feedback sequence configured for this journey.</summary>
    public ObservableCollection<JourneyFeedbackStepViewModel> FeedbackSteps
    {
        get
        {
            if (_feedbackSteps == null)
            {
                RefreshFeedbackSteps();
            }
            return _feedbackSteps!;
        }
    }

    [RelayCommand]
    private void AddFeedbackStep()
    {
        _journey.FeedbackSequence.Add(new JourneyFeedbackStep { InPort = 1 });
        RefreshFeedbackSteps();
    }

    [RelayCommand]
    private void DeleteFeedbackStep(JourneyFeedbackStepViewModel step)
    {
        _journey.FeedbackSequence.Remove(step.Model);
        RefreshFeedbackSteps();
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

    /// <summary>Gets the progress within the currently expected feedback step.</summary>
    public uint CurrentStepOccurrence => _state.CurrentStepOccurrence;

    /// <summary>Gets the repeat count required by the currently expected feedback step.</summary>
    public uint CurrentStepRepeatCount => _journey.FeedbackSequence.ElementAtOrDefault(_state.CurrentFeedbackIndex)?.RepeatCount ?? 1;

    /// <summary>
    /// Gets the current station index within the journey from the runtime session state.
    /// Read-only from the ViewModel perspective – updated via runtime snapshots.
    /// </summary>
    public int CurrentPos => _state.CurrentPos;

    /// <summary>Gets the index of the next expected feedback sequence entry.</summary>
    public int CurrentFeedbackIndex => _state.CurrentFeedbackIndex;

    /// <summary>Gets the InPort expected by the next feedback sequence entry, if any.</summary>
    public uint? NextFeedbackInPort => _journey.FeedbackSequence.ElementAtOrDefault(_state.CurrentFeedbackIndex)?.InPort;

    /// <summary>
    /// Updates the local SessionState from a runtime projection and notifies UI.
    /// </summary>
    /// <param name="state">The updated session state from the runtime projection</param>
    public void UpdateFromSessionState(JourneySessionState state)
    {
        _state.CurrentPos = state.CurrentPos;
        _state.CurrentStationName = state.CurrentStationName;
        _state.CurrentStationId = state.CurrentStationId;
        _state.CurrentFeedbackIndex = state.CurrentFeedbackIndex;
        _state.CurrentStepOccurrence = state.CurrentStepOccurrence;
        _state.LastFeedbackTime = state.LastFeedbackTime;
        _state.IsActive = state.IsActive;

        UpdateStationHighlights();

        // Notify UI about property changes
        OnPropertyChanged(nameof(CurrentStation));
        OnPropertyChanged(nameof(CurrentStepOccurrence));
        OnPropertyChanged(nameof(CurrentStepRepeatCount));
        OnPropertyChanged(nameof(CurrentPos));
        OnPropertyChanged(nameof(CurrentFeedbackIndex));
        OnPropertyChanged(nameof(NextFeedbackInPort));
    }

    /// <summary>
    /// Updates the local runtime state from an immutable runtime snapshot.
    /// </summary>
    public void UpdateFromRuntimeSnapshot(JourneyRuntimeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        _state.CurrentPos = snapshot.CurrentPos;
        _state.CurrentStationName = snapshot.CurrentStationName;
        _state.CurrentStationId = snapshot.CurrentStationId;
        _state.CurrentFeedbackIndex = snapshot.CurrentFeedbackIndex;
        _state.CurrentStepOccurrence = snapshot.CurrentStepOccurrence;
        _state.LastFeedbackTime = snapshot.LastFeedbackTime;
        _state.IsActive = snapshot.IsActive;

        UpdateStationHighlights(snapshot.CurrentPos);

        OnPropertyChanged(nameof(CurrentStation));
        OnPropertyChanged(nameof(CurrentStepOccurrence));
        OnPropertyChanged(nameof(CurrentStepRepeatCount));
        OnPropertyChanged(nameof(CurrentPos));
        OnPropertyChanged(nameof(CurrentFeedbackIndex));
        OnPropertyChanged(nameof(NextFeedbackInPort));
    }

    /// <summary>
    /// Resets the projected runtime state to the initial journey position.
    /// </summary>
    public void ResetRuntimeState()
    {
        _state.Reset((int)_journey.FirstPos);

        foreach (var stationVm in Stations)
        {
            stationVm.IsCurrentStation = false;
        }

        OnPropertyChanged(nameof(CurrentStation));
        OnPropertyChanged(nameof(CurrentStepOccurrence));
        OnPropertyChanged(nameof(CurrentStepRepeatCount));
        OnPropertyChanged(nameof(CurrentPos));
        OnPropertyChanged(nameof(CurrentFeedbackIndex));
        OnPropertyChanged(nameof(NextFeedbackInPort));
    }

    /// <summary>
    /// Resets the journey to its initial state.
    /// Clears counter, position, and station highlighting.
    /// </summary>
    [RelayCommand]
    private void Reset()
    {
        ResetRuntimeState();
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
        var newStation = new Station { Name = "New Station", IsExitOnLeft = false };

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
            var vm = new StationViewModel(station, _project)
            {
                Position = index + 1  // 1-based position
            };

            _stations.Add(vm);
            index++;
        }

        UpdateStationHighlights();

        // Notify UI
        OnPropertyChanged(nameof(Stations));
        OnPropertyChanged(nameof(FilteredStations));
    }

    public void RefreshFeedbackSteps()
    {
        _feedbackSteps ??= [];
        _feedbackSteps.Clear();
        foreach (var step in _journey.FeedbackSequence)
        {
            _feedbackSteps.Add(new JourneyFeedbackStepViewModel(step, _project));
        }

        OnPropertyChanged(nameof(FeedbackSteps));
        OnPropertyChanged(nameof(NextFeedbackInPort));
    }

    private void UpdateStationHighlights() => UpdateStationHighlights(_state.CurrentPos);

    private void UpdateStationHighlights(int currentPos)
    {
        for (var i = 0; i < Stations.Count; i++)
        {
            Stations[i].IsCurrentStation = StationListViewMode == StationListViewMode.FullTimeline
                ? i == currentPos
                : Stations[i].IsRealStation && IsApproachSegmentActive(i, currentPos);
        }
    }

    private bool IsApproachSegmentActive(int realStationIndex, int currentPos)
    {
        if (realStationIndex < 0 || realStationIndex >= Stations.Count || !Stations[realStationIndex].IsRealStation)
        {
            return false;
        }

        var segmentStart = 0;
        for (var j = realStationIndex - 1; j >= 0; j--)
        {
            if (Stations[j].IsRealStation)
            {
                segmentStart = j + 1;
                break;
            }
        }

        return currentPos >= segmentStart && currentPos <= realStationIndex;
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

    public void MoveStationTo(StationViewModel station, int insertIndex)
    {
        var currentIndex = Stations.IndexOf(station);
        if (currentIndex < 0)
        {
            return;
        }

        var targetIndex = Math.Clamp(insertIndex, 0, Stations.Count);
        if (currentIndex < targetIndex)
        {
            targetIndex--;
        }

        targetIndex = Math.Clamp(targetIndex, 0, Stations.Count - 1);
        if (currentIndex == targetIndex)
        {
            return;
        }

        Stations.Move(currentIndex, targetIndex);
        StationsReorderedCommand.Execute(null);
        OnPropertyChanged(nameof(FilteredStations));
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
