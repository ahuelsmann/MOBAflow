// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Interface;

using Domain;

/// <summary>
/// Service for logging trips and stop times via the TrainControlPage.
/// Calls RecordStateChange on each change of speed or DCC address.
/// </summary>
public interface ITripLogService
{
    /// <summary>
    /// Raised when a new trip log entry has been added.
    /// Enables UI updates without polling.
    /// </summary>
    event EventHandler? EntryAdded;

    /// <summary>
    /// Records a state change (address or speed).
    /// No logging when project is null.
    /// </summary>
    /// <param name="project">Current project (null = no logging)</param>
    /// <param name="locoAddress">DCC address of the locomotive</param>
    /// <param name="speed">Speed (0–126, 0 = stop)</param>
    /// <param name="timestamp">Time of change (UTC recommended)</param>
    void RecordStateChange(Project? project, int locoAddress, int speed, DateTime timestamp);
}
