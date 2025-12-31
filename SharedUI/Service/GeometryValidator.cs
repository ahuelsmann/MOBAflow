// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using Domain.TrackPlan;
using Renderer;
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

    /// <summary>
    /// Validate all geometry definitions in the library.
    /// Checks: Connector count, heading count, PathData existence.
    /// </summary>
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

            // Validate connector count matches heading count
            if (geometry.Endpoints.Count != geometry.EndpointHeadingsDeg.Count)
            {
                errors.Add($"❌ {articleCode}: Endpoint count ({geometry.Endpoints.Count}) != Heading count ({geometry.EndpointHeadingsDeg.Count})");
            }

            // Validate PathData exists
            if (string.IsNullOrWhiteSpace(geometry.PathData))
            {
                errors.Add($"❌ {articleCode}: Missing PathData");
            }

            // Validate headings are in [0, 360) range
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

    /// <summary>
    /// Validate connector index mapping for imported segments.
    /// Checks if connector indices are within valid range for the geometry.
    /// </summary>
    public List<string> ValidateConnections(TrackLayout layout)
    {
        var errors = new List<string>();

        foreach (var conn in layout.Connections)
        {
            var seg1 = layout.Segments.FirstOrDefault(s => s.Id == conn.Segment1Id);
            var seg2 = layout.Segments.FirstOrDefault(s => s.Id == conn.Segment2Id);

            if (seg1 == null)
            {
                errors.Add($"❌ Connection references missing Segment1Id: {conn.Segment1Id}");
                continue;
            }

            if (seg2 == null)
            {
                errors.Add($"❌ Connection references missing Segment2Id: {conn.Segment2Id}");
                continue;
            }

            var geom1 = _geometryLibrary.GetGeometry(seg1.ArticleCode);
            var geom2 = _geometryLibrary.GetGeometry(seg2.ArticleCode);

            if (geom1 == null)
            {
                errors.Add($"❌ Missing geometry for {seg1.ArticleCode} (Segment {seg1.Id})");
                continue;
            }

            if (geom2 == null)
            {
                errors.Add($"❌ Missing geometry for {seg2.ArticleCode} (Segment {seg2.Id})");
                continue;
            }

            // Validate connector indices are within range
            if (conn.Segment1ConnectorIndex < 0 || conn.Segment1ConnectorIndex >= geom1.Endpoints.Count)
            {
                errors.Add($"❌ Invalid connector index: {seg1.Id} ({seg1.ArticleCode})[{conn.Segment1ConnectorIndex}] - " +
                    $"geometry only has {geom1.Endpoints.Count} connectors");
            }

            if (conn.Segment2ConnectorIndex < 0 || conn.Segment2ConnectorIndex >= geom2.Endpoints.Count)
            {
                errors.Add($"❌ Invalid connector index: {seg2.Id} ({seg2.ArticleCode})[{conn.Segment2ConnectorIndex}] - " +
                    $"geometry only has {geom2.Endpoints.Count} connectors");
            }
        }

        return errors;
    }

    /// <summary>
    /// Validate WorldTransform results after rendering.
    /// Detects common issues: segments at origin (0,0), extreme coordinates, NaN values.
    /// </summary>
    public List<string> ValidateRendering(TrackLayout layout)
    {
        var warnings = new List<string>();
        int segmentsAtOrigin = 0;
        int segmentsWithExtremeCoords = 0;

        foreach (var segment in layout.Segments)
        {
            var t = segment.WorldTransform;

            // Check for NaN
            if (double.IsNaN(t.TranslateX) || double.IsNaN(t.TranslateY) || double.IsNaN(t.RotationDegrees))
            {
                warnings.Add($"❌ {segment.Id} ({segment.ArticleCode}): WorldTransform contains NaN values");
                continue;
            }

            // Check if multiple segments are at origin (suspicious)
            if (Math.Abs(t.TranslateX) < 0.1 && Math.Abs(t.TranslateY) < 0.1 && t.RotationDegrees < 0.1)
            {
                segmentsAtOrigin++;
            }

            // Check for extreme coordinates (> 10m from origin - likely drift)
            if (Math.Abs(t.TranslateX) > 10000 || Math.Abs(t.TranslateY) > 10000)
            {
                segmentsWithExtremeCoords++;
                warnings.Add($"⚠️ {segment.Id} ({segment.ArticleCode}): Extreme coordinates ({t.TranslateX:F0}, {t.TranslateY:F0}) - possible drift");
            }
        }

        if (segmentsAtOrigin > 1)
        {
            warnings.Add($"⚠️ {segmentsAtOrigin} segments at origin (0,0) - likely traversal issue or disconnected components");
        }

        if (segmentsWithExtremeCoords > 0)
        {
            warnings.Add($"⚠️ {segmentsWithExtremeCoords} segments have extreme coordinates - likely heading/constraint error");
        }

        return warnings;
    }

    /// <summary>
    /// Run all validations and log results.
    /// </summary>
    public void RunFullValidation(TrackLayout layout)
    {
        Debug.WriteLine("🔍 === GeometryValidator: Full Validation ===");

        var libraryErrors = ValidateLibrary();
        if (libraryErrors.Count > 0)
        {
            Debug.WriteLine($"📚 Library validation: {libraryErrors.Count} errors");
            foreach (var error in libraryErrors)
            {
                Debug.WriteLine($"   {error}");
            }
        }
        else
        {
            Debug.WriteLine("✅ Library validation: PASSED");
        }

        var connectionErrors = ValidateConnections(layout);
        if (connectionErrors.Count > 0)
        {
            Debug.WriteLine($"🔗 Connection validation: {connectionErrors.Count} errors");
            foreach (var error in connectionErrors)
            {
                Debug.WriteLine($"   {error}");
            }
        }
        else
        {
            Debug.WriteLine("✅ Connection validation: PASSED");
        }

        var renderingWarnings = ValidateRendering(layout);
        if (renderingWarnings.Count > 0)
        {
            Debug.WriteLine($"🎨 Rendering validation: {renderingWarnings.Count} warnings");
            foreach (var warning in renderingWarnings)
            {
                Debug.WriteLine($"   {warning}");
            }
        }
        else
        {
            Debug.WriteLine("✅ Rendering validation: PASSED");
        }

        Debug.WriteLine("🔍 === GeometryValidator: Validation Complete ===");
    }
}
