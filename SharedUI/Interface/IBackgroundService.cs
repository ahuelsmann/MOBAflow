// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

/// <summary>
/// Interface for platform-specific background service operations.
/// Allows apps to continue running critical operations (e.g., Z21 connection) when in background.
/// </summary>
public interface IBackgroundService
{
    /// <summary>
    /// Starts the background service with a notification (Android Foreground Service).
    /// </summary>
    /// <param name="title">Notification title (e.g., "MOBAsmart Active")</param>
    /// <param name="message">Notification message (e.g., "Z21 connection maintained")</param>
    Task StartAsync(string title, string message);

    /// <summary>
    /// Stops the background service and removes notification.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Gets whether the background service is currently running.
    /// </summary>
    bool IsRunning { get; }
}