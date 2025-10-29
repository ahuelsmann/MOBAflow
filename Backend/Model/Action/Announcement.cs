namespace Moba.Backend.Model.Action;

using Enum;

/// <summary>
/// This action performs text-to-speech announcements using the Azure Speech Service.
/// This action can be used for announcements of various services. For example, it could be used for loudspeaker announcements on a train, in a station, or on a platform.
/// </summary>
public class Announcement : Base
{
    /// <summary>
    /// Initializes a new instance of the Announcement class with the specified text.
    /// Sets the default name to "New Announcement".
    /// </summary>
    /// <param name="textToSpeak">The text that will be converted to speech</param>
    public Announcement(string textToSpeak)
    {
        TextToSpeak = textToSpeak;
        Name = "New Announcement";
    }

    /// <summary>
    /// Gets the action type, always returns ActionType.Announcement.
    /// </summary>
    public override ActionType Type => ActionType.Announcement;

    /// <summary>
    /// The text that will be converted to speech and played through the speaker engine.
    /// </summary>
    public string? TextToSpeak { get; set; }

    /// <summary>
    /// Executes the announcement by converting the text to speech using the SpeakerEngine.
    /// If no SpeakerEngine is available or TextToSpeak is empty, the announcement will be skipped with a debug message.
    /// </summary>
    /// <param name="context">Execution context containing the SpeakerEngine for text-to-speech conversion</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public override async Task ExecuteAsync(ActionExecutionContext context)
    {
        if (!string.IsNullOrEmpty(TextToSpeak) && context.SpeakerEngine != null)
        {
            await context.SpeakerEngine.AnnouncementAsync(TextToSpeak, null);
        }
    }
}