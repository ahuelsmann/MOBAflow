// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Graph;

public enum EndcapKind
{
    Default,
    Isolated,
    Prellbock
}

public sealed class Endcap
{
    public required Guid Id { get; init; }
    public required EndcapKind Kind { get; init; }
    public required Endpoint AttachedTo { get; init; }
}