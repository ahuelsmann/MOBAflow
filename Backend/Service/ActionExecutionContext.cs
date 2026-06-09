// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Domain;

using Interface;

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
    /// Current project context for workflow actions that need project-level configuration.
    /// </summary>
    public Project? CurrentProject { get; set; }

    /// <summary>
    /// Current journey context for workflow actions triggered by JourneyManager.
    /// </summary>
    public Journey? CurrentJourney { get; set; }

    /// <summary>
    /// Current journey session state for workflow actions triggered by JourneyManager.
    /// </summary>
    public JourneySessionState? CurrentJourneySessionState { get; set; }

    /// <summary>
    /// Current station context for template replacements in announcements.
    /// Set by JourneyManager when workflow is triggered at a station.
    /// </summary>
    public Station? CurrentStation { get; set; }

    public Platform? CurrentPlatform { get; set; }

    /// <summary>
    /// Journey template text for {JourneyName} placeholder in announcements.
    /// Set by JourneyManager when workflow is triggered during a journey.
    /// </summary>
    public string? JourneyTemplateText { get; set; }

    /// <summary>
    /// Current station index within the active journey (1-based).
    /// Optional and used for announcement placeholder rendering.
    /// </summary>
    public int? CurrentStationIndex { get; set; }
}

/// <summary>
/// Per-workflow mutable state used to create an isolated action execution context.
/// </summary>
public sealed class ActionExecutionContextState
{
    public Project? CurrentProject { get; init; }

    public Journey? CurrentJourney { get; init; }

    public JourneySessionState? CurrentJourneySessionState { get; init; }

    public Station? CurrentStation { get; init; }

    public Platform? CurrentPlatform { get; init; }

    public string? JourneyTemplateText { get; init; }

    public int? CurrentStationIndex { get; init; }
}

/// <summary>
/// Creates isolated action execution contexts for individual workflow runs.
/// </summary>
public interface IActionExecutionContextFactory
{
    ActionExecutionContext Create(ActionExecutionContextState? state = null);
}

/// <summary>
/// Copies stable action dependencies into a fresh context for each workflow run.
/// </summary>
public sealed class ActionExecutionContextFactory(ActionExecutionContext services) : IActionExecutionContextFactory
{
    public ActionExecutionContext Create(ActionExecutionContextState? state = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return new ActionExecutionContext
        {
            Z21 = services.Z21,
            SpeakerEngine = services.SpeakerEngine,
            SoundPlayer = services.SoundPlayer,
            CurrentProject = state?.CurrentProject,
            CurrentJourney = state?.CurrentJourney,
            CurrentJourneySessionState = state?.CurrentJourneySessionState,
            CurrentStation = state?.CurrentStation,
            CurrentPlatform = state?.CurrentPlatform,
            JourneyTemplateText = state?.JourneyTemplateText,
            CurrentStationIndex = state?.CurrentStationIndex
        };
    }
}
