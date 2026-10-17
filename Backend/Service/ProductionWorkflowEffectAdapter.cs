// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Common.IO;
using Interface;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;

/// <summary>Owns environment probes and real workflow output; never used by isolated execution.</summary>
public sealed partial class ProductionWorkflowEffectAdapter(
    IAnnouncementService announcementService,
    IFileSystem fileSystem,
    ILogger<ProductionWorkflowEffectAdapter> logger) : IWorkflowEffectAdapter
{
    private readonly IAnnouncementService _announcementService = announcementService ?? throw new ArgumentNullException(nameof(announcementService));
    private readonly IFileSystem _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    private readonly ILogger<ProductionWorkflowEffectAdapter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public Task ExecuteAsync(WorkflowEffectRequest request, ActionExecutionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        return request switch
        {
            WorkflowCommandEffectRequest command => ExecuteCommandAsync(command, context, cancellationToken),
            WorkflowAudioEffectRequest audio => ExecuteAudioAsync(audio, context, cancellationToken),
            WorkflowAnnouncementEffectRequest announcement => ExecuteAnnouncementAsync(announcement, cancellationToken),
            WorkflowScriptEffectRequest script => ExecuteScriptAsync(script, cancellationToken),
            WorkflowSignalEffectRequest signal => context.Z21.SetTurnoutAsync(signal.Command.DccAddress, signal.Command.Output,
                signal.Command.Activate, false, cancellationToken),
            WorkflowDisplayEffectRequest display => ExecuteDisplay(display),
            _ => throw new NotSupportedException($"Workflow effect '{request.GetType().Name}' is not supported.")
        };
    }

    private async Task ExecuteCommandAsync(WorkflowCommandEffectRequest request, ActionExecutionContext context, CancellationToken cancellationToken)
    {
        LogSendingCommand(_logger, request.Bytes.Length);
        await context.Z21.SendCommandAsync(request.Bytes.ToArray(), cancellationToken).ConfigureAwait(false);
        LogCommandSent(_logger, request.Bytes.Length);
    }

    private async Task ExecuteAudioAsync(WorkflowAudioEffectRequest request, ActionExecutionContext context, CancellationToken cancellationToken)
    {
        if (context.SoundPlayer == null)
            throw new ArgumentException("Audio action requires SoundPlayer");
        if (!_fileSystem.FileExists(request.FilePath))
        {
            LogMissingAudio(_logger, request.FilePath);
            throw new FileNotFoundException($"Audio file not found: {request.FilePath}", request.FilePath);
        }
        await context.SoundPlayer.PlayAsync(request.FilePath, cancellationToken).ConfigureAwait(false);
        LogAudioPlayed(_logger, request.FilePath);
    }

    private async Task ExecuteAnnouncementAsync(WorkflowAnnouncementEffectRequest request, CancellationToken cancellationToken)
    {
        if (request.Station == null) return;
        await _announcementService.GenerateAndSpeakAnnouncementAsync(request.TemplateText, request.Station, request.StationIndex,
            cancellationToken, request.ActionName).ConfigureAwait(false);
        LogAnnouncement(_logger, request.ActionName, request.RenderedText);
    }

    private Task ExecuteDisplay(WorkflowDisplayEffectRequest request)
    {
        LogUnconfiguredDisplay(_logger, request.ActionName);
        return Task.CompletedTask;
    }

    private async Task ExecuteScriptAsync(WorkflowScriptEffectRequest request, CancellationToken cancellationToken)
    {
        if (!_fileSystem.FileExists(request.ScriptPath))
            throw new FileNotFoundException($"PowerShell script file not found: {request.ScriptPath}", request.ScriptPath);

        var executable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "powershell.exe" : "pwsh";
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = BuildPowerShellArguments(request.ScriptPath, request.Arguments),
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        LogScriptStarting(_logger, request.ActionName, request.ScriptPath);
        process.Start();
        using var cancellationRegistration = cancellationToken.Register(static state => TryTerminate((Process)state!), process);
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        var output = await outputTask.ConfigureAwait(false);
        var error = await errorTask.ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(output))
            LogScriptOutput(_logger, request.ActionName, output);
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"PowerShell action '{request.ActionName}' failed with exit code {process.ExitCode}: {error}");
    }

    private static string BuildPowerShellArguments(string scriptPath, string? scriptArguments)
    {
        var arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? $"-NoProfile -ExecutionPolicy Bypass -File {Quote(scriptPath)}"
            : $"-NoProfile -File {Quote(scriptPath)}";
        return string.IsNullOrWhiteSpace(scriptArguments) ? arguments : $"{arguments} {scriptArguments}";
    }

    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";

    private static void TryTerminate(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // The process exited between the cancellation check and termination request.
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Sending command bytes: {ByteCount}")]
    private static partial void LogSendingCommand(ILogger logger, int byteCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Command sent: {ByteCount} bytes")]
    private static partial void LogCommandSent(ILogger logger, int byteCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Audio action failed: Audio file not found: {FilePath}")]
    private static partial void LogMissingAudio(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Audio played: {FilePath}")]
    private static partial void LogAudioPlayed(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Announcement executed for action '{ActionName}': {AnnouncementText}")]
    private static partial void LogAnnouncement(ILogger logger, string actionName, string? announcementText);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Train destination display action '{ActionName}' skipped: display service not configured")]
    private static partial void LogUnconfiguredDisplay(ILogger logger, string actionName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Executing PowerShell action '{ActionName}' with script {ScriptPath}")]
    private static partial void LogScriptStarting(ILogger logger, string actionName, string scriptPath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "PowerShell action '{ActionName}' output: {Output}")]
    private static partial void LogScriptOutput(ILogger logger, string actionName, string output);
}
