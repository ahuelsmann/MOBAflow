// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using Backend.Service.Interlocking;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain;

/// <summary>
/// Selected operational object projected by one page-scoped workbench.
/// </summary>
public enum SelectedOperationalContext
{
    None,
    Unbound,
    Turnout,
    Block,
    Signal,
    Route
}

public sealed partial class InterlockingControlViewModel
{
    private bool _isProjectingSelection;
    private InterlockingRuntimeState _projectedState = InterlockingRuntimeState.Empty;
    private Guid? _lastCorrelationId;
    private DateTime? _lastRuntimeUpdateUtc;

    [ObservableProperty]
    public partial SelectedOperationalContext SelectedContext { get; private set; }

    public InterlockingItemViewState? SelectedObject => SelectedContext switch
    {
        SelectedOperationalContext.Turnout => SelectedTurnout,
        SelectedOperationalContext.Block => SelectedBlock,
        SelectedOperationalContext.Signal => SelectedSignal,
        SelectedOperationalContext.Route => SelectedRoute,
        _ => null
    };

    public string SelectedObjectTitle => SelectedObject?.Name ?? SelectedContext switch
    {
        SelectedOperationalContext.Unbound => "Unbound object",
        _ => "No object selected"
    };

    public string SelectedObjectKind => SelectedObject?.Kind ?? SelectedContext switch
    {
        SelectedOperationalContext.Unbound => "Representation",
        _ => "None"
    };

    public string SelectedObjectState => SelectedObject?.State ?? SelectedContext switch
    {
        SelectedOperationalContext.Unbound => "Read-only",
        _ => "No selection"
    };

    public string SelectedObjectDetail => SelectedObject?.Detail ?? SelectedContext switch
    {
        SelectedOperationalContext.Unbound => "No operational binding",
        _ => "Select an object on the canvas or choose one below."
    };

    public bool HasOperationalSelection => SelectedObject != null;

    public string AvailabilityText => (IsSynchronized, SelectedObject?.IsFaulted) switch
    {
        (false, _) => "Offline",
        (_, true) => "Fault",
        _ => "Synchronized"
    };

    public string AvailabilityDescription =>
        $"{AvailabilityText}. {StatusText}";

    public string SafetySeverity => AvailabilityText switch
    {
        "Fault" => "error",
        "Offline" => "warning",
        _ => "success"
    };

    public string DiagnosticsText =>
        $"Revision {Revision}; status code {StatusCode}; synchronized {IsSynchronized}; "
        + $"correlation {_lastCorrelationId?.ToString("D") ?? "not available"}; "
        + $"updated {_lastRuntimeUpdateUtc?.ToString("O") ?? "not available"}; "
        + $"context {SelectedContext}; state {SelectedObjectKind} {SelectedObjectTitle}: "
        + $"{SelectedObjectState}. {SelectedObjectDetail}; "
        + $"processed correlations {_projectedState.ProcessedCorrelationIds.Count}.";

    public bool IsTurnoutContext => SelectedContext == SelectedOperationalContext.Turnout;

    public bool IsBlockContext => SelectedContext == SelectedOperationalContext.Block;

    public bool IsSignalContext => SelectedContext == SelectedOperationalContext.Signal;

    public bool IsRouteContext => SelectedContext == SelectedOperationalContext.Route;

    public bool IsUnboundContext => SelectedContext == SelectedOperationalContext.Unbound;

    public bool IsStraightActionVisible => SupportsTurnoutPosition(TurnoutPosition.Straight);

    public bool IsDivergingLeftActionVisible => SupportsTurnoutPosition(TurnoutPosition.DivergingLeft);

    public bool IsDivergingRightActionVisible => SupportsTurnoutPosition(TurnoutPosition.DivergingRight);

    public bool HasLiveActionControls =>
        IsRouteContext ||
        IsStraightActionVisible ||
        IsDivergingLeftActionVisible ||
        IsDivergingRightActionVisible;

    public bool ShowNoAuthorizedLiveActionMessage =>
        HasOperationalSelection && !HasLiveActionControls;

    public RouteLifecycle? SelectedRouteLifecycle =>
        SelectedRoute != null &&
        _projectedState.Routes.TryGetValue(SelectedRoute.Id, out var route)
            ? route.Lifecycle
            : null;

    public bool IsPreviewRouteVisible => SelectedRouteLifecycle == RouteLifecycle.Available;

    public bool IsCancelRouteVisible =>
        SelectedRouteLifecycle is RouteLifecycle.Selected or RouteLifecycle.Setting;

    public bool CancelRouteRequiresConfirmation =>
        SelectedRouteLifecycle == RouteLifecycle.Setting;

    public bool IsRoutineCancelRouteVisible =>
        SelectedRouteLifecycle == RouteLifecycle.Selected;

    public bool IsRoutineCancelRouteAvailable =>
        IsSynchronized && IsRoutineCancelRouteVisible;

    public string RoutineCancelRouteDisabledReason => IsRoutineCancelRouteAvailable
        ? "Cancels the selected route before hardware dispatch."
        : "Cancel is unavailable while the interlocking is offline.";

    public bool IsReconcileRouteVisible =>
        SelectedRouteLifecycle is RouteLifecycle.Failed or RouteLifecycle.Conflicting;

    public bool ReconcileRouteRequiresConfirmation => IsReconcileRouteVisible;

    public bool IsSafeStopRouteVisible =>
        SelectedRouteLifecycle is RouteLifecycle.Setting
            or RouteLifecycle.Established
            or RouteLifecycle.Occupied
            or RouteLifecycle.Releasing
            or RouteLifecycle.Failed;

    public bool IsReleaseRouteVisible =>
        SelectedRouteLifecycle is RouteLifecycle.Established or RouteLifecycle.Occupied;

    public bool CanReleaseRoute =>
        IsSynchronized &&
        IsReleaseRouteVisible &&
        AreProtectedBlocksExplicitlyFree();

    public string ReleaseRouteDisabledReason => CanReleaseRoute
        ? string.Empty
        : "Release is available only when every protected block is explicitly free.";

    public string PrimaryRouteActionLabel => PrimaryRouteAction.Label;

    public IAsyncRelayCommand? PrimaryRouteActionCommand => PrimaryRouteAction.Command;

    private RouteActionPresentation PrimaryRouteAction => SelectedRouteLifecycle switch
    {
        RouteLifecycle.Available => new("Select route", SelectRouteCommand),
        RouteLifecycle.Selected => new("Set route", SetRouteCommand),
        RouteLifecycle.Setting => new("Cancel setting", CancelRouteCommand),
        RouteLifecycle.Established or RouteLifecycle.Occupied when CanReleaseRoute =>
            new("Release route", ReleaseRouteCommand),
        RouteLifecycle.Established or RouteLifecycle.Occupied or RouteLifecycle.Releasing =>
            new("Safe stop", SafeStopRouteCommand),
        RouteLifecycle.Failed or RouteLifecycle.Conflicting => new("Reconcile route", ReconcileRouteCommand),
        _ => new("No route action", null)
    };

    public bool IsPrimaryRouteActionAvailable =>
        IsSynchronized && PrimaryRouteActionCommand != null;

    public string PrimaryRouteActionDisabledReason =>
        (IsPrimaryRouteActionAvailable, IsSynchronized) switch
        {
            (true, _) => string.Empty,
            (_, false) => "Live actions are unavailable while the interlocking is offline.",
            _ => "No lifecycle action is available for the selected route."
        };

    partial void OnSelectedTurnoutChanged(InterlockingItemViewState? value) =>
        ProjectDirectSelection(value, SelectedOperationalContext.Turnout);

    partial void OnSelectedBlockChanged(InterlockingItemViewState? value) =>
        ProjectDirectSelection(value, SelectedOperationalContext.Block);

    partial void OnSelectedSignalChanged(InterlockingItemViewState? value) =>
        ProjectDirectSelection(value, SelectedOperationalContext.Signal);

    partial void OnSelectedRouteChanged(InterlockingItemViewState? value) =>
        ProjectDirectSelection(value, SelectedOperationalContext.Route);

    partial void OnSelectedOperationalElementChanged(OperationalElementOption? value)
    {
        if (!_isProjectingSelection && value != null)
            SelectOperationalId(value.Id);
    }

    partial void OnIsSynchronizedChanged(bool value)
    {
        _ = value;
        NotifyPresentationChanged();
    }

    private void UpdateProjectedState(InterlockingRuntimeState state) =>
        _projectedState = state;

    private void ProjectDirectSelection(
        InterlockingItemViewState? value,
        SelectedOperationalContext context)
    {
        if (_isProjectingSelection || value == null)
            return;

        ProjectOperationalSelection(value.Id);
        SelectedContext = context;
        NotifyPresentationChanged();
    }

    private void ProjectOperationalSelection(Guid operationalId)
    {
        _isProjectingSelection = true;
        try
        {
            SelectedTurnout = Turnouts.FirstOrDefault(item => item.Id == operationalId);
            SelectedBlock = Blocks.FirstOrDefault(item => item.Id == operationalId);
            SelectedSignal = Signals.FirstOrDefault(item => item.Id == operationalId);
            SelectedRoute = Routes.FirstOrDefault(item => item.Id == operationalId);
            SelectedOperationalElement = OperationalElements.FirstOrDefault(item => item.Id == operationalId);
            SelectedContext = ResolveSelectedContext();
        }
        finally
        {
            _isProjectingSelection = false;
        }

        NotifyPresentationChanged();
    }

    private SelectedOperationalContext ResolveSelectedContext()
    {
        if (SelectedTurnout != null)
            return SelectedOperationalContext.Turnout;

        if (SelectedBlock != null)
            return SelectedOperationalContext.Block;

        if (SelectedSignal != null)
            return SelectedOperationalContext.Signal;

        return SelectedRoute != null
            ? SelectedOperationalContext.Route
            : SelectedOperationalContext.Unbound;
    }

    private void ClearOperationalSelection(SelectedOperationalContext context)
    {
        _isProjectingSelection = true;
        try
        {
            SelectedTurnout = null;
            SelectedBlock = null;
            SelectedSignal = null;
            SelectedRoute = null;
            SelectedOperationalElement = null;
            SelectedContext = context;
        }
        finally
        {
            _isProjectingSelection = false;
        }

        NotifyPresentationChanged();
    }

    private void UpdateSelectedContextAfterSnapshot()
    {
        if (SelectedObject != null)
        {
            ProjectOperationalSelection(SelectedObject.Id);
            return;
        }

        NotifyPresentationChanged();
    }

    private bool SupportsTurnoutPosition(TurnoutPosition position)
    {
        if (SelectedTurnout == null)
            return false;

        return CurrentProject?.Interlocking.Turnouts
            .FirstOrDefault(turnout => turnout.Id == SelectedTurnout.Id)?
            .Commands.Any(command => command.Position == position) == true;
    }

    private bool AreProtectedBlocksExplicitlyFree()
    {
        if (SelectedRoute == null)
            return false;

        var route = CurrentProject?.Interlocking.Routes
            .FirstOrDefault(candidate => candidate.Id == SelectedRoute.Id);
        return route != null &&
            route.ProtectedBlockIds.All(blockId =>
                _projectedState.Blocks.TryGetValue(blockId, out var block) &&
                block.Occupancy == BlockOccupancy.Free);
    }

    private void NotifyPresentationChanged()
    {
        OnPropertyChanged(nameof(SelectedObject));
        OnPropertyChanged(nameof(SelectedObjectTitle));
        OnPropertyChanged(nameof(SelectedObjectKind));
        OnPropertyChanged(nameof(SelectedObjectState));
        OnPropertyChanged(nameof(SelectedObjectDetail));
        OnPropertyChanged(nameof(HasOperationalSelection));
        OnPropertyChanged(nameof(AvailabilityText));
        OnPropertyChanged(nameof(AvailabilityDescription));
        OnPropertyChanged(nameof(SafetySeverity));
        OnPropertyChanged(nameof(DiagnosticsText));
        OnPropertyChanged(nameof(IsTurnoutContext));
        OnPropertyChanged(nameof(IsBlockContext));
        OnPropertyChanged(nameof(IsSignalContext));
        OnPropertyChanged(nameof(IsRouteContext));
        OnPropertyChanged(nameof(IsUnboundContext));
        OnPropertyChanged(nameof(IsStraightActionVisible));
        OnPropertyChanged(nameof(IsDivergingLeftActionVisible));
        OnPropertyChanged(nameof(IsDivergingRightActionVisible));
        OnPropertyChanged(nameof(HasLiveActionControls));
        OnPropertyChanged(nameof(ShowNoAuthorizedLiveActionMessage));
        OnPropertyChanged(nameof(SelectedRouteLifecycle));
        OnPropertyChanged(nameof(IsPreviewRouteVisible));
        OnPropertyChanged(nameof(IsCancelRouteVisible));
        OnPropertyChanged(nameof(CancelRouteRequiresConfirmation));
        OnPropertyChanged(nameof(IsRoutineCancelRouteVisible));
        OnPropertyChanged(nameof(IsRoutineCancelRouteAvailable));
        OnPropertyChanged(nameof(RoutineCancelRouteDisabledReason));
        OnPropertyChanged(nameof(IsReconcileRouteVisible));
        OnPropertyChanged(nameof(ReconcileRouteRequiresConfirmation));
        OnPropertyChanged(nameof(IsSafeStopRouteVisible));
        OnPropertyChanged(nameof(IsReleaseRouteVisible));
        OnPropertyChanged(nameof(CanReleaseRoute));
        OnPropertyChanged(nameof(ReleaseRouteDisabledReason));
        OnPropertyChanged(nameof(PrimaryRouteActionLabel));
        OnPropertyChanged(nameof(PrimaryRouteActionCommand));
        OnPropertyChanged(nameof(IsPrimaryRouteActionAvailable));
        OnPropertyChanged(nameof(PrimaryRouteActionDisabledReason));
        CancelRouteCommand.NotifyCanExecuteChanged();
        ReleaseRouteCommand.NotifyCanExecuteChanged();
        SafeStopRouteCommand.NotifyCanExecuteChanged();
        ReconcileRouteCommand.NotifyCanExecuteChanged();
    }

    private readonly record struct RouteActionPresentation(string Label, IAsyncRelayCommand? Command);
}
