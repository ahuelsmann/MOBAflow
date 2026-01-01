// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Domain;

/// <summary>
/// Geometry description for a track article (endpoints, headings, SVG path data).
/// </summary>
public class TrackGeometry
{
    public string ArticleCode { get; set; } = string.Empty;
    public List<TrackPoint> Endpoints { get; set; } = new();
    public List<double> EndpointHeadingsDeg { get; set; } = new();
    public string PathData { get; set; } = string.Empty;
}
