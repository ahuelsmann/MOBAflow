// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Common.IO;

using Domain;
using Domain.Enum;

using Interface;

using Microsoft.Extensions.Logging;

using System.Diagnostics;
using System.Runtime.InteropServices;

/// <summary>
/// Sends raw Z21 command bytes.
/// </summary>
public sealed class CommandWorkflowActionHandler(ILogger<CommandWorkflowActionHandler>? logger = null) : IWorkflowActionHandler
{
    public ActionType ActionType => ActionType.Command;

    public async Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context)
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
}

/// <summary>
/// Plays an audio file through the configured sound player.
/// </summary>
public sealed class AudioWorkflowActionHandler(
    ILogger<AudioWorkflowActionHandler>? logger = null,
    IFileSystem? fileSystem = null) : IWorkflowActionHandler
{
    private readonly IFileSystem _fileSystem = fileSystem ?? SystemFileSystem.Instance;

    public ActionType ActionType => ActionType.Audio;

    public async Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context)
    {
        if (context.SoundPlayer == null)
            throw new ArgumentException("Audio action requires SoundPlayer");

        if (action.Audio == null)
            throw new ArgumentException("Audio action requires an audio payload");

        if (!WorkflowActionParameterBinding.TryGetAudioFilePath(action, out var filePath))
            throw new ArgumentException("Audio action requires a valid FilePath parameter");

        if (!_fileSystem.FileExists(filePath))
        {
            var error = $"Audio file not found: {filePath}";
            logger?.LogWarning("Audio action failed: {Error}", error);
            throw new FileNotFoundException(error, filePath);
        }

        await context.SoundPlayer.PlayAsync(filePath).ConfigureAwait(false);
        logger?.LogDebug("Audio played: {FilePath}", filePath);
    }
}

/// <summary>
/// Generates and speaks a text announcement.
/// </summary>
public sealed class AnnouncementWorkflowActionHandler(
    IAnnouncementService? announcementService = null,
    ILogger<AnnouncementWorkflowActionHandler>? logger = null) : IWorkflowActionHandler
{
    public ActionType ActionType => ActionType.Announcement;

    public async Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context)
    {
        var templateText = action.Announcement?.Message ?? context.JourneyTemplateText;
        if (string.IsNullOrEmpty(templateText))
        {
            logger?.LogWarning("Announcement '{ActionName}' skipped: missing announcement template text", action.Name);
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
        var announcementText = announcementService.GenerateAnnouncementText(
            templateText,
            context.CurrentStation,
            stationIndex,
            action.Name);

        await announcementService.GenerateAndSpeakAnnouncementAsync(
            templateText,
            context.CurrentStation,
            stationIndex,
            CancellationToken.None,
            action.Name).ConfigureAwait(false);

        logger?.LogInformation("Announcement executed for action '{ActionName}': {AnnouncementText}", action.Name, announcementText);
    }
}

/// <summary>
/// Sends journey data to a train destination display.
/// </summary>
public sealed class TrainDestinationDisplayWorkflowActionHandler(
    ILogger<TrainDestinationDisplayWorkflowActionHandler>? logger = null) : IWorkflowActionHandler
{
    public ActionType ActionType => ActionType.TrainDestinationDisplay;

    public Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context)
    {
        logger?.LogWarning(
            "Train destination display action '{ActionName}' skipped: display service not configured",
            action.Name);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Sets a signal aspect by resolving the configured multiplexer command.
/// </summary>
public sealed class SelectSignalAspectWorkflowActionHandler : IWorkflowActionHandler
{
    public ActionType ActionType => ActionType.SelectSignalAspect;

    public async Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context)
    {
        var payload = action.SelectSignalAspect ?? throw new ArgumentException("Select signal aspect action requires a signal aspect payload");

        var command = MultiplexerCommandResolver.Resolve(
            payload.BaseAddress,
            payload.MultiplexerArticleNumber,
            payload.SignalArticleNumber,
            payload.SignalAspect);

        await context.Z21.SetTurnoutAsync(
                command.DccAddress,
                command.Output,
                command.Activate,
                false)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Executes a PowerShell script action.
/// </summary>
public sealed class ExecuteScriptWorkflowActionHandler(
    ILogger<ExecuteScriptWorkflowActionHandler>? logger = null,
    IFileSystem? fileSystem = null) : IWorkflowActionHandler
{
    private readonly IFileSystem _fileSystem = fileSystem ?? SystemFileSystem.Instance;

    public ActionType ActionType => ActionType.ExecuteScript;

    public async Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context)
    {
        _ = context;

        var payload = action.PowerShell ?? throw new ArgumentException("ExecuteScript action requires a PowerShell payload");
        if (string.IsNullOrWhiteSpace(payload.ScriptPath))
            throw new ArgumentException("ExecuteScript action requires a script path");

        if (!_fileSystem.FileExists(payload.ScriptPath))
            throw new FileNotFoundException($"PowerShell script file not found: {payload.ScriptPath}", payload.ScriptPath);

        var executable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "powershell.exe" : "pwsh";
        var arguments = BuildPowerShellArguments(payload.ScriptPath, payload.Arguments);
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        logger?.LogInformation("Executing PowerShell action '{ActionName}' with script {ScriptPath}", action.Name, payload.ScriptPath);

        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().ConfigureAwait(false);

        var output = await outputTask.ConfigureAwait(false);
        var error = await errorTask.ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(output))
            logger?.LogDebug("PowerShell action '{ActionName}' output: {Output}", action.Name, output);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"PowerShell action '{action.Name}' failed with exit code {process.ExitCode}: {error}");
    }

    private static string BuildPowerShellArguments(string scriptPath, string? scriptArguments)
    {
        var arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? $"-NoProfile -ExecutionPolicy Bypass -File {Quote(scriptPath)}"
            : $"-NoProfile -File {Quote(scriptPath)}";

        return string.IsNullOrWhiteSpace(scriptArguments)
            ? arguments
            : $"{arguments} {scriptArguments}";
    }

    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
}

/// <summary>Moves the active journey to the next or an explicitly configured stop.</summary>
public sealed class ChangeJourneyStopWorkflowActionHandler(IJourneyStopTransitionService? transitionService = null) : IWorkflowActionHandler
{
    private readonly IJourneyStopTransitionService _transitionService = transitionService ?? new JourneyStopTransitionService();

    public ActionType ActionType => ActionType.ChangeJourneyStop;

    public Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context)
    {
        var payload = action.ChangeJourneyStop ?? throw new ArgumentException("Change journey stop action requires a payload");
        var journey = context.CurrentJourney ?? throw new InvalidOperationException("Change journey stop action requires a journey context");
        var state = context.CurrentJourneySessionState ?? throw new InvalidOperationException("Change journey stop action requires a journey state");

        var result = _transitionService.Apply(journey, state, new JourneyStopTransition
        {
            Mode = payload.MoveToNextStop ? JourneyStopTransitionMode.Next : JourneyStopTransitionMode.SpecificStation,
            StationId = payload.TargetStationId
        });
        context.CurrentStation = result.CurrentStation;
        context.CurrentStationIndex = result.CurrentStation == null ? null : journey.Stations.IndexOf(result.CurrentStation) + 1;
        return Task.CompletedTask;
    }
}
