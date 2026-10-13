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
    public async Task<SpeechResult> SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BeginCall();
        try
        {
            using var call = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdown.Token);
            return await SpeakCoreAsync(text, call.Token).ConfigureAwait(false);
        }
        finally { EndCall(); }
    }

    /// <summary>Returns retained WAV bytes for the later Settings size display.</summary>
    public Task<long> GetCacheSizeAsync(CancellationToken cancellationToken = default) => _cache.GetSizeAsync(cancellationToken);

    /// <summary>Clears cached audio; older syntheses cannot refill the cleared generation.</summary>
    public Task ClearCacheAsync(CancellationToken cancellationToken = default) => _cache.ClearAsync(cancellationToken);

    /// <summary>Cancels and awaits synthesis and playback. Shutdown is asynchronous only, so callers cannot skip the wait.</summary>
    public ValueTask DisposeAsync() => new(BeginShutdown());

    private async Task<SpeechResult> SpeakCoreAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text)) return SpeechResult.Failure(SpeechFailureKind.InvalidRequest, "Speech text is empty.");
        var provider = ResolveProvider();
        if (provider is null) return ReportFailure(SpeechResult.Failure(SpeechFailureKind.Unavailable, "No speech provider is available."));
        var speech = _settings.Speech;
        var request = new SpeechRequest(text.Normalize(), speech.VoiceName, speech.Language, speech.Rate, speech.Volume);
        try
        {
            var prepared = provider.PrepareSynthesis(request);
            var key = SpeechCacheKey.Create(provider.Id, prepared.OptionsFingerprint, request);
            var audio = await ReadCacheAsync(key, cancellationToken).ConfigureAwait(false);
            // Read after the lookup: it reloads the shared index, including a clear made by another process.
            var generation = _cache.Generation;
            var result = audio is null
                ? await _synthesis.RunAsync(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{generation}:{key}"),
                    token => SynthesizeAndCacheAsync(key, generation, prepared, token), cancellationToken).ConfigureAwait(false)
                : SpeechResult.Success(audio);
            cancellationToken.ThrowIfCancellationRequested();
            if (!result.IsSuccess) return ReportFailure(result);
            await _player.PlayAsync(result.WaveAudio, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await UseCacheAsync(() => _cache.MarkPlayedAsync(key, cancellationToken)).ConfigureAwait(false);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or ArgumentException or NotSupportedException or System.ComponentModel.Win32Exception or UnauthorizedAccessException or TimeoutException or System.Runtime.InteropServices.COMException)
        {
            return ReportFailure(SpeechResult.Failure(SpeechFailureKind.Failed, "Speech synthesis or playback failed."));
        }
    }

    private async Task<SpeechResult> SynthesizeAndCacheAsync(string key, long generation, PreparedSpeechSynthesis prepared, CancellationToken token)
    {
        // Recheck inside the shared flight: another completed flight may have filled the cache after the first miss.
        var audio = await ReadCacheAsync(key, token).ConfigureAwait(false);
        if (audio is not null) return SpeechResult.Success(audio);
        var result = await prepared.SynthesizeAsync(token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        if (result.IsSuccess)
        {
            await UseCacheAsync(() => _cache.StoreAsync(key, result.WaveAudio, generation, token)).ConfigureAwait(false);
        }
        return result;
    }

    private async Task<byte[]?> ReadCacheAsync(string key, CancellationToken token)
    {
        try { return await _cache.TryGetAsync(key, token).ConfigureAwait(false); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            CacheUnavailable(_logger);
            return null;
        }
    }

    private async Task UseCacheAsync(Func<Task> operation)
    {
        try { await operation().ConfigureAwait(false); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { CacheUnavailable(_logger); }
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
