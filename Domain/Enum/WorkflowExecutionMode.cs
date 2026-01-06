// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.Enum;

/// <summary>
/// Defines how workflow actions are executed.
/// </summary>
public enum WorkflowExecutionMode
{
    /// <summary>
    /// Sequential execution: Each action waits for the previous one to complete.
    /// DelayAfterMs: Additional delay AFTER action completes before next starts.
    /// Use for: Precise sequences (Gong → Wait 1s → Announcement)
    /// </summary>
    Sequential = 0,

    /// <summary>
    /// Parallel execution: Actions start with staggered delays (overlapping).
    /// DelayAfterMs: Cumulative delay from start. Each action's DelayAfterMs is added to previous total.
    /// Use for: Layered effects (Gong at t=0, Announcement at t=500ms, Light at t=2000ms)
    /// Example: Action1(Delay=0)→t=0, Action2(Delay=500)→t=500, Action3(Delay=1500)→t=2000
    /// </summary>
    Parallel = 1
}
