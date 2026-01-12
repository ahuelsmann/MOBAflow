// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Graph;

public sealed class TrackNode
{
    public required Guid Id { get; init; }
    public List<TrackPort> Ports { get; init; } = [];
}