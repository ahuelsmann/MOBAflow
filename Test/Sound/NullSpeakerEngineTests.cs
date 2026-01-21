// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Test.Sound;

using Moba.Sound;

/// <summary>
/// Tests for NullSpeakerEngine - the NullObject implementation for platforms without TTS.
/// </summary>
[TestFixture]
public class NullSpeakerEngineTests
{
    private ISpeakerEngine _engine = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new NullSpeakerEngine();
    }

    [Test]
    public void Name_HasDefaultValue()
    {
        Assert.That(_engine.Name, Does.Contain("Null"));
    }

    [Test]
    public void Name_CanBeSet()
    {
        _engine.Name = "Custom Name";

        Assert.That(_engine.Name, Is.EqualTo("Custom Name"));
    }

    [Test]
    public async Task AnnouncementAsync_CompletesSuccessfully()
    {
        await _engine.AnnouncementAsync("Test message", "de-DE-KatjaNeural");

        Assert.Pass("AnnouncementAsync completed without throwing");
    }

    [Test]
    public async Task AnnouncementAsync_WithNullVoice_CompletesSuccessfully()
    {
        await _engine.AnnouncementAsync("Test message", null);

        Assert.Pass("AnnouncementAsync with null voice completed");
    }

    [Test]
    public async Task AnnouncementAsync_WithEmptyMessage_CompletesSuccessfully()
    {
        await _engine.AnnouncementAsync("", null);

        Assert.Pass("AnnouncementAsync with empty message completed");
    }

    [Test]
    public async Task AnnouncementAsync_MultipleCalls_AllComplete()
    {
        await _engine.AnnouncementAsync("First", null);
        await _engine.AnnouncementAsync("Second", null);
        await _engine.AnnouncementAsync("Third", null);

        Assert.Pass("Multiple announcements completed");
    }
}
