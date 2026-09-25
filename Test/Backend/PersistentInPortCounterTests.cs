// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moq;

[TestFixture]
public sealed class PersistentInPortCounterTests
{
    [Test]
    public async Task InventoryChanges_AreImmediatePreserveExistingCountsAndPersistRemoval()
    {
        var settings = Settings(2);
        var z21 = new Mock<IZ21>();
        var store = new MemoryStore();
        await using var counters = new InPortCounterService(z21.Object, settings, store: store);
        await counters.InitializeAsync();
        var activations = 0;
        counters.Counted += (_, _) => activations++;
        counters.Set(1, 42);
        settings.Counter.CountOfFeedbackPoints = 3;
        Assert.That(counters.GetSnapshot().Select(item => item.Count), Is.EqualTo(new ulong[] { 42, 0, 0 }));
        counters.Set(3, 9);
        settings.Counter.CountOfFeedbackPoints = 2;
        InPortCounterServiceTests.Raise(z21, 3);
        await counters.FlushAsync();

        Assert.Multiple(() =>
        {
            Assert.That(store.Counts, Is.EquivalentTo(new Dictionary<uint, ulong> { [1] = 42, [2] = 0 }));
            Assert.That(activations, Is.Zero);
        });
        settings.Counter.CountOfFeedbackPoints = 3;
        Assert.That(counters.GetSnapshot().Single(item => item.InPort == 3).Count, Is.Zero);
    }

    [Test]
    public async Task FileStore_RestartRestoresExactValuesWithoutFeedbackOrTimerHistory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "moba-counter-test-" + Guid.NewGuid());
        var store = new FileInPortCounterStore(Path.Combine(directory, "inport-counters.json"));
        try
        {
            var z21 = new Mock<IZ21>();
            await using (var first = new InPortCounterService(z21.Object, Settings(2), store: store))
            {
                await first.InitializeAsync();
                first.Set(1, ulong.MaxValue - 1);
                InPortCounterServiceTests.Raise(z21, 1);
                first.Set(2, 37);
            }

            await using var restarted = new InPortCounterService(z21.Object, Settings(3), store: store);
            var activations = 0;
            restarted.Counted += (_, _) => activations++;
            await restarted.InitializeAsync();
            Assert.Multiple(() =>
            {
                Assert.That(restarted.GetSnapshot().Select(item => item.Count), Is.EqualTo(new ulong[] { ulong.MaxValue, 37, 0 }));
                Assert.That(restarted.GetSnapshot().All(item => item.LastFeedbackTime is null && item.LastLapTime is null), Is.True);
                Assert.That(activations, Is.Zero);
            });
            InPortCounterServiceTests.Raise(z21, 1);
            Assert.That(activations, Is.Zero, "A saturated counter must not wrap around.");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task IndependentStores_DoNotShareCountsAndExplicitResetsSurviveRestart()
    {
        var firstStore = new MemoryStore();
        var secondStore = new MemoryStore();
        var z21 = new Mock<IZ21>();
        await using var first = new InPortCounterService(z21.Object, Settings(2), store: firstStore);
        await using var second = new InPortCounterService(z21.Object, Settings(2), store: secondStore);
        await first.InitializeAsync();
        await second.InitializeAsync();
        first.Set(1, 25);
        first.Set(2, 7);
        second.Set(1, 81);
        first.Set(1, 0);
        await first.FlushAsync();
        Assert.That(firstStore.Counts.Values, Is.EqualTo(new ulong[] { 0, 7 }));
        first.ResetAll();
        await first.FlushAsync();
        await second.FlushAsync();
        Assert.Multiple(() =>
        {
            Assert.That(firstStore.Counts.Values, Is.All.Zero);
            Assert.That(secondStore.Counts[1], Is.EqualTo(81UL));
        });
    }

    [Test]
    public async Task FeedbackDuringLoad_IsAppliedAfterRestorationInArrivalOrder()
    {
        var loaded = new TaskCompletionSource<IReadOnlyDictionary<uint, ulong>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new Mock<IInPortCounterStore>();
        store.Setup(value => value.LoadAsync(default)).Returns(loaded.Task);
        store.Setup(value => value.SaveAsync(It.IsAny<IReadOnlyDictionary<uint, ulong>>(), default)).Returns(Task.CompletedTask);
        var z21 = new Mock<IZ21>();
        await using var counters = new InPortCounterService(z21.Object, Settings(1), store: store.Object);
        var values = new List<ulong>();
        counters.Counted += (_, args) => values.Add(args.Snapshot.Count);
        var initialization = counters.InitializeAsync();
        InPortCounterServiceTests.Raise(z21, 1);
        InPortCounterServiceTests.Raise(z21, 1);
        Assert.That(values, Is.Empty);
        loaded.SetResult(new Dictionary<uint, ulong> { [1] = 40 });
        await initialization;
        Assert.That(values, Is.EqualTo(new ulong[] { 41, 42 }));
    }

    [Test]
    public async Task SlowSave_CoalescesConcurrentChangesAndFlushWaitsForLatestValue()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stored = new List<ulong>();
        var store = new Mock<IInPortCounterStore>();
        store.Setup(value => value.LoadAsync(default)).ReturnsAsync(new Dictionary<uint, ulong>());
        store.Setup(value => value.SaveAsync(It.IsAny<IReadOnlyDictionary<uint, ulong>>(), default))
            .Returns(async (IReadOnlyDictionary<uint, ulong> counts, CancellationToken _) =>
            {
                entered.TrySetResult();
                await release.Task;
                stored.Add(counts[1]);
            });
        var z21 = new Mock<IZ21>();
        await using var counters = new InPortCounterService(z21.Object, Settings(1), store: store.Object);
        await counters.InitializeAsync();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => Task.Run(() => InPortCounterServiceTests.Raise(z21, 1))));
            counters.Set(1, 200);
            InPortCounterServiceTests.Raise(z21, 1);
            Assert.That(counters.FlushAsync().IsCompleted, Is.False);
        }
        finally
        {
            release.SetResult();
        }
        await counters.FlushAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(stored, Is.EqualTo(new ulong[] { 0, 201 }));
    }

    [Test]
    public async Task FeedbackBufferedDuringLoad_UsesArrivalTimesForTimerFiltering()
    {
        var loaded = new TaskCompletionSource<IReadOnlyDictionary<uint, ulong>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new Mock<IInPortCounterStore>();
        store.Setup(value => value.LoadAsync(default)).Returns(loaded.Task);
        store.Setup(value => value.SaveAsync(It.IsAny<IReadOnlyDictionary<uint, ulong>>(), default)).Returns(Task.CompletedTask);
        var settings = Settings(1);
        settings.Counter.UseTimerFilter = true;
        settings.Counter.TimerIntervalSeconds = 10;
        var clock = new CounterClock();
        var z21 = new Mock<IZ21>();
        await using var counters = new InPortCounterService(z21.Object, settings, clock, store: store.Object);
        var initialization = counters.InitializeAsync();
        InPortCounterServiceTests.Raise(z21, 1);
        clock.Now += TimeSpan.FromSeconds(9);
        InPortCounterServiceTests.Raise(z21, 1);
        clock.Now += TimeSpan.FromSeconds(1);
        InPortCounterServiceTests.Raise(z21, 1);
        loaded.SetResult(new Dictionary<uint, ulong> { [1] = 10 });
        await initialization;
        Assert.Multiple(() =>
        {
            Assert.That(counters.GetSnapshot().Single().Count, Is.EqualTo(12UL));
            Assert.That(counters.GetSnapshot().Single().LastLapTime, Is.EqualTo(TimeSpan.FromSeconds(10)));
        });
    }

    [Test]
    public async Task FailedWrite_IsVisibleAndNextMutationCanPersistTheCurrentValue()
    {
        var store = new MemoryStore { FailWrites = true };
        using var counters = new InPortCounterService(Mock.Of<IZ21>(), Settings(1), store: store);
        await counters.InitializeAsync();
        counters.Set(1, 12);
        Assert.ThrowsAsync<IOException>(async () => await counters.FlushAsync());
        Assert.That(counters.PersistenceError, Does.Contain("Could not save"));
        store.FailWrites = false;
        counters.Set(1, 13);
        await counters.FlushAsync();
        Assert.Multiple(() =>
        {
            Assert.That(counters.PersistenceError, Is.Null);
            Assert.That(store.Counts[1], Is.EqualTo(13UL));
        });
    }

    [Test]
    public async Task Flush_CompletesOnceItsValueIsSavedEvenWhenLaterFeedbackIsStillSaving()
    {
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSecond = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new Mock<IInPortCounterStore>();
        store.Setup(value => value.LoadAsync(default)).ReturnsAsync(new Dictionary<uint, ulong>());
        store.Setup(value => value.SaveAsync(It.IsAny<IReadOnlyDictionary<uint, ulong>>(), default))
            .Returns(async (IReadOnlyDictionary<uint, ulong> counts, CancellationToken _) =>
            {
                if (counts[1] == 0)
                {
                    firstEntered.TrySetResult();
                    await releaseFirst.Task;
                }
                else
                {
                    secondEntered.TrySetResult();
                    await releaseSecond.Task;
                }
            });
        var z21 = new Mock<IZ21>();
        await using var counters = new InPortCounterService(z21.Object, Settings(1), store: store.Object);
        await counters.InitializeAsync();
        await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var flushed = counters.FlushAsync();
        try
        {
            InPortCounterServiceTests.Raise(z21, 1);
            releaseFirst.SetResult();
            await secondEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await flushed.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(counters.FlushAsync().IsCompleted, Is.False, "Later feedback still has its own persistence barrier.");
        }
        finally
        {
            releaseFirst.TrySetResult();
            releaseSecond.SetResult();
        }
    }

    [TestCase("null")]
    [TestCase("{\"1\":-1}")]
    [TestCase("{\"1\":1,\"1\":2}")]
    [TestCase("{\"0\":5}")]
    [TestCase("{\"1\":18446744073709551616}")]
    [TestCase("incomplete {")]
    public async Task InvalidFile_IsReportedAndNeverOverwritten(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), "moba-counter-test-" + Guid.NewGuid() + ".json");
        await File.WriteAllTextAsync(path, json);
        try
        {
            using var counters = new InPortCounterService(Mock.Of<IZ21>(), Settings(1), store: new FileInPortCounterStore(path));
            Assert.CatchAsync<Exception>(async () => await counters.InitializeAsync());
            Assert.Throws<InvalidOperationException>(() => counters.Set(1, 0));
            Assert.That(counters.PersistenceError, Does.Contain("Could not load"));
            Assert.That(await File.ReadAllTextAsync(path), Is.EqualTo(json));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static AppSettings Settings(int count) => new() { Counter = { CountOfFeedbackPoints = count, UseTimerFilter = false } };

    private sealed class CounterClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 9, 25, 10, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class MemoryStore : IInPortCounterStore
    {
        public IReadOnlyDictionary<uint, ulong> Counts { get; private set; } = new Dictionary<uint, ulong>();
        public bool FailWrites { get; set; }
        public Task<IReadOnlyDictionary<uint, ulong>> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Counts);
        public Task SaveAsync(IReadOnlyDictionary<uint, ulong> counts, CancellationToken cancellationToken = default)
        {
            if (FailWrites) throw new IOException("Test write failure.");
            Counts = new Dictionary<uint, ulong>(counts);
            return Task.CompletedTask;
        }
    }
}
