// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Sound;

using Moba.Sound;

/// <summary>
/// Tests for NullSoundPlayer - the NullObject implementation for platforms without audio.
/// </summary>
[TestFixture]
public class NullSoundPlayerTests
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
}
