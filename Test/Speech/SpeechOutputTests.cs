// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Speech;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Domain;
using Moba.Speech.Base;
using Moq;

[TestFixture]
internal sealed class SpeechOutputTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("removed-engine")]
    public async Task UnknownOrMissingId_UsesSystemSpeechAndReportsDiagnostic(string? id)
    {
        var settings = new AppSettings();
        settings.Speech.ProviderId = id!;
        var provider = new FakeProvider("system-speech");
        var logger = new Mock<ILogger<SpeechOutput>>();
        logger.Setup(value => value.IsEnabled(LogLevel.Warning)).Returns(true);
        var player = new FakePlayer();
        var output = new SpeechOutput(settings, [provider], player, logger.Object, TestSpeechCache.Empty());
        await using var outputLifetime = output.ConfigureAwait(false);

        var result = await output.SpeakAsync("Next stop Central.").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(provider.Requests, Has.Count.EqualTo(1));
            Assert.That(player.PlayCount, Is.EqualTo(1));
        }
        logger.Verify(value => value.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    public async Task Announcements_ReadCurrentProviderAndSharedOptionsOnEveryCall()
    {
        var settings = new AppSettings();
        var system = new FakeProvider("system-speech");
        var piper = new FakeProvider("piper");
        var player = new FakePlayer();
        var output = new SpeechOutput(settings, [system, piper], player, NullLogger<SpeechOutput>.Instance, TestSpeechCache.Empty());
        await using var outputLifetime = output.ConfigureAwait(false);
        var announcements = new AnnouncementService(output);

        await announcements.GenerateAndSpeakAnnouncementAsync("Next stop {StationName}.",
            new Station { Name = "Central" }, 1).ConfigureAwait(false);
        settings.Speech.ProviderId = "piper";
        settings.Speech.VoiceName = "de_DE-test";
        settings.Speech.Language = "de-DE";
        settings.Speech.Rate = 3;
        settings.Speech.Volume = 42;
        await announcements.GenerateAndSpeakAnnouncementAsync("Next stop {StationName}.",
            new Station { Name = "East" }, 1).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(system.Requests.Single().Text, Is.EqualTo("Next stop Central."));
            Assert.That(piper.Requests.Single(), Is.EqualTo(new SpeechRequest("Next stop East.", "de_DE-test", "de-DE", 3, 42)));
            Assert.That(player.PlayCount, Is.EqualTo(2));
        }
    }

    [TestCase(SpeechFailureKind.Unavailable)]
    [TestCase(SpeechFailureKind.InvalidRequest)]
    [TestCase(SpeechFailureKind.Failed)]
    public async Task FailedSynthesis_DoesNotPlayAndWorkflowContinues(SpeechFailureKind failure)
    {
        var provider = new FakeProvider("system-speech") { Result = SpeechResult.Failure(failure, "Speech unavailable.") };
        var player = new FakePlayer();
        var output = new SpeechOutput(new AppSettings(), [provider], player, NullLogger<SpeechOutput>.Instance, TestSpeechCache.Empty());
        await using var outputLifetime = output.ConfigureAwait(false);
        await new AnnouncementService(output).GenerateAndSpeakAnnouncementAsync("Next stop Central.", new Station(), 1)
            .ConfigureAwait(false);
        Assert.That(player.PlayCount, Is.Zero);
    }

    [Test]
    public async Task CancellationAfterSynthesis_PreventsPlayback()
    {
        using var cancellation = new CancellationTokenSource();
        var provider = new FakeProvider("system-speech")
        {
            Synthesize = async (_, _) =>
            {
                await cancellation.CancelAsync().ConfigureAwait(false);
                return SpeechResult.Success([1]);
            }
        };
        var player = new FakePlayer();
        var output = new SpeechOutput(new AppSettings(), [provider], player, NullLogger<SpeechOutput>.Instance, TestSpeechCache.Empty());
        await using var outputLifetime = output.ConfigureAwait(false);
        await Assert.CatchAsync<OperationCanceledException>(() => output.SpeakAsync("Test", cancellation.Token))
            .ConfigureAwait(false);
        Assert.That(player.PlayCount, Is.Zero);
    }

    [Test]
    public async Task MissingDefaultProvider_ReturnsUnavailableWithoutPlaying()
    {
        var player = new FakePlayer();
        var output = new SpeechOutput(new AppSettings(), [], player, NullLogger<SpeechOutput>.Instance, TestSpeechCache.Empty());
        await using var outputLifetime = output.ConfigureAwait(false);
        var result = await output.SpeakAsync("Test").ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.FailureKind, Is.EqualTo(SpeechFailureKind.Unavailable));
            Assert.That(result.Reason, Is.Not.Empty);
            Assert.That(player.PlayCount, Is.Zero);
        }
    }

    [Test]
    public async Task ProviderAndPlaybackExceptions_ReturnSafeFailuresWithoutBlockingWorkflow()
    {
        var provider = new FakeProvider("system-speech")
        {
            Synthesize = (_, _) => throw new InvalidOperationException("raw SDK detail")
        };
        var output = new SpeechOutput(new AppSettings(), [provider], new FakePlayer(), NullLogger<SpeechOutput>.Instance, TestSpeechCache.Empty());
        await using var outputLifetime = output.ConfigureAwait(false);
        var failedSynthesis = await output.SpeakAsync("Test").ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(failedSynthesis.FailureKind, Is.EqualTo(SpeechFailureKind.Failed));
            Assert.That(failedSynthesis.Reason, Does.Not.Contain("raw SDK detail"));
            Assert.That(failedSynthesis.Reason, Is.Not.Empty);
        }
        var player = new Mock<IWavePlayer>();
        player.Setup(value => value.PlayAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("raw playback detail"));
        var playbackOutput = new SpeechOutput(new AppSettings(), [new FakeProvider("system-speech")], player.Object, NullLogger<SpeechOutput>.Instance, TestSpeechCache.Empty());
        await using var playbackOutputLifetime = playbackOutput.ConfigureAwait(false);
        var failedPlayback = await playbackOutput.SpeakAsync("Test").ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(failedPlayback.FailureKind, Is.EqualTo(SpeechFailureKind.Failed));
            Assert.That(failedPlayback.Reason, Does.Not.Contain("raw playback detail"));
            Assert.That(failedPlayback.Reason, Is.Not.Empty);
        }
    }

    [Test]
    public async Task TextThatCannotBeNormalized_ReturnsInvalidRequestWithoutSynthesis()
    {
        var provider = new FakeProvider("system-speech");
        var player = new FakePlayer();
        var output = new SpeechOutput(new AppSettings(), [provider], player, NullLogger<SpeechOutput>.Instance, TestSpeechCache.Empty());
        await using var outputLifetime = output.ConfigureAwait(false);
        var result = await output.SpeakAsync("Next stop \uD800 Central.").ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.FailureKind, Is.EqualTo(SpeechFailureKind.InvalidRequest));
            Assert.That(result.Reason, Is.Not.Empty);
            Assert.That(provider.Requests, Is.Empty);
            Assert.That(player.PlayCount, Is.Zero);
        }
    }

    [Test]
    public async Task EmptyTextAndAlreadyCancelled_DoNotSynthesizeOrPlay()
    {
        var provider = new FakeProvider("system-speech");
        var player = new FakePlayer();
        var output = new SpeechOutput(new AppSettings(), [provider], player, NullLogger<SpeechOutput>.Instance, TestSpeechCache.Empty());
        await using var outputLifetime = output.ConfigureAwait(false);
        var result = await output.SpeakAsync(" ").ConfigureAwait(false);
        Assert.That(result.FailureKind, Is.EqualTo(SpeechFailureKind.InvalidRequest));
        Assert.That(result.Reason, Is.Not.Empty);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(false);
        await Assert.CatchAsync<OperationCanceledException>(() => output.SpeakAsync("Test", cancellation.Token)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(provider.Requests, Is.Empty);
            Assert.That(player.PlayCount, Is.Zero);
        }
    }

    [Test]
    public async Task HealthCheck_UsesTheCurrentSelectionAndCapabilities()
    {
        var settings = new AppSettings();
        var system = new FakeProvider("system-speech");
        var piper = new FakeProvider("piper") { Capabilities = new SpeechCapabilities([], true, true, false) };
        var output = new SpeechOutput(settings, [system, piper], new FakePlayer(), NullLogger<SpeechOutput>.Instance, TestSpeechCache.Empty());
        await using var outputLifetime = output.ConfigureAwait(false);
        var health = new SpeechHealthCheck(output);
        Assert.That(await health.TestConnectivityAsync().ConfigureAwait(false), Is.True);
        settings.Speech.ProviderId = "piper";
        Assert.That(await health.TestConnectivityAsync().ConfigureAwait(false), Is.False);
        var missingOutput = new SpeechOutput(settings, [], new FakePlayer(), NullLogger<SpeechOutput>.Instance, TestSpeechCache.Empty());
        await using var missingOutputLifetime = missingOutput.ConfigureAwait(false);
        var missing = new SpeechHealthCheck(missingOutput);
        Assert.That(await missing.TestConnectivityAsync().ConfigureAwait(false), Is.False);
    }

    [Test]
    public async Task NullOutput_SucceedsWithoutAudioAndHonorsCancellation()
    {
        var output = new NullSpeechOutput();
        var result = await output.SpeakAsync("Test").ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.WaveAudio.IsEmpty, Is.True);
        }
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(false);
        await Assert.CatchAsync<OperationCanceledException>(() => output.SpeakAsync("Test", cancellation.Token)).ConfigureAwait(false);
    }

#if WINDOWS
    [Test]
    public async Task HostHealthStatus_FollowsTheCurrentProviderWithoutSpeaking()
    {
        var settings = new AppSettings();
        var system = new FakeProvider("system-speech");
        var piper = new FakeProvider("piper")
        {
            Capabilities = new SpeechCapabilities([], true, true, false, "Model missing.")
        };
        var player = new FakePlayer();
        var output = new SpeechOutput(settings, [system, piper], player, NullLogger<SpeechOutput>.Instance, TestSpeechCache.Empty());
        await using var outputLifetime = output.ConfigureAwait(false);
        using var health = new Moba.WinUI.Service.HealthCheckService(new SpeechHealthCheck(output),
            NullLogger<Moba.WinUI.Service.HealthCheckService>.Instance,
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        var reported = new List<bool>();
        health.HealthStatusChanged += (_, status) => reported.Add(status.IsHealthy);
        settings.Speech.ProviderId = "piper";
        await health.PerformHealthCheckAsync().ConfigureAwait(false);
        await health.PerformHealthCheckAsync().ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(health.IsSpeechServiceHealthy, Is.False);
            Assert.That(health.SpeechServiceStatus, Does.Contain("Unavailable"));
        }
        settings.Speech.ProviderId = "system-speech";
        await health.PerformHealthCheckAsync().ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(health.IsSpeechServiceHealthy, Is.True);
            Assert.That(health.SpeechServiceStatus, Does.Contain("Ready"));
            Assert.That(reported, Has.Count.EqualTo(3));
            Assert.That(reported[0], Is.False);
            Assert.That(reported[1], Is.False);
            Assert.That(reported[2], Is.True);
            Assert.That(system.Requests, Is.Empty);
            Assert.That(piper.Requests, Is.Empty);
            Assert.That(player.PlayCount, Is.Zero);
        }
    }
#endif
#if WINDOWS
    [Test]
    public async Task HostHealthCheckDiscardsAResultForTheProviderSelectedBeforeTheAwait()
    {
        var settings = new AppSettings();
        var completed = new TaskCompletionSource<SpeechCapabilities>(TaskCreationOptions.RunContinuationsAsynchronously);
        var system = new FakeProvider("system-speech") { CapabilityCheck = token => completed.Task.WaitAsync(token) };
        var piper = new FakeProvider("piper") { Capabilities = new SpeechCapabilities([], true, true, false) };
        var output = new SpeechOutput(settings, [system, piper], new FakePlayer(), NullLogger<SpeechOutput>.Instance, TestSpeechCache.Empty());
        await using var outputLifetime = output.ConfigureAwait(false);
        using var health = new Moba.WinUI.Service.HealthCheckService(new SpeechHealthCheck(output),
            NullLogger<Moba.WinUI.Service.HealthCheckService>.Instance,
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        var reported = new List<bool>();
        health.HealthStatusChanged += (_, status) => reported.Add(status.IsHealthy);
        var check = health.PerformHealthCheckAsync();
        settings.Speech.ProviderId = "piper";
        completed.SetResult(new SpeechCapabilities([], true, true));
        await check.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(reported, Is.Empty);
            Assert.That(health.SpeechServiceStatus, Does.Contain("Initializing"));
        }
        await health.PerformHealthCheckAsync().ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(reported, Has.Count.EqualTo(1));
            Assert.That(reported.Single(), Is.False);
            Assert.That(health.SpeechServiceStatus, Does.Contain("Unavailable"));
        }
    }
#endif
    [Test]
    public void SpeechResultsPreserveAudioAndCacheabilityWithoutInventingAFailure()
    {
        var audio = TestWaveAudio.Create([123]);
        var result = SpeechResult.Success(audio);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.FailureKind, Is.Null);
            Assert.That(result.Reason, Is.Empty);
            Assert.That(result.WaveAudio.ToArray(), Is.EqualTo(audio));
            Assert.That(result.IsCacheable, Is.True);
            Assert.That(SpeechResult.Success(audio, isCacheable: false).IsCacheable, Is.False);
        }
        Assert.Throws<ArgumentNullException>(() => SpeechResult.Success(null!));
    }

    [TestCase(SpeechFailureKind.Unavailable)]
    [TestCase(SpeechFailureKind.InvalidRequest)]
    [TestCase(SpeechFailureKind.Failed)]
    public void FailureResultsNeverContainCacheableAudio(SpeechFailureKind kind)
    {
        var result = SpeechResult.Failure(kind, "Speech unavailable.");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureKind, Is.EqualTo(kind));
            Assert.That(result.Reason, Is.EqualTo("Speech unavailable."));
            Assert.That(result.WaveAudio.IsEmpty, Is.True);
            Assert.That(result.IsCacheable, Is.False);
        }
    }

    internal sealed class FakeProvider(string id) : ISpeechProvider
    {
        public string Id => id;
        public string DisplayName => id;
        public string OptionsFingerprint { get; set; } = "default";
        PreparedSpeechSynthesis ISpeechProvider.PrepareSynthesis(SpeechRequest request) => new(OptionsFingerprint, token => SynthesizeAsync(request, token));
        public bool IsLocal => true;
        public List<SpeechRequest> Requests { get; } = [];
        public SpeechCapabilities Capabilities { get; init; } = new([], true, true);
        public Func<CancellationToken, Task<SpeechCapabilities>>? CapabilityCheck { get; init; }
        public SpeechResult Result { get; set; } = SpeechResult.Success(TestWaveAudio.Create([1]));
        public Func<SpeechRequest, CancellationToken, Task<SpeechResult>>? Synthesize { get; init; }
        public Task<SpeechCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default) =>
            CapabilityCheck?.Invoke(cancellationToken) ?? Task.FromResult(Capabilities);
        public Task<SpeechResult> SynthesizeAsync(SpeechRequest request, CancellationToken cancellationToken = default)
        {
            lock (Requests) { Requests.Add(request); }
            return Synthesize?.Invoke(request, cancellationToken) ?? Task.FromResult(Result);
        }
    }

    internal sealed class FakePlayer : IWavePlayer
    {
        private int _playCount;
        public int PlayCount => Volatile.Read(ref _playCount);
        public Task PlayAsync(ReadOnlyMemory<byte> waveAudio, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _playCount);
            return Task.CompletedTask;
        }
    }
}
