// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Benchmarks;

using BenchmarkDotNet.Attributes;

using Moba.Contracts.Runtime;

/// <summary>
/// JSON round trip of the runtime snapshot that MOBApi and SignalR send to clients, for a small and a large layout.
/// </summary>
[MemoryDiagnoser]
public class RuntimeSnapshotSerializationBenchmarks
{
    private MobaRuntimeSnapshot _snapshot = new();
    private string _json = string.Empty;

    /// <summary>Number of locomotives, journeys and signal-box elements each.</summary>
    [Params(10, 100)]
    public int Items { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _snapshot = new MobaRuntimeSnapshot
        {
            ProjectId = Guid.NewGuid(),
            IsConnected = true,
            IsTrackPowerOn = true,
            StatusText = "Connected",
            LocomotiveStates = Enumerable.Range(1, Items).ToDictionary(
                address => address,
                address => new LocomotiveRuntimeSnapshot { Address = address, Speed = address % 126, IsForward = true }),
            JourneyStates = Enumerable.Range(1, Items).Select(_ => Guid.NewGuid()).ToDictionary(
                id => id,
                id => new JourneyRuntimeSnapshot { JourneyId = id, CurrentStationName = "Station", CurrentPos = 1, IsActive = true }),
            SignalBoxElements = [.. Enumerable.Range(1, Items).Select(index =>
                new SignalBoxElementRuntimeSnapshot { ElementId = Guid.NewGuid(), Name = $"A{index}", X = index, Y = index })]
        };
        _json = RuntimeJsonSerializer.Serialize(_snapshot);
    }

    [Benchmark]
    public string Serialize() => RuntimeJsonSerializer.Serialize(_snapshot);

    [Benchmark]
    public MobaRuntimeSnapshot? Deserialize() => RuntimeJsonSerializer.Deserialize(_json);
}
