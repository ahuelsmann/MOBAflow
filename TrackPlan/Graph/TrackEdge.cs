// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Graph;

public sealed class TrackEdge
{
    public required Guid Id { get; init; }
    public required string TemplateId { get; init; }
    public Dictionary<string, Endpoint> Connections { get; init; } = [];

    public int? FeedbackPointNumber { get; set; }
}