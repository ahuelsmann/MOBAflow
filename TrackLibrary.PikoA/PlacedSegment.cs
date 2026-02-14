namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// Ein platzierter Gleisabschnitt auf dem editierbaren TrackPlan.
/// Enthält das Segment, Position (mm) und Rotation (Grad).
/// </summary>
/// <param name="Segment">Das Gleissegment (G119, R9, WR, etc.)</param>
/// <param name="X">X-Position in mm (Canvas-Koordinaten)</param>
/// <param name="Y">Y-Position in mm (Canvas-Koordinaten)</param>
/// <param name="RotationDegrees">Rotation in Grad (0 = rechts, 90 = oben)</param>
public sealed record PlacedSegment(Segment Segment, double X, double Y, double RotationDegrees)
{
    /// <summary>Position und Rotation für Copy/Clone bei Bewegungen.</summary>
    public PlacedSegment WithPosition(double x, double y, double rotationDegrees) =>
        new(Segment, x, y, rotationDegrees);
}
