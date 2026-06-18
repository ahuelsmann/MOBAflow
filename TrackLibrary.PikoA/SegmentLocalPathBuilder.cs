// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// Generates path geometry for track segments in local coordinates (Port A = origin, angle 0 = +X).
/// Uses the same formulas as TrackPlanSvgRenderer for consistent display.
/// </summary>
public static class SegmentLocalPathBuilder
{
    /// <summary>Command in path (platform-independent).</summary>
    public abstract record PathCommand;

    /// <summary>Moves to position without drawing (start of new subpath).</summary>
    public sealed record MoveTo(double X, double Y) : PathCommand;

    /// <summary>Line to the specified point.</summary>
    public sealed record LineTo(double X, double Y) : PathCommand;

    /// <summary>Arc to endpoint (radius in mm, Clockwise = sweep direction, LargeArc = arc &gt; 180°).</summary>
    public sealed record ArcTo(double EndX, double EndY, double Radius, bool Clockwise, bool LargeArc = false) : PathCommand;

    /// <summary>
    /// Returns the path commands for a segment in local coordinates.
    /// Start point is always (0, 0) = Port A.
    /// Uses the same formulas as TrackPlanSvgRenderer.
    /// </summary>
    public static IReadOnlyList<PathCommand> GetPath(Segment segment)
    {
        return segment switch
        {
            Straight s => GetStraightPath(s.LengthInMm),
            Curved c => GetCurvedPath(c.ArcInDegree, c.RadiusInMm),
            WR wr => GetWrPath(wr.LengthInMm, wr.ArcInDegree, wr.RadiusInMm),
            WL wl => GetWlPath(wl.LengthInMm, wl.ArcInDegree, wl.RadiusInMm),
            WY wy => GetWyPath(wy.ArcInDegree, wy.RadiusInMm),
            W3 w3 => GetW3Path(w3.LengthInMm, w3.ArcInDegree, w3.RadiusInMm),
            BWL bwl => GetBwlPath(bwl.ArcInDegreeR2, bwl.RadiusInMmR2, bwl.RadiusInMmR3),
            BWR bwr => GetBwrPath(bwr.ArcInDegreeR2, bwr.RadiusInMmR2, bwr.RadiusInMmR3),
            BWLR3 bwlr3 => GetBwlr3Path(bwlr3.RadiusInMmR3),
            BWRR3 bwrr3 => GetBwrr3Path(bwrr3.RadiusInMmR3),
            DKW dkw => GetDkwPath(dkw.LengthInMm, dkw.ArcInDegree, dkw.RadiusInMm),
            K15 k15 => GetCrossingPath(k15.ArcInDegree, k15.LengthInMm),
            K30 k30 => GetCrossingPath(k30.ArcInDegree, k30.LengthInMm),
            _ => throw new NotSupportedException($"No local path geometry registered for segment type '{segment.GetType().Name}'.")
        };
    }

    /// <summary>Returns the bounding box of the path (MinX, MinY, MaxX, MaxY) in mm.</summary>
    public static (double MinX, double MinY, double MaxX, double MaxY) GetBounds(IReadOnlyList<PathCommand> path)
    {
        double minX = 0, minY = 0, maxX = 0, maxY = 0;
        double x = 0, y = 0;
        var hasPoint = false;

        foreach (var cmd in path)
        {
            if (cmd is MoveTo move)
            {
                x = move.X;
                y = move.Y;
            }
            else if (cmd is LineTo line)
            {
                x = line.X;
                y = line.Y;
            }
            else if (cmd is ArcTo arc)
            {
                x = arc.EndX;
                y = arc.EndY;
            }

            hasPoint = true;
            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
        }

        if (!hasPoint)
            return (0, 0, 1, 1);

        return (minX, minY, maxX, maxY);
    }

    private static IReadOnlyList<PathCommand> GetStraightPath(double length)
    {
        return [new LineTo(length, 0)];
    }

    private static IReadOnlyList<PathCommand> GetCurvedPath(double arcDegree, double radius)
    {
        const int curveDirection = 1;
        var centerAngleRad = (90 * curveDirection) * Math.PI / 180;
        var centerX = radius * Math.Cos(centerAngleRad);
        var centerY = radius * Math.Sin(centerAngleRad);
        var endAngle = arcDegree * curveDirection * Math.PI / 180;
        var endLocalAngleRad = (90 * curveDirection) * Math.PI / 180;
        var endX = centerX + radius * Math.Cos(endAngle - endLocalAngleRad);
        var endY = centerY + radius * Math.Sin(endAngle - endLocalAngleRad);
        return [new ArcTo(endX, endY, radius, true)];
    }

    private static IReadOnlyList<PathCommand> GetWrPath(double straightLength, double arcDegree, double radius)
    {
        var centerAngleRad = 90 * Math.PI / 180;
        var centerX = radius * Math.Cos(centerAngleRad);
        var centerY = radius * Math.Sin(centerAngleRad);
        var endAngleRad = (arcDegree - 90) * Math.PI / 180;
        var portCx = centerX + radius * Math.Cos(endAngleRad);
        var portCy = centerY + radius * Math.Sin(endAngleRad);
        return
        [
            new LineTo(straightLength, 0),
            new MoveTo(0, 0),
            new ArcTo(portCx, portCy, radius, true)
        ];
    }

    private static IReadOnlyList<PathCommand> GetWlPath(double straightLength, double arcDegree, double radius)
    {
        var centerAngleRad = -90 * Math.PI / 180;
        var centerX = radius * Math.Cos(centerAngleRad);
        var centerY = radius * Math.Sin(centerAngleRad);
        var endAngleRad = (-arcDegree + 90) * Math.PI / 180;
        var portCx = centerX + radius * Math.Cos(endAngleRad);
        var portCy = centerY + radius * Math.Sin(endAngleRad);
        return
        [
            new LineTo(straightLength, 0),
            new MoveTo(0, 0),
            new ArcTo(portCx, portCy, radius, false)
        ];
    }

    /// <summary>WY: Y-switch like W3 without straight – two R9 arcs of 15° each (Port B left like WL, Port C right like WR).</summary>
    private static IReadOnlyList<PathCommand> GetWyPath(double arcDegree, double radius)
    {
        var halfArc = arcDegree / 2;
        // Port B: linker Ast wie WL
        var centerL = -90 * Math.PI / 180;
        var centerLx = radius * Math.Cos(centerL);
        var centerLy = radius * Math.Sin(centerL);
        var endAngleL = (-halfArc + 90) * Math.PI / 180;
        var portBx = centerLx + radius * Math.Cos(endAngleL);
        var portBy = centerLy + radius * Math.Sin(endAngleL);
        // Port C: rechter Ast wie WR
        var centerR = 90 * Math.PI / 180;
        var centerRx = radius * Math.Cos(centerR);
        var centerRy = radius * Math.Sin(centerR);
        var endAngleR = (halfArc - 90) * Math.PI / 180;
        var portCx = centerRx + radius * Math.Cos(endAngleR);
        var portCy = centerRy + radius * Math.Sin(endAngleR);
        return
        [
            new ArcTo(portBx, portBy, radius, false),
            new MoveTo(0, 0),
            new ArcTo(portCx, portCy, radius, true)
        ];
    }

    /// <summary>W3 (Piko 55225): Straight G239 to Port B, two R9 branches 15° each – Port C like WL (left), Port D like WR (right).</summary>
    private static IReadOnlyList<PathCommand> GetW3Path(double length, double arcDegree, double radius)
    {
        var halfArc = arcDegree / 2;
        // Port C: left branch like WL – center (0, -radius), arc 15° counter-clockwise
        var centerL = -90 * Math.PI / 180;
        var centerLx = radius * Math.Cos(centerL);
        var centerLy = radius * Math.Sin(centerL);
        var endAngleL = (-halfArc + 90) * Math.PI / 180;
        var portCx = centerLx + radius * Math.Cos(endAngleL);
        var portCy = centerLy + radius * Math.Sin(endAngleL);
        // Port D: right branch like WR – center (0, radius), arc 15° clockwise
        var centerR = 90 * Math.PI / 180;
        var centerRx = radius * Math.Cos(centerR);
        var centerRy = radius * Math.Sin(centerR);
        var endAngleR = (halfArc - 90) * Math.PI / 180;
        var portDx = centerRx + radius * Math.Cos(endAngleR);
        var portDy = centerRy + radius * Math.Sin(endAngleR);
        return
        [
            new LineTo(length, 0),
            new MoveTo(0, 0),
            new ArcTo(portCx, portCy, radius, false),
            new MoveTo(0, 0),
            new ArcTo(portDx, portDy, radius, true)
        ];
    }

    /// <summary>BWL: Curved turnout left R2→R3. Main track R2 curve, branch R3 curve.</summary>
    private static IReadOnlyList<PathCommand> GetBwlPath(double arcR2, double radiusR2, double radiusR3)
    {
        return GetParallelCurvedTurnoutPath(arcR2, radiusR2, radiusR3, curveDirection: -1);
    }

    /// <summary>BWR: Curved turnout right R2→R3. Main track R2 curve, branch R3 curve.</summary>
    private static IReadOnlyList<PathCommand> GetBwrPath(double arcR2, double radiusR2, double radiusR3)
    {
        return GetParallelCurvedTurnoutPath(arcR2, radiusR2, radiusR3, curveDirection: 1);
    }

    /// <summary>BWLR3: Curved turnout left R3→R4.</summary>
    private static IReadOnlyList<PathCommand> GetBwlr3Path(double radiusR3)
    {
        var radiusR4 = radiusR3 + 61.88;
        return GetParallelCurvedTurnoutPath(30, radiusR3, radiusR4, curveDirection: -1);
    }

    /// <summary>BWRR3: Curved turnout right R3→R4.</summary>
    private static IReadOnlyList<PathCommand> GetBwrr3Path(double radiusR3)
    {
        var radiusR4 = radiusR3 + 61.88;
        return GetParallelCurvedTurnoutPath(30, radiusR3, radiusR4, curveDirection: 1);
    }

    private static IReadOnlyList<PathCommand> GetParallelCurvedTurnoutPath(
        double arcDegree,
        double mainRadius,
        double branchRadius,
        int curveDirection)
    {
        var centerAngle = (90 * curveDirection) * Math.PI / 180;
        var centerX = mainRadius * Math.Cos(centerAngle);
        var centerY = mainRadius * Math.Sin(centerAngle);
        var sweep = (arcDegree * curveDirection * Math.PI / 180) - centerAngle;
        var clockwise = curveDirection > 0;
        var mainEndX = centerX + mainRadius * Math.Cos(sweep);
        var mainEndY = centerY + mainRadius * Math.Sin(sweep);
        var branchEndX = centerX + branchRadius * Math.Cos(sweep);
        var branchEndY = centerY + branchRadius * Math.Sin(sweep);

        return
        [
            new ArcTo(mainEndX, mainEndY, mainRadius, clockwise),
            new MoveTo(0, 0),
            new ArcTo(branchEndX, branchEndY, branchRadius, clockwise)
        ];
    }

    /// <summary>
    /// DKW (Piko 55224): Two straight tracks crossing at <paramref name="arcDegree"/> (15°) through the midpoint.
    /// Same crossing topology as K15, but with four switchable ports (slip semantics handled by IsStartPort).
    /// Port C/D end vertically at ±½·length·sin(arcDegree) ≈ ±30.93 mm, i.e. half the parallel track spacing (61.88 mm),
    /// so the DKW naturally connects to adjacent WR/WL turnouts bridging the full parallel spacing.
    /// </summary>
    private static IReadOnlyList<PathCommand> GetDkwPath(double length, double arcDegree, double radius)
    {
        _ = radius;
        return GetCrossingPath(arcDegree, length);
    }

    private static IReadOnlyList<PathCommand> GetCrossingPath(double angleDeg, double length)
    {
        var rad = angleDeg * Math.PI / 180;
        var half = length / 2;
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);
        return
        [
            new MoveTo(0, 0),
            new LineTo(length, 0),
            new MoveTo(half - half * cos, -half * sin),
            new LineTo(half + half * cos, half * sin)
        ];
    }
}