// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging;
using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moq;

[TestFixture]
internal sealed class PersistentInPortCounterTests
{
    [Test]
    public async Task InventoryChangesAreImmediatePreserveExistingCountsAndPersistRemoval()
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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(store.Counts, Is.EquivalentTo(new Dictionary<uint, ulong> { [1] = 42, [2] = 0 }));
            Assert.That(activations, Is.Zero);
        }
        settings.Counter.CountOfFeedbackPoints = 3;
        Assert.That(counters.GetSnapshot().Single(item => item.InPort == 3).Count, Is.Zero);
    }

    [Test]
    public async Task FileStoreRestartRestoresExactValuesWithoutFeedbackOrTimerHistory()
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
            using (Assert.EnterMultipleScope())
            {
                Assert.That(restarted.GetSnapshot().Select(item => item.Count), Is.EqualTo(new ulong[] { ulong.MaxValue, 37, 0 }));
                Assert.That(restarted.GetSnapshot().All(item => item.LastFeedbackTime is null && item.LastLapTime is null), Is.True);
                Assert.That(activations, Is.Zero);
            }
            InPortCounterServiceTests.Raise(z21, 1);
            Assert.That(activations, Is.Zero, "A saturated counter must not wrap around.");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task IndependentStoresDoNotShareCountsAndExplicitResetsSurviveRestart()
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstStore.Counts.Values, Is.All.Zero);
            Assert.That(secondStore.Counts[1], Is.EqualTo(81UL));
        }
    }

    [Test]
    public async Task FeedbackDuringLoadIsAppliedAfterRestorationInArrivalOrder()
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
    public async Task FeedbackBufferedDuringLoadIsDiscardedWhenItsInputIsRemovedAndReadded()
    {
        var loaded = new TaskCompletionSource<IReadOnlyDictionary<uint, ulong>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new Mock<IInPortCounterStore>();
        store.Setup(value => value.LoadAsync(default)).Returns(loaded.Task);
        store.Setup(value => value.SaveAsync(It.IsAny<IReadOnlyDictionary<uint, ulong>>(), default)).Returns(Task.CompletedTask);
        var settings = Settings(2);
        var z21 = new Mock<IZ21>();
        await using var counters = new InPortCounterService(z21.Object, settings, store: store.Object);
        var activatedInputs = new List<uint>();
        counters.Counted += (_, args) => activatedInputs.Add(args.Snapshot.InPort);
        var initialization = counters.InitializeAsync();
        InPortCounterServiceTests.Raise(z21, 1);
        InPortCounterServiceTests.Raise(z21, 2);
        settings.Counter.CountOfFeedbackPoints = 1;
        settings.Counter.CountOfFeedbackPoints = 2;
        loaded.SetResult(new Dictionary<uint, ulong> { [1] = 5 });
        await initialization;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(activatedInputs, Is.EqualTo(new uint[] { 1 }), "The removed input's old activation must not run.");
            Assert.That(counters.GetSnapshot().Select(item => item.Count), Is.EqualTo(new ulong[] { 6, 0 }));
        }
    }

    [Test]
    public async Task SlowSaveCoalescesConcurrentChangesAndFlushWaitsForLatestValue()
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
    public async Task FeedbackBufferedDuringLoadUsesArrivalTimesForTimerFiltering()
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(counters.GetSnapshot().Single().Count, Is.EqualTo(12UL));
            Assert.That(counters.GetSnapshot().Single().LastLapTime, Is.EqualTo(TimeSpan.FromSeconds(10)));
        }
    }

    [Test]
    public async Task FailedWriteIsVisibleAndNextMutationCanPersistTheCurrentValue()
    {
        var store = new MemoryStore { FailWrites = true };
        using var counters = new InPortCounterService(Mock.Of<IZ21>(), Settings(1), store: store);
        await counters.InitializeAsync();
        counters.Set(1, 12);
        await Assert.ThrowsAsync<IOException>(async () => await counters.FlushAsync()).ConfigureAwait(false);
        Assert.That(counters.PersistenceError, Does.Contain("Could not save"));
        store.FailWrites = false;
        counters.Set(1, 13);
        await counters.FlushAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(counters.PersistenceError, Is.Null);
            Assert.That(store.Counts[1], Is.EqualTo(13UL));
        }
    }

    [Test]
    public async Task FlushCompletesOnceItsValueIsSavedEvenWhenLaterFeedbackIsStillSaving()
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
    public async Task InvalidFileIsReportedAndNeverOverwritten(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), "moba-counter-test-" + Guid.NewGuid() + ".json");
        await File.WriteAllTextAsync(path, json);
        try
        {
            using var counters = new InPortCounterService(Mock.Of<IZ21>(), Settings(1), store: new FileInPortCounterStore(path));
            await Assert.CatchAsync<Exception>(async () => await counters.InitializeAsync()).ConfigureAwait(false);
            Assert.Throws<InvalidOperationException>(() => counters.Set(1, 0));
            var content = await File.ReadAllTextAsync(path);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(counters.PersistenceError, Does.Contain("Could not load"));
                Assert.That(content, Is.EqualTo(json));
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task ResetAllReplacesUnreadableCountsAndResumesCounting()
    {
        var path = Path.Combine(Path.GetTempPath(), "moba-counter-test-" + Guid.NewGuid() + ".json");
        await File.WriteAllTextAsync(path, "incomplete {");
        try
        {
            var z21 = new Mock<IZ21>();
            using var counters = new InPortCounterService(z21.Object, Settings(2), store: new FileInPortCounterStore(path));
            var activations = 0;
            counters.Counted += (_, _) => activations++;
            Assert.That(await counters.TryInitializeAsync(), Is.False);
            InPortCounterServiceTests.Raise(z21, 1);
            Assert.That(activations, Is.Zero, "Activations must not be counted against unknown saved values.");

            counters.ResetAll();
            InPortCounterServiceTests.Raise(z21, 1);
            await counters.FlushAsync();

            var saved = await new FileInPortCounterStore(path).LoadAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(activations, Is.EqualTo(1));
                Assert.That(counters.PersistenceError, Is.Null);
                Assert.That(counters.GetSnapshot().Select(item => item.Count), Is.EqualTo(new ulong[] { 1, 0 }));
                Assert.That(saved, Is.EquivalentTo(new Dictionary<uint, ulong> { [1] = 1, [2] = 0 }));
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task UnreadableCountsDoNotBlockRuntimeStartOrConnect()
    {
        var store = new Mock<IInPortCounterStore>();
        store.Setup(value => value.LoadAsync(default)).ThrowsAsync(new InvalidDataException("Broken counter file."));
        var z21 = new Mock<IZ21>();
        z21.SetupGet(value => value.TrafficMonitor).Returns((Z21Monitor?)null);
        var settings = Settings(1);
        using var counters = new InPortCounterService(z21.Object, settings, store: store.Object);
        using var runtime = new MobaRuntimeService(z21.Object, Mock.Of<IWorkflowService>(),
            new ActionExecutionContext { Z21 = z21.Object }, settings, Mock.Of<ILogger<MobaRuntimeService>>(),
            inPortCounterService: counters);

        await runtime.StartAsync();
        await runtime.ConnectAsync();

        Assert.That(runtime.Current.InPortCounterPersistenceError, Does.Contain("Broken counter file."));
    }

    private static AppSettings Settings(int count) => new() { Counter = { CountOfFeedbackPoints = count, UseTimerFilter = false } };

    [Test]
    public async Task MobileProjectStores_KeepIndependentCountsAcrossSwitchesAndRestart()
    {
        var station = Guid.NewGuid();
        var yard = Guid.NewGuid();
        var stores = new Dictionary<Guid, MemoryStore> { [station] = new(), [yard] = new() };
        var z21 = new Mock<IZ21>();
        var counters = new InPortCounterService(z21.Object, Settings(1), projectStoreFactory: id => stores[id]);
        await using (counters.ConfigureAwait(false))
        {
            await counters.SelectProjectAsync(station).ConfigureAwait(false);
            counters.Set(1, 42);
            await counters.SelectProjectAsync(yard).ConfigureAwait(false);
            Assert.That(counters.GetSnapshot()[0].Count, Is.Zero);
            counters.Set(1, 7);
            await counters.SelectProjectAsync(station).ConfigureAwait(false);
            Assert.That(counters.GetSnapshot()[0].Count, Is.EqualTo(42UL));
            await counters.SelectProjectAsync(yard).ConfigureAwait(false);
            Assert.That(counters.GetSnapshot()[0].Count, Is.EqualTo(7UL));
        }
        var restarted = new InPortCounterService(z21.Object, Settings(1), projectStoreFactory: id => stores[id]);
        await using var restartedScope = restarted.ConfigureAwait(false);
        await restarted.SelectProjectAsync(station).ConfigureAwait(false);
        Assert.That(restarted.GetSnapshot()[0].Count, Is.EqualTo(42UL));
        await restarted.SelectProjectAsync(yard).ConfigureAwait(false);
        Assert.That(restarted.GetSnapshot()[0].Count, Is.EqualTo(7UL));
    }

    [Test]
    public async Task MobileCounterStore_FlushFailurePreservesPreviousProjectCounts()
    {
        var station = Guid.NewGuid();
        var store = new MemoryStore { FailWrites = true };
        var counters = new InPortCounterService(new Mock<IZ21>().Object, Settings(1), projectStoreFactory: _ => store);
        await using var countersScope = counters.ConfigureAwait(false);
        await counters.SelectProjectAsync(station).ConfigureAwait(false);
        counters.Set(1, 42);

        await Assert.ThrowsAsync<IOException>(() => counters.SelectProjectAsync(Guid.NewGuid())).ConfigureAwait(false);

        Assert.That(counters.GetSnapshot()[0].Count, Is.EqualTo(42UL));
    }

    [Test]
    public async Task RuntimeActivation_SelectsTheNewProjectsCounterStore()
    {
        var station = new Moba.Domain.Project();
        var yard = new Moba.Domain.Project();
        var stores = new Dictionary<Guid, MemoryStore> { [station.Id] = new(), [yard.Id] = new() };
        var z21 = new Mock<IZ21>();
        var settings = Settings(1);
        var counters = new InPortCounterService(z21.Object, settings, projectStoreFactory: id => stores[id]);
        await using var countersScope = counters.ConfigureAwait(false);
        using var runtime = new MobaRuntimeService(z21.Object, Mock.Of<IWorkflowService>(),
            new ActionExecutionContext { Z21 = z21.Object }, settings, Mock.Of<ILogger<MobaRuntimeService>>(), inPortCounterService: counters);
        await runtime.ActivateProjectAsync(station).ConfigureAwait(false);
        counters.Set(1, 42);

        await runtime.ActivateProjectAsync(yard).ConfigureAwait(false);
        Assert.That(runtime.Current.InPortCounters[0].Count, Is.Zero);
        await runtime.ActivateProjectAsync(station).ConfigureAwait(false);
        Assert.That(runtime.Current.InPortCounters[0].Count, Is.EqualTo(42UL));
    }

    [Test]
    public async Task MobileCounterStore_UnreadableProjectDoesNotBlockSelectingAnotherProject()
    {
        var badProject = Guid.NewGuid();
        var broken = new Mock<IInPortCounterStore>();
        broken.Setup(value => value.LoadAsync(default)).ThrowsAsync(new InvalidDataException("Unreadable project counts"));
        var good = new MemoryStore();
        var counters = new InPortCounterService(new Mock<IZ21>().Object, Settings(1),
            projectStoreFactory: id => id == badProject ? broken.Object : good);
        await using var scope = counters.ConfigureAwait(false);
        await counters.SelectProjectAsync(badProject).ConfigureAwait(false);
        Assert.That(counters.PersistenceError, Is.Not.Null);

        await counters.SelectProjectAsync(Guid.NewGuid()).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(counters.PersistenceError, Is.Null);
            Assert.That(counters.GetSnapshot()[0].Count, Is.Zero);
        }
        broken.Verify(value => value.SaveAsync(It.IsAny<IReadOnlyDictionary<uint, ulong>>(), default), Times.Never);
    }

    [Test]
    public async Task RuntimeActivation_CancellationDuringCommittedCounterLoadKeepsProjectAndCountsTogether()
    {
        var station = new Moba.Domain.Project();
        var yard = new Moba.Domain.Project();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loaded = new TaskCompletionSource<IReadOnlyDictionary<uint, ulong>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var yardStore = new Mock<IInPortCounterStore>();
        yardStore.Setup(value => value.LoadAsync(default)).Returns(() => { entered.TrySetResult(); return loaded.Task; });
        var stationStore = new MemoryStore();
        var z21 = new Mock<IZ21>();
        var settings = Settings(1);
        var counters = new InPortCounterService(z21.Object, settings,
            projectStoreFactory: id => id == station.Id ? stationStore : yardStore.Object);
        await using var countersScope = counters.ConfigureAwait(false);
        using var runtime = new MobaRuntimeService(z21.Object, Mock.Of<IWorkflowService>(),
            new ActionExecutionContext { Z21 = z21.Object }, settings, Mock.Of<ILogger<MobaRuntimeService>>(), inPortCounterService: counters);
        await runtime.ActivateProjectAsync(station).ConfigureAwait(false);
        counters.Set(1, 42);
        using var cancellation = new CancellationTokenSource();

        var activation = runtime.ActivateProjectAsync(yard, cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        await cancellation.CancelAsync().ConfigureAwait(false);
        loaded.TrySetResult(new Dictionary<uint, ulong> { [1] = 7 });
        await activation.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(runtime.Current.ProjectId, Is.EqualTo(yard.Id));
            Assert.That(runtime.Current.InPortCounters[0].Count, Is.EqualTo(7UL));
            Assert.That(stationStore.Counts[1], Is.EqualTo(42UL));
        }
    }

    [Test]
    public async Task MobileCounterStore_HandoffDrainsOldWritesBeforeAcceptingNewCounts()
    {
        var station = Guid.NewGuid();
        var writeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseWrite = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var oldStore = new Mock<IInPortCounterStore>();
        oldStore.Setup(value => value.LoadAsync(default))
            .ReturnsAsync(new Dictionary<uint, ulong>());
        oldStore.Setup(value => value.SaveAsync(It.IsAny<IReadOnlyDictionary<uint, ulong>>(), default))
            .Returns((IReadOnlyDictionary<uint, ulong> counts, CancellationToken _) =>
            {
                if (counts[1] != 42) return Task.CompletedTask;
                writeStarted.TrySetResult();
                return releaseWrite.Task;
            });
        var newStore = new MemoryStore();
        var counters = new InPortCounterService(new Mock<IZ21>().Object, Settings(1),
            projectStoreFactory: id => id == station ? oldStore.Object : newStore);
        await using var scope = counters.ConfigureAwait(false);
        await counters.SelectProjectAsync(station).ConfigureAwait(false);
        counters.Set(1, 42);
        await writeStarted.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        var handoff = counters.SelectProjectAsync(Guid.NewGuid());
        try
        {
            Assert.That(handoff.IsCompleted, Is.False);
            Assert.Throws<InvalidOperationException>(() => counters.Set(1, 99));
            Assert.Throws<InvalidOperationException>(counters.ResetAll);
        }
        finally
        {
            releaseWrite.TrySetResult();
        }
        await handoff.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        await counters.FlushAsync().ConfigureAwait(false);
        Assert.That(newStore.Counts[1], Is.Zero);
    }

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
