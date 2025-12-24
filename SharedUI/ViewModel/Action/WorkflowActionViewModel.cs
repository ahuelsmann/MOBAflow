// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
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

    protected WorkflowActionViewModel(WorkflowAction action, ActionType type)
    {
        _action = action;
        _action.Type = type;
        _action.Parameters ??= new Dictionary<string, object>();
    }

    public Guid Id
    {
        get => _action.Id;
        set => SetProperty(_action.Id, value, _action, (a, v) => a.Id = v);
    }

    public string Name
    {
        get => _action.Name;
        set => SetProperty(_action.Name, value, _action, (a, v) => a.Name = v);
    }

    public uint Number
    {
        get => _action.Number;
        set => SetProperty(_action.Number, value, _action, (a, v) => a.Number = v);
    }

    public ActionType Type => _action.Type;

    /// <summary>
    /// Gets the underlying WorkflowAction (for serialization).
    /// </summary>
    public WorkflowAction ToWorkflowAction() => _action;

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

    protected void SetParameter<T>(string key, T value)
    {
        _action.Parameters ??= new Dictionary<string, object>();
        
        if (EqualityComparer<T>.Default.Equals(value, GetParameter<T>(key)))
            return;
        
        if (value != null)
            _action.Parameters[key] = value;
        else
            _action.Parameters.Remove(key);
        
        OnPropertyChanged(key);
    }
}
