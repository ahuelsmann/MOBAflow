namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// A placed track segment on the editable track plan.
/// Contains the segment, position (mm) and rotation (degrees).
/// </summary>
/// <param name="Segment">The track segment (G119, R9, WR, etc.)</param>
/// <param name="X">X position in mm (canvas coordinates)</param>
/// <param name="Y">Y position in mm (canvas coordinates)</param>
/// <param name="RotationDegrees">Rotation in degrees (0 = right, 90 = up)</param>
public sealed record PlacedSegment(Segment Segment, double X, double Y, double RotationDegrees)
{
    /// <summary>Position and rotation for copy/clone during movements.</summary>
    public PlacedSegment WithPosition(double x, double y, double rotationDegrees) =>
        new(Segment, x, y, rotationDegrees);
}
