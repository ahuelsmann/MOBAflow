// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Moba.Backend.Interface;
using Moba.Domain;
using Sound;

/// <summary>
/// Provides execution context for workflow actions.
/// Contains dependencies and state information needed during action execution.
/// Platform-independent: No UI thread dispatching.
/// </summary>
public class ActionExecutionContext
{
    /// <summary>
    /// Z21 command station interface for sending commands.
    /// </summary>
    public required IZ21 Z21 { get; init; }

    /// <summary>
    /// Speaker engine for text-to-speech announcements.
    /// Optional: May be null if audio is not configured.
    /// </summary>
    public ISpeakerEngine? SpeakerEngine { get; init; }

    /// <summary>
    /// Sound player for audio file playback.
    /// Optional: May be null if audio is not configured.
    /// </summary>
    public ISoundPlayer? SoundPlayer { get; init; }

    /// <summary>
    /// Current station context for template replacements in announcements.
    /// Set by JourneyManager when workflow is triggered at a station.
    /// </summary>
    public Station? CurrentStation { get; set; }

    /// <summary>
    /// Journey template text for {JourneyName} placeholder in announcements.
    /// Set by JourneyManager when workflow is triggered during a journey.
    /// </summary>
    public string? JourneyTemplateText { get; set; }
}
