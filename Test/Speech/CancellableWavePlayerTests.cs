// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
#if WINDOWS
namespace Moba.Test.Speech;

using Moba.WinUI.Service;

[TestFixture]
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

    private sealed partial class FakeSession : IWavePlaybackSession
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Stopped { get; private set; }
        public bool Disposed { get; private set; }
        public void Play() => Started.SetResult();
        public void Stop() => Stopped = true;
        public void Dispose() => Disposed = true;
    }
}
#endif
