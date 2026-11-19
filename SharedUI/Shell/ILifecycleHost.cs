// Copyright (c) 2026 Andreas Huelsmann. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root.

namespace Moba.SharedUI.Shell;

/// <summary>
/// Manages application lifecycle events across platforms.
/// </summary>
public interface ILifecycleHost
{
    /// <summary>
    /// Raised when the application is starting up.
    /// </summary>
    event EventHandler? Starting;

    /// <summary>
    /// Raised when the application has fully started.
    /// </summary>
    event EventHandler? Started;

    /// <summary>
    /// Raised when the application is suspending (e.g., minimized, background).
    /// </summary>
    event EventHandler? Suspending;

    /// <summary>
    /// Raised when the application is resuming from suspension.
    /// </summary>
    event EventHandler? Resuming;

    /// <summary>
    /// Raised when the application is shutting down.
    /// </summary>
    event EventHandler? Stopping;

    /// <summary>
    /// Raised when the application has fully stopped.
    /// </summary>
    event EventHandler? Stopped;

    /// <summary>
    /// Gets the current lifecycle state.
    /// </summary>
    LifecycleState CurrentState { get; }

    /// <summary>
    /// Requests application shutdown.
    /// </summary>
    /// <param name="exitCode">Optional exit code.</param>
    void RequestShutdown(int exitCode = 0);
}

/// <summary>
/// Application lifecycle states.
/// </summary>
public enum LifecycleState
{
    /// <summary>Application is not yet started.</summary>
    NotStarted,

    /// <summary>Application is starting.</summary>
    Starting,

    /// <summary>Application is running.</summary>
    Running,

    /// <summary>Application is suspended/backgrounded.</summary>
    Suspended,

    /// <summary>Application is stopping.</summary>
    Stopping,

    /// <summary>Application has stopped.</summary>
    Stopped
}
