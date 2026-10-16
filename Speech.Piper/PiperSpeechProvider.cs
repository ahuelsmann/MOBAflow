// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.Piper;

using Base;
using Common.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>Local Piper synthesis. Each call owns its temporary WAV and captures current model options.</summary>
public sealed partial class PiperSpeechProvider(
    AppSettings settings,
    ILogger<PiperSpeechProvider> logger,
    IPiperProcessRunner? processRunner = null,
    string? diagnosticDirectory = null) : ISpeechProvider
{
    private readonly IPiperProcessRunner _runner = processRunner ?? new PiperProcessRunner();
    private readonly string _diagnosticDirectory = diagnosticDirectory ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MOBAflow", "PiperDiagnostics");

    /// <inheritdoc />
    public string Id => "piper";
    /// <inheritdoc />
    public string DisplayName => "Piper TTS";
    /// <inheritdoc />
    public bool IsLocal => true;

    /// <inheritdoc />
    public async Task<SpeechCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var options = CaptureOptions();
        var voice = Path.GetFileNameWithoutExtension(options.ModelPath);
        var language = voice.Split('-')[0].Replace('_', '-');
        var available = false;
        if (IsConfigured(options))
        {
            try
            {
                var probe = await _runner.ProbeAsync(options.ExecutablePath,
                    TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 1, 5)), cancellationToken).ConfigureAwait(false);
                available = probe.Succeeded;
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or ArgumentException or NotSupportedException or System.ComponentModel.Win32Exception or UnauthorizedAccessException or TimeoutException)
            {
                // A configured path does not establish that Piper can start. Keep native diagnostics private.
            }
        }
        return new SpeechCapabilities(string.IsNullOrEmpty(voice) ? [] : [new SpeechVoice(voice, language)],
            true, true, available, available ? string.Empty : "Piper executable or voice model is unavailable.");
    }

    /// <inheritdoc />
    public PreparedSpeechSynthesis PrepareSynthesis(SpeechRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var options = CaptureOptions();
        var values = new List<string>
        {
            options.ExecutablePath, options.ModelPath, options.ConfigPath,
            Math.Clamp(options.SentenceSilenceSeconds, 0, 2).ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            options.EnablePronunciationNormalization ? "1" : "0"
        };
        foreach (var pair in options.PronunciationReplacements)
        {
            values.Add(pair.Key);
            values.Add(pair.Value);
        }
        return new PreparedSpeechSynthesis(SpeechCacheKey.Fingerprint(values), token => SynthesizeAsync(request, options, token));
    }

    /// <inheritdoc />
    public Task<SpeechResult> SynthesizeAsync(SpeechRequest request, CancellationToken cancellationToken = default) =>
        PrepareSynthesis(request).SynthesizeAsync(cancellationToken);

    private async Task<SpeechResult> SynthesizeAsync(SpeechRequest request, PiperSpeechSettings options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsConfigured(options))
        {
            return SpeechResult.Failure(SpeechFailureKind.Unavailable, "Piper executable or voice model is not configured.");
        }

        if (string.IsNullOrWhiteSpace(request.Text) || request.Rate is < -10 or > 10 || request.Volume > 100)
        {
            return SpeechResult.Failure(SpeechFailureKind.InvalidRequest, "Invalid speech text, rate or volume.");
        }

        // The selected model owns the voice, including when settings retain another provider's voice name.
        var outputPath = Path.Combine(Path.GetTempPath(), $"mobaflow-piper-{Guid.NewGuid():N}.wav");
        try
        {
            var text = PiperPronunciationNormalizer.Normalize(request.Text, options.PronunciationReplacements, options.EnablePronunciationNormalization);
            var synthesis = new PiperSynthesisRequest(options.ExecutablePath, options.ModelPath, options.ConfigPath,
                outputPath, text, 1.0 - (request.Rate * 0.05), Math.Clamp(options.SentenceSilenceSeconds, 0, 2),
                TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds)));
            var result = await _runner.SynthesizeAsync(synthesis, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!result.Succeeded || !File.Exists(outputPath))
            {
                return SpeechResult.Failure(SpeechFailureKind.Failed, "Piper did not produce speech audio.");
            }

            PcmWavePostProcessor.Process(outputPath);
            var audio = await File.ReadAllBytesAsync(outputPath, cancellationToken).ConfigureAwait(false);
            WaveAudio.ApplyVolume(audio, request.Volume);
            if (options.EnableAudioDiagnostics)
            {
                await SaveDiagnosticAsync(audio, cancellationToken).ConfigureAwait(false);
            }

            return SpeechResult.Success(audio);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or ArgumentException or NotSupportedException or System.ComponentModel.Win32Exception or UnauthorizedAccessException or TimeoutException or System.Runtime.InteropServices.COMException)
        {
            return SpeechResult.Failure(SpeechFailureKind.Failed, "Piper synthesis failed.");
        }
        finally
        {
            try { File.Delete(outputPath); }
            catch (IOException) { CleanupFailed(logger); }
            catch (UnauthorizedAccessException) { CleanupFailed(logger); }
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Unable to remove temporary Piper audio.")]
    private static partial void CleanupFailed(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Unable to save Piper diagnostic audio.")]
    private static partial void DiagnosticSaveFailed(ILogger logger);

    private PiperSpeechSettings CaptureOptions()
    {
        var current = settings.Speech.Piper;
        return new PiperSpeechSettings
        {
            ExecutablePath = ResolvePath(current.ExecutablePath, "PIPER_EXECUTABLE_PATH"),
            ModelPath = ResolvePath(current.ModelPath, "PIPER_MODEL_PATH"),
            ConfigPath = ResolvePath(current.ConfigPath, "PIPER_CONFIG_PATH"),
            TimeoutSeconds = current.TimeoutSeconds,
            SentenceSilenceSeconds = current.SentenceSilenceSeconds,
            EnablePronunciationNormalization = current.EnablePronunciationNormalization,
            EnableAudioDiagnostics = current.EnableAudioDiagnostics,
            PronunciationReplacements = new Dictionary<string, string>(current.PronunciationReplacements)
        };
    }

    private static string ResolvePath(string path, string variable) =>
        string.IsNullOrWhiteSpace(path) ? Environment.GetEnvironmentVariable(variable) ?? string.Empty : path;

    private static bool IsConfigured(PiperSpeechSettings options) =>
        File.Exists(options.ExecutablePath) && File.Exists(options.ModelPath) &&
        (string.IsNullOrWhiteSpace(options.ConfigPath) || File.Exists(options.ConfigPath));

    private async Task SaveDiagnosticAsync(byte[] audio, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(_diagnosticDirectory);
            await File.WriteAllBytesAsync(Path.Combine(_diagnosticDirectory, "last-piper-announcement.wav"), audio, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException) { DiagnosticSaveFailed(logger); }
        catch (UnauthorizedAccessException) { DiagnosticSaveFailed(logger); }
    }
}
