// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.SystemSpeech;

using Base;
using Common.Configuration;
using Microsoft.Win32;
using System.Globalization;
using System.Speech.Synthesis;

/// <summary>Windows SAPI synthesis to memory. The provider never opens an audio device.</summary>
/// <param name="settings">Current speech settings.</param>
/// <param name="windowsDefaultVoice">Reads the Windows default SAPI voice token; defaults to the registry.</param>
public sealed class SystemSpeechProvider(AppSettings settings, Func<string>? windowsDefaultVoice = null) : ISpeechProvider
{
    private const string VoicesKey = @"Software\Microsoft\Speech\Voices";
    private readonly Func<string> _windowsDefaultVoice = windowsDefaultVoice ?? ReadWindowsDefaultVoice;

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

    /// <summary>Selects the voice; returns false when a requested voice is missing and a substitute speaks instead.</summary>
    private static bool SelectVoice(SpeechSynthesizer synthesizer, string voiceName, string defaultVoice, string language)
    {
        var voices = GetAllVoices(synthesizer).Where(voice => voice.Enabled).ToArray();
        var selected = voices.FirstOrDefault(voice => string.Equals(voice.VoiceInfo.Name, voiceName, StringComparison.OrdinalIgnoreCase))
            ?? voices.FirstOrDefault(voice => string.Equals(voice.VoiceInfo.Name, defaultVoice, StringComparison.OrdinalIgnoreCase));
        if (selected is not null)
        {
            synthesizer.SelectVoice(selected.VoiceInfo.Name);
            return string.IsNullOrWhiteSpace(voiceName) ||
                string.Equals(selected.VoiceInfo.Name, voiceName, StringComparison.OrdinalIgnoreCase);
        }
        if (string.IsNullOrWhiteSpace(voiceName) && string.IsNullOrWhiteSpace(defaultVoice))
        {
            if (!string.IsNullOrWhiteSpace(language))
            {
                synthesizer.SelectVoiceByHints(VoiceGender.NotSet, VoiceAge.NotSet, 0, CultureInfo.GetCultureInfo(language));
            }
            return true;
        }
        // An unavailable saved/default voice leaves SAPI's installed default selected.
        return false;
    }

    /// <inheritdoc />
    public PreparedSpeechSynthesis PrepareSynthesis(SpeechRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var defaultVoice = settings.Speech.SystemSpeech.DefaultVoiceName;
        // Without a configured voice SAPI speaks with the Windows default voice; changing it must miss the cache.
        var implicitVoice = string.IsNullOrWhiteSpace(request.VoiceName) && string.IsNullOrWhiteSpace(defaultVoice)
            ? _windowsDefaultVoice()
            : string.Empty;
        return new PreparedSpeechSynthesis(SpeechCacheKey.Fingerprint([defaultVoice, implicitVoice]),
            token => SynthesizeAsync(request, defaultVoice, token));
    }

    /// <summary>The SAPI default voice token, per user with the machine-wide value as fallback; empty if unreadable.</summary>
    private static string ReadWindowsDefaultVoice()
    {
        try
        {
            using var user = Registry.CurrentUser.OpenSubKey(VoicesKey);
            if (user?.GetValue("DefaultTokenId") is string userVoice && userVoice.Length > 0) return userVoice;
            using var machine = Registry.LocalMachine.OpenSubKey(VoicesKey);
            return machine?.GetValue("DefaultTokenId") as string ?? string.Empty;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or IOException or UnauthorizedAccessException)
        {
            return string.Empty;
        }
    }

    /// <inheritdoc />
    public Task<SpeechResult> SynthesizeAsync(SpeechRequest request, CancellationToken cancellationToken = default) =>
        PrepareSynthesis(request).SynthesizeAsync(cancellationToken);

    private static async Task<SpeechResult> SynthesizeAsync(SpeechRequest request, string defaultVoice, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(request.Text) || request.Rate is < -10 or > 10 || request.Volume > 100)
        {
            return SpeechResult.Failure(SpeechFailureKind.InvalidRequest, "Invalid speech text, rate or volume.");
        }

        try
        {
            return await Task.Run(async () =>
            {
                using var synthesizer = new SpeechSynthesizer { Rate = request.Rate, Volume = (int)request.Volume };
                using var stream = new MemoryStream();
                synthesizer.SetOutputToWaveStream(stream);
                var requestedVoice = SelectVoice(synthesizer, request.VoiceName, defaultVoice, request.Language);
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
                // Substitute-voice audio is not cached, so installing the requested voice later takes effect.
                return SpeechResult.Success(stream.ToArray(), requestedVoice);
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
