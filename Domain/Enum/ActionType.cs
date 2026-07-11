// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.Enum;

/// <summary>
/// Represents the type of workflow action to execute.
/// </summary>
public enum ActionType
{
    /// <summary>Play a spoken announcement.</summary>
    Announcement,

    /// <summary>Play an audio file.</summary>
    Audio,

    /// <summary>Send a digital command to the control unit (e.g., Z21).</summary>
    Command,

    /// <summary>Execute a PowerShell script.</summary>
    ExecuteScript,

    /// <summary>Select matrix aspect.</summary>
    Matrix,

    /// <summary>Select signal aspect.</summary>
    SelectSignalAspect,

    /// <summary>Refresh train destination display.</summary>
    TrainDestinationDisplay,

    /// <summary>Changes the current stop of the active journey.</summary>
    ChangeJourneyStop,
}
