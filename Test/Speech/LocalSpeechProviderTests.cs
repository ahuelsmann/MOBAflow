// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
#if WINDOWS
namespace Moba.Test.Speech;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Common.Configuration;
using Moba.Speech.Base;
using Moba.Speech.Piper;
using Moba.Speech.SystemSpeech;
using System.Buffers.Binary;

[TestFixture]
internal sealed class LocalSpeechProviderTests
{
    private string _directory = null!;
    private AppSettings _settings = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "mobaflow-provider-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _settings = new AppSettings();
        _settings.Speech.Piper.ExecutablePath = Path.Combine(_directory, "piper.exe");
        _settings.Speech.Piper.ModelPath = Path.Combine(_directory, "de_DE-test.onnx");
        File.WriteAllText(_settings.Speech.Piper.ExecutablePath, string.Empty);
        File.WriteAllText(_settings.Speech.Piper.ModelPath, string.Empty);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_directory, recursive: true);

    [Test]
    public async Task Piper_ProducesAudioAndHonorsRateVolumeAndCurrentOptionsWithoutPlayback()
    {
        var runner = new FakeRunner();
        var provider = CreatePiper(runner);
        _settings.Speech.Piper.SentenceSilenceSeconds = 0.3;
        var first = await provider.SynthesizeAsync(new SpeechRequest("Nächster Halt Berlin Hbf.", "", "de-DE", -2, 50)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.IsSuccess, Is.True);
            Assert.That(runner.Request!.LengthScale, Is.EqualTo(1.1).Within(0.001));
            Assert.That(runner.Request.SentenceSilenceSeconds, Is.EqualTo(0.3));
            Assert.That(runner.Request.Text, Does.Contain("Hauptbahnhof"));
            Assert.That(BinaryPrimitives.ReadInt16LittleEndian(first.WaveAudio.Span.Slice(44, 2)), Is.EqualTo(6000));
            Assert.That(File.Exists(runner.Request.OutputPath), Is.False);
        }
        _settings.Speech.Piper.SentenceSilenceSeconds = 0.8;
        _settings.Speech.Piper.EnablePronunciationNormalization = false;
        var second = await provider.SynthesizeAsync(new SpeechRequest("Berlin Hbf.", "de_DE-test", "de-DE", 4, 100)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(second.IsSuccess, Is.True);
            Assert.That(runner.Request!.LengthScale, Is.EqualTo(0.8).Within(0.001));
            Assert.That(runner.Request.SentenceSilenceSeconds, Is.EqualTo(0.8));
            Assert.That(runner.Request.Text, Is.EqualTo("Berlin Hbf."));
            Assert.That(BinaryPrimitives.ReadInt16LittleEndian(second.WaveAudio.Span.Slice(44, 2)), Is.EqualTo(12000));
        }
    }

    [Test]
    public async Task Piper_ReportsCurrentCapabilitiesAndMissingConfiguration()
    {
        var provider = CreatePiper(new FakeRunner());
        var available = await provider.GetCapabilitiesAsync().ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(available.IsAvailable, Is.True);
            Assert.That(available.SupportsRate && available.SupportsVolume, Is.True);
            Assert.That(available.Voices.Single(), Is.EqualTo(new SpeechVoice("de_DE-test", "de-DE")));
        }
        File.Delete(_settings.Speech.Piper.ModelPath);
        var result = await provider.SynthesizeAsync(new SpeechRequest("Test", "", "", 0, 100)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.FailureKind, Is.EqualTo(SpeechFailureKind.Unavailable));
            Assert.That((await provider.GetCapabilitiesAsync().ConfigureAwait(false)).IsAvailable, Is.False);
        }
    }

    [Test]
    public async Task Piper_FailureDoesNotExposeProcessOutputAndRemovesTemporaryFile()
    {
        var runner = new FakeRunner { ExitCode = 1 };
        var result = await CreatePiper(runner).SynthesizeAsync(new SpeechRequest("Test", "", "", 0, 100)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.FailureKind, Is.EqualTo(SpeechFailureKind.Failed));
            Assert.That(result.Reason, Does.Not.Contain("raw diagnostic"));
            Assert.That(result.WaveAudio.IsEmpty, Is.True);
            Assert.That(File.Exists(runner.Request!.OutputPath), Is.False);
        }
    }

    [Test]
    public async Task Piper_CancellationReachesRunnerAndRemovesTemporaryFile()
    {
        using var cancellation = new CancellationTokenSource();
        var runner = new FakeRunner { Cancel = cancellation };
        await Assert.CatchAsync<OperationCanceledException>(() => CreatePiper(runner)
            .SynthesizeAsync(new SpeechRequest("Test", "", "", 0, 100), cancellation.Token)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(runner.Token, Is.EqualTo(cancellation.Token));
            Assert.That(File.Exists(runner.Request!.OutputPath), Is.False);
        }
    }

    [Test]
    public async Task Piper_UsesConfiguredModelWhenSavedVoiceBelongsToAnotherProvider()
    {
        var runner = new FakeRunner();
        var result = await CreatePiper(runner).SynthesizeAsync(new SpeechRequest("Test", "unknown", "", 0, 100)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(runner.Request!.ModelPath, Is.EqualTo(_settings.Speech.Piper.ModelPath));
        }
    }

    [Test]
    public async Task SystemSpeech_SynthesizesPcmWithoutOpeningAnAudioDevice()
    {
        var provider = new SystemSpeechProvider(_settings);
        var capabilities = await provider.GetCapabilitiesAsync().ConfigureAwait(false);
        if (!capabilities.IsAvailable) Assert.Ignore(capabilities.Reason);
        Assert.That(capabilities.SupportsRate && capabilities.SupportsVolume, Is.True);
        var result = await provider.SynthesizeAsync(new SpeechRequest("Next stop Central.", capabilities.Voices[0].Name,
            capabilities.Voices[0].Language, 2, 40)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(WaveAudio.GetDuration(result.WaveAudio.Span), Is.GreaterThan(TimeSpan.Zero));
        }
    }

    [Test]
    public async Task SystemSpeech_InvalidOptionsAndCancellationAreReportedBeforeSynthesis()
    {
        var provider = new SystemSpeechProvider(_settings);
        var result = await provider.SynthesizeAsync(new SpeechRequest("Test", "", "", 11, 100)).ConfigureAwait(false);
        Assert.That(result.FailureKind, Is.EqualTo(SpeechFailureKind.InvalidRequest));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(false);
        await Assert.CatchAsync<OperationCanceledException>(() => provider.SynthesizeAsync(
            new SpeechRequest("Test", "", "", 0, 100), cancellation.Token)).ConfigureAwait(false);
    }

    [Test]
    public async Task Piper_OverlappingSynthesisOwnsIndependentFilesAndCanFinishOutOfOrder()
    {
        var firstCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = new List<PiperSynthesisRequest>();
        var runner = new Moq.Mock<IPiperProcessRunner>();
        runner.Setup(value => value.SynthesizeAsync(Moq.It.IsAny<PiperSynthesisRequest>(), Moq.It.IsAny<CancellationToken>()))
            .Returns(async (PiperSynthesisRequest request, CancellationToken token) =>
            {
                requests.Add(request);
                await File.WriteAllBytesAsync(request.OutputPath, TestWaveAudio.Create([12000]), token).ConfigureAwait(false);
                await (request.Text == "First" ? firstCompletion.Task : secondCompletion.Task).WaitAsync(token).ConfigureAwait(false);
                return new PiperProcessResult(0, string.Empty, string.Empty);
            });
        var provider = CreatePiper(runner.Object);
        var first = provider.SynthesizeAsync(new SpeechRequest("First", "", "", 0, 100));
        var second = provider.SynthesizeAsync(new SpeechRequest("Second", "", "", 0, 100));
        try
        {
            Assert.That(requests.Select(request => request.OutputPath).Distinct().Count(), Is.EqualTo(2));
            secondCompletion.SetResult();
            var secondResult = await second.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(secondResult.IsSuccess, Is.True);
                Assert.That(first.IsCompleted, Is.False);
                Assert.That(File.Exists(requests[1].OutputPath), Is.False);
            }
            firstCompletion.SetResult();
            using (Assert.EnterMultipleScope())
            {
                Assert.That((await first.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false)).IsSuccess, Is.True);
                Assert.That(requests.All(request => !File.Exists(request.OutputPath)), Is.True);
            }
        }
        finally
        {
            firstCompletion.TrySetResult();
            secondCompletion.TrySetResult();
            await Task.WhenAll(first, second).ConfigureAwait(false);
        }
    }

    [Test]
    public async Task PiperProcessRunner_CapturesStandardOutputAndError()
    {
        var request = CreateProcessRequest("echo fake-piper-output\r\necho fake-piper-error 1>&2\r\nexit 0", TimeSpan.FromSeconds(10));
        var result = await new PiperProcessRunner().SynthesizeAsync(request).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.StandardOutput, Does.Contain("fake-piper-output"));
            Assert.That(result.ErrorOutput, Does.Contain("fake-piper-error"));
        }
    }

    [Test]
    public async Task PiperProcessRunner_TimeoutTerminatesTheControlledProcess()
    {
        var request = CreateProcessRequest("ping -n 30 127.0.0.1 > nul", TimeSpan.Zero);
        var result = await new PiperProcessRunner().SynthesizeAsync(request).WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(-1));
            Assert.That(result.ErrorOutput, Does.Contain("timed out"));
        }
    }

    [Test]
    public async Task PiperProcessRunner_CancellationTerminatesTheControlledProcess()
    {
        using var cancellation = new CancellationTokenSource();
        var clock = new ProcessStartedClock();
        var running = new PiperProcessRunner(clock).SynthesizeAsync(
            CreateProcessRequest("ping -n 30 127.0.0.1 > nul", TimeSpan.FromSeconds(30)), cancellation.Token);
        try
        {
            await clock.TimeoutScheduled.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            await cancellation.CancelAsync().ConfigureAwait(false);
            await Assert.CatchAsync<OperationCanceledException>(() => running.WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
        }
        finally
        {
            await cancellation.CancelAsync().ConfigureAwait(false);
        }
    }

    [Test]
    public async Task Piper_DiagnosticsContainTheProcessedAudioInAnOwnedDirectory()
    {
        _settings.Speech.Piper.EnableAudioDiagnostics = true;
        var result = await CreatePiper(new FakeRunner()).SynthesizeAsync(new SpeechRequest("Test", "", "", 0, 50)).ConfigureAwait(false);
        var saved = await File.ReadAllBytesAsync(Path.Combine(_directory, "diagnostics", "last-piper-announcement.wav")).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(saved, Is.EqualTo(result.WaveAudio.ToArray()));
        }
    }

    [Test]
    public async Task Piper_CorruptExecutableIsUnavailableEvenWhenAllConfiguredFilesExist()
    {
        var provider = new PiperSpeechProvider(_settings, NullLogger<PiperSpeechProvider>.Instance);
        var capabilities = await provider.GetCapabilitiesAsync().ConfigureAwait(false);
        Assert.That(capabilities.IsAvailable, Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task SystemSpeech_UsesProviderDefaultWhenSavedVoiceIsNoLongerInstalled(bool missingConfiguredDefault)
    {
        var provider = new SystemSpeechProvider(_settings);
        var capabilities = await provider.GetCapabilitiesAsync().ConfigureAwait(false);
        if (!capabilities.IsAvailable) Assert.Ignore(capabilities.Reason);
        _settings.Speech.SystemSpeech.DefaultVoiceName = missingConfiguredDefault
            ? "removed provider default" : capabilities.Voices[0].Name;
        var result = await provider.SynthesizeAsync(new SpeechRequest(
            "Next stop Central.", "voice from another provider", "", 0, 100)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(WaveAudio.GetDuration(result.WaveAudio.Span), Is.GreaterThan(TimeSpan.Zero));
        }
    }
    [Test]
    public async Task PiperProcessProbeStartsAndExitsWithoutSynthesisInput()
    {
        var executable = Environment.GetEnvironmentVariable("ComSpec")
            ?? throw new InvalidOperationException("Windows command processor is unavailable.");
        var probe = await new PiperProcessRunner().ProbeAsync(executable, TimeSpan.FromSeconds(5))
            .WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        Assert.That(probe.Succeeded, Is.True);
    }

    [Test]
    public async Task PiperProcessProbeAlreadyCancelledDoesNotStartTheExecutable()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(false);
        await Assert.CatchAsync<OperationCanceledException>(() => new PiperProcessRunner().ProbeAsync(
            Path.Combine(_directory, "missing.exe"), TimeSpan.FromSeconds(5), cancellation.Token)).ConfigureAwait(false);
    }

    [TestCase(1)]
    [TestCase(-1)]
    public async Task PiperUnsuccessfulStartupProbeReportsUnavailable(int exitCode)
    {
        var provider = CreatePiper(new FakeRunner { ExitCode = exitCode });
        var capabilities = await provider.GetCapabilitiesAsync().ConfigureAwait(false);
        Assert.That(capabilities.IsAvailable, Is.False);
    }
    [Test]
    public async Task PiperPreparedSynthesisKeepsCapturedModelAndPronunciationOptions()
    {
        var runner = new FakeRunner();
        var provider = CreatePiper(runner);
        _settings.Speech.Piper.SentenceSilenceSeconds = 0.4;
        _settings.Speech.Piper.PronunciationReplacements["Central"] = "Captured";
        var model = _settings.Speech.Piper.ModelPath;
        var prepared = provider.PrepareSynthesis(new SpeechRequest("Central", "", "", 0, 100));
        _settings.Speech.Piper.ModelPath = Path.Combine(_directory, "changed.onnx");
        _settings.Speech.Piper.PronunciationReplacements["Central"] = "Changed";
        _settings.Speech.Piper.SentenceSilenceSeconds = 0.9;
        var result = await prepared.SynthesizeAsync().ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(runner.Request!.ModelPath, Is.EqualTo(model));
            Assert.That(runner.Request.Text, Is.EqualTo("Captured"));
            Assert.That(runner.Request.SentenceSilenceSeconds, Is.EqualTo(0.4));
        }
    }

    [Test]
    public void PiperFingerprintChangesForEverySynthesisAffectingOption()
    {
        var provider = CreatePiper(new FakeRunner());
        var request = new SpeechRequest("Test", "", "", 0, 100);
        var fingerprints = new List<string> { provider.PrepareSynthesis(request).OptionsFingerprint };
        _settings.Speech.Piper.SentenceSilenceSeconds = 0.8;
        fingerprints.Add(provider.PrepareSynthesis(request).OptionsFingerprint);
        _settings.Speech.Piper.EnablePronunciationNormalization = false;
        fingerprints.Add(provider.PrepareSynthesis(request).OptionsFingerprint);
        _settings.Speech.Piper.PronunciationReplacements["Central"] = "Centre";
        fingerprints.Add(provider.PrepareSynthesis(request).OptionsFingerprint);
        _settings.Speech.Piper.ModelPath = Path.Combine(_directory, "changed-model.onnx");
        fingerprints.Add(provider.PrepareSynthesis(request).OptionsFingerprint);
        _settings.Speech.Piper.ConfigPath = Path.Combine(_directory, "config.json");
        File.WriteAllText(_settings.Speech.Piper.ConfigPath, "config");
        fingerprints.Add(provider.PrepareSynthesis(request).OptionsFingerprint);
        _settings.Speech.Piper.ExecutablePath = Path.Combine(_directory, "other.exe");
        fingerprints.Add(provider.PrepareSynthesis(request).OptionsFingerprint);
        Assert.That(fingerprints.Distinct().Count(), Is.EqualTo(fingerprints.Count));
        var latest = fingerprints[^1];
        _settings.Speech.Piper.TimeoutSeconds = 120;
        _settings.Speech.Piper.EnableAudioDiagnostics = true;
        Assert.That(provider.PrepareSynthesis(request).OptionsFingerprint, Is.EqualTo(latest));
    }

    [Test]
    public void PiperFingerprintIsIndependentOfProviderAvailabilitySoCachedAudioRemainsUsable()
    {
        var provider = CreatePiper(new FakeRunner());
        var request = new SpeechRequest("Test", "", "", 0, 100);
        var available = provider.PrepareSynthesis(request).OptionsFingerprint;
        File.Delete(_settings.Speech.Piper.ExecutablePath);
        File.Delete(_settings.Speech.Piper.ModelPath);
        Assert.That(provider.PrepareSynthesis(request).OptionsFingerprint, Is.EqualTo(available));
    }

    [Test]
    public void SystemSpeechFingerprintTracksConfiguredDefaultVoice()
    {
        var provider = new SystemSpeechProvider(_settings);
        var request = new SpeechRequest("Test", "", "", 0, 100);
        var first = provider.PrepareSynthesis(request);
        _settings.Speech.SystemSpeech.DefaultVoiceName = "Other voice";
        var second = provider.PrepareSynthesis(request);
        Assert.That(first.OptionsFingerprint, Is.Not.EqualTo(second.OptionsFingerprint));
    }
    private PiperSynthesisRequest CreateProcessRequest(string input, TimeSpan timeout) => new(
        Environment.GetEnvironmentVariable("ComSpec") ?? throw new InvalidOperationException("Windows command processor is unavailable."),
        _settings.Speech.Piper.ModelPath,
        "optional-config.json",
        Path.Combine(_directory, "process.wav"),
        input,
        1.25,
        0.4,
        timeout);

    private sealed class ProcessStartedClock : TimeProvider
    {
        public TaskCompletionSource TimeoutScheduled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = System.CreateTimer(callback, state, dueTime, period);
            TimeoutScheduled.TrySetResult();
            return timer;
        }
    }

    private PiperSpeechProvider CreatePiper(IPiperProcessRunner runner) => new(_settings, NullLogger<PiperSpeechProvider>.Instance, runner, Path.Combine(_directory, "diagnostics"));

    private sealed class FakeRunner : IPiperProcessRunner
    {
        public PiperSynthesisRequest? Request { get; private set; }
        public CancellationToken Token { get; private set; }
        public int ExitCode { get; init; }
        public CancellationTokenSource? Cancel { get; init; }
        public Task<PiperProcessResult> ProbeAsync(string executablePath, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new PiperProcessResult(ExitCode, string.Empty, string.Empty));
        }
        public async Task<PiperProcessResult> SynthesizeAsync(PiperSynthesisRequest request, CancellationToken cancellationToken = default)
        {
            Request = request;
            Token = cancellationToken;
            await File.WriteAllBytesAsync(request.OutputPath, TestWaveAudio.Create([12000, -12000]), cancellationToken).ConfigureAwait(false);
            if (Cancel is not null)
            {
                await Cancel.CancelAsync().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }
            return new PiperProcessResult(ExitCode, string.Empty, "raw diagnostic");
        }
    }
}
#endif
