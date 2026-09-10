// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Runtime;

/// <summary>Immutable application-lifetime statistics for one feedback input.</summary>
/// <param name="InPort">One-based feedback input.</param>
/// <param name="Count">Number of accepted activations since the last explicit reset.</param>
/// <param name="LastFeedbackTime">Time of the last accepted activation.</param>
/// <param name="LastLapTime">Time between the two most recent accepted activations.</param>
public sealed record InPortCounterSnapshot(
    uint InPort,
    ulong Count,
    DateTimeOffset? LastFeedbackTime,
    TimeSpan? LastLapTime);
