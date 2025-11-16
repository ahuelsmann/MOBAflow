namespace Moba.Backend.Model.Action;

using Sound;

/// <summary>
/// Provides execution context and dependencies for action execution
/// </summary>
public class ActionExecutionContext
{
    /// <summary>
    /// Z21 command station.
    /// </summary>
    public Z21? Z21 { get; set; }

    /// <summary>
    /// Speaker engine for text-to-speech announcements.
    /// </summary>
    public ISpeakerEngine? SpeakerEngine { get; set; }

    /// <summary>
    /// Current project including journeys and their stations. A workflow with actions can be defined at a station.
    /// </summary>
    public Project? Project { get; set; }

    /// <summary>
    /// Template text from Journey that can contain placeholders like {StationName}, {StationIsExitOnLeft}, etc.
    /// This will be used by Announcement actions to generate context-specific announcements.
    /// </summary>
    public string? JourneyTemplateText { get; set; }

    /// <summary>
    /// Current station context for template variable replacement.
    /// </summary>
    public Station? CurrentStation { get; set; }
}