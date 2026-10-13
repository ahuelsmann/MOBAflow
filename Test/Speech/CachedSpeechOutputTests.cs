// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Speech;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Common.Configuration;
using Moba.Speech.Base;
using Moq;

[TestFixture]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812", Justification = "NUnit creates this fixture through reflection.")]
internal sealed partial class CachedSpeechOutputTests : IDisposable
{
    private static readonly string[] FullCacheAnnouncements = ["First", "Second", "Unheard", "First", "Second"];
    private static readonly string[] FullCacheSyntheses = ["First", "Second", "Unheard"];
    private string _directory = null!;
    private AudioCache _cache = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "mobaflow-output-cache-tests-" + Guid.NewGuid().ToString("N"));
        _cache = new AudioCache(_directory, () => 1000000, TimeProvider.System);
    }

    [TearDown]
    public void TearDown()
    {
        Dispose();
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }

    public void Dispose() => _cache?.Dispose();

    private SpeechOutput CreateOutput(SpeechOutputTests.FakeProvider provider, IWavePlayer player, AppSettings? settings = null, IAudioCache? cache = null) =>
        new(settings ?? new AppSettings(), [provider], player, NullLogger<SpeechOutput>.Instance, cache ?? _cache);

    [Test]
    public async Task IdenticalAnnouncementUsesCacheAndStillPlaysEveryTime()
    {
        var provider = new SpeechOutputTests.FakeProvider("system-speech");
        var player = new SpeechOutputTests.FakePlayer();
        var output = CreateOutput(provider, player);
        await using var outputLifetime = output.ConfigureAwait(false);
        await (output.SpeakAsync("Next stop Central.")).ConfigureAwait(false);
        await (output.SpeakAsync("Next stop Central.")).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(provider.Requests, Has.Count.EqualTo(1));
            Assert.That(player.PlayCount, Is.EqualTo(2));
            Assert.That(await (_cache.GetSizeAsync()).ConfigureAwait(false), Is.GreaterThan(0));
        }
    }

    [Test]
    public async Task ConcurrentIdenticalMissesShareOneSynthesis()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource<SpeechResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new SpeechOutputTests.FakeProvider("system-speech")
        {
            Synthesize = (_, token) => { started.TrySetResult(); return completed.Task.WaitAsync(token); }
        };
        var player = new SpeechOutputTests.FakePlayer();
        var output = CreateOutput(provider, player);
        await using var outputLifetime = output.ConfigureAwait(false);
        var first = output.SpeakAsync("Same text");
        await (started.Task.WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
        var second = output.SpeakAsync("Same text");
        completed.SetResult(SpeechResult.Success(TestWaveAudio.Create([1])));
        await (Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(provider.Requests, Has.Count.EqualTo(1));
            Assert.That(player.PlayCount, Is.EqualTo(2));
        }
    }
    [Test]
    public async Task OneCancelledWaiterDoesNotCancelSynthesisForAnotherCaller()
    {
        var started = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource<SpeechResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new SpeechOutputTests.FakeProvider("system-speech")
        {
            Synthesize = (_, token) => { started.TrySetResult(token); return completed.Task.WaitAsync(token); }
        };
        var player = new SpeechOutputTests.FakePlayer();
        var output = CreateOutput(provider, player, cache: TestSpeechCache.Empty());
        await using var outputLifetime = output.ConfigureAwait(false);
        using var cancellation = new CancellationTokenSource();
        var first = output.SpeakAsync("Shared", cancellation.Token);
        var synthesisToken = await (started.Task.WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
        var second = output.SpeakAsync("Shared");
        await (cancellation.CancelAsync()).ConfigureAwait(false);
        await (Assert.CatchAsync<OperationCanceledException>(() => first.WaitAsync(TimeSpan.FromSeconds(10)))).ConfigureAwait(false);
        Assert.That(synthesisToken.IsCancellationRequested, Is.False);
        completed.SetResult(SpeechResult.Success(TestWaveAudio.Create([1])));
        await (second.WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(provider.Requests, Has.Count.EqualTo(1));
            Assert.That(player.PlayCount, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task LastCancelledWaiterCancelsProviderAndNextCallCanStartBeforeOldWorkAcknowledges()
    {
        var started = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var oldCompleted = new TaskCompletionSource<SpeechResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var provider = new SpeechOutputTests.FakeProvider("system-speech")
        {
            Synthesize = (_, token) =>
            {
                if (Interlocked.Increment(ref calls) == 1) { started.SetResult(token); return oldCompleted.Task; }
                return Task.FromResult(SpeechResult.Success(TestWaveAudio.Create([2])));
            }
        };
        var player = new SpeechOutputTests.FakePlayer();
        var output = CreateOutput(provider, player, cache: TestSpeechCache.Empty());
        await using var outputLifetime = output.ConfigureAwait(false);
        using var firstCancellation = new CancellationTokenSource();
        using var secondCancellation = new CancellationTokenSource();
        var first = output.SpeakAsync("Shared", firstCancellation.Token);
        var synthesisToken = await (started.Task.WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
        var second = output.SpeakAsync("Shared", secondCancellation.Token);
        await (firstCancellation.CancelAsync()).ConfigureAwait(false);
        await (Assert.CatchAsync<OperationCanceledException>(() => first.WaitAsync(TimeSpan.FromSeconds(10)))).ConfigureAwait(false);
        Assert.That(synthesisToken.IsCancellationRequested, Is.False);
        await (secondCancellation.CancelAsync()).ConfigureAwait(false);
        await (Assert.CatchAsync<OperationCanceledException>(() => second.WaitAsync(TimeSpan.FromSeconds(10)))).ConfigureAwait(false);
        Assert.That(synthesisToken.IsCancellationRequested, Is.True);
        await (output.SpeakAsync("Shared").WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
        oldCompleted.SetResult(SpeechResult.Success(TestWaveAudio.Create([1])));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(provider.Requests, Has.Count.EqualTo(2));
            Assert.That(player.PlayCount, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task ShutdownCancelsAndWaitsForSharedSynthesisWithoutPlayback()
    {
        var started = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource<SpeechResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new SpeechOutputTests.FakeProvider("system-speech")
        {
            Synthesize = (_, token) => { started.SetResult(token); return completed.Task; }
        };
        var player = new SpeechOutputTests.FakePlayer();
        var output = CreateOutput(provider, player);
        await using var outputLifetime = output.ConfigureAwait(false);
        var speech = output.SpeakAsync("Shutdown");
        var synthesisToken = await (started.Task.WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
        var synthesisCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellationObserved = synthesisToken.Register(() => synthesisCancelled.TrySetResult());
        var stopping = output.DisposeAsync().AsTask();
        await (synthesisCancelled.Task.WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(synthesisToken.IsCancellationRequested, Is.True);
            Assert.That(stopping.IsCompleted, Is.False);
        }
        completed.SetResult(SpeechResult.Success(TestWaveAudio.Create([1])));
        await (stopping.WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
        await (Assert.CatchAsync<OperationCanceledException>(() => speech)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.PlayCount, Is.Zero);
            Assert.That(await (_cache.GetSizeAsync()).ConfigureAwait(false), Is.Zero);
        }
        await (Assert.ThrowsAsync<ObjectDisposedException>(() => output.SpeakAsync("After shutdown"))).ConfigureAwait(false);
    }

    [Test]
    public async Task ShutdownStopsPlaybackIncludingCacheHits()
    {
        var provider = new SpeechOutputTests.FakeProvider("system-speech");
        var playbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var plays = 0;
        var player = new Moq.Mock<IWavePlayer>();
        player.Setup(value => value.PlayAsync(Moq.It.IsAny<ReadOnlyMemory<byte>>(), Moq.It.IsAny<CancellationToken>()))
            .Returns(async (ReadOnlyMemory<byte> _, CancellationToken token) =>
            {
                if (Interlocked.Increment(ref plays) == 1) return;
                playbackStarted.SetResult();
                await (Task.Delay(Timeout.InfiniteTimeSpan, token)).ConfigureAwait(false);
            });
        var output = CreateOutput(provider, player.Object);
        await using var outputLifetime = output.ConfigureAwait(false);
        await (output.SpeakAsync("Cached")).ConfigureAwait(false);
        var cachedSpeech = output.SpeakAsync("Cached");
        await (playbackStarted.Task.WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
        await (output.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
        await (Assert.CatchAsync<OperationCanceledException>(() => cachedSpeech)).ConfigureAwait(false);
        Assert.That(provider.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task SharedAndProviderOptionChangesCauseFreshSynthesis()
    {
        var settings = new AppSettings();
        var provider = new SpeechOutputTests.FakeProvider("system-speech");
        var output = CreateOutput(provider, new SpeechOutputTests.FakePlayer(), settings);
        await using var outputLifetime = output.ConfigureAwait(false);
        await (output.SpeakAsync("Text")).ConfigureAwait(false);
        await (output.SpeakAsync("Text")).ConfigureAwait(false);
        settings.Speech.VoiceName = "New voice";
        await (output.SpeakAsync("Text")).ConfigureAwait(false);
        settings.Speech.Language = "de-DE";
        await (output.SpeakAsync("Text")).ConfigureAwait(false);
        settings.Speech.Rate = 3;
        await (output.SpeakAsync("Text")).ConfigureAwait(false);
        settings.Speech.Volume = 10;
        await (output.SpeakAsync("Text")).ConfigureAwait(false);
        provider.OptionsFingerprint = "new model";
        await (output.SpeakAsync("Text")).ConfigureAwait(false);
        await (output.SpeakAsync("New text")).ConfigureAwait(false);
        Assert.That(provider.Requests, Has.Count.EqualTo(7));
    }

    [Test]
    public async Task DifferentKeysSynthesizeIndependently()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource<SpeechResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new SpeechOutputTests.FakeProvider("system-speech")
        {
            Synthesize = (request, token) => request.Text == "Held"
                ? HoldAsync(token) : Task.FromResult(SpeechResult.Success(TestWaveAudio.Create([2])))
        };
        Task<SpeechResult> HoldAsync(CancellationToken token)
        {
            started.SetResult();
            return completed.Task.WaitAsync(token);
        }
        var player = new SpeechOutputTests.FakePlayer();
        var output = CreateOutput(provider, player);
        await using var outputLifetime = output.ConfigureAwait(false);
        var held = output.SpeakAsync("Held");
        await (started.Task.WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
        await (output.SpeakAsync("Independent").WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
        Assert.That(held.IsCompleted, Is.False);
        completed.SetResult(SpeechResult.Success(TestWaveAudio.Create([1])));
        await (held.WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
        Assert.That(player.PlayCount, Is.EqualTo(2));
    }

    [Test]
    public async Task FailedSynthesisIsNotCachedAndCanRecover()
    {
        var provider = new SpeechOutputTests.FakeProvider("system-speech")
        {
            Result = SpeechResult.Failure(SpeechFailureKind.Unavailable, "Unavailable.")
        };
        var player = new SpeechOutputTests.FakePlayer();
        var output = CreateOutput(provider, player);
        await using var outputLifetime = output.ConfigureAwait(false);
        await (output.SpeakAsync("Retry")).ConfigureAwait(false);
        provider.Result = SpeechResult.Success(TestWaveAudio.Create([1]));
        await (output.SpeakAsync("Retry")).ConfigureAwait(false);
        await (output.SpeakAsync("Retry")).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(provider.Requests, Has.Count.EqualTo(2));
            Assert.That(player.PlayCount, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task CorruptCachedWaveIsResynthesized()
    {
        var provider = new SpeechOutputTests.FakeProvider("system-speech");
        var output = CreateOutput(provider, new SpeechOutputTests.FakePlayer());
        await using var outputLifetime = output.ConfigureAwait(false);
        await (output.SpeakAsync("Corrupt")).ConfigureAwait(false);
        var path = Directory.GetFiles(_directory, "*.wav").Single();
        await (File.WriteAllBytesAsync(path, [1, 2])).ConfigureAwait(false);
        await (output.SpeakAsync("Corrupt")).ConfigureAwait(false);
        Assert.That(provider.Requests, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task ClearServiceMakesNextAnnouncementMiss()
    {
        var provider = new SpeechOutputTests.FakeProvider("system-speech");
        var output = CreateOutput(provider, new SpeechOutputTests.FakePlayer());
        await using var outputLifetime = output.ConfigureAwait(false);
        await (output.SpeakAsync("Clear")).ConfigureAwait(false);
        Assert.That(await (output.GetCacheSizeAsync()).ConfigureAwait(false), Is.GreaterThan(0));
        await (output.ClearCacheAsync()).ConfigureAwait(false);
        Assert.That(await (output.GetCacheSizeAsync()).ConfigureAwait(false), Is.Zero);
        await (output.SpeakAsync("Clear")).ConfigureAwait(false);
        Assert.That(provider.Requests, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task ClearDuringSynthesisPreventsLateRefillAndNewCallUsesNewFlight()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource<SpeechResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var provider = new SpeechOutputTests.FakeProvider("system-speech")
        {
            Synthesize = (_, token) =>
            {
                if (Interlocked.Increment(ref calls) == 1) { started.SetResult(); return completed.Task.WaitAsync(token); }
                return Task.FromResult(SpeechResult.Success(TestWaveAudio.Create([2])));
            }
        };
        var output = CreateOutput(provider, new SpeechOutputTests.FakePlayer());
        await using var outputLifetime = output.ConfigureAwait(false);
        var oldSpeech = output.SpeakAsync("Clear while active");
        await (started.Task.WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
        await (output.ClearCacheAsync()).ConfigureAwait(false);
        completed.SetResult(SpeechResult.Success(TestWaveAudio.Create([1])));
        await (oldSpeech.WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
        Assert.That(await (output.GetCacheSizeAsync()).ConfigureAwait(false), Is.Zero);
        await (output.SpeakAsync("Clear while active")).ConfigureAwait(false);
        Assert.That(provider.Requests, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task CacheIoFailureDoesNotSilenceAnnouncementOrExposeRawError()
    {
        var cache = new Moq.Mock<IAudioCache>();
        cache.Setup(value => value.TryGetAsync(Moq.It.IsAny<string>(), Moq.It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Private disk detail"));
        cache.Setup(value => value.StoreAsync(Moq.It.IsAny<string>(), Moq.It.IsAny<ReadOnlyMemory<byte>>(), Moq.It.IsAny<long>(), Moq.It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Private disk detail"));
        cache.Setup(value => value.MarkPlayedAsync(Moq.It.IsAny<string>(), Moq.It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Private disk detail"));
        var provider = new SpeechOutputTests.FakeProvider("system-speech");
        var player = new SpeechOutputTests.FakePlayer();
        var output = CreateOutput(provider, player, cache: cache.Object);
        await using var outputLifetime = output.ConfigureAwait(false);
        var result = await (output.SpeakAsync("Still speaks")).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Reason, Does.Not.Contain("Private disk detail"));
            Assert.That(player.PlayCount, Is.EqualTo(1));
        }
    }
    [Test]
    public async Task CacheHitPlaysEvenWhenProviderHasBecomeUnavailable()
    {
        var provider = new SpeechOutputTests.FakeProvider("system-speech");
        var player = new SpeechOutputTests.FakePlayer();
        var output = CreateOutput(provider, player);
        await using var lifetime = output.ConfigureAwait(false);
        await output.SpeakAsync("Unavailable but cached").ConfigureAwait(false);
        provider.Result = SpeechResult.Failure(SpeechFailureKind.Unavailable, "Unavailable.");
        var result = await output.SpeakAsync("Unavailable but cached").ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(provider.Requests, Has.Count.EqualTo(1));
            Assert.That(player.PlayCount, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task SwitchingProviderUsesItsOwnCacheAndSwitchingBackReusesPreviousAudio()
    {
        var settings = new AppSettings();
        var system = new SpeechOutputTests.FakeProvider("system-speech");
        var piper = new SpeechOutputTests.FakeProvider("piper");
        var output = new SpeechOutput(settings, [system, piper], new SpeechOutputTests.FakePlayer(),
            NullLogger<SpeechOutput>.Instance, _cache);
        await using var lifetime = output.ConfigureAwait(false);
        await output.SpeakAsync("Provider cache").ConfigureAwait(false);
        settings.Speech.ProviderId = "piper";
        await output.SpeakAsync("Provider cache").ConfigureAwait(false);
        settings.Speech.ProviderId = "system-speech";
        await output.SpeakAsync("Provider cache").ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(system.Requests, Has.Count.EqualTo(1));
            Assert.That(piper.Requests, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task ClearByAnotherProcessDoesNotPreventCachingTheNextMiss()
    {
        var provider = new SpeechOutputTests.FakeProvider("system-speech");
        var output = CreateOutput(provider, new SpeechOutputTests.FakePlayer());
        await using var lifetime = output.ConfigureAwait(false);
        await output.SpeakAsync("Before clear").ConfigureAwait(false);
        using (var otherProcess = new AudioCache(_directory, () => 1000000, TimeProvider.System))
        {
            await otherProcess.ClearAsync().ConfigureAwait(false);
        }
        await output.SpeakAsync("After clear").ConfigureAwait(false);
        await output.SpeakAsync("After clear").ConfigureAwait(false);
        Assert.That(provider.Requests.Count(request => request.Text == "After clear"), Is.EqualTo(1));
    }

    [Test]
    public async Task ClearAfterTheLookupRejectsAudioOfThatMiss()
    {
        var provider = new SpeechOutputTests.FakeProvider("system-speech");
        var output = CreateOutput(provider, new SpeechOutputTests.FakePlayer(), cache: new ClearAfterFirstLookup(_cache));
        await using var lifetime = output.ConfigureAwait(false);
        await output.SpeakAsync("Raced with clear").ConfigureAwait(false);
        await output.SpeakAsync("Raced with clear").ConfigureAwait(false);
        Assert.That(provider.Requests, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task AudioWhosePlaybackFailsIsNotCached()
    {
        var provider = new SpeechOutputTests.FakeProvider("system-speech");
        var player = new Mock<IWavePlayer>();
        player.SetupSequence(value => value.PlayAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Device lost."))
            .Returns(Task.CompletedTask);
        var output = CreateOutput(provider, player.Object);
        await using var lifetime = output.ConfigureAwait(false);
        var failed = await output.SpeakAsync("Unheard").ConfigureAwait(false);
        var sizeAfterFailure = await output.GetCacheSizeAsync().ConfigureAwait(false);
        await output.SpeakAsync("Unheard").ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(failed.IsSuccess, Is.False);
            Assert.That(sizeAfterFailure, Is.Zero);
            Assert.That(provider.Requests, Has.Count.EqualTo(2));
            Assert.That(await output.GetCacheSizeAsync().ConfigureAwait(false), Is.GreaterThan(0));
        }
    }

    [Test]
    public async Task FullCacheKeepsPlayedAudioWhenNewPlaybackFails()
    {
        var audioLength = TestWaveAudio.Create([1]).Length;
        using var cache = new AudioCache(Path.Combine(_directory, "small"), () => audioLength * 2L, TimeProvider.System);
        var provider = new SpeechOutputTests.FakeProvider("system-speech");
        var player = new Mock<IWavePlayer>();
        player.SetupSequence(value => value.PlayAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Returns(Task.CompletedTask)
            .ThrowsAsync(new IOException("Device lost."))
            .Returns(Task.CompletedTask)
            .Returns(Task.CompletedTask);
        var output = CreateOutput(provider, player.Object, cache: cache);
        await using var lifetime = output.ConfigureAwait(false);
        foreach (var text in FullCacheAnnouncements)
        {
            await output.SpeakAsync(text).ConfigureAwait(false);
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(provider.Requests.Select(request => request.Text), Is.EqualTo(FullCacheSyntheses));
            Assert.That(await output.GetCacheSizeAsync().ConfigureAwait(false), Is.EqualTo(audioLength * 2L));
        }
    }

    [Test]
    public async Task RequestAfterClearDoesNotJoinOlderFlightOrReceiveItsLateAudio()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource<SpeechResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var provider = new SpeechOutputTests.FakeProvider("system-speech")
        {
            Synthesize = (_, token) =>
            {
                if (Interlocked.Increment(ref calls) == 1) { started.SetResult(); return completed.Task.WaitAsync(token); }
                return Task.FromResult(SpeechResult.Success(TestWaveAudio.Create([2])));
            }
        };
        var output = CreateOutput(provider, new SpeechOutputTests.FakePlayer());
        await using var lifetime = output.ConfigureAwait(false);
        var oldSpeech = output.SpeakAsync("Same key after clear");
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        await output.ClearCacheAsync().ConfigureAwait(false);
        await output.SpeakAsync("Same key after clear").WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        completed.SetResult(SpeechResult.Success(TestWaveAudio.Create([1])));
        await oldSpeech.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        var cached = await output.SpeakAsync("Same key after clear").ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cached.WaveAudio.ToArray(), Is.EqualTo(TestWaveAudio.Create([2])));
            Assert.That(provider.Requests, Has.Count.EqualTo(2));
        }
    }

    /// <summary>Clears the cache right after the first lookup, as a concurrent clear could.</summary>
    private sealed class ClearAfterFirstLookup(IAudioCache inner) : IAudioCache
    {
        private int _lookups;
        public long Generation => inner.Generation;
        public async Task<AudioCacheLookup> TryGetAsync(string key, CancellationToken cancellationToken = default)
        {
            var lookup = await inner.TryGetAsync(key, cancellationToken).ConfigureAwait(false);
            if (Interlocked.Increment(ref _lookups) == 1) await inner.ClearAsync(cancellationToken).ConfigureAwait(false);
            return lookup;
        }
        public Task StoreAsync(string key, ReadOnlyMemory<byte> audio, long generation, CancellationToken cancellationToken = default) =>
            inner.StoreAsync(key, audio, generation, cancellationToken);
        public Task MarkPlayedAsync(string key, CancellationToken cancellationToken = default) => inner.MarkPlayedAsync(key, cancellationToken);
        public Task<long> GetSizeAsync(CancellationToken cancellationToken = default) => inner.GetSizeAsync(cancellationToken);
        public Task ClearAsync(CancellationToken cancellationToken = default) => inner.ClearAsync(cancellationToken);
    }
}
