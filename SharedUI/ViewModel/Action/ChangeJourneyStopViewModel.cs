// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel.Action;

using Domain;
using Domain.Enum;

/// <summary>Editor view model for a journey stop transition action.</summary>
public sealed class ChangeJourneyStopViewModel : WorkflowActionViewModel
{
    public ChangeJourneyStopViewModel(WorkflowAction action) : base(action, ActionType.ChangeJourneyStop)
    {
        action.ChangeJourneyStop ??= new ChangeJourneyStopActionPayload();
    }

    private ChangeJourneyStopActionPayload Payload => UnderlyingAction.ChangeJourneyStop ??= new ChangeJourneyStopActionPayload();

    public bool MoveToNextStop
    {
        get => Payload.MoveToNextStop;
        set => SetProperty(Payload.MoveToNextStop, value, Payload, (payload, next) => payload.MoveToNextStop = next);
    }

    public string TargetStationId
    {
        get => Payload.TargetStationId?.ToString() ?? string.Empty;
        set
        {
            Payload.TargetStationId = Guid.TryParse(value, out var id) ? id : null;
            OnPropertyChanged();
        }
    }
}
