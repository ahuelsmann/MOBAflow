namespace Moba.Backend.Model.Action;

using Enum;
using System.Diagnostics;

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
    /// <exception cref="Exception">Thrown when the speech synthesis fails</exception>
    public override async Task ExecuteAsync(ActionExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(TextToSpeak))
        {
            Debug.WriteLine("  ⚠ Announcement has no text to speak");
            return;
        }

        Debug.WriteLine($"  🗣 Announcement: '{TextToSpeak}'");

        if (context.SpeakerEngine != null)
        {
            try
            {
                await context.SpeakerEngine.AnnouncementAsync(TextToSpeak, null);
                Debug.WriteLine("  ✅ Announcement completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ❌ Error playing announcement: {ex.Message}");
                throw;
            }
        }
        else
        {
            Debug.WriteLine("  ⚠ No SpeakerEngine available - announcement not played");
        }
    }
}