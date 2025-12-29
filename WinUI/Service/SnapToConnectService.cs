// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using SharedUI.ViewModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Windows.Foundation;

/// <summary>
/// Service for snap-to-connect logic in track plan editor (AnyRail-style).
/// Provides magnetic snapping for connecting track segments at their endpoints.
/// Endpoints are extracted from PathData (absolute coordinates).
/// </summary>
public class SnapToConnectService
{
    #region Fields
    private const double SnapDistance = 40; // Pixels - magnetic snap threshold
    #endregion

    /// <summary>
    /// Represents a connection point on a track segment.
    /// </summary>
    public record TrackEndpoint(Point Position, double Angle, TrackSegmentViewModel Segment, bool IsStart);

    /// <summary>
    /// Get all endpoints for a track segment by parsing PathData.
    /// Extracts start point (M command) and end point (last coordinate).
    /// </summary>
    public List<TrackEndpoint> GetEndpoints(TrackSegmentViewModel segment)
    {
        var endpoints = new List<TrackEndpoint>();
        var pathData = segment.PathData;

        if (string.IsNullOrEmpty(pathData))
            return endpoints;

        // Parse PathData to extract coordinates
        var coords = ExtractCoordinates(pathData);

        if (coords.Count >= 2)
        {
            var startPoint = coords[0];
            var endPoint = coords[^1]; // Last coordinate

            // Calculate angle from start to end
            var angle = Math.Atan2(endPoint.Y - startPoint.Y, endPoint.X - startPoint.X) * 180 / Math.PI;

            // Start point: incoming connection (angle points INTO the track)
            endpoints.Add(new TrackEndpoint(startPoint, angle + 180, segment, true));

            // End point: outgoing connection (angle points OUT of the track)
            endpoints.Add(new TrackEndpoint(endPoint, angle, segment, false));

            Debug.WriteLine($"📍 Endpoints for {segment.ArticleCode}: Start=({startPoint.X:F0},{startPoint.Y:F0}), End=({endPoint.X:F0},{endPoint.Y:F0})");
        }

        return endpoints;
    }

    /// <summary>
    /// Extract all coordinate pairs from PathData.
    /// Matches patterns like "123.45,678.90"
    /// </summary>
    private static List<Point> ExtractCoordinates(string pathData)
    {
        var points = new List<Point>();
        var ic = CultureInfo.InvariantCulture;

        // Match coordinate pairs like "123.45,678.90" or "-10.5,20.3"
        var regex = new Regex(@"(-?\d+\.?\d*),(-?\d+\.?\d*)");
        var matches = regex.Matches(pathData);

        foreach (Match match in matches)
        {
            if (double.TryParse(match.Groups[1].Value, NumberStyles.Float, ic, out var x) &&
                double.TryParse(match.Groups[2].Value, NumberStyles.Float, ic, out var y))
            {
                points.Add(new Point(x, y));
            }
        }

        return points;
    }

    /// <summary>
    /// Find the nearest snap target endpoint from all placed segments.
    /// Checks BOTH start and end points of each segment.
    /// Returns the position to snap to and the required rotation.
    /// </summary>
    public (Point SnapPosition, double SnapRotation)? FindSnapEndpoint(
        Point currentPosition,
        double currentRotation,
        IEnumerable<TrackSegmentViewModel> segments)
    {
        double minDistance = SnapDistance;
        TrackEndpoint? bestTarget = null;

        foreach (var segment in segments)
        {
            foreach (var endpoint in GetEndpoints(segment))
            {
                var distance = Math.Sqrt(
                    Math.Pow(endpoint.Position.X - currentPosition.X, 2) +
                    Math.Pow(endpoint.Position.Y - currentPosition.Y, 2)
                );

                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestTarget = endpoint;
                }
            }
        }

        if (bestTarget != null)
        {
            Debug.WriteLine($"🎯 Snap target found: ({bestTarget.Position.X:F0},{bestTarget.Position.Y:F0}) distance={minDistance:F0}px");
            return (bestTarget.Position, bestTarget.Angle);
        }

        return null;
    }

    /// <summary>
    /// Find the nearest snap target within SnapDistance from current position.
    /// Simplified version that returns just the position.
    /// </summary>
    public Point? FindSnapTarget(Point currentPosition, IEnumerable<TrackSegmentViewModel> segments)
    {
        var result = FindSnapEndpoint(currentPosition, 0, segments);
        return result?.SnapPosition;
    }

    /// <summary>
    /// Generate SVG path data for snap preview line.
    /// </summary>
    public string GenerateSnapPreviewPath(Point from, Point to)
    {
        var ic = CultureInfo.InvariantCulture;
        return string.Format(ic, "M {0:F2},{1:F2} L {2:F2},{3:F2}", from.X, from.Y, to.X, to.Y);
    }
}