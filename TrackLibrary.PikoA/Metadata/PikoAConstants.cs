namespace Moba.TrackLibrary.PikoA.Metadata;

public static class PikoAConstants
{
    // ------------------------------------------------------------
    // STRAIGHTS (Längen in mm)
    // ------------------------------------------------------------
    public const double G940Length = 940.0;   // Piko 55209
    public const double G239Length = 239.0;   // Piko 55205
    public const double G231Length = 231.0;   // Piko 55200
    public const double G119Length = 119.2;   // Piko 55201
    public const double G115Length = 115.0;   // Piko 55206
    public const double G107Length = 107.0;   // Piko 55207
    public const double G62Length = 62.0;     // Piko 55202
    public const double G56Length = 55.5;     // Piko 55203
    public const double G31Length = 31.0;     // Piko 55204
    public const double G55620Length = 37.5;  // Piko 55620 (Anschlussgleis)
    public const double G55621Length = 77.5;  // Piko 55621 (Anschlussgleis)

    // ------------------------------------------------------------
    // CURVES (Radien in mm)
    // ------------------------------------------------------------
    public const double R1 = 358.0;           // Piko 55211
    public const double R2 = 421.6;           // Piko 55212
    public const double R3 = 484.5;           // Piko 55213
    public const double R4 = 545.0;           // Piko 55214
    public const double R9 = 907.97;          // Piko 55219
    
    // Prellböcke (Endstücke) - verwenden R1/R2 Radius
    public const double R1X = R1;             // Piko 55290 (R1 Prellbock)
    public const double R2X = R2;             // Piko 55291 (R2 Prellbock)

    // Standard-Kurvenwinkel (Piko A: 30°-Segmente für R1-R4)
    public const double StandardAngle = 30.0;

    // R9-Kurvenwinkel (Piko 55219: 15°-Segmente)
    public const double R9Angle = 15.0;

    // Prellbock-Winkel (7.5° = halber R9-Winkel)
    public const double EndcapAngle = 7.5;

    // ------------------------------------------------------------
    // WEICHEN (Piko 55220/55221/55222/55223)
    // ------------------------------------------------------------

    // Weichenwinkel
    public const double SwitchAngle = 15.0;

    // Gerade Weichenlänge (entlang der Geraden)
    public const double SwitchStraightLengthMm = 239.07;  // Piko 55220/55221

    // Bogenradius des Abzweigs (entspricht R9)
    public const double SwitchRadiusMm = R9;

    // Bogenweiche R3 - Radius für Abzweig
    public const double SwitchR3RadiusMm = R3;

    // Abstand von A bis zum Bogenbeginn (Junction) entlang der Geraden
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

    // ------------------------------------------------------------
    // KREUZUNGEN (K30, K10)
    // ------------------------------------------------------------
    
    // Kreuzung 30° (Piko 55240)
    public const double K30Angle = 30.0;
    public const double K30LengthMm = 111.0;
    
    // Kreuzung 10° (Piko 55245)
    public const double K10Angle = 10.0;
    public const double K10LengthMm = 111.0;

    // ------------------------------------------------------------
    // DOPPELKREUZUNGSWEICHEN (DKW, DKW3)
    // ------------------------------------------------------------
    
    // DKW Standard (Piko 55241)
    public const double DKWLengthMm = 225.0;
    public const double DKWAngle = 15.0;
    
    // DKW3 (Piko 55242)
    public const double DKW3LengthMm = 225.0;
    public const double DKW3Angle = 15.0;
}