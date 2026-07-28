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

    private static readonly Action<ILogger, Guid, Exception?> LogRoutePersistenceFailure =
        LoggerMessage.Define<Guid>(
            LogLevel.Error,
            new EventId(1, nameof(LogRoutePersistenceFailure)),
            "Persist interlocking route {RouteId} failed");

    private static readonly Action<ILogger, Guid, Exception?> LogRouteActivationFailure =
        LoggerMessage.Define<Guid>(
            LogLevel.Error,
            new EventId(2, nameof(LogRouteActivationFailure)),
            "Activate persisted interlocking route {RouteId} failed");

    private readonly IInterlockingRuntime _runtime;
    private readonly IEventBus _eventBus;
    private readonly IProjectContext _projectContext;
    private readonly IInterlockingDefinitionValidator _validator;
    private readonly IDialogService _dialogService;
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
        IDialogService dialogService,
        ILogger<InterlockingControlViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(projectContext);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(logger);

        _runtime = runtime;
        _eventBus = eventBus;
        _projectContext = projectContext;
        _validator = validator;
        _dialogService = dialogService;
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
    [NotifyPropertyChangedFor(nameof(CanOperateTurnout))]
    public partial InterlockingItemViewState? SelectedTurnout { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanOperateRoute))]
    [NotifyPropertyChangedFor(nameof(HasSelectedRoute))]
    public partial InterlockingItemViewState? SelectedRoute { get; set; }

    [ObservableProperty]
    public partial InterlockingItemViewState? SelectedBlock { get; set; }

    [ObservableProperty]
    public partial InterlockingItemViewState? SelectedSignal { get; set; }

    [ObservableProperty]
    public partial OperationalElementOption? SelectedOperationalElement { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanOperateTurnout))]
    [NotifyPropertyChangedFor(nameof(CanOperateRoute))]
    public partial bool IsSynchronized { get; set; }

    [ObservableProperty]
    public partial long Revision { get; set; }

    private string _statusText = "Interlocking is not synchronized.";

    private string _statusCode = "interlocking.unsynchronized";

    [ObservableProperty]
    public partial string DraftName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial TurnoutPosition DraftTurnoutPosition { get; set; } = TurnoutPosition.Straight;

    [ObservableProperty]
    public partial SignalAspect DraftSignalAspect { get; set; } = SignalAspect.Ks1;

    [ObservableProperty]
    public partial string DraftSummary { get; set; } = "No route draft.";

    public bool CanOperateTurnout => IsSynchronized && SelectedTurnout != null && !SelectedTurnout.IsLocked;

    public bool CanOperateRoute => IsSynchronized && SelectedRoute != null;

    public bool HasSelectedRoute => SelectedRoute != null;

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (SetProperty(ref _statusText, value))
                OnPropertyChanged(nameof(AvailabilityDescription));
        }
    }

    public string StatusCode
    {
        get => _statusCode;
        private set
        {
            if (SetProperty(ref _statusCode, value))
                OnPropertyChanged(nameof(DiagnosticsText));
        }
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

    [RelayCommand(CanExecute = nameof(CanCancelSelectedRoute))]
    private async Task CancelRouteAsync()
    {
        if (SelectedRoute == null)
        {
            SetStatus("route.selection.missing", "Select a route first.");
            return;
        }

        if (SelectedRouteLifecycle == RouteLifecycle.Setting)
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Cancel route setting?",
                $"Cancel setting '{SelectedRoute.Name}'? The route becomes failed, protected signals remain at stop, "
                + "and uncertain resources remain locked until reconciliation.",
                "Cancel setting",
                "Keep setting").ConfigureAwait(true);
            if (!confirmed)
                return;
        }

        await ExecuteRouteAsync(_runtime.CancelRouteAsync).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanReleaseSelectedRoute))]
    private Task ReleaseRouteAsync() => ExecuteRouteAsync(_runtime.ReleaseRouteAsync);

    [RelayCommand(CanExecute = nameof(CanSafeStopSelectedRoute))]
    private Task SafeStopRouteAsync() => ExecuteRouteAsync(_runtime.SafeStopRouteAsync);

    [RelayCommand(CanExecute = nameof(CanReconcileSelectedRoute))]
    private async Task ReconcileRouteAsync()
    {
        if (SelectedRoute == null)
        {
            SetStatus("route.selection.missing", "Select a route first.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Reconcile route?",
            $"Reconcile '{SelectedRoute.Name}'? Successful reconciliation may release retained locks and "
            + "reservations after failed, timed-out, or late hardware feedback.",
            "Reconcile",
            "Keep locked").ConfigureAwait(true);
        if (!confirmed)
            return;

        await ExecuteRouteAsync(_runtime.ReconcileRouteAsync).ConfigureAwait(true);
    }

    private bool CanCancelSelectedRoute() => IsCancelRouteVisible;

    private bool CanReleaseSelectedRoute() => CanReleaseRoute;

    private bool CanSafeStopSelectedRoute() => IsSafeStopRouteVisible;

    private bool CanReconcileSelectedRoute() => IsReconcileRouteVisible;

    [RelayCommand]
    private void BeginRouteDraft()
    {
        _draftRouteId = Guid.NewGuid();
        DraftEntryId = Guid.Empty;
        DraftExitId = Guid.Empty;
        _draftPath.Clear();
        _draftTurnouts.Clear();
        _draftBlocks.Clear();
        _draftSignals.Clear();
        ValidationMessages = [];
        OnPropertyChanged(nameof(ValidationMessages));
        DraftName = "New route";
        UpdateDraftSummary();
        SetStatus("route.draft.started", "Route draft started.");
        ValidateAndRequestDefinitionSave();
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
        ValidateAndRequestDefinitionSave();
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
        ValidateAndRequestDefinitionSave();
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
        ValidateAndRequestDefinitionSave();
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
        ValidateAndRequestDefinitionSave();
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
        ValidateAndRequestDefinitionSave();
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
        ValidateAndRequestDefinitionSave();
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

    private Guid DraftEntryId { get; set; }

    private Guid DraftExitId { get; set; }

    private Project? CurrentProject => _projectContext.SelectedProject?.Model;

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
        foreach (var route in definition.Routes)
            OperationalElements.Add(new OperationalElementOption(route.Id, route.Name, "Route"));

        ApplySnapshot(_runtime.Current, _runtime.IsSynchronized, StatusCode);
    }

    private void ApplySnapshot(InterlockingRuntimeState state, bool isSynchronized, string code)
    {
        UpdateProjectedState(state);
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
        UpdateSelectedContextAfterSnapshot();
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
        ClearOperationalSelection(
            representationId.HasValue
                ? SelectedOperationalContext.Unbound
                : SelectedOperationalContext.None);
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
        ProjectOperationalSelection(operationalId);
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
        var route = CreateDraftRoute();
        return ValidateRouteCandidate(project, route);
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
