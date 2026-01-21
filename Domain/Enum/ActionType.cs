// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.Enum;

/// <summary>
/// Represents the type of workflow action to execute.
/// </summary>
public enum ActionType
{
    /// <summary>Play a spoken announcement.</summary>
    Announcement,

    /// <summary>Send a digital command to the control unit (e.g., Z21).</summary>
    Command,

    /// <summary>Play an audio file.</summary>
    Audio,

    /// <summary>Execute a PowerShell script.</summary>
    ExecutePowerShellScript,
}