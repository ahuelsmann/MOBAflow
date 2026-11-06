// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.MOBApi;

using Moba.Contracts.Runtime;
using Moba.MOBApi.Service;

using Moq;

using System.Collections.Concurrent;

/// <summary>
/// The remote command queue under concurrent producers (RF-18): the capacity is the contract, every rejection is
/// reported and counted, and each producer's accepted commands keep their order.
/// </summary>
[TestFixture]
internal sealed class RuntimeCommandLoadTests
{
    private const int Producers = 8;
    private const int CommandsPerProducer = 64;
    private static readonly Guid Project = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Test]
    public async Task ConcurrentProducers_FillTheQueueExactly_AndEveryOtherCommandIsRejectedAsQueueFull()
    {
        var queue = new RuntimeCommandQueue(RuntimeCommandQueue.DefaultCapacity);
        var admission = TestCommandAdmission.Create(queue, KnownProject());

        var results = await ProduceConcurrentlyAsync(admission).ConfigureAwait(false);
        var drained = Drain(queue);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(results.Count(result => result.Accepted), Is.EqualTo(RuntimeCommandQueue.DefaultCapacity));
            Assert.That(
                results.Where(result => !result.Accepted).Select(result => result.Status),
                Has.All.EqualTo(RuntimeCommandAdmissionStatus.QueueFull));
            Assert.That(drained, Has.Count.EqualTo(RuntimeCommandQueue.DefaultCapacity));
            Assert.That(drained.Select(Key), Is.EquivalentTo(results.Where(result => result.Accepted).Select(result => result.Key)));
            Assert.That(OrderPerProducer(drained), Has.All.Ordered);
        }
    }

    [Test]
    public async Task ConcurrentProducersAndAConsumer_LoseNoAcceptedCommand_AndKeepEachProducersOrder()
    {
        var queue = new RuntimeCommandQueue(capacity: 16);
        var admission = TestCommandAdmission.Create(queue, KnownProject());
        var drained = new List<RuntimeCommandEnvelope>();
        using var stop = new CancellationTokenSource();
        var consumer = Task.Run(async () =>
        {
            while (!stop.IsCancellationRequested)
            {
                if (queue.TryDequeue(out var command))
                    drained.Add(command!);
                else
                    await Task.Yield();
            }
        });

        var results = await ProduceConcurrentlyAsync(admission).ConfigureAwait(false);
        await stop.CancelAsync().ConfigureAwait(false);
        await consumer.ConfigureAwait(false);
        drained.AddRange(Drain(queue));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(results, Has.Count.EqualTo(Producers * CommandsPerProducer));
            Assert.That(drained.Select(Key), Is.EquivalentTo(results.Where(result => result.Accepted).Select(result => result.Key)));
            Assert.That(
                results.Where(result => !result.Accepted).Select(result => result.Status),
                Has.All.EqualTo(RuntimeCommandAdmissionStatus.QueueFull));
            Assert.That(OrderPerProducer(drained), Has.All.Ordered);
        }
    }

    private static async Task<List<(bool Accepted, RuntimeCommandAdmissionStatus Status, (int Producer, int Sequence) Key)>> ProduceConcurrentlyAsync(
        RuntimeCommandAdmission admission)
    {
        var results = new ConcurrentBag<(bool, RuntimeCommandAdmissionStatus, (int, int))>();
        using var start = new ManualResetEventSlim();
        var producers = Enumerable.Range(1, Producers).Select(producer => Task.Run(() =>
        {
            start.Wait();
            for (var sequence = 0; sequence < CommandsPerProducer; sequence++)
            {
                var result = admission.Enqueue(Command(producer, sequence));
                results.Add((result.IsAccepted, result.Status, (producer, sequence)));
            }
        })).ToArray();
        start.Set();
        await Task.WhenAll(producers).ConfigureAwait(false);
        return [.. results];
    }

    // The producer is the locomotive address and the sequence is the speed, so each command identifies itself.
    private static RuntimeCommandEnvelope Command(int producer, int sequence) => new()
    {
        ProjectId = Project,
        Type = RuntimeCommandType.SetLocomotiveDrive,
        LocomotiveAddress = producer,
        Speed = sequence,
        Forward = true
    };

    private static (int Producer, int Sequence) Key(RuntimeCommandEnvelope command) =>
        (command.LocomotiveAddress!.Value, command.Speed!.Value);

    private static IEnumerable<int[]> OrderPerProducer(IEnumerable<RuntimeCommandEnvelope> commands) =>
        commands.GroupBy(command => command.LocomotiveAddress!.Value)
            .Select(group => group.Select(command => command.Speed!.Value).ToArray());

    private static List<RuntimeCommandEnvelope> Drain(RuntimeCommandQueue queue)
    {
        var drained = new List<RuntimeCommandEnvelope>();
        while (queue.TryDequeue(out var command))
            drained.Add(command!);
        return drained;
    }

    private static ISolutionCache KnownProject()
    {
        var solutionCache = new Mock<ISolutionCache>();
        solutionCache.Setup(cache => cache.ContainsProject(Project)).Returns(true);
        return solutionCache.Object;
    }
}
