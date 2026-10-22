// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Speech;

using Microsoft.Extensions.Logging;
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

    private SpeechOutput CreateOutput(SpeechOutputTests.FakeProvider provider, IWavePlayer player, AppSettings? settings = null,
        IAudioCache? cache = null, ILogger<SpeechOutput>? logger = null) =>
        new(settings ?? new AppSettings(), (ISpeechProvider[])[provider], player, logger ?? NullLogger<SpeechOutput>.Instance, cache ?? _cache);

    [Test]
    public async Task IdenticalAnnouncementUsesCacheAndStillPlaysEveryTime()
    {
        var provider = new SpeechOutputTests.FakeProvider("system-speech");
        var original = provider.Result.WaveAudio.ToArray();
        var played = new List<byte[]>();
        var player = new Mock<IWavePlayer>();
        player.Setup(value => value.PlayAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Callback((ReadOnlyMemory<byte> audio, CancellationToken _) => played.Add(audio.ToArray()))
            .Returns(Task.CompletedTask);
        var output = CreateOutput(provider, player.Object);
        await using var outputLifetime = output.ConfigureAwait(false);
        await (output.SpeakAsync("Next stop Central.")).ConfigureAwait(false);
        provider.Result = SpeechResult.Success(TestWaveAudio.Create([222]));
        var cached = await output.SpeakAsync("Next stop Central.").ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(provider.Requests, Has.Count.EqualTo(1));
            Assert.That(played, Has.Count.EqualTo(2));
            Assert.That(played[0], Is.EqualTo(original));
            Assert.That(played[1], Is.EqualTo(original));
            Assert.That(cached.WaveAudio.ToArray(), Is.EqualTo(original));
            Assert.That(await (_cache.GetSizeAsync()).ConfigureAwait(false), Is.GreaterThan(0));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CachedPlaybackSucceedsWhenRecordingThePlaybackFails(bool accessDenied)
    {
        var audio = TestWaveAudio.Create([123]);
        var cache = new Mock<IAudioCache>();
        cache.Setup(value => value.TryGetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioCacheLookup(audio, 0));
        cache.Setup(value => value.MarkPlayedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(accessDenied ? new UnauthorizedAccessException("Private disk detail") : new IOException("Private disk detail"));
        var provider = new SpeechOutputTests.FakeProvider("system-speech");
        var player = new SpeechOutputTests.FakePlayer();
        var logger = new Mock<ILogger<SpeechOutput>>();
        logger.Setup(value => value.IsEnabled(LogLevel.Warning)).Returns(true);
        var output = CreateOutput(provider, player, cache: cache.Object, logger: logger.Object);
        await using var lifetime = output.ConfigureAwait(false);
        var result = await output.SpeakAsync("Cached despite metadata failure").ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Reason, Is.Empty);
            Assert.That(result.WaveAudio.ToArray(), Is.EqualTo(audio));
            Assert.That(player.PlayCount, Is.EqualTo(1));
            Assert.That(provider.Requests, Is.Empty);
        }
        cache.Verify(value => value.MarkPlayedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(value => value.StoreAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyCacheWarningWithoutPrivateDetails(logger);
    }

    [Test]
    public async Task CancellationAfterCacheHitDoesNotStartPlaybackOrRecordIt()
    {
        using var cancellation = new CancellationTokenSource();
        var cache = new Mock<IAudioCache>();
        cache.Setup(value => value.TryGetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken _) =>
            {
                await cancellation.CancelAsync().ConfigureAwait(false);
                return new AudioCacheLookup(TestWaveAudio.Create([1]), 0);
            });
        var player = new Mock<IWavePlayer>();
        player.Setup(value => value.PlayAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var provider = new SpeechOutputTests.FakeProvider("system-speech");
        var output = CreateOutput(provider, player.Object, cache: cache.Object);
        await using var lifetime = output.ConfigureAwait(false);
        await Assert.CatchAsync<OperationCanceledException>(() => output.SpeakAsync("Cancelled hit", cancellation.Token)
            .WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
        player.Verify(value => value.PlayAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Never);
        cache.Verify(value => value.MarkPlayedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.That(provider.Requests, Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task AlreadyCancelledCacheCommandsDoNotReachStorageAndCanBeRetried(bool clear)
    {
        var cache = new Mock<IAudioCache>();
        cache.Setup(value => value.GetSizeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(123L);
        cache.Setup(value => value.ClearAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var output = CreateOutput(new SpeechOutputTests.FakeProvider("system-speech"), new SpeechOutputTests.FakePlayer(), cache: cache.Object);
        await using var lifetime = output.ConfigureAwait(false);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(false);
        var command = clear ? output.ClearCacheAsync(cancellation.Token) : output.GetCacheSizeAsync(cancellation.Token);
        await Assert.CatchAsync<OperationCanceledException>(() => command.WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
        cache.Verify(value => value.ClearAsync(It.IsAny<CancellationToken>()), Times.Never);
        cache.Verify(value => value.GetSizeAsync(It.IsAny<CancellationToken>()), Times.Never);
        if (clear)
        {
            await output.ClearCacheAsync().ConfigureAwait(false);
            cache.Verify(value => value.ClearAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        else
        {
            Assert.That(await output.GetCacheSizeAsync().ConfigureAwait(false), Is.EqualTo(123));
            cache.Verify(value => value.GetSizeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task CancelledPlaybackDoesNotRecordAudioEvenIfThePlayerReturns(bool cacheHit, bool interrupted)
    {
        using var cancellation = new CancellationTokenSource();
        var audio = TestWaveAudio.Create([1]);
        var cache = new Mock<IAudioCache>();
        cache.Setup(value => value.TryGetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioCacheLookup(cacheHit ? audio : (ReadOnlyMemory<byte>?)null, 0));
        var player = new Mock<IWavePlayer>();
        player.Setup(value => value.PlayAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(async (ReadOnlyMemory<byte> _, CancellationToken _) =>
            {
                await cancellation.CancelAsync().ConfigureAwait(false);
                if (interrupted) throw new WavePlaybackInterruptedException();
            });
        var provider = new SpeechOutputTests.FakeProvider("system-speech");
        var output = CreateOutput(provider, player.Object, cache: cache.Object);
        await using var lifetime = output.ConfigureAwait(false);
        await Assert.CatchAsync<OperationCanceledException>(() => output.SpeakAsync("Cancelled playback", cancellation.Token)
            .WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
        player.Verify(value => value.PlayAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(value => value.MarkPlayedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        cache.Verify(value => value.StoreAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.That(provider.Requests, Has.Count.EqualTo(cacheHit ? 0 : 1));
    }

    [Test]
    public async Task SharedSynthesisRechecksTheCacheBeforeCallingTheProvider()
    {
        var audio = TestWaveAudio.Create([321]);
        var cache = new Mock<IAudioCache>();
        cache.SetupSequence(value => value.TryGetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioCacheLookup((ReadOnlyMemory<byte>?)null, 0))
            .ReturnsAsync(new AudioCacheLookup(audio, 0));
        var provider = new SpeechOutputTests.FakeProvider("system-speech");
        var player = new Mock<IWavePlayer>();
        player.Setup(value => value.PlayAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var output = CreateOutput(provider, player.Object, cache: cache.Object);
        await using var lifetime = output.ConfigureAwait(false);
        var result = await output.SpeakAsync("Stored by another caller").ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.WaveAudio.ToArray(), Is.EqualTo(audio));
            Assert.That(provider.Requests, Is.Empty);
        }
        player.Verify(value => value.PlayAsync(It.Is<ReadOnlyMemory<byte>>(bytes => bytes.ToArray().SequenceEqual(audio)), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(value => value.TryGetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
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

    [TestCase(false)]
    [TestCase(true)]
    public async Task ShutdownDrainsAbandonedSynthesisAfterItsLastCallerHasCancelled(bool lateFailure)
    {
        var started = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource<SpeechResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var providerReturned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new SpeechOutputTests.FakeProvider("system-speech")
        {
            Synthesize = async (_, token) =>
            {
                started.TrySetResult(token);
                try { return await completed.Task.ConfigureAwait(false); }
                finally { providerReturned.TrySetResult(); }
            }
        };
        var player = new SpeechOutputTests.FakePlayer();
        var cache = TestSpeechCache.Empty();
        var output = CreateOutput(provider, player, cache: cache);
        using var cancellation = new CancellationTokenSource();
        try
        {
            var speech = output.SpeakAsync("Abandoned cache miss", cancellation.Token);
            var synthesisToken = await started.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            await cancellation.CancelAsync().ConfigureAwait(false);
            await Assert.CatchAsync<OperationCanceledException>(() => speech.WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
            Assert.That(synthesisToken.IsCancellationRequested, Is.True);
            var stopping = output.DisposeAsync().AsTask();
            // No caller remains to keep shutdown pending: only the unfinished provider work can do that.
            await Assert.ThrowsAsync<TimeoutException>(() => stopping.WaitAsync(TimeSpan.FromSeconds(1))).ConfigureAwait(false);
        }
        finally
        {
            if (lateFailure) completed.TrySetException(new IOException("Late provider failure."));
            else completed.TrySetResult(SpeechResult.Success(TestWaveAudio.Create([1])));
            if (started.Task.IsCompletedSuccessfully)
            {
                await providerReturned.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            }
            await output.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        }
        Assert.That(player.PlayCount, Is.Zero);
        Mock.Get(cache).Verify(value => value.StoreAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CompletedSharedSynthesisClosesItsCancellationWaitHandle(bool synthesisFailure)
    {
        var handleReady = new TaskCompletionSource<WaitHandle>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new SpeechOutputTests.FakeProvider("system-speech")
        {
            Synthesize = (_, token) =>
            {
                handleReady.TrySetResult(token.WaitHandle);
                return synthesisFailure
                    ? Task.FromException<SpeechResult>(new IOException("Provider failed."))
                    : Task.FromResult(SpeechResult.Success(TestWaveAudio.Create([1])));
            }
        };
        var player = new SpeechOutputTests.FakePlayer();
        var output = CreateOutput(provider, player, cache: TestSpeechCache.Empty());
        await using var lifetime = output.ConfigureAwait(false);
        var speech = output.SpeakAsync("Release shared cancellation resources");
        // Also clean up a handle leaked by an injected mutation, after checking production ownership.
        using var handle = await handleReady.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        var result = await speech.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        await output.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.EqualTo(!synthesisFailure));
            Assert.That(player.PlayCount, Is.EqualTo(synthesisFailure ? 0 : 1));
            Assert.That(handle.SafeWaitHandle.IsClosed, Is.True);
        }
    }

    [Test]
    public async Task SharedCancellationResourcesStayOpenUntilTheirCallbackHasFinished()
    {
        using var releaseCallback = new ManualResetEventSlim();
        var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var providerReturned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ready = new TaskCompletionSource<(WaitHandle Handle, CancellationTokenRegistration Registration)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource<SpeechResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new SpeechOutputTests.FakeProvider("system-speech")
        {
            Synthesize = async (_, token) =>
            {
                var registration = token.Register(() =>
                {
                    callbackStarted.TrySetResult();
                    // Cancellation callbacks are synchronous; hold this one at an explicitly controlled boundary.
                    if (!releaseCallback.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Cancellation callback was not released.");
                });
                ready.TrySetResult((token.WaitHandle, registration));
                try { return await completed.Task.ConfigureAwait(false); }
                finally { providerReturned.TrySetResult(); }
            }
        };
        var output = CreateOutput(provider, new SpeechOutputTests.FakePlayer(), cache: TestSpeechCache.Empty());
        using var cancellation = new CancellationTokenSource();
        var speech = output.SpeakAsync("Cancellation callback still running", cancellation.Token);
        var resources = await ready.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        using var registration = resources.Registration;
        using var handle = resources.Handle;
        try
        {
            await cancellation.CancelAsync().ConfigureAwait(false);
            await callbackStarted.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            completed.TrySetResult(SpeechResult.Success(TestWaveAudio.Create([1])));
            await providerReturned.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            var stopping = output.DisposeAsync().AsTask();
            await Assert.ThrowsAsync<TimeoutException>(() => stopping.WaitAsync(TimeSpan.FromSeconds(1))).ConfigureAwait(false);
            Assert.That(handle.SafeWaitHandle.IsClosed, Is.False);
        }
        finally
        {
            releaseCallback.Set();
            completed.TrySetResult(SpeechResult.Success(TestWaveAudio.Create([1])));
            try { await speech.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            await output.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        }
        Assert.That(handle.SafeWaitHandle.IsClosed, Is.True);
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
        var logger = new Mock<ILogger<SpeechOutput>>();
        logger.Setup(value => value.IsEnabled(LogLevel.Warning)).Returns(true);
        var output = CreateOutput(provider, player, logger: logger.Object);
        await using var outputLifetime = output.ConfigureAwait(false);
        var failure = await output.SpeakAsync("Retry").ConfigureAwait(false);
        provider.Result = SpeechResult.Success(TestWaveAudio.Create([1]));
        await (output.SpeakAsync("Retry")).ConfigureAwait(false);
        await (output.SpeakAsync("Retry")).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(failure.FailureKind, Is.EqualTo(SpeechFailureKind.Unavailable));
            Assert.That(provider.Requests, Has.Count.EqualTo(2));
            Assert.That(player.PlayCount, Is.EqualTo(2));
        }
        logger.Verify(value => value.Log(LogLevel.Warning, It.Is<EventId>(eventId => eventId.Id == 2),
            It.IsAny<It.IsAnyType>(), It.Is<Exception?>(error => error == null),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
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

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task CacheIoFailureDoesNotSilenceAnnouncementOrExposeRawError(bool lookupFailure, bool accessDenied)
    {
        Exception error = accessDenied ? new UnauthorizedAccessException("Private disk detail") : new IOException("Private disk detail");
        var cache = new Mock<IAudioCache>();
        if (lookupFailure)
        {
            cache.Setup(value => value.TryGetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(error);
            cache.Setup(value => value.StoreAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }
        else
        {
            cache.Setup(value => value.TryGetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AudioCacheLookup((ReadOnlyMemory<byte>?)null, 0));
            cache.Setup(value => value.StoreAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(error);
        }
        var logger = new Mock<ILogger<SpeechOutput>>();
        logger.Setup(value => value.IsEnabled(LogLevel.Warning)).Returns(true);
        var provider = new SpeechOutputTests.FakeProvider("system-speech");
        var player = new SpeechOutputTests.FakePlayer();
        var output = CreateOutput(provider, player, cache: cache.Object, logger: logger.Object);
        await using var outputLifetime = output.ConfigureAwait(false);
        var result = await (output.SpeakAsync("Still speaks")).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Reason, Does.Not.Contain("Private disk detail"));
            Assert.That(player.PlayCount, Is.EqualTo(1));
        }
        VerifyCacheWarningWithoutPrivateDetails(logger);
    }

    private static void VerifyCacheWarningWithoutPrivateDetails(Mock<ILogger<SpeechOutput>> logger)
    {
        logger.Verify(value => value.Log(LogLevel.Warning, It.Is<EventId>(eventId => eventId.Id == 3),
            It.Is<It.IsAnyType>((state, _) => state != null && !state.ToString()!.Contains("Private disk detail", StringComparison.Ordinal)),
            It.Is<Exception?>(error => error == null), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
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
        var output = new SpeechOutput(settings, (ISpeechProvider[])[system, piper], new SpeechOutputTests.FakePlayer(),
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
    public async Task IdenticalAnnouncementDuringPlaybackJoinsTheCompletedSynthesis()
    {
        var provider = new SpeechOutputTests.FakeProvider("system-speech");
        var player = new GatedFirstPlayback();
        var output = CreateOutput(provider, player);
        await using var lifetime = output.ConfigureAwait(false);
        var first = output.SpeakAsync("Overlapping");
        await player.FirstStarted.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        var second = await output.SpeakAsync("Overlapping").WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        player.ReleaseFirst();
        var firstResult = await first.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(second.IsSuccess, Is.True);
            Assert.That(provider.Requests, Has.Count.EqualTo(1));
            Assert.That(player.PlayCount, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task PlaybackInterruptedByANewerAnnouncementSucceedsWithoutCaching()
    {
        var provider = new SpeechOutputTests.FakeProvider("system-speech");
        var player = new Mock<IWavePlayer>();
        player.SetupSequence(value => value.PlayAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new WavePlaybackInterruptedException())
            .Returns(Task.CompletedTask);
        var output = CreateOutput(provider, player.Object);
        await using var lifetime = output.ConfigureAwait(false);
        var interrupted = await output.SpeakAsync("Interrupted").ConfigureAwait(false);
        var sizeAfterInterruption = await output.GetCacheSizeAsync().ConfigureAwait(false);
        await output.SpeakAsync("Interrupted").ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(interrupted.IsSuccess, Is.True);
            Assert.That(sizeAfterInterruption, Is.Zero);
            Assert.That(provider.Requests, Has.Count.EqualTo(2));
        }
    }

    [Test]
    public async Task SubstituteVoiceAudioIsPlayedButNotCached()
    {
        var provider = new SpeechOutputTests.FakeProvider("system-speech")
        {
            Synthesize = (_, _) => Task.FromResult(SpeechResult.Success(TestWaveAudio.Create([1]), isCacheable: false))
        };
        var player = new SpeechOutputTests.FakePlayer();
        var output = CreateOutput(provider, player);
        await using var lifetime = output.ConfigureAwait(false);
        await output.SpeakAsync("Missing voice").ConfigureAwait(false);
        await output.SpeakAsync("Missing voice").ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.PlayCount, Is.EqualTo(2));
            Assert.That(provider.Requests, Has.Count.EqualTo(2));
            Assert.That(await output.GetCacheSizeAsync().ConfigureAwait(false), Is.Zero);
        }
    }

    [Test]
    public async Task ShutdownCancelsAndWaitsForCacheOperations()
    {
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cache = new Mock<IAudioCache>();
        cache.Setup(value => value.GetSizeAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken token) =>
            {
                await using var registration = token.Register(() => cancelled.TrySetResult()).ConfigureAwait(false);
                await release.Task.ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                return 0L;
            });
        var output = CreateOutput(new SpeechOutputTests.FakeProvider("system-speech"), new SpeechOutputTests.FakePlayer(), cache: cache.Object);
        var size = output.GetCacheSizeAsync();
        var disposing = output.DisposeAsync().AsTask();
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        Assert.That(disposing.IsCompleted, Is.False);
        release.SetResult();
        await disposing.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        await Assert.CatchAsync<OperationCanceledException>(() => size).ConfigureAwait(false);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => output.ClearCacheAsync()).ConfigureAwait(false);
    }

    [Test]
    public async Task FailedLookupKeepsTheGenerationFromBeforeAConcurrentClear()
    {
        var provider = new SpeechOutputTests.FakeProvider("system-speech");
        var output = CreateOutput(provider, new SpeechOutputTests.FakePlayer(), cache: new ClearThenFailFirstLookup(_cache));
        await using var lifetime = output.ConfigureAwait(false);
        var first = await output.SpeakAsync("Lookup failed").ConfigureAwait(false);
        await output.SpeakAsync("Lookup failed").ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.IsSuccess, Is.True);
            Assert.That(provider.Requests, Has.Count.EqualTo(2));
        }
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

    /// <summary>Holds the first playback open until released; later playbacks complete at once.</summary>
    private sealed class GatedFirstPlayback : IWavePlayer
    {
        private readonly TaskCompletionSource _firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _playCount;
        public Task FirstStarted => _firstStarted.Task;
        public int PlayCount => Volatile.Read(ref _playCount);
        public void ReleaseFirst() => _releaseFirst.TrySetResult();
        public Task PlayAsync(ReadOnlyMemory<byte> waveAudio, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _playCount) != 1) return Task.CompletedTask;
            _firstStarted.TrySetResult();
            return _releaseFirst.Task.WaitAsync(cancellationToken);
        }
    }

    /// <summary>Lets a clear complete during the first lookup, which then fails with an I/O error.</summary>
    private sealed class ClearThenFailFirstLookup(IAudioCache inner) : IAudioCache
    {
        private int _lookups;
        public long Generation => inner.Generation;
        public async Task<AudioCacheLookup> TryGetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _lookups) != 1) return await inner.TryGetAsync(key, cancellationToken).ConfigureAwait(false);
            await inner.ClearAsync(cancellationToken).ConfigureAwait(false);
            throw new IOException("Cache unavailable.");
        }
        public Task StoreAsync(string key, ReadOnlyMemory<byte> audio, long generation, CancellationToken cancellationToken = default) =>
            inner.StoreAsync(key, audio, generation, cancellationToken);
        public Task MarkPlayedAsync(string key, CancellationToken cancellationToken = default) => inner.MarkPlayedAsync(key, cancellationToken);
        public Task<long> GetSizeAsync(CancellationToken cancellationToken = default) => inner.GetSizeAsync(cancellationToken);
        public Task ClearAsync(CancellationToken cancellationToken = default) => inner.ClearAsync(cancellationToken);
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
