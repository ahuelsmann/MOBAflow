// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel.Action;

using Domain;
using Domain.Enum;

/// <summary>
/// ViewModel for PowerShell script workflow actions.
/// </summary>
public sealed class PowerShellActionViewModel : WorkflowActionViewModel
{
    public PowerShellActionViewModel(WorkflowAction action) : base(action, ActionType.ExecuteScript)
    {
        action.PowerShell ??= new PowerShellActionPayload();
    }

    private PowerShellActionPayload Payload => UnderlyingAction.PowerShell ??= new PowerShellActionPayload();

    /// <summary>
    /// Gets or sets the script file path.
    /// </summary>
    public string ScriptPath
    {
        get => Payload.ScriptPath ?? string.Empty;
        set
        {
            if (Payload.ScriptPath == value)
                return;
            Payload.ScriptPath = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets arguments passed to the script.
    /// </summary>
    public string Arguments
    {
        get => Payload.Arguments ?? string.Empty;
        set
        {
            if (Payload.Arguments == value)
                return;
            Payload.Arguments = value;
            OnPropertyChanged();
        }
    }
}
