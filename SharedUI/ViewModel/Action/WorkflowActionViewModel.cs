// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel.Action;

using CommunityToolkit.Mvvm.ComponentModel;

using Domain;
using Domain.Enum;

/// <summary>
/// Base class for Action ViewModels that wrap WorkflowAction.
/// Provides common fields shared by concrete workflow action editors.
/// </summary>
public abstract class WorkflowActionViewModel : ObservableObject
{
    #region Fields
    // Model
    private readonly WorkflowAction _action;
    private double _canvasX;
    private double _canvasY;
    private bool _hasCustomPosition;
    private bool _hasValidationIssue;
    private string _validationMessage = string.Empty;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowActionViewModel"/> base class.
    /// </summary>
    /// <param name="action">The underlying workflow action model.</param>
    /// <param name="type">The concrete action type represented by the derived ViewModel.</param>
    protected WorkflowActionViewModel(WorkflowAction action, ActionType type)
    {
        ArgumentNullException.ThrowIfNull(action);
        _action = action;
        _action.Type = type;
    }

    /// <summary>
    /// Gets or sets the unique identifier of the underlying workflow action.
    /// </summary>
    public Guid Id
    {
        get => _action.Id;
        set => SetProperty(_action.Id, value, _action, (a, v) => a.Id = v);
    }

    /// <summary>
    /// Gets or sets the display name of the action.
    /// </summary>
    public string Name
    {
        get => _action.Name;
        set => SetProperty(_action.Name, value, _action, (a, v) => a.Name = v);
    }

    /// <summary>
    /// Gets or sets the sequential number of the action within its workflow.
    /// </summary>
    public uint Number
    {
        get => _action.Number;
        set
        {
            if (SetProperty(_action.Number, value, _action, (a, v) => a.Number = v))
            {
                OnPropertyChanged(nameof(StepLabel));
            }
        }
    }

    /// <summary>
    /// Gets or sets the delay (in milliseconds) after this action has executed.
    /// </summary>
    public int DelayAfterMs
    {
        get => _action.DelayAfterMs;
        set
        {
            if (SetProperty(_action.DelayAfterMs, value, _action, (a, v) => a.DelayAfterMs = v))
            {
                OnPropertyChanged(nameof(HasDelayAfter));
                OnPropertyChanged(nameof(DelayBadgeText));
            }
        }
    }

    /// <summary>
    /// Gets the concrete type of the underlying workflow action.
    /// </summary>
    public ActionType Type => _action.Type;

    /// <summary>
    /// Gets the formatted step badge label (e.g. "Step #1").
    /// </summary>
    public string StepLabel => $"Step #{Number}";

    /// <summary>
    /// Gets whether a post-action delay is configured on this step.
    /// </summary>
    public bool HasDelayAfter => DelayAfterMs > 0;

    /// <summary>
    /// Gets the formatted delay badge text shown on graph connectors when <see cref="DelayAfterMs"/> is positive.
    /// </summary>
    public string DelayBadgeText => DelayAfterMs > 0 ? $"+{DelayAfterMs} ms" : string.Empty;

    /// <summary>
    /// Gets a human-readable display label for <see cref="Type"/>.
    /// </summary>
    public string TypeDisplayName => Type switch
    {
        ActionType.Announcement => "Announcement",
        ActionType.Audio => "Audio Playback",
        ActionType.Command => "DCC Command",
        ActionType.ExecuteScript => "PowerShell Script",
        ActionType.SelectSignalAspect => "Signal Aspect",
        ActionType.TrainDestinationDisplay => "Destination Display",
        ActionType.ChangeJourneyStop => "Journey Stop",
        _ => "Invalid Action"
    };

    /// <summary>
    /// Gets the Fluent glyph icon representing <see cref="Type"/> on graph node cards.
    /// </summary>
    public string IconGlyph => Type switch
    {
        ActionType.Announcement => "\uE720",
        ActionType.Audio => "\uE8D6",
        ActionType.Command => "\uE945",
        ActionType.ExecuteScript => "\uE756",
        ActionType.SelectSignalAspect => "\uE819",
        ActionType.TrainDestinationDisplay => "\uE7F4",
        ActionType.ChangeJourneyStop => "\uE815",
        _ => "\uE783"
    };

    /// <summary>
    /// Gets a concise parameter summary pill text for graph node cards.
    /// </summary>
    public virtual string ParameterSummary => TypeDisplayName;

    /// <summary>
    /// Gets or sets the horizontal position of the node card on the workflow graph canvas.
    /// </summary>
    public double CanvasX
    {
        get => _canvasX;
        set => SetProperty(ref _canvasX, value);
    }

    /// <summary>
    /// Gets or sets the vertical position of the node card on the workflow graph canvas.
    /// </summary>
    public double CanvasY
    {
        get => _canvasY;
        set => SetProperty(ref _canvasY, value);
    }

    /// <summary>
    /// Gets or sets whether the operator manually positioned this node on the graph canvas.
    /// </summary>
    public bool HasCustomPosition
    {
        get => _hasCustomPosition;
        set => SetProperty(ref _hasCustomPosition, value);
    }

    /// <summary>
    /// Gets or sets whether the workflow validator reported an issue for this step.
    /// </summary>
    public bool HasValidationIssue
    {
        get => _hasValidationIssue;
        set
        {
            if (SetProperty(ref _hasValidationIssue, value))
            {
                OnPropertyChanged(nameof(IsValidAction));
            }
        }
    }

    /// <summary>
    /// Gets or sets the validation message associated with this step, if any.
    /// </summary>
    public string ValidationMessage
    {
        get => _validationMessage;
        set => SetProperty(ref _validationMessage, value);
    }

    /// <summary>
    /// Gets whether the action has a valid payload and no active validation issues.
    /// </summary>
    public bool IsValidAction => _action.HasPayload && !_hasValidationIssue;

    /// <summary>
    /// Updates the node card's 2D canvas coordinates.
    /// </summary>
    public void SetCanvasPosition(double x, double y, bool isCustomPosition = false)
    {
        CanvasX = x;
        CanvasY = y;
        HasCustomPosition = isCustomPosition;
    }

    /// <summary>
    /// Gets the underlying WorkflowAction (for serialization).
    /// </summary>
    public WorkflowAction ToWorkflowAction() => _action;

    /// <summary>
    /// Gets the wrapped domain action for derived editors (payloads).
    /// </summary>
    protected WorkflowAction UnderlyingAction => _action;
}
