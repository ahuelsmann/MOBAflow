// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.SystemSpeech;

using Base;
using Common.Configuration;
using System.Globalization;
using System.Speech.Synthesis;

/// <summary>Windows SAPI synthesis to memory. The provider never opens an audio device.</summary>
public sealed class SystemSpeechProvider(AppSettings settings) : ISpeechProvider
{
    /// <inheritdoc />
    public string Id => "system-speech";
    /// <inheritdoc />
    public string DisplayName => "System Speech (Windows SAPI)";
    /// <inheritdoc />
    public bool IsLocal => true;

    /// <inheritdoc />
    public Task<SpeechCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var synthesizer = new SpeechSynthesizer();
            var voices = GetAllVoices(synthesizer).Where(voice => voice.Enabled)
                .Select(voice => new SpeechVoice(voice.VoiceInfo.Name, voice.VoiceInfo.Culture.Name)).ToArray();
            return new SpeechCapabilities(voices, true, true, voices.Length > 0, voices.Length > 0 ? string.Empty : "No Windows speech voice is installed.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or ArgumentException or NotSupportedException or System.ComponentModel.Win32Exception or UnauthorizedAccessException or TimeoutException or System.Runtime.InteropServices.COMException)
        {
            return new SpeechCapabilities([], true, true, false, "Windows speech is unavailable.");
        }
    }, cancellationToken);

    // The .NET parameterless overload enumerates all cultures; the overload with CultureInfo filters them.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1304", Justification = "All installed voices are required; a culture filter would discard usable voices. The parameterless .NET implementation is culture-independent.")]
    private static System.Collections.ObjectModel.ReadOnlyCollection<InstalledVoice> GetAllVoices(SpeechSynthesizer synthesizer) => synthesizer.GetInstalledVoices();

    private static bool TrySelectVoice(SpeechSynthesizer synthesizer, string voiceName, string language)
    {
        if (!string.IsNullOrWhiteSpace(voiceName))
        {
            if (!GetAllVoices(synthesizer).Any(voice => voice.Enabled && voice.VoiceInfo.Name == voiceName))
            {
                return false;
            }
            synthesizer.SelectVoice(voiceName);
        }
        else if (!string.IsNullOrWhiteSpace(language))
        {
            synthesizer.SelectVoiceByHints(VoiceGender.NotSet, VoiceAge.NotSet, 0, CultureInfo.GetCultureInfo(language));
        }
        return true;
    }

    /// <inheritdoc />
    public async Task<SpeechResult> SynthesizeAsync(SpeechRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(request.Text) || request.Rate is < -10 or > 10 || request.Volume > 100)
        {
            return SpeechResult.Failure(SpeechFailureKind.InvalidRequest, "Invalid speech text, rate or volume.");
        }

        var defaultVoice = settings.Speech.SystemSpeech.DefaultVoiceName;
        try
        {
            return await Task.Run(async () =>
            {
                using var synthesizer = new SpeechSynthesizer { Rate = request.Rate, Volume = (int)request.Volume };
                using var stream = new MemoryStream();
                synthesizer.SetOutputToWaveStream(stream);
                var voiceName = string.IsNullOrWhiteSpace(request.VoiceName) ? defaultVoice : request.VoiceName;
                if (!TrySelectVoice(synthesizer, voiceName, request.Language))
                {
                    return SpeechResult.Failure(SpeechFailureKind.InvalidRequest, "The requested Windows speech voice is not installed.");
                }
                var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                synthesizer.SpeakCompleted += (_, args) =>
                {
                    if (args.Error is not null) completed.TrySetException(args.Error);
                    else completed.TrySetResult();
                };
                cancellationToken.ThrowIfCancellationRequested();
                synthesizer.SpeakAsync(request.Text);
                using var registration = cancellationToken.Register(synthesizer.SpeakAsyncCancelAll);
                // Keep the synthesizer/stream alive until SAPI acknowledges completion or cancellation.
                await completed.Task.ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                synthesizer.SetOutputToNull();
                return SpeechResult.Success(stream.ToArray());
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or ArgumentException or NotSupportedException or System.ComponentModel.Win32Exception or UnauthorizedAccessException or TimeoutException or System.Runtime.InteropServices.COMException)
        {
            return SpeechResult.Failure(SpeechFailureKind.Unavailable, "Windows speech synthesis is unavailable.");
        }
    }
}
