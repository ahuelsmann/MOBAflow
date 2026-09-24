// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using Backend.Events;
using Backend.Interface;
using Backend.Service.Interlocking;
using Common.Events;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain;
using Interface;
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
    private readonly IUiDispatcher _uiDispatcher;
    private Guid? _runtimeSubscriptionId;

    public InterlockingControlViewModel(
        IInterlockingRuntime runtime,
        IEventBus eventBus,
        IProjectContext projectContext,
        IUiDispatcher uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(projectContext);
        ArgumentNullException.ThrowIfNull(uiDispatcher);

        _runtime = runtime;
        _eventBus = eventBus;
        _projectContext = projectContext;
        _uiDispatcher = uiDispatcher;
        RefreshDefinitions();
        ApplySnapshot(_runtime.Current, _runtime.IsSynchronized, "interlocking.current");
    }

    public ObservableCollection<InterlockingItemViewState> Turnouts { get; } = [];

    public ObservableCollection<InterlockingItemViewState> Blocks { get; } = [];

    public ObservableCollection<InterlockingItemViewState> Signals { get; } = [];

    public ObservableCollection<OperationalElementOption> OperationalElements { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanOperateTurnout))]
    public partial InterlockingItemViewState? SelectedTurnout { get; set; }

    [ObservableProperty]
    public partial InterlockingItemViewState? SelectedBlock { get; set; }

    [ObservableProperty]
    public partial InterlockingItemViewState? SelectedSignal { get; set; }

    [ObservableProperty]
    public partial OperationalElementOption? SelectedOperationalElement { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanOperateTurnout))]
    public partial bool IsSynchronized { get; set; }

    [ObservableProperty]
    public partial long Revision { get; set; }

    private string _statusText = "Layout observations are incomplete.";

    private string _statusCode = "interlocking.unsynchronized";

    public bool CanOperateTurnout => SelectedTurnout != null &&
        SelectedTurnout.State is not "Requested" and not "Pending";

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

    private Project? CurrentProject => _projectContext.SelectedProject?.Model;

    private async Task SetTurnoutAsync(TurnoutPosition position)
    {
        if (SelectedTurnout == null)
        {
            SetStatus("turnout.selection.missing", "Select a turnout first.");
            return;
        }

        var selectedTurnoutId = SelectedTurnout.Id;
        var correlationId = Guid.NewGuid();
        var result = await _runtime.SetTurnoutAsync(
            selectedTurnoutId,
            position,
            correlationId).ConfigureAwait(false);
        await ApplyResultOnUiAsync(result.Code, result.Message, result.State, result.CorrelationId)
            .ConfigureAwait(false);
    }

    private void OnRuntimeSnapshotChanged(InterlockingRuntimeSnapshotChangedEvent @event)
    {
        _lastCorrelationId = @event.CorrelationId;
        _lastRuntimeUpdateUtc = @event.CreatedUtc;
        ApplySnapshot(@event.Snapshot, @event.IsSynchronized, @event.Code);
    }

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
        UpdateProjectedState(state);
        var definition = CurrentProject?.Interlocking;
        var selectedTurnoutId = SelectedTurnout?.Id;
        var selectedBlockId = SelectedBlock?.Id;
        var selectedSignalId = SelectedSignal?.Id;

        Replace(
            Turnouts,
            definition?.Turnouts.Select(item => CreateTurnoutState(item, state)) ?? []);
        Replace(
            Blocks,
            definition?.Blocks.Select(item => CreateBlockState(item, state)) ?? []);
        Replace(
            Signals,
            definition?.Signals.Select(item => CreateSignalState(item, state)) ?? []);

        SelectedTurnout = Turnouts.FirstOrDefault(item => item.Id == selectedTurnoutId);
        SelectedBlock = Blocks.FirstOrDefault(item => item.Id == selectedBlockId);
        SelectedSignal = Signals.FirstOrDefault(item => item.Id == selectedSignalId);
        Revision = state.Revision;
        IsSynchronized = isSynchronized;
        StatusCode = code;
        if (code is "interlocking.current" or "interlocking.project.changed")
            StatusText = isSynchronized ? "Layout observations synchronized." : "Layout observations are incomplete.";
        UpdateSelectedContextAfterSnapshot();
    }

    private Task ApplyResultOnUiAsync(
        string code,
        string message,
        InterlockingRuntimeState state,
        Guid correlationId) =>
        _uiDispatcher.InvokeOnUiAsync(() =>
        {
            _lastCorrelationId = correlationId;
            _lastRuntimeUpdateUtc = DateTime.UtcNow;
            ApplySnapshot(state, _runtime.IsSynchronized, code);
            StatusText = message;
            return Task.CompletedTask;
        });

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
        Turnouts.Concat(Blocks).Concat(Signals).FirstOrDefault(item => item.Id == operationalId);

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
            return new(definition.Id, definition.Name, "Turnout", UnknownStateText, NoRuntimeStateText, true);

        var position = runtime.ConfirmedPosition?.ToString()
            ?? runtime.RequestedPosition?.ToString()
            ?? "unknown position";
        return new(
            definition.Id,
            definition.Name,
            "Turnout",
            runtime.Lifecycle.ToString(),
            position,
            runtime.Lifecycle is TurnoutLifecycle.Failed or TurnoutLifecycle.Unknown);
    }

    private static InterlockingItemViewState CreateBlockState(
        BlockDefinition definition,
        InterlockingRuntimeState state)
    {
        if (!state.Blocks.TryGetValue(definition.Id, out var runtime))
            return new(definition.Id, definition.Name, "Block", UnknownStateText, NoRuntimeStateText, true);

        return new(
            definition.Id,
            definition.Name,
            "Block",
            runtime.Occupancy.ToString(),
            "Observed block occupancy",
            runtime.Occupancy is BlockOccupancy.Fault or BlockOccupancy.Unknown);
    }

    private static InterlockingItemViewState CreateSignalState(
        SignalDefinition definition,
        InterlockingRuntimeState state)
    {
        if (!state.Signals.TryGetValue(definition.Id, out var runtime))
            return new(definition.Id, definition.Name, "Signal", UnknownStateText, NoRuntimeStateText, true);

        return new(
            definition.Id,
            definition.Name,
            "Signal",
            runtime.Aspect?.ToString() ?? UnknownStateText,
            "Use the existing signal controls to select an aspect.",
            false);
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
    bool IsFaulted)
{
    public string AccessibleState => $"{Kind} {Name}: {State}. {Detail}.";
}

/// <summary>
/// Selectable operational identity shared by the layout pages.
/// </summary>
public sealed record OperationalElementOption(Guid Id, string Name, string Kind)
{
    public string DisplayName => $"{Name} ({Kind})";
}
