// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Interface;

/// <summary>
/// Interface for managers that handle feedback events from Z21 track feedback points.
/// Provides common operations for workflow execution, timer management, and resource cleanup.
/// </summary>
public interface IFeedbackManager : IDisposable
{
    /// <summary>
    /// Resets all feedback timers and state information.
    /// This allows feedback events to be processed immediately without timer-based filtering.
    /// </summary>
    void ResetAll();
}
