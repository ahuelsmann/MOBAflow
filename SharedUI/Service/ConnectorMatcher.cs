// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using Domain.TrackPlan;
using Renderer;

/// <summary>
/// Matches track connectors during import based on position and heading tolerance.
/// Track-Graph Architecture: Converts temporary coordinates → connector-based connections.
/// </summary>
public class ConnectorMatcher
{
    private const double PositionToleranceMm = 1.0; // 1mm tolerance for position matching
    private const double HeadingToleranceDeg = 5.0; // 5° tolerance for heading matching

    /// <summary>
    /// Match connectors using AnyRail endpoint world coordinates.
    /// Each segment provides a list of endpoint coordinates from AnyRail XML.
    /// </summary>
    /// <param name="segments">List of segments with their endpoint world coordinates from AnyRail</param>
    /// <param name="geometryLibrary">Geometry library for connector count validation</param>
    /// <returns>List of connections between matched connectors</returns>
    public List<TrackConnection> MatchConnectorsFromEndpoints(
        List<(string SegmentId, string ArticleCode, List<(double X, double Y, double Heading)> EndpointCoords)> segments,
        TrackGeometryLibrary geometryLibrary)
    {
        var connections = new List<TrackConnection>();
        var matched = new HashSet<(string, int)>(); // Track which connectors are already matched
        
        System.Diagnostics.Debug.WriteLine($"🔍 ConnectorMatcher: Matching {segments.Count} segments using AnyRail endpoint coordinates...");

        for (int i = 0; i < segments.Count; i++)
        {
            var seg1 = segments[i];
            var geom1 = geometryLibrary.GetGeometry(seg1.ArticleCode);
            if (geom1 == null)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ ConnectorMatcher: No geometry for {seg1.ArticleCode} (segment {seg1.SegmentId})");
                continue;
            }
            
            // Verify endpoint count matches geometry definition
            if (seg1.EndpointCoords.Count != geom1.Endpoints.Count)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ ConnectorMatcher: Endpoint count mismatch for {seg1.SegmentId} ({seg1.ArticleCode}): AnyRail={seg1.EndpointCoords.Count}, Library={geom1.Endpoints.Count}");
                continue;
            }
            
            System.Diagnostics.Debug.WriteLine($"🔍 Segment {seg1.SegmentId} ({seg1.ArticleCode}): {seg1.EndpointCoords.Count} connectors");

            for (int conn1Idx = 0; conn1Idx < seg1.EndpointCoords.Count; conn1Idx++)
            {
                // Skip if already matched
                if (matched.Contains((seg1.SegmentId, conn1Idx)))
                    continue;

                var ep1 = seg1.EndpointCoords[conn1Idx];
                System.Diagnostics.Debug.WriteLine($"  🔍 Connector [{conn1Idx}] at ({ep1.X:F1}, {ep1.Y:F1}, {ep1.Heading:F1}°)");

                // Search for matching connector on other segments
                bool foundMatch = false;
                for (int j = i + 1; j < segments.Count; j++)
                {
                    var seg2 = segments[j];
                    var geom2 = geometryLibrary.GetGeometry(seg2.ArticleCode);
                    if (geom2 == null) continue;

                    if (seg2.EndpointCoords.Count != geom2.Endpoints.Count)
                        continue;

                    for (int conn2Idx = 0; conn2Idx < seg2.EndpointCoords.Count; conn2Idx++)
                    {
                        // Skip if already matched
                        if (matched.Contains((seg2.SegmentId, conn2Idx)))
                            continue;

                        var ep2 = seg2.EndpointCoords[conn2Idx];

                        // Check if connectors match (position + heading)
                        if (IsMatch(ep1.X, ep1.Y, ep1.Heading, ep2.X, ep2.Y, ep2.Heading))
                        {
                            // Found a match!
                            var distance = Math.Sqrt((ep2.X - ep1.X) * (ep2.X - ep1.X) + (ep2.Y - ep1.Y) * (ep2.Y - ep1.Y));
                            var headingDiff = Math.Abs(NormalizeAngle(ep2.Heading - ep1.Heading - 180));
                            if (headingDiff > 180) headingDiff = 360 - headingDiff;
                            
                            System.Diagnostics.Debug.WriteLine($"    ✅ MATCH: {seg1.SegmentId}[{conn1Idx}] ↔ {seg2.SegmentId}[{conn2Idx}] (dist={distance:F2}mm, heading={headingDiff:F1}°)");
                            
                            connections.Add(new TrackConnection
                            {
                                Segment1Id = seg1.SegmentId,
                                Segment1ConnectorIndex = conn1Idx,
                                Segment2Id = seg2.SegmentId,
                                Segment2ConnectorIndex = conn2Idx,
                                ConstraintType = DetermineConstraintType(geom1, conn1Idx, geom2, conn2Idx)
                            });

                            matched.Add((seg1.SegmentId, conn1Idx));
                            matched.Add((seg2.SegmentId, conn2Idx));
                            foundMatch = true;
                            break; // Move to next connector on seg1
                        }
                    }
                    if (foundMatch) break;
                }
                
                if (!foundMatch)
                {
                    System.Diagnostics.Debug.WriteLine($"    ⚠️ NO MATCH for {seg1.SegmentId}[{conn1Idx}] at ({ep1.X:F1}, {ep1.Y:F1}, {ep1.Heading:F1}°)");
                }
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"🔍 ConnectorMatcher: Found {connections.Count} connections, {matched.Count} connectors matched");

        return connections;
    }

    /// <summary>
    /// Match connectors between segments based on position and heading.
    /// Used during AnyRail import to convert coordinates → topology.
    /// </summary>
    /// <param name="segments">List of segments with temporary world positions</param>
    /// <param name="geometryLibrary">Geometry library for connector definitions</param>
    /// <returns>List of connections between matched connectors</returns>
    public List<TrackConnection> MatchConnectors(
        List<(string SegmentId, string ArticleCode, double X, double Y, double Rotation)> segments,
        TrackGeometryLibrary geometryLibrary)
    {
        var connections = new List<TrackConnection>();
        var matched = new HashSet<(string, int)>(); // Track which connectors are already matched
        
        System.Diagnostics.Debug.WriteLine($"🔍 ConnectorMatcher: Matching {segments.Count} segments...");

        for (int i = 0; i < segments.Count; i++)
        {
            var seg1 = segments[i];
            var geom1 = geometryLibrary.GetGeometry(seg1.ArticleCode);
            if (geom1 == null)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ ConnectorMatcher: No geometry for {seg1.ArticleCode} (segment {seg1.SegmentId})");
                continue;
            }
            
            System.Diagnostics.Debug.WriteLine($"🔍 Segment {seg1.SegmentId} ({seg1.ArticleCode}): {geom1.Endpoints.Count} connectors at ({seg1.X:F1}, {seg1.Y:F1}, {seg1.Rotation:F1}°)");

            for (int conn1Idx = 0; conn1Idx < geom1.Endpoints.Count; conn1Idx++)
            {
                // Skip if already matched
                if (matched.Contains((seg1.SegmentId, conn1Idx)))
                    continue;

                // Calculate world position of this connector
                var (worldX1, worldY1, worldHeading1) = CalculateConnectorWorldTransform(
                    seg1, geom1, conn1Idx);
                
                System.Diagnostics.Debug.WriteLine($"  🔍 Connector [{conn1Idx}] at ({worldX1:F1}, {worldY1:F1}, {worldHeading1:F1}°)");

                // Search for matching connector on other segments
                bool foundMatch = false;
                for (int j = i + 1; j < segments.Count; j++)
                {
                    var seg2 = segments[j];
                    var geom2 = geometryLibrary.GetGeometry(seg2.ArticleCode);
                    if (geom2 == null) continue;

                    for (int conn2Idx = 0; conn2Idx < geom2.Endpoints.Count; conn2Idx++)
                    {
                        // Skip if already matched
                        if (matched.Contains((seg2.SegmentId, conn2Idx)))
                            continue;

                        // Calculate world position of candidate connector
                        var (worldX2, worldY2, worldHeading2) = CalculateConnectorWorldTransform(
                            seg2, geom2, conn2Idx);

                        // Check if connectors match
                        if (IsMatch(worldX1, worldY1, worldHeading1, worldX2, worldY2, worldHeading2))
                        {
                            // Found a match!
                            var distance = Math.Sqrt((worldX2 - worldX1) * (worldX2 - worldX1) + (worldY2 - worldY1) * (worldY2 - worldY1));
                            var headingDiff = Math.Abs(NormalizeAngle(worldHeading2 - worldHeading1 - 180));
                            if (headingDiff > 180) headingDiff = 360 - headingDiff;
                            
                            System.Diagnostics.Debug.WriteLine($"    ✅ MATCH: {seg1.SegmentId}[{conn1Idx}] ↔ {seg2.SegmentId}[{conn2Idx}] (dist={distance:F2}mm, heading={headingDiff:F1}°)");
                            
                            connections.Add(new TrackConnection
                            {
                                Segment1Id = seg1.SegmentId,
                                Segment1ConnectorIndex = conn1Idx,
                                Segment2Id = seg2.SegmentId,
                                Segment2ConnectorIndex = conn2Idx,
                                ConstraintType = DetermineConstraintType(geom1, conn1Idx, geom2, conn2Idx)
                            });

                            matched.Add((seg1.SegmentId, conn1Idx));
                            matched.Add((seg2.SegmentId, conn2Idx));
                            foundMatch = true;
                            break; // Move to next connector on seg1
                        }
                    }
                    if (foundMatch) break;
                }
                
                if (!foundMatch)
                {
                    System.Diagnostics.Debug.WriteLine($"    ⚠️ NO MATCH for {seg1.SegmentId}[{conn1Idx}] at ({worldX1:F1}, {worldY1:F1}, {worldHeading1:F1}°)");
                }
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"🔍 ConnectorMatcher: Found {connections.Count} connections, {matched.Count} connectors matched");

        return connections;
    }

    /// <summary>
    /// Calculate world transform of a connector.
    /// </summary>
    private (double X, double Y, double Heading) CalculateConnectorWorldTransform(
        (string SegmentId, string ArticleCode, double X, double Y, double Rotation) segment,
        TrackGeometry geometry,
        int connectorIndex)
    {
        var localConnector = geometry.Endpoints[connectorIndex];
        var localHeading = geometry.EndpointHeadingsDeg[connectorIndex];

        var (worldX, worldY) = RotateAndTranslate(
            localConnector.X, localConnector.Y,
            segment.Rotation, segment.X, segment.Y);

        var worldHeading = segment.Rotation + localHeading;

        return (worldX, worldY, NormalizeAngle(worldHeading));
    }

    /// <summary>
    /// Check if two connectors match (position + heading within tolerance).
    /// Connectors must be close in position and have opposite headings (±180°).
    /// </summary>
    private bool IsMatch(
        double x1, double y1, double heading1,
        double x2, double y2, double heading2)
    {
        // Position tolerance
        var distance = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
        if (distance > PositionToleranceMm)
            return false;

        // Heading tolerance (must be opposite: ±180°)
        var headingDiff = Math.Abs(NormalizeAngle(heading2 - heading1 - 180));
        if (headingDiff > 180)
            headingDiff = 360 - headingDiff;

        return headingDiff < HeadingToleranceDeg;
    }

    /// <summary>
    /// Determine constraint type based on connector types.
    /// </summary>
    private ConstraintType DetermineConstraintType(
        TrackGeometry geom1, int conn1Idx,
        TrackGeometry geom2, int conn2Idx)
    {
        // For now: All connections are Rigid
        // Future: Check connector types and determine constraint type
        return ConstraintType.Rigid;
    }

    /// <summary>
    /// Rotate and translate a point.
    /// </summary>
    private static (double X, double Y) RotateAndTranslate(
        double x, double y, double rotationDeg, double tx, double ty)
    {
        var rad = rotationDeg * Math.PI / 180.0;
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);

        var rotatedX = x * cos - y * sin;
        var rotatedY = x * sin + y * cos;

        return (rotatedX + tx, rotatedY + ty);
    }

    /// <summary>
    /// Normalize angle to [0, 360) range.
    /// </summary>
    private static double NormalizeAngle(double angleDeg)
    {
        var result = angleDeg % 360;
        if (result < 0)
            result += 360;
        return result;
    }
}
