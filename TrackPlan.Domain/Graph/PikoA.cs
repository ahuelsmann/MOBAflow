// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Graph;

/// <summary>
/// Factory for Piko A track types. Enables fluent syntax like:
/// topology.Add(PikoA.R9).Port("A").ConnectTo(PikoA.WR).Port("B")
/// 
/// Each call creates a NEW TrackEdge instance with the corresponding template ID.
/// </summary>
public static class PikoA
{
    /// <summary>
    /// R1 curve (large radius).
    /// </summary>
    public static ITrackTypeFactory R1 => new TrackTypeFactoryImpl("R1");

    /// <summary>
    /// R2 curve.
    /// </summary>
    public static ITrackTypeFactory R2 => new TrackTypeFactoryImpl("R2");

    /// <summary>
    /// R3 curve (medium radius).
    /// </summary>
    public static ITrackTypeFactory R3 => new TrackTypeFactoryImpl("R3");

    /// <summary>
    /// R9 curve (small radius, commonly used).
    /// </summary>
    public static ITrackTypeFactory R9 => new TrackTypeFactoryImpl("R9");

    /// <summary>
    /// WL switch (left, +15°).
    /// </summary>
    public static ITrackTypeFactory WL => new TrackTypeFactoryImpl("WL");

    /// <summary>
    /// WR switch (right, -15°).
    /// </summary>
    public static ITrackTypeFactory WR => new TrackTypeFactoryImpl("WR");

    /// <summary>
    /// BWL curved switch (left).
    /// </summary>
    public static ITrackTypeFactory BWL => new TrackTypeFactoryImpl("BWL");

    /// <summary>
    /// BWR curved switch (right).
    /// </summary>
    public static ITrackTypeFactory BWR => new TrackTypeFactoryImpl("BWR");

    /// <summary>
    /// G231 straight track (standard length).
    /// </summary>
    public static ITrackTypeFactory G231 => new TrackTypeFactoryImpl("G231");

    /// <summary>
    /// G119 straight track (short).
    /// </summary>
    public static ITrackTypeFactory G119 => new TrackTypeFactoryImpl("G119");

    /// <summary>
    /// Internal implementation of ITrackTypeFactory.
    /// </summary>
    private sealed record TrackTypeFactoryImpl(string TemplateId) : ITrackTypeFactory
    {
        public TrackEdge CreateEdge() => new(Guid.NewGuid(), TemplateId);
    }
}
