// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Benchmarks;

using BenchmarkDotNet.Attributes;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Common.Events;

/// <summary>Publish cost of the shared event bus for a feedback event with several subscribers.</summary>
[MemoryDiagnoser]
public class EventBusBenchmarks
{
    private EventBus _bus = null!;
    private int _received;

    [Params(1, 10)]
    public int Subscribers { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _bus = new EventBus(NullLogger<EventBus>.Instance);
        for (var index = 0; index < Subscribers; index++)
        {
            _bus.Subscribe<FeedbackReceivedEvent>(_ => _received++);
        }
    }

    [Benchmark]
    public int PublishFeedback()
    {
        _bus.Publish(new FeedbackReceivedEvent(5));
        return _received;
    }
}
