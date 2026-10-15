// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Editor;

using TrackLibrary.Base;

/// <summary>
/// World-space geometry of placed segments: connector positions, outward angles and placements that align
/// a connector with a target point. Catalogue specifics come from <see cref="ITrackCatalog"/>.
/// </summary>
public sealed class SegmentPlacementGeometry(ITrackCatalog catalog)
{
    /// <summary>The catalogue that supplies ports, paths and segment codes.</summary>
    public ITrackCatalog Catalog { get; } = catalog ?? throw new ArgumentNullException(nameof(catalog));

    /// <summary>Transforms a local point of a segment into world coordinates.</summary>
    public static (double Wx, double Wy) ToWorld(double x, double y, double segmentX, double segmentY, double rotationDegrees)
    {
        var r = rotationDegrees * Math.PI / 180;
        var cos = Math.Cos(r);
        var sin = Math.Sin(r);
        return (
            segmentX + x * cos - y * sin,
            segmentY + x * sin + y * cos
        );
    }

    /// <summary>World position and track direction of one connector; the segment origin if the port is unknown.</summary>
    public (double X, double Y, double AngleDegrees) GetPortWorldPosition(PlacedSegment placed, string portName)
    {
        ArgumentNullException.ThrowIfNull(placed);
        var port = GetPort(placed.Segment, portName);
        if (port == null)
            return (placed.X, placed.Y, placed.RotationDegrees);

        var (wx, wy) = ToWorld(port.LocalX, port.LocalY, placed.X, placed.Y, placed.RotationDegrees);
        return (wx, wy, placed.RotationDegrees + port.LocalAngleDegrees);
    }

    /// <summary>World positions and track directions of all connectors of a placed segment.</summary>
    public IReadOnlyList<(string PortName, double X, double Y, double AngleDegrees)> GetAllPortWorldPositions(PlacedSegment placed)
    {
        ArgumentNullException.ThrowIfNull(placed);
        var result = new List<(string, double, double, double)>();
        foreach (var port in Catalog.GetPorts(placed.Segment))
        {
            var (wx, wy) = ToWorld(port.LocalX, port.LocalY, placed.X, placed.Y, placed.RotationDegrees);
            result.Add((port.Name, wx, wy, placed.RotationDegrees + port.LocalAngleDegrees));
        }

        return result;
    }

    /// <summary>Direction in which the track leaves the segment at the port, in local degrees.</summary>
    public double GetPortOutwardLocalAngleDegrees(Segment segment, string portName)
    {
        var port = GetPort(segment, portName);
        if (port == null)
            return 0;

        var angle = port.LocalAngleDegrees;
        if (port.IsStart)
            angle += 180;

        return NormalizeAngle(angle);
    }

    /// <summary>Direction in which the track leaves the placed segment at the port, in world degrees.</summary>
    public double GetPortOutwardWorldAngleDegrees(PlacedSegment placed, string portName)
    {
        ArgumentNullException.ThrowIfNull(placed);
        return NormalizeAngle(placed.RotationDegrees + GetPortOutwardLocalAngleDegrees(placed.Segment, portName));
    }

    /// <summary>
    /// Placement that puts <paramref name="portName"/> at the world point and lets it point in the desired outward direction.
    /// </summary>
    public (double X, double Y, double RotationDegrees) GetPlacementForPort(
        Segment segment,
        string portName,
        double worldX,
        double worldY,
        double desiredOutwardAngleDegrees)
    {
        var port = GetPort(segment, portName);
        if (port == null)
            return (worldX, worldY, NormalizeAngle(desiredOutwardAngleDegrees));

        var rotationDegrees = NormalizeAngle(desiredOutwardAngleDegrees - GetPortOutwardLocalAngleDegrees(segment, portName));
        var r = rotationDegrees * Math.PI / 180;
        var cos = Math.Cos(r);
        var sin = Math.Sin(r);
        var originX = worldX - (port.LocalX * cos - port.LocalY * sin);
        var originY = worldY - (port.LocalX * sin + port.LocalY * cos);
        return (originX, originY, rotationDegrees);
    }

    /// <summary>Moves a placed segment without rotating it so that the port lands on the world point.</summary>
    public (double X, double Y, double RotationDegrees) TranslatePlacementForPort(
        PlacedSegment placed,
        string portName,
        double worldX,
        double worldY)
    {
        ArgumentNullException.ThrowIfNull(placed);
        var port = GetPort(placed.Segment, portName);
        if (port == null)
            return (worldX, worldY, placed.RotationDegrees);

        var r = placed.RotationDegrees * Math.PI / 180;
        var cos = Math.Cos(r);
        var sin = Math.Sin(r);
        var originX = worldX - (port.LocalX * cos - port.LocalY * sin);
        var originY = worldY - (port.LocalX * sin + port.LocalY * cos);
        return (originX, originY, placed.RotationDegrees);
    }

    /// <summary>Drawing path of the segment in local coordinates.</summary>
    public IReadOnlyList<ITrackPathCommand> GetPath(Segment segment) => Catalog.GetPath(segment);

    private SegmentPort? GetPort(Segment segment, string portName) =>
        Catalog.GetPorts(segment).FirstOrDefault(port => port.Name == portName);

    private static double NormalizeAngle(double degrees)
    {
        while (degrees >= 360)
            degrees -= 360;
        while (degrees < 0)
            degrees += 360;
        return degrees;
    }
}
