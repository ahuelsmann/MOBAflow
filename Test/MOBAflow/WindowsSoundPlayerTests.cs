#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.MOBAflow;

using global::Moba.WinUI.Service;

using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Failure and cancellation boundaries of the Windows WAV adapter.
/// </summary>
[TestFixture]
internal sealed class WindowsSoundPlayerTests
{
    [Test]
    public async Task WindowsAdapterMissingWaveFilePreservesTheCauseAndReportsTheRequestedPath()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mobaflow-missing-{Guid.NewGuid():N}.wav");
        Assert.That(File.Exists(path), Is.False);
        var player = new WindowsSoundPlayer(NullLogger<WindowsSoundPlayer>.Instance);

        // SoundPlayer validates the missing file before calling the native playback API.
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => player.PlayAsync(path)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(failure?.InnerException, Is.TypeOf<FileNotFoundException>());
            Assert.That(failure?.Message, Does.Contain(path));
        }
    }

    [Test]
    public async Task WindowsAdapterAlreadyCancelledPropagatesCancellationBeforeNativePlayback()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(false);
        var player = new WindowsSoundPlayer(NullLogger<WindowsSoundPlayer>.Instance);

        await Assert.CatchAsync<OperationCanceledException>(() => player.PlayAsync(
            "cancelled.wav", cancellation.Token)).ConfigureAwait(false);
    }
}
#endif
