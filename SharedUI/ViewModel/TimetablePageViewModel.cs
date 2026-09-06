// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using System.Collections.ObjectModel;
using System.ComponentModel;

using Backend.Interface;

using Common.Events;
using Common.Runtime;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;

using Microsoft.Extensions.Logging;

/// <summary>
/// Owns timetable editing, operating decisions and live runtime projection for TimetablePage.
/// </summary>
public sealed partial class TimetablePageViewModel : ObservableObject, IDisposable
{
    private readonly MainWindowViewModel _mainWindow;
    private readonly ITimetableEvaluationService _evaluation;
    private readonly ITimetableOperationsService _operations;
    private readonly ITimetableTimingService _timing;
    private readonly ITimetableRuntimeProjectionService _projection;
    private readonly IEventBus _eventBus;
    private readonly ILogger<TimetablePageViewModel> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly Guid _runtimeSubscription;
    private readonly Guid _stationReachedSubscription;
    private readonly SemaphoreSlim _projectionGate = new(1, 1);
    private bool _disposed;
    private List<TimetableServiceRowViewModel> _allRows = [];
    private MobaRuntimeSnapshot _latestSnapshot = MobaRuntimeSnapshot.Empty;

    /// <summary>Initializes the timetable page state and runtime subscriptions.</summary>
    public TimetablePageViewModel(
        MainWindowViewModel mainWindow,
        ITimetableEvaluationService evaluation,
        ITimetableOperationsService operations,
        ITimetableTimingService timing,
        ITimetableRuntimeProjectionService projection,
        IEventBus eventBus,
        ILogger<TimetablePageViewModel> logger,
        TimeProvider timeProvider)
    {
        _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        _evaluation = evaluation ?? throw new ArgumentNullException(nameof(evaluation));
        _operations = operations ?? throw new ArgumentNullException(nameof(operations));
        _timing = timing ?? throw new ArgumentNullException(nameof(timing));
        _projection = projection ?? throw new ArgumentNullException(nameof(projection));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        _mainWindow.PropertyChanged += OnMainWindowPropertyChanged;
        _runtimeSubscription = _eventBus.Subscribe<RuntimeSnapshotChangedEvent>(OnRuntimeSnapshotChanged);
        _stationReachedSubscription = _eventBus.Subscribe<JourneyStationReachedEvent>(OnJourneyStationReached);
    }

    public ObservableCollection<TimetableServiceRowViewModel> Services { get; } = [];

    public ObservableCollection<TimetableCallRowViewModel> Calls { get; } = [];

    public ObservableCollection<TimetableIssueRowViewModel> Issues { get; } = [];

    public IReadOnlyList<string> FocusOptions { get; } = ["All", "Station", "Train", "Time window"];

    [ObservableProperty]
    private string _selectedFocus = "All";

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private double _timeWindowHours = 4;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedServiceCommand))]
    [NotifyCanExecuteChangedFor(nameof(HoldSelectedServiceCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReleaseSelectedServiceCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelSelectedServiceCommand))]
    [NotifyCanExecuteChangedFor(nameof(CompleteSelectedServiceCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReassignSelectedTrainCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReassignSelectedJourneyCommand))]
    [NotifyCanExecuteChangedFor(nameof(RecordArrivalCommand))]
    [NotifyCanExecuteChangedFor(nameof(RecordDepartureCommand))]
    private TimetableServiceRowViewModel? _selectedService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RecordArrivalCommand))]
    [NotifyCanExecuteChangedFor(nameof(RecordDepartureCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShiftSelectedCallEarlierCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShiftSelectedCallLaterCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReassignSelectedPlatformCommand))]
    private TimetableCallRowViewModel? _selectedCall;

    [ObservableProperty]
    private string _validationSummary = "No timetable loaded";

    [ObservableProperty]
    private string _statusText = "Ready";

    public bool HasProject => _mainWindow.SelectedProject is not null;

    /// <summary>Reloads definitions, operating state, live progress and conflict findings.</summary>
    [RelayCommand]
    public async Task RefreshAsync()
    {
        var project = CurrentProject;
        Services.Clear();
        Calls.Clear();
        Issues.Clear();
        _allRows = [];
        SelectedService = null;
        SelectedCall = null;

        if (project is null)
        {
            ValidationSummary = "Select a project to view its timetable.";
            return;
        }

        var states = await _operations.GetStatesAsync(project.Id);
        var stateByService = states.ToDictionary(state => state.ServiceId);
        _allRows = project.TimetableServices
            .OrderBy(service => service.ServiceDate)
            .ThenBy(service => service.ServiceNumber, StringComparer.OrdinalIgnoreCase)
            .Select(definition => new TimetableServiceRowViewModel(definition, stateByService.GetValueOrDefault(definition.Id), _timing, _latestSnapshot, project))
            .ToList();
        ApplyFilter();

        var result = _evaluation.Evaluate(project, states);
        foreach (var issue in result.Issues)
        {
            Issues.Add(new TimetableIssueRowViewModel(issue));
        }
        ValidationSummary = result.IsValid
            ? $"{Services.Count} services; no conflicts"
            : $"{result.Issues.Count} validation issues or conflicts";
        StatusText = "Timetable refreshed";
    }

    [RelayCommand]
    private async Task AddServiceAsync()
    {
        var project = CurrentProject;
        var journey = project?.Journeys.FirstOrDefault(candidate => candidate.Stations.Count > 0);
        var station = project?.Stations.FirstOrDefault(candidate => candidate.Platforms.Count > 0);
        if (project is null || journey is null || station is null)
        {
            StatusText = "Create a journey stop and a station platform before adding a service.";
            return;
        }

        var now = _timeProvider.GetLocalNow();
        var arrival = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Offset).AddHours(1);
        var definition = new TimetableService
        {
            ServiceNumber = $"S{project.TimetableServices.Count + 1:000}",
            Name = "New service",
            JourneyId = journey.Id,
            TrainId = project.Trains.FirstOrDefault()?.Id,
            ServiceDate = DateOnly.FromDateTime(arrival.Date),
            Calls =
            [
                new TimetableCall
                {
                    JourneyStopId = journey.Stations[0].Id,
                    StationId = station.Id,
                    PlatformId = station.Platforms[0].Id,
                    ScheduledArrival = arrival,
                    ScheduledDeparture = arrival.AddMinutes(2)
                }
            ]
        };
        project.TimetableServices.Add(definition);
        await _mainWindow.SaveSolutionInternalAsync();
        await RefreshAsync();
        SelectedService = Services.FirstOrDefault(service => service.Id == definition.Id);
        StatusText = "Service added";
    }

    [RelayCommand(CanExecute = nameof(HasSelectedService))]
    private async Task DeleteSelectedServiceAsync()
    {
        var project = CurrentProject;
        if (project is null || SelectedService is null) return;
        project.TimetableServices.Remove(SelectedService.Model);
        await _mainWindow.SaveSolutionInternalAsync();
        await RefreshAsync();
        StatusText = "Service deleted";
    }

    [RelayCommand(CanExecute = nameof(CanHoldSelectedService))]
    private async Task HoldSelectedServiceAsync()
    {
        if (CurrentProject is null || SelectedService is null) return;
        await _operations.HoldAsync(CurrentProject.Id, SelectedService.Id, _timeProvider.GetUtcNow().AddMinutes(5), "Operator hold");
        await RefreshAndReselectAsync(SelectedService.Id);
        StatusText = "Service held for five minutes";
    }

    [RelayCommand(CanExecute = nameof(CanReleaseSelectedService))]
    private async Task ReleaseSelectedServiceAsync()
    {
        if (CurrentProject is null || SelectedService is null) return;
        await _operations.ReleaseAsync(CurrentProject.Id, SelectedService.Id);
        await RefreshAndReselectAsync(SelectedService.Id);
        StatusText = "Service released";
    }

    [RelayCommand(CanExecute = nameof(CanCancelSelectedService))]
    private async Task CancelSelectedServiceAsync()
    {
        if (CurrentProject is null || SelectedService is null) return;
        await _operations.CancelAsync(CurrentProject.Id, SelectedService.Id);
        await RefreshAndReselectAsync(SelectedService.Id);
        StatusText = "Service cancelled";
    }

    [RelayCommand(CanExecute = nameof(CanCompleteSelectedService))]
    private async Task CompleteSelectedServiceAsync()
    {
        if (CurrentProject is null || SelectedService is null) return;
        await _operations.CompleteAsync(CurrentProject.Id, SelectedService.Id);
        await RefreshAndReselectAsync(SelectedService.Id);
        StatusText = "Service completed";
    }

    [RelayCommand(CanExecute = nameof(CanChangeSelectedService))]
    private async Task ReassignSelectedTrainAsync()
    {
        var project = CurrentProject;
        if (project is null || SelectedService is null || project.Trains.Count == 0) return;
        var currentId = SelectedService.State?.AssignedTrainId ?? SelectedService.Model.TrainId;
        var currentIndex = project.Trains.FindIndex(train => train.Id == currentId);
        var train = project.Trains[(currentIndex + 1 + project.Trains.Count) % project.Trains.Count];
        await _operations.ReassignTrainAsync(project.Id, SelectedService.Id, train.Id);
        await RefreshAndReselectAsync(SelectedService.Id);
        StatusText = $"Train reassigned to {train.Name}";
    }

    [RelayCommand(CanExecute = nameof(CanChangeSelectedService))]
    private async Task ReassignSelectedJourneyAsync()
    {
        var project = CurrentProject;
        if (project is null || SelectedService is null || project.Journeys.Count == 0) return;
        var currentId = SelectedService.State?.AssignedJourneyId ?? SelectedService.Model.JourneyId;
        var currentIndex = project.Journeys.FindIndex(journey => journey.Id == currentId);
        var journey = project.Journeys[(currentIndex + 1 + project.Journeys.Count) % project.Journeys.Count];
        await _operations.ReassignJourneyAsync(project.Id, SelectedService.Id, journey.Id);
        await RefreshAndReselectAsync(SelectedService.Id);
        StatusText = $"Journey reassigned to {journey.Name}";
    }

    [RelayCommand(CanExecute = nameof(CanRecordArrival))]
    private async Task RecordArrivalAsync()
    {
        if (CurrentProject is null || SelectedService is null || SelectedCall is null) return;
        await _operations.RecordArrivalAsync(CurrentProject.Id, SelectedService.Id, SelectedCall.Id);
        await RefreshAndReselectAsync(SelectedService.Id, SelectedCall.Id);
        StatusText = "Arrival recorded";
    }

    [RelayCommand(CanExecute = nameof(CanRecordDeparture))]
    private async Task RecordDepartureAsync()
    {
        if (CurrentProject is null || SelectedService is null || SelectedCall is null) return;
        await _operations.RecordDepartureAsync(CurrentProject.Id, SelectedService.Id, SelectedCall.Id);
        await RefreshAndReselectAsync(SelectedService.Id, SelectedCall.Id);
        StatusText = "Departure recorded";
    }

    [RelayCommand(CanExecute = nameof(HasSelectedCall))]
    private Task ShiftSelectedCallEarlierAsync() => ShiftSelectedCallAsync(-5);

    [RelayCommand(CanExecute = nameof(HasSelectedCall))]
    private Task ShiftSelectedCallLaterAsync() => ShiftSelectedCallAsync(5);

    private async Task ShiftSelectedCallAsync(int minutes)
    {
        if (SelectedCall is null) return;
        SelectedCall.Model.ScheduledArrival = SelectedCall.Model.ScheduledArrival.AddMinutes(minutes);
        SelectedCall.Model.ScheduledDeparture = SelectedCall.Model.ScheduledDeparture.AddMinutes(minutes);
        await _mainWindow.SaveSolutionInternalAsync();
        await RefreshAndReselectAsync(SelectedService!.Id, SelectedCall.Id);
        StatusText = $"Scheduled call shifted by {minutes} minutes";
    }

    [RelayCommand(CanExecute = nameof(HasSelectedCall))]
    private async Task ReassignSelectedPlatformAsync()
    {
        var project = CurrentProject;
        if (project is null || SelectedCall is null) return;
        var station = project.Stations.FirstOrDefault(candidate => candidate.Id == SelectedCall.Model.StationId);
        if (station is null || station.Platforms.Count == 0) return;
        var currentPlatformId = SelectedCall.State?.AssignedPlatformId ?? SelectedCall.Model.PlatformId;
        var currentIndex = station.Platforms.FindIndex(platform => platform.Id == currentPlatformId);
        var platform = station.Platforms[(currentIndex + 1 + station.Platforms.Count) % station.Platforms.Count];
        await _operations.ReassignPlatformAsync(project.Id, SelectedService!.Id, SelectedCall.Id, platform.Id);
        await RefreshAndReselectAsync(SelectedService!.Id, SelectedCall.Id);
        StatusText = "Platform reassigned";
    }

    [RelayCommand]
    private async Task SaveDefinitionAsync()
    {
        await _mainWindow.SaveSolutionInternalAsync();
        await RefreshAndReselectAsync(SelectedService?.Id, SelectedCall?.Id);
        StatusText = "Timetable definition saved";
    }

    partial void OnSelectedServiceChanged(TimetableServiceRowViewModel? value)
    {
        Calls.Clear();
        SelectedCall = null;
        if (value is null) return;

        var project = CurrentProject;
        foreach (var call in value.Model.Calls.OrderBy(call => call.ScheduledArrival))
        {
            var station = project?.Stations.FirstOrDefault(candidate => candidate.Id == call.StationId);
            var state = value.State?.Calls.FirstOrDefault(candidate => candidate.CallId == call.Id);
            var platformId = state?.AssignedPlatformId ?? call.PlatformId;
            var platform = station?.Platforms.FirstOrDefault(candidate => candidate.Id == platformId);
            Calls.Add(new TimetableCallRowViewModel(call, station?.Name ?? "Missing station", platform?.Name ?? platform?.Number.ToString() ?? "Missing platform", state));
        }
    }

    partial void OnSelectedFocusChanged(string value)
    {
        _ = value;
        ApplyFilter();
    }

    partial void OnFilterTextChanged(string value)
    {
        _ = value;
        ApplyFilter();
    }

    partial void OnTimeWindowHoursChanged(double value)
    {
        _ = value;
        if (SelectedFocus == "Time window") ApplyFilter();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _mainWindow.PropertyChanged -= OnMainWindowPropertyChanged;
        _eventBus.Unsubscribe(_runtimeSubscription);
        _eventBus.Unsubscribe(_stationReachedSubscription);
    }

    private Project? CurrentProject => _mainWindow.SelectedProject?.Model;

    private bool HasSelectedService() => SelectedService is not null;

    private bool HasSelectedCall() => SelectedService is not null
        && SelectedCall is not null
        && SelectedService.State?.Status is not (TimetableServiceStatus.Completed or TimetableServiceStatus.Cancelled);

    private bool CanRecordArrival() => HasSelectedCall()
        && SelectedCall!.State?.ActualArrival is null;

    private bool CanRecordDeparture() => HasSelectedCall()
        && SelectedCall!.State?.ActualArrival is not null
        && SelectedCall.State.ActualDeparture is null;

    private bool CanHoldSelectedService() => SelectedService is not null
        && SelectedService.State?.Status is not (TimetableServiceStatus.Completed or TimetableServiceStatus.Cancelled);

    private bool CanReleaseSelectedService() => SelectedService?.State?.Status == TimetableServiceStatus.Held;

    private bool CanCancelSelectedService() => SelectedService is not null
        && SelectedService.State?.Status != TimetableServiceStatus.Completed;

    private bool CanCompleteSelectedService() => SelectedService is not null
        && SelectedService.State?.Status != TimetableServiceStatus.Cancelled;

    private bool CanChangeSelectedService() => SelectedService is not null
        && SelectedService.State?.Status is not (TimetableServiceStatus.Completed or TimetableServiceStatus.Cancelled);

    private void ApplyFilter()
    {
        var project = CurrentProject;
        IEnumerable<TimetableServiceRowViewModel> filtered = _allRows;
        var text = FilterText.Trim();

        if (SelectedFocus == "Station" && project is not null && text.Length > 0)
        {
            var stationIds = project.Stations
                .Where(station => station.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
                .Select(station => station.Id)
                .ToHashSet();
            filtered = filtered.Where(row => row.Model.Calls.Any(call => stationIds.Contains(call.StationId)));
        }
        else if (SelectedFocus == "Train" && project is not null && text.Length > 0)
        {
            var trainIds = project.Trains
                .Where(train => train.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
                .Select(train => train.Id)
                .ToHashSet();
            filtered = filtered.Where(row => row.EffectiveTrainId is Guid trainId && trainIds.Contains(trainId));
        }
        else if (SelectedFocus == "Time window")
        {
            var start = _timeProvider.GetLocalNow().AddHours(-1);
            var windowHours = double.IsFinite(TimeWindowHours) ? Math.Max(1, TimeWindowHours) : 1;
            var end = _timeProvider.GetLocalNow().AddHours(windowHours);
            filtered = filtered.Where(row => row.Model.Calls.Any(call => call.ScheduledDeparture >= start && call.ScheduledArrival <= end));
        }
        else if (text.Length > 0)
        {
            filtered = filtered.Where(row => row.ServiceNumber.Contains(text, StringComparison.OrdinalIgnoreCase)
                || row.Name.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        Services.Clear();
        foreach (var row in filtered) Services.Add(row);
    }

    private async Task RefreshAndReselectAsync(Guid? serviceId, Guid? callId = null)
    {
        await RefreshAsync();
        SelectedService = Services.FirstOrDefault(service => service.Id == serviceId);
        SelectedCall = Calls.FirstOrDefault(call => call.Id == callId);
    }

    private void OnRuntimeSnapshotChanged(RuntimeSnapshotChangedEvent @event)
    {
        _latestSnapshot = @event.Snapshot;
        foreach (var row in _allRows)
        {
            row.UpdateProgress(@event.Snapshot);
        }
    }

    private void OnJourneyStationReached(JourneyStationReachedEvent @event)
        => _ = ProjectRuntimeSafelyAsync(@event);

    private async Task ProjectRuntimeSafelyAsync(JourneyStationReachedEvent @event)
    {
        await _projectionGate.WaitAsync();
        try
        {
            var project = CurrentProject;
            if (project is null || project.Id != @event.ProjectId) return;
            var selectedServiceId = SelectedService?.Id;
            var selectedCallId = SelectedCall?.Id;
            var result = await _projection.ProjectAsync(project, @event);
            await RefreshAndReselectAsync(selectedServiceId, selectedCallId);
            if (result.SuppressedJourneyIds.Count > 0) StatusText = "Live projection suppressed because a journey has multiple eligible services.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to project runtime state into timetable state");
            StatusText = "Live timetable update failed";
        }
        finally
        {
            _projectionGate.Release();
        }
    }

    private async void OnMainWindowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = sender;
        if (e.PropertyName != nameof(MainWindowViewModel.SelectedProject)) return;
        OnPropertyChanged(nameof(HasProject));
        try
        {
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh timetable after project selection changed");
        }
    }
}

/// <summary>Adapts one timetable service and its operating overlay for dispatch-board binding.</summary>
public sealed partial class TimetableServiceRowViewModel : ObservableObject
{
    public TimetableServiceRowViewModel(TimetableService model, TimetableServiceState? state, ITimetableTimingService timing, MobaRuntimeSnapshot snapshot, Project project)
    {
        Model = model;
        State = state;
        var delays = model.Calls.Select(call => timing.CalculateDelay(call, state?.Calls.FirstOrDefault(candidate => candidate.CallId == call.Id))).ToArray();
        Delay = delays.Length == 0 ? TimeSpan.Zero : delays.Max();
        var journeyId = state?.AssignedJourneyId ?? model.JourneyId;
        var trainId = state?.AssignedTrainId ?? model.TrainId;
        JourneyName = project.Journeys.FirstOrDefault(journey => journey.Id == journeyId)?.Name ?? "Missing journey";
        TrainName = trainId is Guid effectiveTrainId
            ? project.Trains.FirstOrDefault(train => train.Id == effectiveTrainId)?.Name ?? "Missing train"
            : "No train assigned";
        _journeyId = journeyId;
        _progressText = FormatProgress(snapshot, journeyId);
        _serviceNumber = model.ServiceNumber;
        _name = model.Name;
    }

    public TimetableService Model { get; }

    public TimetableServiceState? State { get; }

    public Guid Id => Model.Id;

    public Guid? EffectiveTrainId => State?.AssignedTrainId ?? Model.TrainId;

    public string ServiceDateText => Model.ServiceDate.ToString("yyyy-MM-dd");

    private readonly Guid _journeyId;

    [ObservableProperty]
    private string _progressText;

    public string JourneyName { get; }

    public string TrainName { get; }

    public string Status => (State?.Status ?? TimetableServiceStatus.Scheduled).ToString();

    public TimeSpan Delay { get; }

    public string DelayText => Delay == TimeSpan.Zero ? "On time" : $"{Delay.TotalMinutes:+0;-0} min";

    public string BoardStatus => State?.Status switch
    {
        TimetableServiceStatus.Cancelled => "Cancelled",
        TimetableServiceStatus.Completed => "Completed",
        TimetableServiceStatus.Held => "Held",
        TimetableServiceStatus.Running when Delay > TimeSpan.Zero => "Delayed",
        TimetableServiceStatus.Running => "Active",
        _ when Delay > TimeSpan.Zero => "Delayed",
        _ => "Upcoming"
    };

    public string Schedule => Model.Calls.Count == 0
        ? "No calls"
        : $"{Model.Calls.Min(call => call.ScheduledArrival):HH:mm} - {Model.Calls.Max(call => call.ScheduledDeparture):HH:mm}";

    [ObservableProperty]
    private string _serviceNumber;

    [ObservableProperty]
    private string _name;

    public void UpdateProgress(MobaRuntimeSnapshot snapshot)
        => ProgressText = FormatProgress(snapshot, _journeyId);

    private static string FormatProgress(MobaRuntimeSnapshot snapshot, Guid journeyId)
        => snapshot.JourneyStates.TryGetValue(journeyId, out var journeyState)
            ? $"{journeyState.CurrentStationName} (step {journeyState.CurrentFeedbackIndex + 1})"
            : "No live progress";

    partial void OnServiceNumberChanged(string value) => Model.ServiceNumber = value;

    partial void OnNameChanged(string value) => Model.Name = value;
}

/// <summary>Formats a timetable finding with navigation-ready service references.</summary>
public sealed class TimetableIssueRowViewModel
{
    public TimetableIssueRowViewModel(TimetableIssue issue)
    {
        Kind = issue.Kind.ToString();
        Message = issue.Message;
        Reference = issue.ConflictingServiceId is Guid conflictingId
            ? $"Services {issue.ServiceId} and {conflictingId}"
            : $"Service {issue.ServiceId}";
    }

    public string Kind { get; }

    public string Message { get; }

    public string Reference { get; }
}

/// <summary>Adapts one scheduled call and its actual operating values for timeline binding.</summary>
public sealed class TimetableCallRowViewModel
{
    public TimetableCallRowViewModel(TimetableCall model, string stationName, string platformName, TimetableCallState? state)
    {
        Model = model;
        StationName = stationName;
        PlatformName = platformName;
        State = state;
    }

    public TimetableCall Model { get; }

    public TimetableCallState? State { get; }

    public Guid Id => Model.Id;

    public string StationName { get; }

    public string PlatformName { get; }

    public string ScheduledArrival => Model.ScheduledArrival.ToString("HH:mm");

    public string ScheduledDeparture => Model.ScheduledDeparture.ToString("HH:mm");

    public string ActualArrival => State?.ActualArrival?.ToString("HH:mm:ss") ?? "-";

    public string ActualDeparture => State?.ActualDeparture?.ToString("HH:mm:ss") ?? "-";
}
