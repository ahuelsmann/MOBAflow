// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Benchmarks;

using BenchmarkDotNet.Attributes;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Service;
using Moba.Common.Events;

/// <summary>
/// Throughput of the ordered Z21 event pipeline: enqueue a burst of feedback events and drain them to the event bus.
/// Each operation pushes one burst through a freshly started pipeline, so the result includes pipeline start-up and
/// shutdown; it is a relative baseline on the same machine, not the pure enqueue and drain cost.
/// </summary>
[MemoryDiagnoser]
public class Z21EventPipelineBenchmarks
{
    private EventBus _bus = new(NullLogger<EventBus>.Instance);

    [Params(100, 1000)]
    public int Events { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _bus = new EventBus(NullLogger<EventBus>.Instance);
        _bus.Subscribe<FeedbackReceivedEvent>(_ => { });
    }

    [Benchmark]
    public async Task<long> EnqueueAndDrain()
    {
        await using var pipeline = new Z21EventPipeline(_bus, NullLogger.Instance);
        for (var inPort = 1; inPort <= Events; inPort++)
        {
            pipeline.TryEnqueue(new FeedbackReceivedEvent(inPort));
        }

        await pipeline.StopAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        return pipeline.GetSnapshot().PublishedEvents;
    }
}
