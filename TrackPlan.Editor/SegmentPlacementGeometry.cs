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

    /// <summary>World positions of the connectors that take part in no connection.</summary>
    public IReadOnlyList<(string PortName, double X, double Y)> GetOpenPortWorldPositions(
        PlacedSegment placed,
        IEnumerable<PortConnection> connections)
    {
        ArgumentNullException.ThrowIfNull(placed);
        ArgumentNullException.ThrowIfNull(connections);
        var connectionList = connections as IReadOnlyCollection<PortConnection> ?? connections.ToList();
        return GetAllPortWorldPositions(placed)
            .Where(port => !connectionList.IsConnected(placed.Segment.No, port.PortName))
            .Select(port => (port.PortName, port.X, port.Y))
            .ToList();
    }

    /// <summary>Axis-aligned world bounds (mm) of the rotated drawing path of a placed segment.</summary>
    public (double MinX, double MinY, double MaxX, double MaxY) GetWorldBounds(PlacedSegment placed)
    {
        ArgumentNullException.ThrowIfNull(placed);
        var (localMinX, localMinY, localMaxX, localMaxY) = TrackPathGeometry.GetBounds(GetPath(placed.Segment));
        (double X, double Y)[] corners =
        [
            ToWorld(localMinX, localMinY, placed.X, placed.Y, placed.RotationDegrees),
            ToWorld(localMaxX, localMinY, placed.X, placed.Y, placed.RotationDegrees),
            ToWorld(localMinX, localMaxY, placed.X, placed.Y, placed.RotationDegrees),
            ToWorld(localMaxX, localMaxY, placed.X, placed.Y, placed.RotationDegrees)
        ];

        return (corners.Min(c => c.X), corners.Min(c => c.Y), corners.Max(c => c.X), corners.Max(c => c.Y));
    }

    /// <summary>World bounds (mm) enclosing all placements, or <c>null</c> when there are none.</summary>
    public (double MinX, double MinY, double MaxX, double MaxY)? GetContentBounds(IEnumerable<PlacedSegment> placements)
    {
        ArgumentNullException.ThrowIfNull(placements);
        (double MinX, double MinY, double MaxX, double MaxY)? result = null;
        foreach (var placed in placements)
        {
            var (minX, minY, maxX, maxY) = GetWorldBounds(placed);
            result = result is { } current
                ? (Math.Min(current.MinX, minX), Math.Min(current.MinY, minY), Math.Max(current.MaxX, maxX), Math.Max(current.MaxY, maxY))
                : (minX, minY, maxX, maxY);
        }

        return result;
    }

    /// <summary>
    /// Centroid of the connectors in world coordinates; lies on or next to the track for every segment type.
    /// Falls back to the segment origin when the segment has no connectors.
    /// </summary>
    public (double X, double Y) GetPortCentroid(PlacedSegment placed)
    {
        ArgumentNullException.ThrowIfNull(placed);
        var ports = GetAllPortWorldPositions(placed);
        return ports.Count == 0
            ? (placed.X, placed.Y)
            : (ports.Average(port => port.X), ports.Average(port => port.Y));
    }

    /// <summary>Normalizes an angle to the range [0, 360).</summary>
    public static double NormalizeAngle(double degrees)
    {
        while (degrees >= 360)
            degrees -= 360;
        while (degrees < 0)
            degrees += 360;
        return degrees;
    }

    /// <summary>Smallest absolute difference between two angles in degrees (0 to 180).</summary>
    public static double AngleDeltaDegrees(double leftDegrees, double rightDegrees)
    {
        var delta = NormalizeAngle(leftDegrees - rightDegrees);
        if (delta > 180)
            delta = 360 - delta;
        return Math.Abs(delta);
    }

    private SegmentPort? GetPort(Segment segment, string portName) =>
        Catalog.GetPorts(segment).FirstOrDefault(port => port.Name == portName);
}
