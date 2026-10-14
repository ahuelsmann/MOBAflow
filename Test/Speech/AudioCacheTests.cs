// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Speech;

using Moba.Speech.Base;
using Moba.Test.MOBAdisplay;

[TestFixture]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812", Justification = "NUnit creates this fixture through reflection.")]
internal sealed class AudioCacheTests
{
    private string _directory = null!;
    private readonly ManualTimeProvider _clock = new();
    private static string Key(string text) => SpeechCacheKey.Create("test", "options", new SpeechRequest(text, "", "", 0, 100));

    [SetUp]
    public void SetUp() => _directory = Path.Combine(Path.GetTempPath(), "mobaflow-cache-tests-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }

    [Test]
    public async Task StoresAudioAndIndexAcrossCacheInstancesWithoutExposingText()
    {
        var audio = TestWaveAudio.Create([123, -123]);
        var key = Key("Private station text");
        using (var cache = new AudioCache(_directory, () => 500L * 1024 * 1024, _clock))
        {
            await (cache.StoreAsync(key, audio, cache.Generation)).ConfigureAwait(false);
            Assert.That(await (cache.GetSizeAsync()).ConfigureAwait(false), Is.EqualTo(audio.Length));
        }
        using var reopened = new AudioCache(_directory, () => 500L * 1024 * 1024, _clock);
        Assert.That((await reopened.TryGetAsync(key).ConfigureAwait(false)).Audio?.ToArray(), Is.EqualTo(audio));
        var index = await (File.ReadAllTextAsync(Path.Combine(_directory, "index.json"))).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(index, Does.Not.Contain("Private station text"));
            Assert.That(Directory.GetFiles(_directory).Select(Path.GetFileName), Is.EquivalentTo(new[] { "cache.lock", "index.json", key + ".wav" }));
        }
    }

    [Test]
    public async Task EvictsLeastRecentlyPlayedRatherThanOldestCreatedOrLastRead()
    {
        var audio = TestWaveAudio.Create([1]);
        using var cache = new AudioCache(_directory, () => audio.Length * 2L, _clock);
        await (cache.StoreAsync(Key("first"), audio, cache.Generation)).ConfigureAwait(false);
        _clock.Advance(TimeSpan.FromSeconds(1));
        await (cache.StoreAsync(Key("second"), audio, cache.Generation)).ConfigureAwait(false);
        _clock.Advance(TimeSpan.FromSeconds(1));
        await (cache.MarkPlayedAsync(Key("first"))).ConfigureAwait(false);
        _clock.Advance(TimeSpan.FromSeconds(1));
        await (cache.TryGetAsync(Key("second"))).ConfigureAwait(false);
        await (cache.StoreAsync(Key("third"), audio, cache.Generation)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That((await cache.TryGetAsync(Key("first")).ConfigureAwait(false)).Audio, Is.Not.Null);
            Assert.That((await cache.TryGetAsync(Key("second")).ConfigureAwait(false)).Audio, Is.Null);
            Assert.That((await cache.TryGetAsync(Key("third")).ConfigureAwait(false)).Audio, Is.Not.Null);
            Assert.That(await (cache.GetSizeAsync()).ConfigureAwait(false), Is.EqualTo(audio.Length * 2L));
        }
        using var reopened = new AudioCache(_directory, () => audio.Length * 2L, _clock);
        Assert.That((await reopened.TryGetAsync(Key("second")).ConfigureAwait(false)).Audio, Is.Null);
    }

    [Test]
    public async Task CacheInstancesSharingADirectoryKeepEachOthersEntries()
    {
        var audio = TestWaveAudio.Create([1]);
        using var first = new AudioCache(_directory, () => 1000, _clock);
        using var second = new AudioCache(_directory, () => 1000, _clock);
        Assert.That((await second.TryGetAsync(Key("first")).ConfigureAwait(false)).Audio, Is.Null);
        await first.StoreAsync(Key("first"), audio, first.Generation).ConfigureAwait(false);
        await second.StoreAsync(Key("second"), audio, second.Generation).ConfigureAwait(false);
        await first.MarkPlayedAsync(Key("first")).ConfigureAwait(false);
        using var restarted = new AudioCache(_directory, () => 1000, _clock);
        using (Assert.EnterMultipleScope())
        {
            Assert.That((await restarted.TryGetAsync(Key("first")).ConfigureAwait(false)).Audio, Is.Not.Null);
            Assert.That((await restarted.TryGetAsync(Key("second")).ConfigureAwait(false)).Audio, Is.Not.Null);
            Assert.That(await restarted.GetSizeAsync().ConfigureAwait(false), Is.EqualTo(audio.Length * 2L));
        }
    }

    [Test]
    public async Task ClearInAnotherInstanceInvalidatesItsOlderWrites()
    {
        using var first = new AudioCache(_directory, () => 1000, _clock);
        using var second = new AudioCache(_directory, () => 1000, _clock);
        var before = second.Generation;
        await first.ClearAsync().ConfigureAwait(false);
        await second.StoreAsync(Key("late"), TestWaveAudio.Create([1]), before).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That((await first.TryGetAsync(Key("late")).ConfigureAwait(false)).Audio, Is.Null);
            Assert.That(second.Generation, Is.GreaterThan(before));
            Assert.That(Directory.GetFiles(_directory, "*.wav"), Is.Empty);
        }
    }

    [Test]
    public async Task LookupReportsTheGenerationOfAClearByAnotherInstance()
    {
        using var first = new AudioCache(_directory, () => 1000, _clock);
        using var second = new AudioCache(_directory, () => 1000, _clock);
        var before = (await second.TryGetAsync(Key("before")).ConfigureAwait(false)).Generation;
        await first.ClearAsync().ConfigureAwait(false);
        var lookup = await second.TryGetAsync(Key("after")).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(lookup.Audio, Is.Null);
            Assert.That(lookup.Generation, Is.EqualTo(first.Generation));
            Assert.That(lookup.Generation, Is.GreaterThan(before));
        }
    }

    [Test]
    public async Task WaitsWhileAnotherProcessHoldsTheCacheLock()
    {
        using var cache = new AudioCache(_directory, () => 1000, _clock);
        Directory.CreateDirectory(_directory);
        Task<AudioCacheLookup> lookup;
        using (new FileStream(Path.Combine(_directory, "cache.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        {
            lookup = cache.TryGetAsync(Key("waiting"));
            await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
            Assert.That(lookup.IsCompleted, Is.False);
        }
        Assert.That((await lookup.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false)).Audio, Is.Null);
    }

    [Test]
    public async Task TemporaryFilesFromAnInterruptedWriteAreRemoved()
    {
        Directory.CreateDirectory(_directory);
        var leftovers = new[]
        {
            Path.Combine(_directory, Key("interrupted") + ".wav." + Guid.NewGuid().ToString("N") + ".tmp"),
            Path.Combine(_directory, "index.json." + Guid.NewGuid().ToString("N") + ".tmp")
        };
        foreach (var path in leftovers) await File.WriteAllBytesAsync(path, TestWaveAudio.Create([1])).ConfigureAwait(false);
        using var cache = new AudioCache(_directory, () => 1000, _clock);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(await cache.GetSizeAsync().ConfigureAwait(false), Is.Zero);
            Assert.That(leftovers.Where(File.Exists), Is.Empty);
        }
    }

    [Test]
    public async Task LoweredLimitRemovesRetainedAudioWhenTheNextAudioIsNotStored()
    {
        long limit = 1000;
        using var cache = new AudioCache(_directory, () => limit, _clock);
        await cache.StoreAsync(Key("retained"), TestWaveAudio.Create([1]), cache.Generation).ConfigureAwait(false);
        limit = 0;
        await cache.StoreAsync(Key("rejected"), TestWaveAudio.Create([2]), cache.Generation).ConfigureAwait(false);
        Assert.That(Directory.GetFiles(_directory, "*.wav"), Is.Empty);
    }

    [Test]
    public async Task LoweredLimitAppliesBeforeAHitIsServed()
    {
        long limit = 1000;
        using var cache = new AudioCache(_directory, () => limit, _clock);
        await cache.StoreAsync(Key("retained"), TestWaveAudio.Create([1]), cache.Generation).ConfigureAwait(false);
        await cache.MarkPlayedAsync(Key("retained")).ConfigureAwait(false);
        limit = 0;
        using (Assert.EnterMultipleScope())
        {
            Assert.That((await cache.TryGetAsync(Key("retained")).ConfigureAwait(false)).Audio, Is.Null);
            Assert.That(Directory.GetFiles(_directory, "*.wav"), Is.Empty);
        }
    }

    [Test]
    public async Task OrphansLeftAfterTheFirstSweepAreRemovedBySizeAndClear()
    {
        using var cache = new AudioCache(_directory, () => 1000, _clock);
        Assert.That(await cache.GetSizeAsync().ConfigureAwait(false), Is.Zero);
        var sizeOrphan = Path.Combine(_directory, Key("size orphan") + ".wav");
        await File.WriteAllBytesAsync(sizeOrphan, TestWaveAudio.Create([1])).ConfigureAwait(false);
        Assert.That(await cache.GetSizeAsync().ConfigureAwait(false), Is.Zero);
        var clearOrphan = Path.Combine(_directory, Key("clear orphan") + ".wav");
        await File.WriteAllBytesAsync(clearOrphan, TestWaveAudio.Create([1])).ConfigureAwait(false);
        await cache.ClearAsync().ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(File.Exists(sizeOrphan), Is.False);
            Assert.That(File.Exists(clearOrphan), Is.False);
        }
    }

    [Test]
    public async Task FailedIndexWriteAfterStoringAudioSweepsTheUnindexedAudio()
    {
        var failAfterAudioIsWritten = false;
        var limitReads = 0;
        using var cache = new AudioCache(_directory, () =>
        {
            // The store reads the limit once before and once after moving the WAV into place.
            if (failAfterAudioIsWritten && ++limitReads == 2) throw new IOException("Index update failed.");
            return 1000;
        }, _clock);
        Assert.That((await cache.TryGetAsync(Key("first lookup")).ConfigureAwait(false)).Audio, Is.Null);
        failAfterAudioIsWritten = true;
        await Assert.ThrowsAsync<IOException>(() => cache.StoreAsync(Key("unindexed"), TestWaveAudio.Create([1]), cache.Generation))
            .ConfigureAwait(false);
        var audioPath = Path.Combine(_directory, Key("unindexed") + ".wav");
        Assert.That(File.Exists(audioPath), Is.True);
        using (Assert.EnterMultipleScope())
        {
            Assert.That((await cache.TryGetAsync(Key("unindexed")).ConfigureAwait(false)).Audio, Is.Null);
            Assert.That(File.Exists(audioPath), Is.False);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task MissingOrCorruptWaveIsDiscarded(bool corrupt)
    {
        using var cache = new AudioCache(_directory, () => 1000, _clock);
        var key = Key("broken");
        await (cache.StoreAsync(key, TestWaveAudio.Create([1]), cache.Generation)).ConfigureAwait(false);
        var path = Path.Combine(_directory, key + ".wav");
        if (corrupt) await (File.WriteAllBytesAsync(path, [1, 2])).ConfigureAwait(false);
        else File.Delete(path);
        using (Assert.EnterMultipleScope())
        {
            Assert.That((await cache.TryGetAsync(key).ConfigureAwait(false)).Audio, Is.Null);
            Assert.That(await (cache.GetSizeAsync()).ConfigureAwait(false), Is.Zero);
            Assert.That(File.Exists(path), Is.False);
        }
    }

    [Test]
    public async Task CorruptIndexAndOrphanAudioAreDroppedOnRestart()
    {
        Directory.CreateDirectory(_directory);
        await (File.WriteAllTextAsync(Path.Combine(_directory, "index.json"), "invalid json")).ConfigureAwait(false);
        var path = Path.Combine(_directory, Key("orphan") + ".wav");
        await (File.WriteAllBytesAsync(path, TestWaveAudio.Create([1]))).ConfigureAwait(false);
        using var cache = new AudioCache(_directory, () => 1000, _clock);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(await (cache.GetSizeAsync()).ConfigureAwait(false), Is.Zero);
            Assert.That(File.Exists(path), Is.False);
        }
    }

    [Test]
    public async Task ClearInvalidatesOldWritesAndLeavesNoAudio()
    {
        using var cache = new AudioCache(_directory, () => 1000, _clock);
        var before = cache.Generation;
        await (cache.StoreAsync(Key("first"), TestWaveAudio.Create([1]), before)).ConfigureAwait(false);
        await (cache.ClearAsync()).ConfigureAwait(false);
        await (cache.StoreAsync(Key("late"), TestWaveAudio.Create([1]), before)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(await (cache.GetSizeAsync()).ConfigureAwait(false), Is.Zero);
            Assert.That(Directory.GetFiles(_directory, "*.wav"), Is.Empty);
        }
        await (cache.StoreAsync(Key("new"), TestWaveAudio.Create([1]), cache.Generation)).ConfigureAwait(false);
        Assert.That((await cache.TryGetAsync(Key("new")).ConfigureAwait(false)).Audio, Is.Not.Null);
    }

    [Test]
    public async Task LoweredLimitIgnoresIndexedAudioWhoseFileIsMissing()
    {
        var audio = TestWaveAudio.Create([1]);
        var limit = audio.Length * 3L;
        using var cache = new AudioCache(_directory, () => limit, _clock);
        foreach (var text in new[] { "oldest", "middle", "missing" })
        {
            await cache.StoreAsync(Key(text), audio, cache.Generation).ConfigureAwait(false);
            _clock.Advance(TimeSpan.FromSeconds(1));
        }
        File.Delete(Path.Combine(_directory, Key("missing") + ".wav"));
        limit = audio.Length * 2L;
        using (Assert.EnterMultipleScope())
        {
            Assert.That((await cache.TryGetAsync(Key("oldest")).ConfigureAwait(false)).Audio, Is.Not.Null);
            Assert.That((await cache.TryGetAsync(Key("middle")).ConfigureAwait(false)).Audio, Is.Not.Null);
            Assert.That((await cache.TryGetAsync(Key("missing")).ConfigureAwait(false)).Audio, Is.Null);
        }
    }

    [Test]
    public async Task StaleStoreOfAnExistingEntryRecordsItsPlayback()
    {
        var audio = TestWaveAudio.Create([1]);
        using var cache = new AudioCache(_directory, () => audio.Length * 2L, _clock);
        var stale = cache.Generation;
        await cache.ClearAsync().ConfigureAwait(false);
        await cache.StoreAsync(Key("rechecked"), audio, cache.Generation).ConfigureAwait(false);
        _clock.Advance(TimeSpan.FromSeconds(1));
        await cache.StoreAsync(Key("other"), audio, cache.Generation).ConfigureAwait(false);
        _clock.Advance(TimeSpan.FromSeconds(1));
        await cache.StoreAsync(Key("rechecked"), audio, stale).ConfigureAwait(false);
        _clock.Advance(TimeSpan.FromSeconds(1));
        await cache.StoreAsync(Key("new"), audio, cache.Generation).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That((await cache.TryGetAsync(Key("rechecked")).ConfigureAwait(false)).Audio, Is.Not.Null);
            Assert.That((await cache.TryGetAsync(Key("other")).ConfigureAwait(false)).Audio, Is.Null);
            Assert.That((await cache.TryGetAsync(Key("new")).ConfigureAwait(false)).Audio, Is.Not.Null);
        }
    }

    [Test]
    public async Task StoreRemovesOrphansLeftByAnotherProcess()
    {
        using var cache = new AudioCache(_directory, () => 1000, _clock);
        Assert.That((await cache.TryGetAsync(Key("first lookup")).ConfigureAwait(false)).Audio, Is.Null);
        var orphan = Path.Combine(_directory, Key("other process") + ".wav");
        await File.WriteAllBytesAsync(orphan, TestWaveAudio.Create([1])).ConfigureAwait(false);
        await cache.StoreAsync(Key("stored"), TestWaveAudio.Create([2]), cache.Generation).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(File.Exists(orphan), Is.False);
            Assert.That((await cache.TryGetAsync(Key("stored")).ConfigureAwait(false)).Audio, Is.Not.Null);
        }
    }

    [Test]
    public async Task ChangingLimitEvictsExistingEntriesAndOversizedAudioIsNotStored()
    {
        long limit = 1000;
        using var cache = new AudioCache(_directory, () => limit, _clock);
        await (cache.StoreAsync(Key("first"), TestWaveAudio.Create([1]), cache.Generation)).ConfigureAwait(false);
        limit = 1;
        Assert.That(await (cache.GetSizeAsync()).ConfigureAwait(false), Is.Zero);
        await (cache.StoreAsync(Key("oversized"), TestWaveAudio.Create([2]), cache.Generation)).ConfigureAwait(false);
        Assert.That((await cache.TryGetAsync(Key("oversized")).ConfigureAwait(false)).Audio, Is.Null);
    }

    [Test]
    public async Task InvalidKeyAndInvalidAudioNeverCreateFiles()
    {
        using var cache = new AudioCache(_directory, () => 1000, _clock);
        await (Assert.ThrowsAsync<ArgumentException>(() => cache.StoreAsync("../escaped", TestWaveAudio.Create([1]), 0))).ConfigureAwait(false);
        await (Assert.ThrowsAsync<InvalidDataException>(() => cache.StoreAsync(Key("invalid"), new byte[] { 1 }, 0))).ConfigureAwait(false);
        Assert.That(Directory.Exists(_directory), Is.False);
    }

    [Test]
    public async Task AlreadyCancelledStoreDoesNotWrite()
    {
        using var cache = new AudioCache(_directory, () => 1000, _clock);
        using var cancellation = new CancellationTokenSource();
        await (cancellation.CancelAsync()).ConfigureAwait(false);
        await (Assert.CatchAsync<OperationCanceledException>(() => cache.StoreAsync(Key("cancelled"), TestWaveAudio.Create([1]), 0, cancellation.Token))).ConfigureAwait(false);
        Assert.That(Directory.Exists(_directory), Is.False);
    }

    [Test]
    public void KeyIncludesEveryOptionAndUsesUnambiguousCultureIndependentEncoding()
    {
        var request = new SpeechRequest("Test", "Voice", "en-GB", 0, 90);
        var original = SpeechCacheKey.Create("provider", "model", request);
        var variants = new[]
        {
            SpeechCacheKey.Create("other", "model", request), SpeechCacheKey.Create("provider", "other", request),
            SpeechCacheKey.Create("provider", "model", request with { Text = "Other" }),
            SpeechCacheKey.Create("provider", "model", request with { VoiceName = "Other" }),
            SpeechCacheKey.Create("provider", "model", request with { Language = "de-DE" }),
            SpeechCacheKey.Create("provider", "model", request with { Rate = 1 }),
            SpeechCacheKey.Create("provider", "model", request with { Volume = 91 })
        };
        using (Assert.EnterMultipleScope())
        {
            Assert.That(variants, Has.None.EqualTo(original));
            Assert.That(original, Does.Match("^[A-F0-9]{64}$"));
            Assert.That(SpeechCacheKey.Fingerprint(["ab", "c"]), Is.Not.EqualTo(SpeechCacheKey.Fingerprint(["a", "bc"])));
            Assert.That(SpeechCacheKey.Create("p", "o", request with { Text = "e\u0301" }),
                Is.EqualTo(SpeechCacheKey.Create("p", "o", request with { Text = "\u00e9" })));
        }
    }
}
