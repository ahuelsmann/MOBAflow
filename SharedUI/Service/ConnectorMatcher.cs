// Copyright ...

namespace Moba.SharedUI.Service;

using Microsoft.Extensions.Logging;
using TrackPlan.Renderer;
using ViewModel;

public class ConnectorMatcher
{
    private readonly TrackGeometryLibrary _geometryLibrary;
    private readonly ILogger<ConnectorMatcher> _logger;

    /// <summary>
    /// Maximum distance (in mm) between endpoints to be considered a match.
    /// Hochgesetzt, um Geometrieabweichungen aus AnyRail besser zu tolerieren.
    /// </summary>
    public double PositionToleranceMm { get; set; } = 200.0;

    /// <summary>
    /// Maximum heading difference (in degrees) to be considered a match.
    /// Hochgesetzt, um leicht abweichende Winkel zuzulassen.
    /// </summary>
    public double HeadingToleranceDeg { get; set; } = 45.0;

    public ConnectorMatcher(TrackGeometryLibrary geometryLibrary, ILogger<ConnectorMatcher> logger)
    {
        _geometryLibrary = geometryLibrary;
        _logger = logger;
    }

    public record MatchResult(
        string Segment1Id, int Connector1,
        string Segment2Id, int Connector2,
        double DistanceMm,
        double HeadingDiffDeg);

    /// <summary>
    /// Finds all matching connector pairs between two segments.
    /// Vergleicht Weltkoordinaten der Endpunkte + Heading-Kompatibilität.
    /// </summary>
    public List<MatchResult> FindMatches(
        TrackSegmentViewModel seg1,
        TrackSegmentViewModel seg2)
    {
        var results = new List<MatchResult>();

        var geom1 = _geometryLibrary.GetGeometry(seg1.ArticleCode);
        var geom2 = _geometryLibrary.GetGeometry(seg2.ArticleCode);

        if (geom1 == null || geom2 == null)
        {
            _logger.LogWarning(
                "ConnectorMatcher: Missing geometry for {A} or {B} (A={ArticleA}, B={ArticleB})",
                seg1.Id, seg2.Id, seg1.ArticleCode, seg2.ArticleCode);
            return results;
        }

        for (int i = 0; i < geom1.Endpoints.Count; i++)
        {
            var ep1 = geom1.Endpoints[i];
            var world1 = seg1.WorldTransform.TransformPoint(ep1.X, ep1.Y);
            var heading1 = NormalizeAngle(geom1.EndpointHeadingsDeg[i] + seg1.WorldTransform.RotationDegrees);

            for (int j = 0; j < geom2.Endpoints.Count; j++)
            {
                var ep2 = geom2.Endpoints[j];
                var world2 = seg2.WorldTransform.TransformPoint(ep2.X, ep2.Y);
                var heading2 = NormalizeAngle(geom2.EndpointHeadingsDeg[j] + seg2.WorldTransform.RotationDegrees);

                var dx = world1.X - world2.X;
                var dy = world1.Y - world2.Y;
                var dist = Math.Sqrt(dx * dx + dy * dy);

                // Heading soll in etwa entgegengesetzt sein → +180°
                var headingExpected = NormalizeAngle(heading2 + 180);
                var diff = Math.Abs(NormalizeAngle(heading1 - headingExpected));

                _logger.LogDebug(
                    "ConnectorMatcher: Check {A}[{Ai}]↔{B}[{Bj}] | dist={D:F2}mm (tol={TolD}), headingDiff={H:F1}° (tol={TolH})",
                    seg1.Id, i, seg2.Id, j, dist, PositionToleranceMm, diff, HeadingToleranceDeg);

                if (dist > PositionToleranceMm)
                    continue;

                if (diff > HeadingToleranceDeg)
                    continue;

                var match = new MatchResult(
                    seg1.Id, i,
                    seg2.Id, j,
                    dist,
                    diff);

                results.Add(match);

                _logger.LogInformation(
                    "ConnectorMatcher: ✅ MATCH {A}[{Ai}]↔{B}[{Bj}] | dist={D:F2}mm, headingDiff={H:F1}°",
                    match.Segment1Id, match.Connector1,
                    match.Segment2Id, match.Connector2,
                    match.DistanceMm, match.HeadingDiffDeg);
            }
        }

        if (results.Count == 0)
        {
            _logger.LogDebug(
                "ConnectorMatcher: No matches between {A}({ArticleA}) and {B}({ArticleB})",
                seg1.Id, seg1.ArticleCode, seg2.Id, seg2.ArticleCode);
        }

        return results;
    }

    private static double NormalizeAngle(double deg)
    {
        deg %= 360;
        if (deg < 0) deg += 360;
        return deg;
    }
}
