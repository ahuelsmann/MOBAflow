// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using Backend.Events;
using Backend.Interface;
using Backend.Service.Interlocking;
using Backend.Service.Validation;
using Common.Events;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain;
using Interface;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;

/// <summary>
/// Page-scoped projection of the shared interlocking definition and runtime.
/// </summary>
public sealed partial class InterlockingControlViewModel : ObservableObject
{
    private const string UnknownStateText = "Unknown";
    private const string NoRuntimeStateText = "No runtime state";

    private readonly IInterlockingRuntime _runtime;
    private readonly IEventBus _eventBus;
    private readonly IProjectContext _projectContext;
    private readonly IInterlockingDefinitionValidator _validator;
    private readonly ILogger<InterlockingControlViewModel> _logger;
    private readonly List<RouteTurnoutRequirement> _draftTurnouts = [];
    private readonly List<Guid> _draftBlocks = [];
    private readonly List<RouteSignalRequirement> _draftSignals = [];
    private readonly List<Guid> _draftPath = [];

    private Guid? _runtimeSubscriptionId;
    private Guid _draftRouteId;

    public InterlockingControlViewModel(
        IInterlockingRuntime runtime,
        IEventBus eventBus,
        IProjectContext projectContext,
        IInterlockingDefinitionValidator validator,
        ILogger<InterlockingControlViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(projectContext);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(logger);

        _runtime = runtime;
        _eventBus = eventBus;
        _projectContext = projectContext;
        _validator = validator;
        _logger = logger;
        RefreshDefinitions();
        ApplySnapshot(_runtime.Current, _runtime.IsSynchronized, "interlocking.current");
    }

    public ObservableCollection<InterlockingItemViewState> Turnouts { get; } = [];

    public ObservableCollection<InterlockingItemViewState> Blocks { get; } = [];

    public ObservableCollection<InterlockingItemViewState> Signals { get; } = [];

    public ObservableCollection<InterlockingItemViewState> Routes { get; } = [];

    public ObservableCollection<OperationalElementOption> OperationalElements { get; } = [];

    public IReadOnlyList<TurnoutPosition> TurnoutPositions { get; } = Enum.GetValues<TurnoutPosition>();

    public IReadOnlyList<SignalAspect> SignalAspects { get; } = Enum.GetValues<SignalAspect>();

    public IReadOnlyList<string> ValidationMessages { get; private set; } = [];

    [ObservableProperty]
    private InterlockingItemViewState? _selectedTurnout;

    [ObservableProperty]
    private InterlockingItemViewState? _selectedRoute;

    [ObservableProperty]
    private InterlockingItemViewState? _selectedBlock;

    [ObservableProperty]
    private InterlockingItemViewState? _selectedSignal;

    [ObservableProperty]
    private OperationalElementOption? _selectedOperationalElement;

    [ObservableProperty]
    private bool _isSynchronized;

    private bool _canOperateTurnout;

    private bool _canOperateRoute;

    private bool _hasSelectedRoute;

    [ObservableProperty]
    private long _revision;

    private string _statusText = "Interlocking is not synchronized.";

    private string _statusCode = "interlocking.unsynchronized";

    [ObservableProperty]
    private string _draftName = string.Empty;

    [ObservableProperty]
    private TurnoutPosition _draftTurnoutPosition = TurnoutPosition.Straight;

    [ObservableProperty]
    private SignalAspect _draftSignalAspect = SignalAspect.Ks1;

    [ObservableProperty]
    private string _draftSummary = "No route draft.";

    public bool CanOperateTurnout
    {
        get => _canOperateTurnout;
        private set => SetProperty(ref _canOperateTurnout, value);
    }

    public bool CanOperateRoute
    {
        get => _canOperateRoute;
        private set => SetProperty(ref _canOperateRoute, value);
    }

    public bool HasSelectedRoute
    {
        get => _hasSelectedRoute;
        private set => SetProperty(ref _hasSelectedRoute, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string StatusCode
    {
        get => _statusCode;
        private set => SetProperty(ref _statusCode, value);
    }

    /// <summary>
    /// Begins observing project selection and runtime snapshot events.
    /// </summary>
    public void StartObserving()
    {
        if (_runtimeSubscriptionId.HasValue)
            return;

        _projectContext.PropertyChanged += OnProjectContextPropertyChanged;
        _runtimeSubscriptionId = _eventBus.Subscribe<InterlockingRuntimeSnapshotChangedEvent>(OnRuntimeSnapshotChanged);
        RefreshDefinitions();
        ApplySnapshot(_runtime.Current, _runtime.IsSynchronized, "interlocking.current");
    }

    /// <summary>
    /// Stops page-scoped observations without disposing the application runtime.
    /// </summary>
    public void StopObserving()
    {
        _projectContext.PropertyChanged -= OnProjectContextPropertyChanged;
        if (_runtimeSubscriptionId is not Guid subscriptionId)
            return;

        _eventBus.Unsubscribe(subscriptionId);
        _runtimeSubscriptionId = null;
    }

    /// <summary>
    /// Maps a selected physical track representation to its shared operational identity.
    /// </summary>
    public void SelectTrackRepresentation(Guid? trackSegmentId) =>
        SelectRepresentation(trackSegmentId, binding => binding.TrackSegmentIds);

    /// <summary>
    /// Maps a selected signal-box representation to its shared operational identity.
    /// </summary>
    public void SelectSignalBoxRepresentation(Guid? signalBoxElementId) =>
        SelectRepresentation(signalBoxElementId, binding => binding.SignalBoxElementIds);

    /// <summary>
    /// Returns renderer-neutral state for a physical track representation.
    /// </summary>
    public InterlockingItemViewState? GetTrackVisualState(Guid trackSegmentId) =>
        GetRepresentationState(trackSegmentId, binding => binding.TrackSegmentIds);

    /// <summary>
    /// Returns renderer-neutral state for a logical signal-box representation.
    /// </summary>
    public InterlockingItemViewState? GetSignalBoxVisualState(Guid signalBoxElementId) =>
        GetRepresentationState(signalBoxElementId, binding => binding.SignalBoxElementIds);

    [RelayCommand]
    private Task SetTurnoutStraightAsync() => SetTurnoutAsync(TurnoutPosition.Straight);

    [RelayCommand]
    private Task SetTurnoutDivergingLeftAsync() => SetTurnoutAsync(TurnoutPosition.DivergingLeft);

    [RelayCommand]
    private Task SetTurnoutDivergingRightAsync() => SetTurnoutAsync(TurnoutPosition.DivergingRight);

    [RelayCommand]
    private Task PreviewRouteAsync() => ExecuteRouteAsync(_runtime.PreviewRouteAsync);

    [RelayCommand]
    private Task SelectRouteAsync() => ExecuteRouteAsync(_runtime.SelectRouteAsync);

    [RelayCommand]
    private Task SetRouteAsync() => ExecuteRouteAsync(_runtime.SetRouteAsync);

    [RelayCommand]
    private Task CancelRouteAsync() => ExecuteRouteAsync(_runtime.CancelRouteAsync);

    [RelayCommand]
    private Task ReleaseRouteAsync() => ExecuteRouteAsync(_runtime.ReleaseRouteAsync);

    [RelayCommand]
    private Task ReconcileRouteAsync() => ExecuteRouteAsync(_runtime.ReconcileRouteAsync);

    [RelayCommand]
    private void BeginRouteDraft()
    {
        _draftRouteId = Guid.NewGuid();
        DraftName = "New route";
        _draftPath.Clear();
        _draftTurnouts.Clear();
        _draftBlocks.Clear();
        _draftSignals.Clear();
        ValidationMessages = [];
        OnPropertyChanged(nameof(ValidationMessages));
        UpdateDraftSummary();
        SetStatus("route.draft.started", "Route draft started.");
    }

    [RelayCommand]
    private void SetDraftEntry()
    {
        EnsureDraft();
        if (SelectedOperationalElement == null)
        {
            SetStatus("route.draft.selection.missing", "Select an operational element first.");
            return;
        }

        DraftEntryId = SelectedOperationalElement.Id;
        UpdateDraftSummary();
    }

    [RelayCommand]
    private void AppendDraftPath()
    {
        EnsureDraft();
        if (SelectedOperationalElement == null)
        {
            SetStatus("route.draft.selection.missing", "Select an operational element first.");
            return;
        }

        _draftPath.Add(SelectedOperationalElement.Id);
        UpdateDraftSummary();
    }

    [RelayCommand]
    private void SetDraftExit()
    {
        EnsureDraft();
        if (SelectedOperationalElement == null)
        {
            SetStatus("route.draft.selection.missing", "Select an operational element first.");
            return;
        }

        DraftExitId = SelectedOperationalElement.Id;
        UpdateDraftSummary();
    }

    [RelayCommand]
    private void AddDraftTurnoutRequirement()
    {
        EnsureDraft();
        if (SelectedTurnout == null)
        {
            SetStatus("route.draft.turnout.missing", "Select a turnout first.");
            return;
        }

        _draftTurnouts.RemoveAll(item => item.TurnoutId == SelectedTurnout.Id);
        _draftTurnouts.Add(new RouteTurnoutRequirement
        {
            TurnoutId = SelectedTurnout.Id,
            Position = DraftTurnoutPosition
        });
        UpdateDraftSummary();
    }

    [RelayCommand]
    private void AddDraftBlock()
    {
        EnsureDraft();
        if (SelectedBlock == null)
        {
            SetStatus("route.draft.block.missing", "Select a protected block first.");
            return;
        }

        if (!_draftBlocks.Contains(SelectedBlock.Id))
            _draftBlocks.Add(SelectedBlock.Id);
        UpdateDraftSummary();
    }

    [RelayCommand]
    private void AddDraftSignalRequirement()
    {
        EnsureDraft();
        if (SelectedSignal == null)
        {
            SetStatus("route.draft.signal.missing", "Select a protected signal first.");
            return;
        }

        _draftSignals.RemoveAll(item => item.SignalId == SelectedSignal.Id);
        _draftSignals.Add(new RouteSignalRequirement
        {
            SignalId = SelectedSignal.Id,
            ProceedAspect = DraftSignalAspect
        });
        UpdateDraftSummary();
    }

    [RelayCommand]
    private void ValidateRouteDraft()
    {
        var report = ValidateDraft();
        if (report == null)
            return;

        ValidationMessages = report.Findings
            .Select(finding => $"{finding.Code}: {finding.Message}")
            .ToArray();
        OnPropertyChanged(nameof(ValidationMessages));
        SetStatus(
            report.IsValid ? "route.draft.valid" : "route.draft.invalid",
            report.IsValid ? "Route draft is valid." : $"Route draft has {report.Findings.Count} finding(s).");
    }

    [RelayCommand]
    private async Task SaveRouteDraftAsync()
    {
        var project = CurrentProject;
        var report = ValidateDraft();
        if (project == null || report == null)
            return;

        ValidationMessages = report.Findings
            .Select(finding => $"{finding.Code}: {finding.Message}")
            .ToArray();
        OnPropertyChanged(nameof(ValidationMessages));
        if (!report.IsValid)
        {
            SetStatus("route.draft.invalid", "Resolve route validation findings before saving.");
            return;
        }

        var route = CreateDraftRoute();
        project.Interlocking.Routes.Add(route);
        try
        {
            await _runtime.ActivateAsync(project.Interlocking).ConfigureAwait(true);
            await _projectContext.SaveSolutionInternalAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            project.Interlocking.Routes.Remove(route);
            _logger.LogError(ex, "Save interlocking route {RouteId} failed", route.Id);
            SetStatus("route.draft.save-failed", "The route could not be saved.");
            return;
        }

        RefreshDefinitions();
        SelectedRoute = Routes.FirstOrDefault(item => item.Id == route.Id);
        SetStatus("route.draft.saved", $"Route '{route.Name}' saved.");
        _draftRouteId = Guid.Empty;
        UpdateDraftSummary();
    }

    private Guid DraftEntryId { get; set; }

    private Guid DraftExitId { get; set; }

    private Project? CurrentProject => _projectContext.SelectedProject?.Model;

    partial void OnSelectedTurnoutChanged(InterlockingItemViewState? value)
    {
        _ = value;
        UpdateCommandAvailability();
    }

    partial void OnSelectedRouteChanged(InterlockingItemViewState? value)
    {
        _ = value;
        UpdateCommandAvailability();
    }

    partial void OnIsSynchronizedChanged(bool value)
    {
        _ = value;
        UpdateCommandAvailability();
    }

    private void UpdateCommandAvailability()
    {
        CanOperateTurnout = IsSynchronized && SelectedTurnout != null && !SelectedTurnout.IsLocked;
        CanOperateRoute = IsSynchronized && SelectedRoute != null;
        HasSelectedRoute = SelectedRoute != null;
    }

    private async Task SetTurnoutAsync(TurnoutPosition position)
    {
        if (SelectedTurnout == null)
        {
            SetStatus("turnout.selection.missing", "Select a turnout first.");
            return;
        }

        var result = await _runtime.SetTurnoutAsync(
            SelectedTurnout.Id,
            position,
            Guid.NewGuid()).ConfigureAwait(true);
        ApplyResult(result.Code, result.Message, result.State);
    }

    private async Task ExecuteRouteAsync(
        Func<Guid, Guid, CancellationToken, Task<RouteCoordinatorResult>> operation)
    {
        if (SelectedRoute == null)
        {
            SetStatus("route.selection.missing", "Select a route first.");
            return;
        }

        var result = await operation(
            SelectedRoute.Id,
            Guid.NewGuid(),
            CancellationToken.None).ConfigureAwait(true);
        ApplyResult(result.Code, result.Message, result.State);
    }

    private void OnRuntimeSnapshotChanged(InterlockingRuntimeSnapshotChangedEvent @event) =>
        ApplySnapshot(@event.Snapshot, @event.IsSynchronized, @event.Code);

    private void OnProjectContextPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        _ = sender;
        if (args.PropertyName != nameof(IProjectContext.SelectedProject))
            return;

        RefreshDefinitions();
        ApplySnapshot(_runtime.Current, _runtime.IsSynchronized, "interlocking.project.changed");
    }

    private void RefreshDefinitions()
    {
        var definition = CurrentProject?.Interlocking;
        OperationalElements.Clear();
        if (definition == null)
        {
            Turnouts.Clear();
            Blocks.Clear();
            Signals.Clear();
            Routes.Clear();
            return;
        }

        foreach (var turnout in definition.Turnouts)
            OperationalElements.Add(new OperationalElementOption(turnout.Id, turnout.Name, "Turnout"));
        foreach (var signal in definition.Signals)
            OperationalElements.Add(new OperationalElementOption(signal.Id, signal.Name, "Signal"));
        foreach (var block in definition.Blocks)
            OperationalElements.Add(new OperationalElementOption(block.Id, block.Name, "Block"));

        ApplySnapshot(_runtime.Current, _runtime.IsSynchronized, StatusCode);
    }

    private void ApplySnapshot(InterlockingRuntimeState state, bool isSynchronized, string code)
    {
        var definition = CurrentProject?.Interlocking;
        var selectedTurnoutId = SelectedTurnout?.Id;
        var selectedBlockId = SelectedBlock?.Id;
        var selectedSignalId = SelectedSignal?.Id;
        var selectedRouteId = SelectedRoute?.Id;

        Replace(
            Turnouts,
            definition?.Turnouts.Select(item => CreateTurnoutState(item, state)) ?? []);
        Replace(
            Blocks,
            definition?.Blocks.Select(item => CreateBlockState(item, state)) ?? []);
        Replace(
            Signals,
            definition?.Signals.Select(item => CreateSignalState(item, state)) ?? []);
        Replace(
            Routes,
            definition?.Routes.Select(item => CreateRouteState(item, state)) ?? []);

        SelectedTurnout = Turnouts.FirstOrDefault(item => item.Id == selectedTurnoutId);
        SelectedBlock = Blocks.FirstOrDefault(item => item.Id == selectedBlockId);
        SelectedSignal = Signals.FirstOrDefault(item => item.Id == selectedSignalId);
        SelectedRoute = Routes.FirstOrDefault(item => item.Id == selectedRouteId);
        Revision = state.Revision;
        IsSynchronized = isSynchronized;
        StatusCode = code;
        if (code is "interlocking.current" or "interlocking.project.changed")
            StatusText = isSynchronized ? "Interlocking synchronized." : "Interlocking is not synchronized.";
    }

    private void ApplyResult(string code, string message, InterlockingRuntimeState state)
    {
        ApplySnapshot(state, _runtime.IsSynchronized, code);
        StatusText = message;
    }

    private void SelectRepresentation(
        Guid? representationId,
        Func<OperationalBinding, IReadOnlyList<Guid>> selector)
    {
        if (!representationId.HasValue)
            return;

        var binding = CurrentProject?.Interlocking.Bindings
            .FirstOrDefault(item => selector(item).Contains(representationId.Value));
        if (binding == null)
            return;

        SelectOperationalId(binding.OperationalId);
    }

    private InterlockingItemViewState? GetRepresentationState(
        Guid representationId,
        Func<OperationalBinding, IReadOnlyList<Guid>> selector)
    {
        var binding = CurrentProject?.Interlocking.Bindings
            .FirstOrDefault(item => selector(item).Contains(representationId));
        return binding == null ? null : FindState(binding.OperationalId);
    }

    private void SelectOperationalId(Guid operationalId)
    {
        SelectedTurnout = Turnouts.FirstOrDefault(item => item.Id == operationalId);
        SelectedBlock = Blocks.FirstOrDefault(item => item.Id == operationalId);
        SelectedSignal = Signals.FirstOrDefault(item => item.Id == operationalId);
        SelectedRoute = Routes.FirstOrDefault(item => item.Id == operationalId);
        SelectedOperationalElement = OperationalElements.FirstOrDefault(item => item.Id == operationalId);
    }

    private InterlockingItemViewState? FindState(Guid operationalId) =>
        Turnouts.Concat(Blocks).Concat(Signals).Concat(Routes).FirstOrDefault(item => item.Id == operationalId);

    private InterlockingValidationReport? ValidateDraft()
    {
        var project = CurrentProject;
        if (project == null)
        {
            SetStatus("project.selection.missing", "Select a project first.");
            return null;
        }

        EnsureDraft();
        var existingRoutes = project.Interlocking.Routes;
        var route = CreateDraftRoute();
        existingRoutes.Add(route);
        try
        {
            return _validator.Validate(project);
        }
        finally
        {
            existingRoutes.Remove(route);
        }
    }

    private RouteDefinition CreateDraftRoute() =>
        new()
        {
            Id = _draftRouteId,
            Name = DraftName.Trim(),
            EntryElementId = DraftEntryId,
            ExitElementId = DraftExitId,
            PathElementIds = [.. _draftPath],
            TurnoutRequirements = _draftTurnouts.Select(item => new RouteTurnoutRequirement
            {
                TurnoutId = item.TurnoutId,
                Position = item.Position
            }).ToList(),
            ProtectedBlockIds = [.. _draftBlocks],
            SignalRequirements = _draftSignals.Select(item => new RouteSignalRequirement
            {
                SignalId = item.SignalId,
                ProceedAspect = item.ProceedAspect
            }).ToList()
        };

    private void EnsureDraft()
    {
        if (_draftRouteId != Guid.Empty)
            return;

        BeginRouteDraft();
    }

    private void UpdateDraftSummary()
    {
        if (_draftRouteId == Guid.Empty)
        {
            DraftSummary = "No route draft.";
            return;
        }

        DraftSummary =
            $"Entry: {NameOf(DraftEntryId)} | Path: {_draftPath.Count} | Exit: {NameOf(DraftExitId)} | "
            + $"Turnouts: {_draftTurnouts.Count} | Blocks: {_draftBlocks.Count} | Signals: {_draftSignals.Count}";
    }

    private string NameOf(Guid id) =>
        OperationalElements.FirstOrDefault(item => item.Id == id)?.Name ?? "not set";

    private void SetStatus(string code, string message)
    {
        StatusCode = code;
        StatusText = message;
    }

    private static InterlockingItemViewState CreateTurnoutState(
        TurnoutDefinition definition,
        InterlockingRuntimeState state)
    {
        if (!state.Turnouts.TryGetValue(definition.Id, out var runtime))
            return new(definition.Id, definition.Name, "Turnout", UnknownStateText, NoRuntimeStateText, true, false);

        var position = runtime.ConfirmedPosition?.ToString()
            ?? runtime.RequestedPosition?.ToString()
            ?? "unknown position";
        return new(
            definition.Id,
            definition.Name,
            "Turnout",
            runtime.Lifecycle.ToString(),
            $"{position}; {(runtime.LockOwnerRouteId.HasValue ? "route locked" : "unlocked")}",
            runtime.Lifecycle is TurnoutLifecycle.Failed or TurnoutLifecycle.Unknown,
            runtime.LockOwnerRouteId.HasValue);
    }

    private static InterlockingItemViewState CreateBlockState(
        BlockDefinition definition,
        InterlockingRuntimeState state)
    {
        if (!state.Blocks.TryGetValue(definition.Id, out var runtime))
            return new(definition.Id, definition.Name, "Block", UnknownStateText, NoRuntimeStateText, true, false);

        return new(
            definition.Id,
            definition.Name,
            "Block",
            runtime.Occupancy.ToString(),
            runtime.ReservationOwnerRouteId.HasValue ? "Reserved by route" : "Not reserved",
            runtime.Occupancy is BlockOccupancy.Fault or BlockOccupancy.Unknown,
            runtime.ReservationOwnerRouteId.HasValue);
    }

    private static InterlockingItemViewState CreateSignalState(
        SignalDefinition definition,
        InterlockingRuntimeState state)
    {
        if (!state.Signals.TryGetValue(definition.Id, out var runtime))
            return new(definition.Id, definition.Name, "Signal", UnknownStateText, NoRuntimeStateText, true, false);

        return new(
            definition.Id,
            definition.Name,
            "Signal",
            runtime.Aspect.ToString(),
            runtime.LockOwnerRouteId.HasValue ? "Protected by route" : "Not route locked",
            false,
            runtime.LockOwnerRouteId.HasValue);
    }

    private static InterlockingItemViewState CreateRouteState(
        RouteDefinition definition,
        InterlockingRuntimeState state)
    {
        if (!state.Routes.TryGetValue(definition.Id, out var runtime))
            return new(definition.Id, definition.Name, "Route", UnknownStateText, NoRuntimeStateText, true, false);

        return new(
            definition.Id,
            definition.Name,
            "Route",
            runtime.Lifecycle.ToString(),
            runtime.FailureCode ?? "No failure",
            runtime.Lifecycle is RouteLifecycle.Failed or RouteLifecycle.Conflicting,
            runtime.Lifecycle is RouteLifecycle.Setting or RouteLifecycle.Established or RouteLifecycle.Occupied);
    }

    private static void Replace(
        ObservableCollection<InterlockingItemViewState> target,
        IEnumerable<InterlockingItemViewState> values)
    {
        target.Clear();
        foreach (var value in values)
            target.Add(value);
    }
}

/// <summary>
/// Renderer-neutral operational state, including textual cues for non-color-only presentation.
/// </summary>
public sealed record InterlockingItemViewState(
    Guid Id,
    string Name,
    string Kind,
    string State,
    string Detail,
    bool IsFaulted,
    bool IsLocked)
{
    public string AccessibleState => $"{Kind} {Name}: {State}. {Detail}.";
}

/// <summary>
/// Selectable operational identity used by route authoring.
/// </summary>
public sealed record OperationalElementOption(Guid Id, string Name, string Kind)
{
    public string DisplayName => $"{Name} ({Kind})";
}