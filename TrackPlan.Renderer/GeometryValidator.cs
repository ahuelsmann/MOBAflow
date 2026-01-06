// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Renderer;

using Domain;

using System.Diagnostics;

/// <summary>
/// Validates TrackGeometryLibrary definitions and import connector mappings.
/// Detects common errors: wrong EndpointHeadingsDeg, incorrect connector counts, invalid mappings.
/// </summary>
public class GeometryValidator
{
    private readonly TrackGeometryLibrary _geometryLibrary;

    public GeometryValidator(TrackGeometryLibrary geometryLibrary)
    {
        _geometryLibrary = geometryLibrary;
    }

    public List<string> ValidateLibrary()
    {
        var errors = new List<string>();

        var allArticleCodes = new[]
        {
            "G231", "G119", "G62", "G107", "G115", "G239", "G940",
            "R1", "R2", "R3", "R4", "R9",
            "WL", "WR", "BWL", "BWR", "BWL-R3", "BWR-R3", "WY",
            "W3", "DKW", "K15", "K30"
        };

        foreach (var articleCode in allArticleCodes)
        {
            var geometry = _geometryLibrary.GetGeometry(articleCode);
            if (geometry == null)
            {
                errors.Add($"❌ Missing geometry: {articleCode}");
                continue;
            }

            if (geometry.Endpoints.Count != geometry.EndpointHeadingsDeg.Count)
            {
                errors.Add($"❌ {articleCode}: Endpoint count ({geometry.Endpoints.Count}) != Heading count ({geometry.EndpointHeadingsDeg.Count})");
            }

            if (string.IsNullOrWhiteSpace(geometry.PathData))
            {
                errors.Add($"❌ {articleCode}: Missing PathData");
            }

            for (int i = 0; i < geometry.EndpointHeadingsDeg.Count; i++)
            {
                var heading = geometry.EndpointHeadingsDeg[i];
                if (heading < 0 || heading >= 360)
                {
                    errors.Add($"❌ {articleCode}: Invalid heading at connector {i}: {heading}° (must be [0, 360))");
                }
            }
        }

        return errors;
    }

    public List<string> ValidateConnections(TrackLayout layout)
    {
        var errors = new List<string>();

        foreach (var conn in layout.Connections)
        {
            var seg1 = layout.Segments.FirstOrDefault(s => s.Id == conn.Segment1Id);
            var seg2 = layout.Segments.FirstOrDefault(s => s.Id == conn.Segment2Id);

            if (seg1 == null || seg2 == null)
            {
                errors.Add($"❌ Connection references missing segment(s): {conn.Segment1Id} ↔ {conn.Segment2Id}");
                continue;
            }

            var geom1 = _geometryLibrary.GetGeometry(seg1.ArticleCode);
            var geom2 = _geometryLibrary.GetGeometry(seg2.ArticleCode);

            if (geom1 == null)
            {
                errors.Add($"❌ Missing geometry for segment {seg1.Id} ({seg1.ArticleCode})");
                continue;
            }

            if (geom2 == null)
            {
                errors.Add($"❌ Missing geometry for segment {seg2.Id} ({seg2.ArticleCode})");
                continue;
            }

            if (conn.Segment1ConnectorIndex >= geom1.Endpoints.Count)
            {
                errors.Add($"❌ {seg1.Id} ({seg1.ArticleCode}): ConnectorIndex {conn.Segment1ConnectorIndex} out of range (connectors={geom1.Endpoints.Count})");
            }

            if (conn.Segment2ConnectorIndex >= geom2.Endpoints.Count)
            {
                errors.Add($"❌ {seg2.Id} ({seg2.ArticleCode}): ConnectorIndex {conn.Segment2ConnectorIndex} out of range (connectors={geom2.Endpoints.Count})");
            }
        }

        return errors;
    }

    public bool RunFullValidation(TrackLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var errors = new List<string>();
        errors.AddRange(ValidateLibrary());
        errors.AddRange(ValidateConnections(layout));

        foreach (var error in errors)
        {
            Debug.WriteLine(error);
        }

        return errors.Count == 0;
    }
}
