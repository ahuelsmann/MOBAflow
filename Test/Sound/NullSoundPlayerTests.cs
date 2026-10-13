// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Sound;

using Moba.Sound;

#if WINDOWS
using Microsoft.Extensions.Logging.Abstractions;
#endif

/// <summary>
/// Tests for null sound output and the Windows adapter failure/cancellation boundaries.
/// </summary>
[TestFixture]
internal class NullSoundPlayerTests
{
    private ISoundPlayer _player = null!;

    [SetUp]
    public void SetUp()
    {
        _player = new NullSoundPlayer();
    }

    [Test]
    public async Task PlayAsync_CompletesSuccessfully()
    {
        await _player.PlayAsync("sound.wav");

        Assert.Pass("PlayAsync completed without throwing");
    }

    [Test]
    public async Task PlayAsync_WithCancellationToken_CompletesSuccessfully()
    {
        using var cts = new CancellationTokenSource();

        await _player.PlayAsync("sound.wav", cts.Token);

        Assert.Pass("PlayAsync with cancellation token completed");
    }

    [Test]
    public async Task PlayAsync_WithNonExistentFile_CompletesSuccessfully()
    {
        // NullSoundPlayer doesn't validate file existence
        await _player.PlayAsync("/non/existent/path/sound.wav");

        Assert.Pass("PlayAsync with non-existent file completed");
    }

    [Test]
    public async Task PlayAsync_MultipleCalls_AllComplete()
    {
        await _player.PlayAsync("gong.wav");
        await _player.PlayAsync("announcement.wav");
        await _player.PlayAsync("chime.wav");

        Assert.Pass("Multiple plays completed");
    }

    [Test]
    public async Task PlayAsync_WithEmptyPath_CompletesSuccessfully()
    {
        await _player.PlayAsync("");

        Assert.Pass("PlayAsync with empty path completed");
    }
#if WINDOWS
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

#endif
}