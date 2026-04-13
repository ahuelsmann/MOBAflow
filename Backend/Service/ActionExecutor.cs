// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Domain;
using Domain.Enum;
using Interface;
using Microsoft.Extensions.Logging;

/// <summary>
/// Executes <see cref="WorkflowAction"/> instances (command, audio, announcement) using shared runtime dependencies.
/// </summary>
public class ActionExecutor(AnnouncementService? announcementService = null, ILogger<ActionExecutor>? logger = null) : IActionExecutor
{
    /// <summary>
    /// Executes a WorkflowAction based on its type.
    /// </summary>
    public async Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context)
    {
        logger?.LogDebug("Executing action #{Number}: {Name} (Type: {Type})", action.Number, action.Name, action.Type);

        switch (action.Type)
        {
            case ActionType.Command:
                await ExecuteCommandAsync(action, context).ConfigureAwait(false);
                break;

            case ActionType.Audio:
                await ExecuteAudioAsync(action, context).ConfigureAwait(false);
                break;

            case ActionType.Announcement:
                await ExecuteAnnouncementAsync(action, context).ConfigureAwait(false);
                break;

            default:
                throw new NotSupportedException($"Action type '{action.Type}' is not supported");
        }
    }

    /// <summary>
    /// Executes a Z21 command action using <see cref="WorkflowAction.Command"/> (<c>bytesBase64</c>).
    /// </summary>
    private async Task ExecuteCommandAsync(WorkflowAction action, ActionExecutionContext context)
    {
        if (action.Command == null)
            throw new ArgumentException("Command action requires a command payload");

        if (!WorkflowActionParameterBinding.TryGetCommandBytes(action, out var bytes))
        {
            logger?.LogWarning("Command action skipped: no valid bytes provided");
            return;
        }

        logger?.LogDebug("Sending command bytes: {ByteCount}", bytes.Length);
        await context.Z21.SendCommandAsync(bytes).ConfigureAwait(false);
        logger?.LogDebug("Command sent: {ByteCount} bytes", bytes.Length);
    }

    /// <summary>
    /// Executes an audio playback action using <see cref="WorkflowAction.Audio"/> (<c>filePath</c>).
    /// </summary>
    private async Task ExecuteAudioAsync(WorkflowAction action, ActionExecutionContext context)
    {
        if (context.SoundPlayer == null)
            throw new ArgumentException("Audio action requires SoundPlayer");

        if (action.Audio == null)
            throw new ArgumentException("Audio action requires an audio payload");

        if (!WorkflowActionParameterBinding.TryGetAudioFilePath(action, out var filePath))
            throw new ArgumentException("Audio action requires a valid FilePath parameter");

        // Validate file exists before attempting playback
        if (!File.Exists(filePath))
        {
            var error = $"Audio file not found: {filePath}";
            logger?.LogWarning("Audio action failed: {Error}", error);
            throw new FileNotFoundException(error, filePath);
        }

        await context.SoundPlayer.PlayAsync(filePath).ConfigureAwait(false);

        logger?.LogDebug("Audio played: {FilePath}", filePath);
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
            logger?.LogWarning("Announcement '{ActionName}' skipped: missing journey template text", action.Name);
            return;
        }

        if (context.CurrentStation == null)
        {
            logger?.LogWarning("Announcement '{ActionName}' skipped: no current station", action.Name);
            return;
        }

        if (announcementService == null)
        {
            logger?.LogWarning("Announcement '{ActionName}' skipped: AnnouncementService not configured", action.Name);
            return;
        }

        var stationIndex = context.CurrentStationIndex.GetValueOrDefault(1);

        // Generate announcement text from template
        var announcementText = announcementService.GenerateAnnouncementText(
            context.JourneyTemplateText,
            context.CurrentStation,
            stationIndex,
            action.Name
        );

        // Speak the announcement
        await announcementService.GenerateAndSpeakAnnouncementAsync(
            context.JourneyTemplateText,
            context.CurrentStation,
            stationIndex,
            CancellationToken.None,
            action.Name
        ).ConfigureAwait(false);

        logger?.LogInformation("Announcement executed for action '{ActionName}': {AnnouncementText}", action.Name, announcementText);
    }
}