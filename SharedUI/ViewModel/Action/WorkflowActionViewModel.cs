// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel.Action;

using CommunityToolkit.Mvvm.ComponentModel;
using Domain;
using Domain.Enum;

/// <summary>
/// Base class for Action ViewModels that wrap WorkflowAction.
/// Provides common functionality for parameter management.
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
        _action.Parameters ??= [];
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
    /// Gets a typed parameter value from the underlying workflow action.
    /// </summary>
    /// <typeparam name="T">The expected parameter type.</typeparam>
    /// <param name="key">The parameter key.</param>
    /// <returns>The parameter value converted to <typeparamref name="T"/>, or the default value of <typeparamref name="T"/> when not present.</returns>
    protected T? GetParameter<T>(string key)
    {
        if (_action.Parameters?.TryGetValue(key, out var value) == true)
        {
            if (value is T typedValue)
                return typedValue;
            
            // Handle type conversions (e.g., long → int, string → enum)
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }
        return default;
    }

    /// <summary>
    /// Sets a typed parameter value on the underlying workflow action.
    /// </summary>
    /// <typeparam name="T">The parameter type.</typeparam>
    /// <param name="key">The parameter key.</param>
    /// <param name="value">The value to store; <c>null</c> removes the parameter.</param>
    protected void SetParameter<T>(string key, T value)
    {
        _action.Parameters ??= [];
        
        if (EqualityComparer<T>.Default.Equals(value, GetParameter<T>(key)))
            return;
        
        if (value != null)
            _action.Parameters[key] = value;
        else
            _action.Parameters.Remove(key);
        
        OnPropertyChanged(key);
    }
}
