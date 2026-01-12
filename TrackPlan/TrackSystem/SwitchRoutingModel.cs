// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.TrackSystem;

public sealed class SwitchRoutingModel
{
    public required string InEndId { get; init; }
    public required string StraightEndId { get; init; }
    public required string DivergingEndId { get; init; }

    public string GetActiveOutEnd(bool straight)
        => straight ? StraightEndId : DivergingEndId;
}