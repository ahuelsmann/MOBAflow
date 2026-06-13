// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Sound;

using Common.Speech;

using System.Diagnostics;
using System.Media;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

/// <summary>
/// Local Piper Text-to-Speech implementation.
/// </summary>
public class PiperSpeechEngine : ISpeakerEngine
{
    private readonly IOptionsMonitor<SpeechOptions>? _optionsMonitor;
    private readonly ILogger<PiperSpeechEngine> _logger;
    private readonly IPiperProcessRunner _processRunner;
    private readonly IPiperAudioPlayer _audioPlayer;

    /// <summary>
    /// Initializes a new instance of the <see cref="PiperSpeechEngine"/> for production use.
    /// </summary>
    public PiperSpeechEngine(IOptionsMonitor<SpeechOptions> optionsMonitor, ILogger<PiperSpeechEngine> logger)
        : this(optionsMonitor, logger, new PiperProcessRunner(), new PiperAudioPlayer())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PiperSpeechEngine"/> with testable collaborators.
    /// </summary>
    public PiperSpeechEngine(
        IOptionsMonitor<SpeechOptions>? optionsMonitor,
        ILogger<PiperSpeechEngine>? logger,
        IPiperProcessRunner processRunner,
        IPiperAudioPlayer audioPlayer)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger ?? NullLogger<PiperSpeechEngine>.Instance;
        _processRunner = processRunner;
        _audioPlayer = audioPlayer;
    }

    /// <summary>
    /// Display name of this speech engine.
    /// </summary>
    public string Name { get; } = "Piper TTS";

    /// <summary>
    /// Synthesizes speech with the configured local Piper executable and voice model.
    /// </summary>
    public async Task AnnouncementAsync(string message, string? voiceName)
    {
        _ = voiceName;

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentNullException(nameof(message));
        }

        var options = _optionsMonitor?.CurrentValue ?? new SpeechOptions();
        var executablePath = ResolvePath(options.PiperExecutablePath, "PIPER_EXECUTABLE_PATH");
        var modelPath = ResolvePath(options.PiperModelPath, "PIPER_MODEL_PATH");
        var configPath = ResolvePath(options.PiperConfigPath, "PIPER_CONFIG_PATH");

        ValidateRequiredFile(executablePath, "Piper executable");
        ValidateRequiredFile(modelPath, "Piper model");

        if (!string.IsNullOrWhiteSpace(configPath) && !File.Exists(configPath))
        {
            throw new InvalidOperationException($"Piper config file not found: {configPath}");
        }

        var outputPath = Path.Combine(Path.GetTempPath(), $"mobaflow-piper-{Guid.NewGuid():N}.wav");

        try
        {
            var normalizedMessage = PiperPronunciationNormalizer.Normalize(
                message,
                options.PronunciationReplacements,
                options.EnablePronunciationNormalization);

            var request = new PiperSynthesisRequest(
                executablePath!,
                modelPath!,
                configPath,
                outputPath,
                normalizedMessage,
                CalculateLengthScale(options.Rate),
                Math.Clamp(options.PiperSentenceSilenceSeconds, 0, 2),
                TimeSpan.FromSeconds(Math.Max(1, options.PiperTimeoutSeconds)));

            _logger.LogInformation(
                "Synthesizing speech via Piper: {Message} (Model: {ModelPath}, LengthScale: {LengthScale}, SentenceSilence: {SentenceSilence})",
                normalizedMessage,
                modelPath,
                request.LengthScale,
                request.SentenceSilenceSeconds);

            var result = await _processRunner.SynthesizeAsync(request).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Piper synthesis failed with exit code {result.ExitCode}: {result.ErrorOutput}".Trim());
            }

            if (!File.Exists(outputPath))
            {
                throw new InvalidOperationException("Piper did not create an output WAV file.");
            }

            await _audioPlayer.PlayAsync(outputPath).ConfigureAwait(false);
            _logger.LogInformation("Piper speech synthesized successfully for text: {Message}", message);
        }
        finally
        {
            TryDeleteTempFile(outputPath);
        }
    }

    private static string? ResolvePath(string? configuredPath, string environmentVariableName)
    {
        return string.IsNullOrWhiteSpace(configuredPath)
            ? Environment.GetEnvironmentVariable(environmentVariableName)
            : configuredPath;
    }

    private static void ValidateRequiredFile(string? path, string description)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException($"{description} is not configured.");
        }

        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"{description} not found: {path}");
        }
    }

    private static double CalculateLengthScale(int rate)
    {
        var clampedRate = Math.Clamp(rate, -10, 10);
        return Math.Clamp(1.0 - (clampedRate * 0.05), 0.5, 1.5);
    }

    private static void TryDeleteTempFile(string outputPath)
    {
        try
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
        catch
        {
            // Temporary audio cleanup must not mask synthesis/playback errors.
        }
    }
}

/// <summary>
/// Executes Piper synthesis requests.
/// </summary>
public interface IPiperProcessRunner
{
    /// <summary>
    /// Runs Piper and returns process output.
    /// </summary>
    Task<PiperProcessResult> SynthesizeAsync(PiperSynthesisRequest request);
}

/// <summary>
/// Plays generated Piper WAV files.
/// </summary>
public interface IPiperAudioPlayer
{
    /// <summary>
    /// Plays a WAV file and completes when playback has finished.
    /// </summary>
    Task PlayAsync(string wavPath);
}

/// <summary>
/// Piper process input.
/// </summary>
public sealed record PiperSynthesisRequest(
    string ExecutablePath,
    string ModelPath,
    string? ConfigPath,
    string OutputPath,
    string Text,
    double LengthScale,
    double SentenceSilenceSeconds,
    TimeSpan Timeout);

/// <summary>
/// Piper process output.
/// </summary>
public sealed record PiperProcessResult(int ExitCode, string StandardOutput, string ErrorOutput)
{
    /// <summary>
    /// Gets whether Piper completed successfully.
    /// </summary>
    public bool Succeeded => ExitCode == 0;
}

/// <summary>
/// Default Piper process runner.
/// </summary>
public sealed class PiperProcessRunner : IPiperProcessRunner
{
    /// <inheritdoc />
    public async Task<PiperProcessResult> SynthesizeAsync(PiperSynthesisRequest request)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = request.ExecutablePath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.StartInfo.ArgumentList.Add("--model");
        process.StartInfo.ArgumentList.Add(request.ModelPath);
        process.StartInfo.ArgumentList.Add("--output_file");
        process.StartInfo.ArgumentList.Add(request.OutputPath);
        process.StartInfo.ArgumentList.Add("--length_scale");
        process.StartInfo.ArgumentList.Add(request.LengthScale.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));

        if (request.SentenceSilenceSeconds > 0)
        {
            process.StartInfo.ArgumentList.Add("--sentence_silence");
            process.StartInfo.ArgumentList.Add(request.SentenceSilenceSeconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrWhiteSpace(request.ConfigPath))
        {
            process.StartInfo.ArgumentList.Add("--config");
            process.StartInfo.ArgumentList.Add(request.ConfigPath);
        }

        process.Start();

        await process.StandardInput.WriteLineAsync(request.Text).ConfigureAwait(false);
        process.StandardInput.Close();

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var errorOutputTask = process.StandardError.ReadToEndAsync();
        var exitedTask = process.WaitForExitAsync();
        var completedTask = await Task.WhenAny(exitedTask, Task.Delay(request.Timeout)).ConfigureAwait(false);

        if (completedTask != exitedTask)
        {
            TryKill(process);
            return new PiperProcessResult(-1, string.Empty, $"Piper timed out after {request.Timeout.TotalSeconds:0} seconds.");
        }

        return new PiperProcessResult(
            process.ExitCode,
            await standardOutputTask.ConfigureAwait(false),
            await errorOutputTask.ConfigureAwait(false));
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

/// <summary>
/// Default WAV playback for Piper output.
/// </summary>
public sealed class PiperAudioPlayer : IPiperAudioPlayer
{
    /// <inheritdoc />
    public Task PlayAsync(string wavPath)
    {
        return Task.Run(() =>
        {
            using var player = new SoundPlayer(wavPath);
            player.PlaySync();
        });
    }
}
