// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Common.Multiplex;

using Domain;
using Domain.Enum;

using Interface;

using Microsoft.Extensions.Logging;

/// <summary>
/// Executes <see cref="WorkflowAction"/> instances (command, audio, announcement) using shared runtime dependencies.
/// </summary>
public class ActionExecutor(
    AnnouncementService? announcementService = null,
    ITrainDestinationDisplayService? trainDestinationDisplayService = null,
    ILogger<ActionExecutor>? logger = null) : IActionExecutor
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

            case ActionType.TrainDestinationDisplay:
                await ExecuteTrainDestinationDisplayAsync(action, context).ConfigureAwait(false);
                break;

            case ActionType.SelectSignalAspect:
                await ExecuteSelectSignalAspectAsync(action, context).ConfigureAwait(false);
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

    private async Task ExecuteTrainDestinationDisplayAsync(WorkflowAction action, ActionExecutionContext context)
    {
        if (trainDestinationDisplayService == null)
        {
            logger?.LogWarning("Train destination display action '{ActionName}' skipped: TrainDestinationDisplayService not configured", action.Name);
            return;
        }

        await trainDestinationDisplayService.UpdateAsync(action, context).ConfigureAwait(false);
    }
    private async Task ExecuteSelectSignalAspectAsync(WorkflowAction action, ActionExecutionContext context)
    {
        var payload = action.SelectSignalAspect ?? throw new ArgumentException("Select signal aspect action requires a signal aspect payload");

        if (payload.BaseAddress is < 1 or > 2044)
            throw new ArgumentOutOfRangeException(nameof(payload.BaseAddress), "Base DCC address must be in the range 1-2044.");

        if (!MultiplexerHelper.TryGetMaxAddressOffset(
                payload.MultiplexerArticleNumber,
                payload.SignalArticleNumber,
                out var maxOffset))
        {
            throw new ArgumentException(
                $"No multiplexer mapping found for multiplexer '{payload.MultiplexerArticleNumber}' and signal article '{payload.SignalArticleNumber}'.");
        }

        if (payload.BaseAddress + maxOffset > 2044)
            throw new ArgumentOutOfRangeException(nameof(payload.BaseAddress), "Base DCC address plus multiplexer offset exceeds 2044.");

        if (!MultiplexerHelper.TryGetTurnoutCommand(
                payload.MultiplexerArticleNumber,
                payload.SignalArticleNumber,
                payload.SignalAspect,
                out var turnoutCommand))
        {
            throw new ArgumentException(
                $"Signal aspect '{payload.SignalAspect}' is not supported for multiplexer '{payload.MultiplexerArticleNumber}' and signal article '{payload.SignalArticleNumber}'.");
        }

        var dccAddress = payload.BaseAddress + turnoutCommand.AddressOffset;
        await context.Z21.SetTurnoutAsync(
                dccAddress,
                turnoutCommand.Output,
                turnoutCommand.Activate,
                false)
            .ConfigureAwait(false);
    }
}