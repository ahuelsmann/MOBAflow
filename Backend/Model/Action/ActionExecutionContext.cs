namespace Moba.Backend.Model.Action;

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
    public Sound.SpeakerEngine? SpeakerEngine { get; set; }

    /// <summary>
    /// Current project including journeys and their stations. A workflow with actions can be defined at a station.
    /// </summary>
    public Project? Project { get; set; }
}