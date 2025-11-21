// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
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
    /// If a JourneyTemplateText is provided in the context, it will be used instead of TextToSpeak and placeholders will be replaced.
    /// If no SpeakerEngine is available or TextToSpeak is empty, the announcement will be skipped with a debug message.
    /// </summary>
    /// <param name="context">Execution context containing the SpeakerEngine for text-to-speech conversion</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public override async Task ExecuteAsync(ActionExecutionContext context)
    {
        // Use JourneyTemplateText if available, otherwise fall back to TextToSpeak
        string? textToAnnounce = !string.IsNullOrEmpty(context.JourneyTemplateText)
            ? context.JourneyTemplateText
            : TextToSpeak;

        if (string.IsNullOrEmpty(textToAnnounce))
        {
            Debug.WriteLine("⚠ Announcement skipped: No text to speak");
            return;
        }

        // Replace placeholders if CurrentStation is available
        if (context.CurrentStation != null)
        {
            textToAnnounce = ReplacePlaceholders(textToAnnounce, context.CurrentStation);
        }

        if (context.SpeakerEngine != null)
        {
            Debug.WriteLine($"📢 Announcement: {textToAnnounce}");
            await context.SpeakerEngine.AnnouncementAsync(textToAnnounce, null);
        }
        else
        {
            Debug.WriteLine("⚠ Announcement skipped: No SpeakerEngine available");
        }
    }

    /// <summary>
    /// Replaces placeholders in the template text with actual station data.
    /// Supported placeholders:
    /// - {StationName}: Name of the station
    /// - {StationIsExitOnLeft}: "links" if exit is on left, "rechts" otherwise
    /// </summary>
    /// <param name="template">Template text with placeholders</param>
    /// <param name="station">Current station for data replacement</param>
    /// <returns>Text with replaced placeholders</returns>
    private static string ReplacePlaceholders(string template, Station station)
    {
        var result = template;

        // Replace {StationName}
        result = result.Replace("{StationName}", station.Name);

        // Replace {StationIsExitOnLeft}
        string exitDirection = station.IsExitOnLeft ? "links" : "rechts";
        result = result.Replace("{StationIsExitOnLeft}", exitDirection);

        return result;
    }
}