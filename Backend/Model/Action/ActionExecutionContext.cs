namespace Moba.Backend.Model.Action;

/// <summary>
/// Provides execution context and dependencies for action execution
/// </summary>
public class ActionExecutionContext
{
    /// <summary>
    /// Z21 command station for sending commands
    /// </summary>
    public Z21? Z21 { get; set; }

    /// <summary>
    /// Speaker engine for text-to-speech announcements
    /// </summary>
    public Sound.SpeakerEngine? SpeakerEngine { get; set; }

    /// <summary>
    /// Current project settings
    /// </summary>
    public Project? Project { get; set; }
}
