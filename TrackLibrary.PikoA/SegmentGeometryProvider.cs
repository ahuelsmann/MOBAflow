// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// Provides path and port geometry for track segments.
/// </summary>
public interface ISegmentGeometryProvider
{
    IReadOnlyList<SegmentLocalPathBuilder.PathCommand> GetPath(Segment segment);

    IReadOnlyList<SegmentPortGeometry.PortInfo> GetPorts(Segment segment);

    (double X, double Y, double AngleDegrees) GetPortWorldPosition(PlacedSegment placed, string portName);

    double GetPortOutwardWorldAngleDegrees(PlacedSegment placed, string portName);

    (double X, double Y, double RotationDegrees) GetPlacementForPort(
        Segment segment,
        string portName,
        double worldX,
        double worldY,
        double desiredOutwardAngleDegrees);
}

/// <summary>
/// Default Piko A-Track geometry provider.
/// </summary>
public sealed class PikoASegmentGeometryProvider : ISegmentGeometryProvider
{
    public static PikoASegmentGeometryProvider Instance { get; } = new();

    public IReadOnlyList<SegmentLocalPathBuilder.PathCommand> GetPath(Segment segment) =>
        SegmentLocalPathBuilder.GetPath(segment);

    public IReadOnlyList<SegmentPortGeometry.PortInfo> GetPorts(Segment segment) =>
        SegmentPortGeometry.GetPorts(segment);

    public (double X, double Y, double AngleDegrees) GetPortWorldPosition(PlacedSegment placed, string portName) =>
        SegmentPortGeometry.GetPortWorldPosition(placed, portName);

    public double GetPortOutwardWorldAngleDegrees(PlacedSegment placed, string portName) =>
        SegmentPortGeometry.GetPortOutwardWorldAngleDegrees(placed, portName);

    public (double X, double Y, double RotationDegrees) GetPlacementForPort(
        Segment segment,
        string portName,
        double worldX,
        double worldY,
        double desiredOutwardAngleDegrees) =>
        SegmentPortGeometry.GetPlacementForPort(segment, portName, worldX, worldY, desiredOutwardAngleDegrees);
}