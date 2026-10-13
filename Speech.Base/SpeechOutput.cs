// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.Base;

using Common.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>Resolves a provider and captures shared settings for every announcement.</summary>
public sealed partial class SpeechOutput(
    AppSettings settings,
    IEnumerable<ISpeechProvider> providers,
    IWavePlayer player,
    ILogger<SpeechOutput> logger) : ISpeechOutput
{
    private readonly Dictionary<string, ISpeechProvider> _providers = providers.ToDictionary(provider => provider.Id, StringComparer.Ordinal);

    /// <summary>Resolves the current selection, diagnosing unknown or missing IDs.</summary>
    public ISpeechProvider? ResolveProvider()
    {
        var id = settings.Speech.ProviderId;
        if (!string.IsNullOrWhiteSpace(id) && _providers.TryGetValue(id, out var selected))
        {
            return selected;
        }

        UnknownProvider(logger);
        return _providers.GetValueOrDefault("system-speech");
    }

    /// <inheritdoc />
    public async Task<SpeechResult> SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(text))
        {
            return SpeechResult.Failure(SpeechFailureKind.InvalidRequest, "Speech text is empty.");
        }

        var provider = ResolveProvider();
        if (provider is null)
        {
            return ReportFailure(SpeechResult.Failure(SpeechFailureKind.Unavailable, "No speech provider is available."));
        }

        var speech = settings.Speech;
        var request = new SpeechRequest(text, speech.VoiceName, speech.Language, speech.Rate, speech.Volume);
        try
        {
            var result = await provider.SynthesizeAsync(request, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!result.IsSuccess)
            {
                return ReportFailure(result);
            }

            await player.PlayAsync(result.WaveAudio, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or ArgumentException or NotSupportedException or System.ComponentModel.Win32Exception or UnauthorizedAccessException or TimeoutException or System.Runtime.InteropServices.COMException)
        {
            // Provider/SDK exceptions can contain credentials or request text. Keep diagnostics safe.
            return ReportFailure(SpeechResult.Failure(SpeechFailureKind.Failed, "Speech synthesis or playback failed."));
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Unknown or missing speech provider ID; selecting system-speech.")]
    private static partial void UnknownProvider(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Announcement skipped: {FailureKind}. {Reason}")]
    private static partial void AnnouncementSkipped(ILogger logger, SpeechFailureKind? failureKind, string reason);

    private SpeechResult ReportFailure(SpeechResult result)
    {
        AnnouncementSkipped(logger, result.FailureKind, result.Reason);
        return result;
    }
}
