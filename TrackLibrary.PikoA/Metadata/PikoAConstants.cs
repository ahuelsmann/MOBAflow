namespace Moba.TrackLibrary.PikoA.Metadata;

public static class PikoAConstants
{
    // ------------------------------------------------------------
    // STRAIGHTS (Längen in mm)
    // ------------------------------------------------------------
    public const double G231Length = 231.0;   // Piko 55200
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
    public const double R9 = 907.97;  // Piko 55219, korrigiert von 888

    // Standard-Kurvenwinkel (Piko A: 30°-Segmente für R1-R3)
    public const double StandardAngle = 30.0;

    // R9-Kurvenwinkel (Piko 55219: 15°-Segmente)
    public const double R9Angle = 15.0;

    // ------------------------------------------------------------
    // WEICHEN (Piko 55220/55221)
    // ------------------------------------------------------------

    // Weichenwinkel
    public const double SwitchAngle = 15.0;

    // Gerade Weichenlänge (entlang der Geraden)
    public const double SwitchStraightLengthMm = 239.07;  // Piko 55220/55221

    // Bogenradius des Abzweigs (entspricht R9)
    public const double SwitchRadiusMm = R9;

    // Abstand von A bis zum Bogenbeginn (Junction) entlang der Geraden.
    // Bei Piko A Weichen beginnt der Abzweig-Bogen am Weichen-Anfang (Port A).
    public const double SwitchJunctionOffsetMm = 0.0;

    // ------------------------------------------------------------
    // DREIWEGWEICHE W3 (Piko 55224)
    // ------------------------------------------------------------

    // Dreiwegweiche: Gerade Länge
    public const double ThreeWaySwitchLengthMm = 239.07;

    // Dreiwegweiche: Abzweig-Radius (wie normale Weiche)
    public const double ThreeWaySwitchRadiusMm = R9;

    // Dreiwegweiche: Abzweigwinkel (links +15°, rechts -15°)
    public const double ThreeWaySwitchAngle = 15.0;
}