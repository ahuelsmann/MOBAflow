// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Services;

/// <summary>
/// Runtime state for Train execution (not persisted to disk).
/// Managed by TrainManager during train operation.
/// This class contains session-specific data that is separate from user project data (Domain.Train).
/// </summary>
public class TrainSessionState
{
    /// <summary>
    /// Unique identifier of the train this state belongs to.
    /// </summary>
    public Guid TrainId { get; set; }

    /// <summary>
    /// Current speed of the train (0-127 for DCC).
    /// </summary>
    public int CurrentSpeed { get; set; }

    /// <summary>
    /// Indicates whether the train is currently running.
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// Timestamp of the last command sent to this train.
    /// Used for throttling and debugging.
    /// </summary>
    public DateTime? LastCommandTime { get; set; }
}
