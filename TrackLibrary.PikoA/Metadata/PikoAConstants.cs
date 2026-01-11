// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackLibrary.PikoA.Metadata;

/// <summary>
/// Physical constants for Piko A-Gleis H0 track system.
/// All measurements in millimeters.
/// </summary>
public static class PikoAConstants
{
    /// <summary>Track gauge (H0 = 16.5mm)</summary>
    public const double TrackGaugeMm = 16.5;

    /// <summary>Rail profile height</summary>
    public const double RailHeightMm = 2.1;

    /// <summary>Roadbed width</summary>
    public const double RoadbedWidthMm = 41.3;

    // Curve radii
    public const double R1 = 360.0;
    public const double R2 = 421.88;
    public const double R3 = 483.75;
    public const double R4 = 545.63;
    public const double R9 = 907.97;

    // Standard angles
    public const double StandardAngle = 30.0;
    public const double SwitchAngle = 15.0;
    public const double CrossingAngle = 30.0;

    // Straight lengths
    public const double G231Length = 231.0;
    public const double G119Length = 119.0;
    public const double G62Length = 62.0;
    public const double G56Length = 56.0;
    public const double G31Length = 31.0;
}
