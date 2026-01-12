namespace Moba.TrackLibrary.PikoA.Metadata;

public static class PikoAConstants
{
    // ------------------------------------------------------------
    // STRAIGHTS (Längen in mm)
    // ------------------------------------------------------------
    public const double G231Length = 231.0;
    public const double G119Length = 119.2;
    public const double G62Length = 62.0;
    public const double G56Length = 55.5;
    public const double G31Length = 31.0;

    // ------------------------------------------------------------
    // CURVES (Radien in mm)
    // ------------------------------------------------------------
    public const double R1 = 358.0;
    public const double R2 = 421.6;
    public const double R3 = 484.5;
    public const double R9 = 888.0;

    // Standard‑Kurvenwinkel (Piko A: 30°‑Segmente)
    public const double StandardAngle = 30.0;

    // ------------------------------------------------------------
    // WEICHEN
    // ------------------------------------------------------------

    // Weichenwinkel
    public const double SwitchAngle = 15.0;

    // Gerade Weichenlänge (entlang der Geraden)
    public const double SwitchStraightLengthMm = G231Length;

    // Bogenradius des Abzweigs
    public const double SwitchRadiusMm = R9;

    // Abstand von A bis zum Bogenbeginn (Junction) entlang der Geraden.
    // Realwert aus Geometrie grob ~120 mm; bei Bedarf später verfeinern.
    public const double SwitchJunctionOffsetMm = 120.0;
}