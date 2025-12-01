// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Services;

using Moba.Domain;
using Moba.Domain.Enum;
using Moba.Backend.Protocol;
using System.Diagnostics;

/// <summary>
/// Action Executor Service.
/// Executes WorkflowActions based on their type and parameters.
/// This implements Clean Architecture by separating domain models (WorkflowAction) from execution logic.
/// </summary>
public class ActionExecutor
{
    private readonly Moba.Backend.Interface.IZ21? _z21;

    // ctor to allow tests to provide IZ21 when creating mocks
    public ActionExecutor(Moba.Backend.Interface.IZ21? z21 = null)
    {
        _z21 = z21;
    }
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
            Debug.WriteLine($"    ⚠ Command skipped: No valid bytes");
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
    /// Parameters: Message (string), VoiceName (string, optional)
    /// Supports template placeholders: {StationName}, {JourneyName}
    /// </summary>
    private async Task ExecuteAnnouncementAsync(WorkflowAction action, ActionExecutionContext context)
    {
        if (action.Parameters == null || context.SpeakerEngine == null)
            throw new ArgumentException("Announcement action requires Parameters and SpeakerEngine");

        var message = action.Parameters["Message"].ToString()!;
        var voiceName = action.Parameters.ContainsKey("VoiceName") 
            ? action.Parameters["VoiceName"].ToString() 
            : null;

        // ✅ Replace template placeholders with actual values
        message = ReplaceTemplatePlaceholders(message, context);

        await context.SpeakerEngine.AnnouncementAsync(message, voiceName);

        Debug.WriteLine($"    ✓ Announcement: {message} (Voice: {voiceName ?? "default"})");
    }

    /// <summary>
    /// Replaces template placeholders in announcement messages with actual values from context.
    /// Supported placeholders:
    /// - {StationName} - Current station name
    /// - {JourneyName} - Journey template text
    /// - {ExitSide} - Exit side (left/right) based on Station.IsExitOnLeft
    /// </summary>
    private string ReplaceTemplatePlaceholders(string message, ActionExecutionContext context)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        // Replace {StationName}
        if (context.CurrentStation != null && message.Contains("{StationName}", StringComparison.OrdinalIgnoreCase))
        {
            message = message.Replace("{StationName}", context.CurrentStation.Name, StringComparison.OrdinalIgnoreCase);
            Debug.WriteLine($"    → Replaced {{StationName}} with '{context.CurrentStation.Name}'");
        }

        // Replace {JourneyName}
        if (!string.IsNullOrEmpty(context.JourneyTemplateText) && message.Contains("{JourneyName}", StringComparison.OrdinalIgnoreCase))
        {
            message = message.Replace("{JourneyName}", context.JourneyTemplateText, StringComparison.OrdinalIgnoreCase);
            Debug.WriteLine($"    → Replaced {{JourneyName}} with '{context.JourneyTemplateText}'");
        }

        // Replace {ExitSide}
        if (context.CurrentStation != null && message.Contains("{ExitSide}", StringComparison.OrdinalIgnoreCase))
        {
            var exitSide = context.CurrentStation.IsExitOnLeft ? "links" : "rechts";
            message = message.Replace("{ExitSide}", exitSide, StringComparison.OrdinalIgnoreCase);
            Debug.WriteLine($"    → Replaced {{ExitSide}} with '{exitSide}'");
        }

        return message;
    }
}
