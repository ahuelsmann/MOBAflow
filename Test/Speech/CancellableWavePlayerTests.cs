// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
#if WINDOWS
namespace Moba.Test.Speech;

using Moba.WinUI.Service;

[TestFixture]
[NonParallelizable]
internal sealed partial class CancellableWavePlayerTests
{
    [Test]
    public async Task CancellationAfterAudibleStart_StopsTheProductionPlayerAndDisposesItsSession()
    {
        using var cancellation = new CancellationTokenSource();
        using var session = new FakeSession();
        var player = new CancellableWavePlayer(_ => session);
        var playing = player.PlayAsync(TestWaveAudio.Create(new short[60000]), cancellation.Token);
        await session.Started.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        Assert.That(playing.IsCompleted, Is.False);
        await cancellation.CancelAsync().ConfigureAwait(false);
        await Assert.CatchAsync<OperationCanceledException>(() => playing).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(session.Stopped, Is.True);
            Assert.That(session.Disposed, Is.True);
        }
    }

    [Test]
    public async Task AlreadyCancelled_DoesNotCreateOrStartPlayback()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(false);
        var created = false;
        var player = new CancellableWavePlayer(_ => { created = true; return new FakeSession(); });
        await Assert.CatchAsync<OperationCanceledException>(() => player.PlayAsync(
            TestWaveAudio.Create([1000]), cancellation.Token)).ConfigureAwait(false);
        Assert.That(created, Is.False);
    }

    [Test]
    public async Task NormalCompletion_StopsAndDisposesPlayback()
    {
        using var session = new FakeSession();
        await new CancellableWavePlayer(_ => session).PlayAsync(TestWaveAudio.Create([1000])).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(session.Started.Task.IsCompleted, Is.True);
            Assert.That(session.Stopped, Is.True);
            Assert.That(session.Disposed, Is.True);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task OlderPlaybackCompletionOrCancellationDoesNotStopTheNewerSession(bool cancelOlder)
    {
        var clock = new PlaybackClock();
        var device = new FakeAudioDevice();
        using var oldCancellation = new CancellationTokenSource();
        using var newCancellation = new CancellationTokenSource();
        using var older = new FakeSession(device);
        using var newer = new FakeSession(device);
        var oldAudio = TestWaveAudio.Create(new short[16000]);
        var first = new CancellableWavePlayer(_ => older, clock).PlayAsync(oldAudio, oldCancellation.Token);
        Task second = Task.CompletedTask;
        try
        {
            await clock.FirstScheduled.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            second = new CancellableWavePlayer(_ => newer, clock).PlayAsync(
                TestWaveAudio.Create(new short[60000]), newCancellation.Token);
            await clock.SecondScheduled.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            if (cancelOlder)
            {
                await oldCancellation.CancelAsync().ConfigureAwait(false);
                await Assert.CatchAsync<OperationCanceledException>(() => first).ConfigureAwait(false);
            }
            else
            {
                clock.Clock.Advance(Moba.Speech.Base.WaveAudio.GetDuration(oldAudio));
                // The older playback was replaced, so it reports the interruption instead of a full playback.
                await Assert.ThrowsAsync<Moba.Speech.Base.WavePlaybackInterruptedException>(
                    () => first.WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
            }
            using (Assert.EnterMultipleScope())
            {
                Assert.That(device.ActiveSession, Is.SameAs(newer));
                Assert.That(older.Stopped, Is.False);
                Assert.That(second.IsCompleted, Is.False);
            }
            await newCancellation.CancelAsync().ConfigureAwait(false);
            await Assert.CatchAsync<OperationCanceledException>(() => second).ConfigureAwait(false);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(newer.Stopped, Is.True);
                Assert.That(device.ActiveSession, Is.Null);
            }
        }
        finally
        {
            await oldCancellation.CancelAsync().ConfigureAwait(false);
            await newCancellation.CancelAsync().ConfigureAwait(false);
            try { await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }
    }

    private sealed class PlaybackClock : TimeProvider
    {
        private int _scheduled;
        public Moba.Test.MOBAdisplay.ManualTimeProvider Clock { get; } = new();
        public TaskCompletionSource FirstScheduled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SecondScheduled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = Clock.CreateTimer(callback, state, dueTime, period);
            if (Interlocked.Increment(ref _scheduled) == 1) FirstScheduled.TrySetResult();
            else SecondScheduled.TrySetResult();
            return timer;
        }
    }

    private sealed class FakeAudioDevice
    {
        public IWavePlaybackSession? ActiveSession { get; set; }
    }
    private sealed partial class FakeSession(FakeAudioDevice? device = null) : IWavePlaybackSession
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Stopped { get; private set; }
        public bool Disposed { get; private set; }
        public void Play()
        {
            if (device is not null) device.ActiveSession = this;
            Started.SetResult();
        }
        public void Stop()
        {
            Stopped = true;
            if (device is not null) device.ActiveSession = null;
        }
        public void Dispose() => Disposed = true;
    }
}
#endif
