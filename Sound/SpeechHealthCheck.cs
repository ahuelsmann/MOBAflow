// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Sound;

using System.Diagnostics;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Health check service for local Piper TTS.
/// Verifies configuration and whether the Piper executable can be started.
/// </summary>
public class SpeechHealthCheck(IOptions<SpeechOptions> options, ILogger<SpeechHealthCheck> logger)
{
    private readonly SpeechOptions _options = options.Value;

    /// <summary>
    /// Checks if Piper TTS is properly configured.
    /// </summary>
    /// <returns>True if required Piper paths are configured, false otherwise.</returns>
    public bool IsConfigured()
    {
        var executablePath = ResolvePath(_options.PiperExecutablePath, "PIPER_EXECUTABLE_PATH");
        var modelPath = ResolvePath(_options.PiperModelPath, "PIPER_MODEL_PATH");

        bool isConfigured = File.Exists(executablePath) && File.Exists(modelPath);

        if (!isConfigured)
        {
            logger.LogWarning("Piper TTS is not configured. Set PiperExecutablePath and PiperModelPath");
        }
        else
        {
            logger.LogInformation("Piper TTS is configured. Executable: {ExecutablePath}, Model: {ModelPath}", executablePath, modelPath);
        }

        return isConfigured;
    }

    /// <summary>
    /// Performs a simple startup test for the Piper executable.
    /// </summary>
    /// <returns>True if the local Piper process can be started, false otherwise.</returns>
    public async Task<bool> TestConnectivityAsync()
    {
        if (!IsConfigured())
        {
            logger.LogWarning("Cannot test Piper startup - service not configured");
            return false;
        }

        var executablePath = ResolvePath(_options.PiperExecutablePath, "PIPER_EXECUTABLE_PATH");

        try
        {
            logger.LogInformation("Testing Piper TTS executable startup...");
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = executablePath!,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            process.StartInfo.ArgumentList.Add("--help");

            process.Start();
            var completedTask = await Task.WhenAny(process.WaitForExitAsync(), Task.Delay(TimeSpan.FromSeconds(5))).ConfigureAwait(false);
            if (!process.HasExited || completedTask.Status != TaskStatus.RanToCompletion)
            {
                TryKill(process);
                logger.LogWarning("Piper TTS startup test timed out");
                return false;
            }

            var isHealthy = process.ExitCode == 0;
            logger.LogInformation("Piper TTS startup test {Status}", isHealthy ? "passed" : "failed");
            return isHealthy;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Piper TTS startup test failed. Executable: {ExecutablePath}", executablePath);
            return false;
        }
    }

    /// <summary>
    /// Gets detailed configuration status information.
    /// </summary>
    /// <returns>Configuration status message</returns>
    public string GetStatusMessage()
    {
        var executablePath = ResolvePath(_options.PiperExecutablePath, "PIPER_EXECUTABLE_PATH");
        var modelPath = ResolvePath(_options.PiperModelPath, "PIPER_MODEL_PATH");

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return "❌ Piper executable not configured";
        }

        if (!File.Exists(executablePath))
        {
            return $"❌ Piper executable not found: {executablePath}";
        }

        if (string.IsNullOrWhiteSpace(modelPath))
        {
            return "❌ Piper model not configured";
        }

        if (!File.Exists(modelPath))
        {
            return $"❌ Piper model not found: {modelPath}";
        }

        return $"✅ Configured: {Path.GetFileName(executablePath)}, {Path.GetFileName(modelPath)}";
    }

    private static string? ResolvePath(string? configuredPath, string environmentVariableName)
    {
        return string.IsNullOrWhiteSpace(configuredPath)
            ? Environment.GetEnvironmentVariable(environmentVariableName)
            : configuredPath;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best-effort process cleanup after timeout.
        }
    }
}