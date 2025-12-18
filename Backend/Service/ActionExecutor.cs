// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Domain;

using Moba.Domain.Enum;

using System.Diagnostics;

/// <summary>
/// Action Executor Service.
/// Executes WorkflowActions based on their type and parameters.
/// This implements Clean Architecture by separating domain models (WorkflowAction) from execution logic.
/// </summary>
public class ActionExecutor(AnnouncementService? announcementService = null)
{
    /// <summary>
    /// Executes a WorkflowAction based on its type.
    /// </summary>
    public async Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context)
    {
        Debug.WriteLine($"  ▶ Executing action #{action.Number}: {action.Name} (Type: {action.Type})");

        switch (action.Type)
        {
            case ActionType.Command:
                await ExecuteCommandAsync(action, context);
                break;

            case ActionType.Audio:
                await ExecuteAudioAsync(action, context);
                break;

            case ActionType.Announcement:
                await ExecuteAnnouncementAsync(action, context);
                break;

            default:
                throw new NotSupportedException($"Action type '{action.Type}' is not supported");
        }
    }

    /// <summary>
    /// Executes a Z21 command action.
    /// Parameters: Bytes (byte[]) - Raw Z21 command bytes
    /// </summary>
    private async Task ExecuteCommandAsync(WorkflowAction action, ActionExecutionContext context)
    {
        if (action.Parameters == null)
            throw new ArgumentException("Command action requires Parameters");

        // Get bytes from parameters (stored as base64 string or byte array)
        byte[]? bytes = null;

        if (action.Parameters.TryGetValue("Bytes", out var bytesObj))
        {
            if (bytesObj is byte[] byteArray)
            {
                bytes = byteArray;
            }
            else if (bytesObj is string base64String)
            {
                bytes = Convert.FromBase64String(base64String);
            }
        }

        if (bytes != null && bytes.Length > 0)
        {
            await context.Z21.SendCommandAsync(bytes);
            Debug.WriteLine($"    ✓ Command sent: {bytes.Length} bytes");
        }
        else
        {
            Debug.WriteLine("    ⚠ Command skipped: No valid bytes");
        }
    }

    /// <summary>
    /// Executes an audio playback action.
    /// Parameters: FilePath (string)
    /// </summary>
    private async Task ExecuteAudioAsync(WorkflowAction action, ActionExecutionContext context)
    {
        if (action.Parameters == null || context.SoundPlayer == null)
            throw new ArgumentException("Audio action requires Parameters and SoundPlayer");

        var filePath = action.Parameters["FilePath"].ToString()!;

        await Task.Run(() => context.SoundPlayer.Play(filePath));

        Debug.WriteLine($"    ✓ Audio played: {filePath}");
    }

    /// <summary>
    /// Executes a text-to-speech announcement action.
    /// Uses Journey template text with placeholder replacement:
    /// - {StationName} → Current station name
    /// - {ExitDirection} → "links" or "rechts" based on Station.IsExitOnLeft
    /// - {StationNumber} → Position in journey (1-based)
    /// - {TrackNumber} → Station track number
    /// 
    /// Template comes from: Journey.Text (set in ActionExecutionContext.JourneyTemplateText)
    /// </summary>
    private async Task ExecuteAnnouncementAsync(WorkflowAction action, ActionExecutionContext context)
    {
        // Verify prerequisites
        if (string.IsNullOrEmpty(context.JourneyTemplateText))
        {
            Debug.WriteLine($"    ⚠ Announcement '{action.Name}' skipped: No Journey template text");
            return;
        }

        if (context.CurrentStation == null)
        {
            Debug.WriteLine($"    ⚠ Announcement '{action.Name}' skipped: No current station");
            return;
        }

        if (announcementService == null)
        {
            Debug.WriteLine($"    ⚠ Announcement '{action.Name}' skipped: AnnouncementService not configured");
            return;
        }

        // Generate announcement text from template
        var announcementText = announcementService.GenerateAnnouncementText(
            new Journey { Text = context.JourneyTemplateText },
            context.CurrentStation,
            stationIndex: 1  // Will be calculated from context if needed
        );

        // Speak the announcement
        await announcementService.GenerateAndSpeakAnnouncementAsync(
            new Journey { Text = context.JourneyTemplateText },
            context.CurrentStation,
            stationIndex: 1,
            CancellationToken.None
        ).ConfigureAwait(false);

        Debug.WriteLine($"    ✓ Announcement: \"{announcementText}\"");
    }
}