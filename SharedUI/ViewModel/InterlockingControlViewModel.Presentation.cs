// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using Backend.Service.Interlocking;
using CommunityToolkit.Mvvm.ComponentModel;
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
    Signal
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
        (false, _) => "Incomplete",
        (_, true) => "Fault",
        _ => "Synchronized"
    };

    public string AvailabilityDescription =>
        $"{AvailabilityText}. {StatusText}";

    public string SafetySeverity => AvailabilityText switch
    {
        "Fault" => "error",
        "Incomplete" => "warning",
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

    public bool IsUnboundContext => SelectedContext == SelectedOperationalContext.Unbound;

    public bool IsStraightActionVisible => SupportsTurnoutPosition(TurnoutPosition.Straight);

    public bool IsDivergingLeftActionVisible => SupportsTurnoutPosition(TurnoutPosition.DivergingLeft);

    public bool IsDivergingRightActionVisible => SupportsTurnoutPosition(TurnoutPosition.DivergingRight);

    public bool HasLiveActionControls =>
        IsStraightActionVisible ||
        IsDivergingLeftActionVisible ||
        IsDivergingRightActionVisible;

    public bool ShowNoAuthorizedLiveActionMessage =>
        HasOperationalSelection && !HasLiveActionControls;

    partial void OnSelectedTurnoutChanged(InterlockingItemViewState? value) =>
        ProjectDirectSelection(value, SelectedOperationalContext.Turnout);

    partial void OnSelectedBlockChanged(InterlockingItemViewState? value) =>
        ProjectDirectSelection(value, SelectedOperationalContext.Block);

    partial void OnSelectedSignalChanged(InterlockingItemViewState? value) =>
        ProjectDirectSelection(value, SelectedOperationalContext.Signal);

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

        return SelectedOperationalContext.Unbound;
    }

    private void ClearOperationalSelection(SelectedOperationalContext context)
    {
        _isProjectingSelection = true;
        try
        {
            SelectedTurnout = null;
            SelectedBlock = null;
            SelectedSignal = null;
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
        OnPropertyChanged(nameof(IsUnboundContext));
        OnPropertyChanged(nameof(IsStraightActionVisible));
        OnPropertyChanged(nameof(IsDivergingLeftActionVisible));
        OnPropertyChanged(nameof(IsDivergingRightActionVisible));
        OnPropertyChanged(nameof(HasLiveActionControls));
        OnPropertyChanged(nameof(ShowNoAuthorizedLiveActionMessage));
    }
}
