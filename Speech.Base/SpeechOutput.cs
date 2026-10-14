// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.Base;

using Common.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>Resolves current settings, shares cache misses and owns cancellable announcement playback.</summary>
public sealed partial class SpeechOutput : ISpeechOutput, IAsyncDisposable
{
    private readonly AppSettings _settings;
    private readonly Dictionary<string, ISpeechProvider> _providers;
    private readonly IWavePlayer _player;
    private readonly ILogger<SpeechOutput> _logger;
    private readonly IAudioCache _cache;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly SharedSpeechSynthesis _synthesis;
    private readonly Lock _lifetimeGate = new();
    private TaskCompletionSource _drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _activeCalls;
    private Task? _shutdownTask;

    /// <summary>Creates one shared output for the host's provider registry, player and cache.</summary>
    public SpeechOutput(AppSettings settings, IEnumerable<ISpeechProvider> providers, IWavePlayer player,
        ILogger<SpeechOutput> logger, IAudioCache cache)
    {
        _settings = settings;
        _providers = providers.ToDictionary(provider => provider.Id, StringComparer.Ordinal);
        _player = player;
        _logger = logger;
        _cache = cache;
        _synthesis = new SharedSpeechSynthesis(_shutdown.Token);
        _drained.SetResult();
    }

    /// <summary>Resolves the current selection, diagnosing unknown or missing IDs.</summary>
    public ISpeechProvider? ResolveProvider()
    {
        var id = _settings.Speech.ProviderId;
        if (!string.IsNullOrWhiteSpace(id) && _providers.TryGetValue(id, out var selected)) return selected;
        UnknownProvider(_logger);
        return _providers.GetValueOrDefault("system-speech");
    }

    /// <inheritdoc />
    public Task<SpeechResult> SpeakAsync(string text, CancellationToken cancellationToken = default) =>
        TrackAsync(token => SpeakCoreAsync(text, token), cancellationToken);

    /// <summary>Returns retained WAV bytes for the later Settings size display.</summary>
    public Task<long> GetCacheSizeAsync(CancellationToken cancellationToken = default) =>
        TrackAsync(token => _cache.GetSizeAsync(token), cancellationToken);

    /// <summary>Clears cached audio; older syntheses cannot refill the cleared generation.</summary>
    public async Task ClearCacheAsync(CancellationToken cancellationToken = default) =>
        await TrackAsync(async token =>
        {
            await _cache.ClearAsync(token).ConfigureAwait(false);
            return true;
        }, cancellationToken).ConfigureAwait(false);

    /// <summary>Cancels and awaits synthesis and playback. Shutdown is asynchronous only, so callers cannot skip the wait.</summary>
    public ValueTask DisposeAsync() => new(BeginShutdown());

    private async Task<SpeechResult> SpeakCoreAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text)) return SpeechResult.Failure(SpeechFailureKind.InvalidRequest, "Speech text is empty.");
        var provider = ResolveProvider();
        if (provider is null) return ReportFailure(SpeechResult.Failure(SpeechFailureKind.Unavailable, "No speech provider is available."));
        var speech = _settings.Speech;
        if (!TryNormalize(text, out var normalized))
        {
            return ReportFailure(SpeechResult.Failure(SpeechFailureKind.InvalidRequest, "Speech text contains invalid characters."));
        }
        var request = new SpeechRequest(normalized, speech.VoiceName, speech.Language, speech.Rate, speech.Volume);
        try
        {
            var prepared = provider.PrepareSynthesis(request);
            var key = SpeechCacheKey.Create(provider.Id, prepared.OptionsFingerprint, request);
            // The generation comes from the same locked lookup, so a clear after it rejects this miss's audio.
            var lookup = await ReadCacheAsync(key, cancellationToken).ConfigureAwait(false);
            if (lookup.Audio is { } cached)
            {
                return await PlayAsync(SpeechResult.Success(cached.ToArray()),
                    () => _cache.MarkPlayedAsync(key, cancellationToken), cancellationToken).ConfigureAwait(false);
            }
            // Playback and storage run inside the shared flight, so an overlapping identical request joins it instead
            // of synthesizing again. Only audio that played successfully enters the cache.
            return await _synthesis.RunAsync(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{lookup.Generation}:{key}"),
                token => SynthesizeSharedAsync(key, prepared, token),
                result => PlayAsync(result, () => result.IsCacheable
                    ? _cache.StoreAsync(key, result.WaveAudio, lookup.Generation, cancellationToken)
                    : Task.CompletedTask, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or ArgumentException or NotSupportedException or System.ComponentModel.Win32Exception or UnauthorizedAccessException or TimeoutException or System.Runtime.InteropServices.COMException)
        {
            return ReportFailure(SpeechResult.Failure(SpeechFailureKind.Failed, "Speech synthesis or playback failed."));
        }
    }

    /// <summary>Canonical normalization; user text with an unpaired UTF-16 surrogate cannot be normalized.</summary>
    private static bool TryNormalize(string text, out string normalized)
    {
        try
        {
            normalized = text.Normalize();
            return true;
        }
        catch (ArgumentException)
        {
            normalized = string.Empty;
            return false;
        }
    }

    private async Task<SpeechResult> PlayAsync(SpeechResult result, Func<Task> recordPlayback, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!result.IsSuccess) return ReportFailure(result);
        await _player.PlayAsync(result.WaveAudio, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        await UseCacheAsync(recordPlayback).ConfigureAwait(false);
        return result;
    }

    private async Task<SpeechResult> SynthesizeSharedAsync(string key, PreparedSpeechSynthesis prepared, CancellationToken token)
    {
        // Recheck inside the shared flight: another caller may have stored this audio after the first miss.
        var lookup = await ReadCacheAsync(key, token).ConfigureAwait(false);
        if (lookup.Audio is { } audio) return SpeechResult.Success(audio.ToArray());
        var result = await prepared.SynthesizeAsync(token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return result;
    }

    private async Task<AudioCacheLookup> ReadCacheAsync(string key, CancellationToken token)
    {
        // Read before the lookup: if it fails, a clear completing meanwhile must still reject this request's audio.
        var generationBefore = _cache.Generation;
        try { return await _cache.TryGetAsync(key, token).ConfigureAwait(false); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            CacheUnavailable(_logger);
            return new AudioCacheLookup((ReadOnlyMemory<byte>?)null, generationBefore);
        }
    }

    private async Task UseCacheAsync(Func<Task> operation)
    {
        try { await operation().ConfigureAwait(false); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { CacheUnavailable(_logger); }
    }

    /// <summary>Counts the operation for shutdown, which cancels it and waits before dependent services are disposed.</summary>
    private async Task<T> TrackAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BeginCall();
        try
        {
            using var call = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdown.Token);
            return await operation(call.Token).ConfigureAwait(false);
        }
        finally { EndCall(); }
    }

    private void BeginCall()
    {
        lock (_lifetimeGate)
        {
            ObjectDisposedException.ThrowIf(_shutdownTask is not null, this);
            if (_activeCalls++ == 0) _drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    private void EndCall()
    {
        lock (_lifetimeGate) { if (--_activeCalls == 0) _drained.TrySetResult(); }
    }

    private Task BeginShutdown()
    {
        lock (_lifetimeGate) { return _shutdownTask ??= StopAsync(_drained.Task); }
    }

    private async Task StopAsync(Task callersDrained)
    {
        await _shutdown.CancelAsync().ConfigureAwait(false);
        await _synthesis.DrainAsync().ConfigureAwait(false);
        await callersDrained.ConfigureAwait(false);
        _shutdown.Dispose();
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Unknown or missing speech provider ID; selecting system-speech.")]
    private static partial void UnknownProvider(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Announcement skipped: {FailureKind}. {Reason}")]
    private static partial void AnnouncementSkipped(ILogger logger, SpeechFailureKind? failureKind, string reason);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Speech cache unavailable; announcement continues without cache storage.")]
    private static partial void CacheUnavailable(ILogger logger);

    private SpeechResult ReportFailure(SpeechResult result)
    {
        AnnouncementSkipped(_logger, result.FailureKind, result.Reason);
        return result;
    }
}
