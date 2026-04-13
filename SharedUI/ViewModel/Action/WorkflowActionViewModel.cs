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
        set => SetProperty(_action.Number, value, _action, (a, v) => a.Number = v);
    }

    /// <summary>
    /// Gets or sets the delay (in milliseconds) after this action has executed.
    /// </summary>
    public int DelayAfterMs
    {
        get => _action.DelayAfterMs;
        set => SetProperty(_action.DelayAfterMs, value, _action, (a, v) => a.DelayAfterMs = v);
    }

    /// <summary>
    /// Gets the concrete type of the underlying workflow action.
    /// </summary>
    public ActionType Type => _action.Type;

    /// <summary>
    /// Gets the underlying WorkflowAction (for serialization).
    /// </summary>
    public WorkflowAction ToWorkflowAction() => _action;

    /// <summary>
    /// Gets the wrapped domain action for derived editors (payloads).
    /// </summary>
    protected WorkflowAction UnderlyingAction => _action;
}
