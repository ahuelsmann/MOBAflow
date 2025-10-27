namespace Moba.Backend.Model.Action;

using Enum;
using System.Diagnostics;

public class Announcement : Base
{
    public Announcement(string textToSpeak)
    {
        TextToSpeak = textToSpeak;
        Name = "New Announcement";
    }

    public override ActionType Type => ActionType.Announcement;

    public string? TextToSpeak { get; set; }

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