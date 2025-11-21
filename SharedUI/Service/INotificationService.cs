// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

/// <summary>
/// Service for playing notification sounds when user actions require attention.
/// Platform-specific implementations handle the actual sound playback.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Plays a notification sound (e.g., when lap target is reached).
    /// </summary>
    void PlayNotificationSound();
}
