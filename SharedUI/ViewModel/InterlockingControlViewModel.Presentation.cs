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

    public string AvailabilityText => !IsSynchronized
        ? "Offline"
        : SelectedObject?.IsFaulted == true
            ? "Fault"
            : "Synchronized";

    public string AvailabilityDescription =>
        $"{AvailabilityText}. {StatusText}";

    public string SafetySeverity => AvailabilityText switch
    {
        "Fault" => "error",
        "Offline" => "warning",
        _ => "success"
    };

    public string DiagnosticsText =>
        $"Revision {Revision}; status code {StatusCode}; context {SelectedContext}.";

    public bool IsTurnoutContext => SelectedContext == SelectedOperationalContext.Turnout;

    public bool IsBlockContext => SelectedContext == SelectedOperationalContext.Block;

    public bool IsSignalContext => SelectedContext == SelectedOperationalContext.Signal;

    public bool IsRouteContext => SelectedContext == SelectedOperationalContext.Route;

    public bool IsUnboundContext => SelectedContext == SelectedOperationalContext.Unbound;

    public bool IsStraightActionVisible => SupportsTurnoutPosition(TurnoutPosition.Straight);

    public bool IsDivergingLeftActionVisible => SupportsTurnoutPosition(TurnoutPosition.DivergingLeft);

    public bool IsDivergingRightActionVisible => SupportsTurnoutPosition(TurnoutPosition.DivergingRight);

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

    public string PrimaryRouteActionLabel => SelectedRouteLifecycle switch
    {
        RouteLifecycle.Available => "Select route",
        RouteLifecycle.Selected => "Set route",
        RouteLifecycle.Setting => "Cancel setting",
        RouteLifecycle.Established or RouteLifecycle.Occupied when CanReleaseRoute => "Release route",
        RouteLifecycle.Established or RouteLifecycle.Occupied or RouteLifecycle.Releasing => "Safe stop",
        RouteLifecycle.Failed or RouteLifecycle.Conflicting => "Reconcile route",
        _ => "No route action"
    };

    public IAsyncRelayCommand? PrimaryRouteActionCommand => SelectedRouteLifecycle switch
    {
        RouteLifecycle.Available => SelectRouteCommand,
        RouteLifecycle.Selected => SetRouteCommand,
        RouteLifecycle.Setting => CancelRouteCommand,
        RouteLifecycle.Established or RouteLifecycle.Occupied when CanReleaseRoute => ReleaseRouteCommand,
        RouteLifecycle.Established or RouteLifecycle.Occupied or RouteLifecycle.Releasing => SafeStopRouteCommand,
        RouteLifecycle.Failed or RouteLifecycle.Conflicting => ReconcileRouteCommand,
        _ => null
    };

    public bool IsPrimaryRouteActionAvailable =>
        IsSynchronized && PrimaryRouteActionCommand != null;

    public string PrimaryRouteActionDisabledReason => IsPrimaryRouteActionAvailable
        ? string.Empty
        : !IsSynchronized
            ? "Live actions are unavailable while the interlocking is offline."
            : "No lifecycle action is available for the selected route.";

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
            SelectedContext = SelectedTurnout != null
                ? SelectedOperationalContext.Turnout
                : SelectedBlock != null
                    ? SelectedOperationalContext.Block
                    : SelectedSignal != null
                        ? SelectedOperationalContext.Signal
                        : SelectedRoute != null
                            ? SelectedOperationalContext.Route
                            : SelectedOperationalContext.Unbound;
        }
        finally
        {
            _isProjectingSelection = false;
        }

        NotifyPresentationChanged();
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
        OnPropertyChanged(nameof(SelectedRouteLifecycle));
        OnPropertyChanged(nameof(IsPreviewRouteVisible));
        OnPropertyChanged(nameof(IsCancelRouteVisible));
        OnPropertyChanged(nameof(CancelRouteRequiresConfirmation));
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
}
