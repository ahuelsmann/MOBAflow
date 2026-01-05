// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using TrackPlan.Domain;
using ViewModel;

/// <summary>
/// Service for snap-to-connect logic in track plan editor (AnyRail-style).
/// Provides magnetic snapping for connecting track segments at their endpoints.
/// Endpoints are extracted from PathData (absolute coordinates).
/// All operations are async to avoid blocking UI thread during PathData parsing.
/// </summary>
public class SnapToConnectService
{
    private const double SnapDistance = 40;

    public record TrackEndpoint(TrackPoint Position, double Angle, TrackSegmentViewModel Segment, bool IsStart);

    /// <summary>
    /// Extracts endpoints from a track segment's PathData asynchronously.
    /// </summary>
    public Task<List<TrackEndpoint>> GetEndpointsAsync(TrackSegmentViewModel segment)
    {
        var endpoints = new List<TrackEndpoint>();
        var pathData = segment.PathData;

        if (string.IsNullOrEmpty(pathData))
            return Task.FromResult(endpoints);

        var coords = ExtractCoordinates(pathData);

        if (coords.Count >= 2)
        {
            var startPoint = coords[0];
            var endPoint = coords[^1];

            var angle = Math.Atan2(endPoint.Y - startPoint.Y, endPoint.X - startPoint.X) * 180 / Math.PI;

            endpoints.Add(new TrackEndpoint(startPoint, angle + 180, segment, true));
            endpoints.Add(new TrackEndpoint(endPoint, angle, segment, false));

            Debug.WriteLine($"📍 Endpoints for {segment.ArticleCode}: Start=({startPoint.X:F0},{startPoint.Y:F0}), End=({endPoint.X:F0},{endPoint.Y:F0})");
        }

        return Task.FromResult(endpoints);
    }

    private static List<TrackPoint> ExtractCoordinates(string pathData)
    {
        var points = new List<TrackPoint>();
        var ic = CultureInfo.InvariantCulture;

        var regex = new Regex(@"(-?\d+\.?\d*),(-?\d+\.?\d*)");
        var matches = regex.Matches(pathData);

        foreach (Match match in matches)
        {
            if (double.TryParse(match.Groups[1].Value, NumberStyles.Float, ic, out var x) &&
                double.TryParse(match.Groups[2].Value, NumberStyles.Float, ic, out var y))
            {
                points.Add(new TrackPoint(x, y));
            }
        }

        return points;
    }

    /// <summary>
    /// Finds the nearest snap endpoint within snap distance asynchronously.
    /// </summary>
    public async Task<(TrackPoint SnapPosition, double SnapRotation)?> FindSnapEndpointAsync(
        TrackPoint currentPosition,
        double currentRotation,
        IEnumerable<TrackSegmentViewModel> segments)
    {
        _ = currentRotation;
        double minDistance = SnapDistance;
        TrackEndpoint? bestTarget = null;

        foreach (var segment in segments)
        {
            var endpoints = await GetEndpointsAsync(segment);
            foreach (var endpoint in endpoints)
            {
                var distance = Math.Sqrt(
                    Math.Pow(endpoint.Position.X - currentPosition.X, 2) +
                    Math.Pow(endpoint.Position.Y - currentPosition.Y, 2));

                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestTarget = endpoint;
                }
            }
        }

        if (bestTarget is not null)
        {
            Debug.WriteLine($"🎯 Snap target found: ({bestTarget.Position.X:F0},{bestTarget.Position.Y:F0}) distance={minDistance:F0}px");
            return (bestTarget.Position, bestTarget.Angle);
        }

        return null;
    }

    /// <summary>
    /// Finds the nearest snap target position asynchronously.
    /// </summary>
    public async Task<TrackPoint?> FindSnapTargetAsync(TrackPoint currentPosition, IEnumerable<TrackSegmentViewModel> segments)
    {
        var result = await FindSnapEndpointAsync(currentPosition, 0, segments);
        return result?.SnapPosition;
    }

    /// <summary>
    /// Generates SVG path for snap preview line (synchronous - simple string formatting).
    /// </summary>
    public string GenerateSnapPreviewPath(TrackPoint from, TrackPoint to)
    {
        var ic = CultureInfo.InvariantCulture;
        return string.Format(ic, "M {0:F2},{1:F2} L {2:F2},{3:F2}", from.X, from.Y, to.X, to.Y);
    }
}
