// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Domain;

/// <summary>
/// Static library of Piko A-Gleis H0 track system definitions.
/// Based on official Piko documentation (docs/Piko-A-Gleis.txt).
/// </summary>
public static class PikoATrackLibrary
{
    #region Track Templates
    /// <summary>
    /// All available track templates in the Piko A-Gleis system.
    /// </summary>
    public static readonly List<TrackTemplate> Templates =
    [
        // Straight Tracks (Gerade Gleise)
        new() { ArticleCode = "G239", Name = "Gerades Gleis 239 mm", Type = TrackType.Straight, Length = 239.07, Radius = 0, Angle = 0, ProductCode = "55200" },
        new() { ArticleCode = "G231", Name = "Gerades Gleis 231 mm", Type = TrackType.Straight, Length = 230.93, Radius = 0, Angle = 0, ProductCode = "55201" },
        new() { ArticleCode = "G119", Name = "Gerades Gleis 119 mm", Type = TrackType.Straight, Length = 119.54, Radius = 0, Angle = 0, ProductCode = "55202" },
        new() { ArticleCode = "G115", Name = "Gerades Gleis 115 mm", Type = TrackType.Straight, Length = 115.46, Radius = 0, Angle = 0, ProductCode = "55203" },
        new() { ArticleCode = "G107", Name = "Gerades Gleis 107 mm", Type = TrackType.Straight, Length = 107.32, Radius = 0, Angle = 0, ProductCode = "55204" },
        new() { ArticleCode = "G62", Name = "Gerades Gleis 62 mm", Type = TrackType.Straight, Length = 61.88, Radius = 0, Angle = 0, ProductCode = "55205" },
        new() { ArticleCode = "G940", Name = "Flexibles Gleis 940 mm", Type = TrackType.Straight, Length = 940, Radius = 0, Angle = 0, ProductCode = "55209" },

        // Curved Tracks 30° (Bogengleise)
        new() { ArticleCode = "R1", Name = "Bogen 30° R1", Type = TrackType.Curve, Length = 188.5, Radius = 360, Angle = 30, ProductCode = "55211" },
        new() { ArticleCode = "R2", Name = "Bogen 30° R2", Type = TrackType.Curve, Length = 220.9, Radius = 421.88, Angle = 30, ProductCode = "55212" },
        new() { ArticleCode = "R3", Name = "Bogen 30° R3", Type = TrackType.Curve, Length = 253.3, Radius = 483.75, Angle = 30, ProductCode = "55213" },
        new() { ArticleCode = "R4", Name = "Bogen 30° R4", Type = TrackType.Curve, Length = 285.6, Radius = 545.63, Angle = 30, ProductCode = "55214" },

        // Curved Track 15° (for switches)
        new() { ArticleCode = "R9", Name = "Bogen 15° R9", Type = TrackType.Curve, Length = 237.8, Radius = 907.97, Angle = 15, ProductCode = "55219" },

        // Turnouts (Weichen) - 15° angle, straight = G239, turnout = R9
        new() { ArticleCode = "WL", Name = "Weiche links", Type = TrackType.TurnoutLeft, Length = 239.07, Radius = 907.97, Angle = 15, ProductCode = "55220" },
        new() { ArticleCode = "WR", Name = "Weiche rechts", Type = TrackType.TurnoutRight, Length = 239.07, Radius = 907.97, Angle = 15, ProductCode = "55221" },

        // Curved Turnouts (Bogenweichen) - R2/R3 transition
        new() { ArticleCode = "BWL-R2", Name = "Bogenweiche links R2/R3", Type = TrackType.CurvedTurnoutLeft, Length = 220.9, Radius = 421.88, Angle = 30, ProductCode = "55222" },
        new() { ArticleCode = "BWR-R2", Name = "Bogenweiche rechts R2/R3", Type = TrackType.CurvedTurnoutRight, Length = 220.9, Radius = 421.88, Angle = 30, ProductCode = "55223" },

        // Curved Turnouts (Bogenweichen) - R3/R4 transition
        new() { ArticleCode = "BWL-R3", Name = "Bogenweiche links R3/R4", Type = TrackType.CurvedTurnoutLeft, Length = 253.3, Radius = 483.75, Angle = 30, ProductCode = "55227" },
        new() { ArticleCode = "BWR-R3", Name = "Bogenweiche rechts R3/R4", Type = TrackType.CurvedTurnoutRight, Length = 253.3, Radius = 483.75, Angle = 30, ProductCode = "55228" },

        // Special Switches
        new() { ArticleCode = "DKW", Name = "Doppelkreuzungsweiche", Type = TrackType.DoubleCrossover, Length = 239.07, Radius = 907.97, Angle = 15, ProductCode = "55224" },
        new() { ArticleCode = "DWW", Name = "Dreiwegweiche", Type = TrackType.ThreeWay, Length = 239.07, Radius = 907.97, Angle = 15, ProductCode = "55225" },
        new() { ArticleCode = "W3", Name = "3-Wegeweiche", Type = TrackType.ThreeWay, Length = 237.8, Radius = 907.97, Angle = 30, ProductCode = "55225" },

        // Crossings (Kreuzungen)
        new() { ArticleCode = "K15", Name = "Kreuzung 15°", Type = TrackType.Crossing, Length = 239.07, Radius = 0, Angle = 15, ProductCode = "55240" },
        new() { ArticleCode = "K30", Name = "Kreuzung 30°", Type = TrackType.Crossing, Length = 119.54, Radius = 0, Angle = 30, ProductCode = "55241" },
    ];
    #endregion

    #region Constants
    /// <summary>
    /// Parallel track spacing (Parallelgleisabstand) in mm.
    /// This is the distance between parallel tracks created by switches.
    /// </summary>
    public const double ParallelTrackSpacing = 61.88;

    /// <summary>
    /// Standard turnout radius (Abzweigradius) in mm.
    /// All WL/WR switches use this radius.
    /// </summary>
    public const double StandardTurnoutRadius = 907.97;

    /// <summary>
    /// Standard turnout angle in degrees.
    /// All WL/WR switches use 15° angle.
    /// </summary>
    public const double StandardTurnoutAngle = 15.0;

    /// <summary>
    /// Track profile height (Code 100) in mm.
    /// NEM 120 standard: 2.5 mm.
    /// </summary>
    public const double TrackProfileHeight = 2.5;

    /// <summary>
    /// Rail gauge (Spurweite) in mm.
    /// H0 standard: 16.5 mm.
    /// </summary>
    public const double RailGauge = 16.5;
    #endregion

    #region Helper Methods
    public static TrackTemplate? GetByArticleCode(string articleCode)
        => Templates.FirstOrDefault(t => t.ArticleCode.Equals(articleCode, StringComparison.OrdinalIgnoreCase));

    public static List<TrackTemplate> GetStraightTracks()
        => Templates.Where(t => t.Type == TrackType.Straight).ToList();

    public static List<TrackTemplate> GetCurvedTracks()
        => Templates.Where(t => t.Type == TrackType.Curve).ToList();

    public static List<TrackTemplate> GetTurnouts()
        => Templates.Where(t => t.Type is TrackType.TurnoutLeft or TrackType.TurnoutRight
                                        or TrackType.CurvedTurnoutLeft or TrackType.CurvedTurnoutRight
                                        or TrackType.DoubleCrossover or TrackType.ThreeWay).ToList();

    public static List<TrackTemplate> GetCrossings()
        => Templates.Where(t => t.Type == TrackType.Crossing).ToList();

    public static double CalculateArcLength(double radiusMm, double angleDegrees)
        => angleDegrees / 360.0 * 2.0 * Math.PI * radiusMm;

    public static int PiecesForCompleteCircle(double angleDegrees)
        => (int)Math.Round(360.0 / angleDegrees);
    #endregion
}

/// <summary>
/// Template for a track piece in the Piko A-Gleis system.
/// Defines the geometry and metadata for catalog items.
/// </summary>
public class TrackTemplate
{
    public required string ArticleCode { get; init; }
    public required string Name { get; init; }
    public required TrackType Type { get; init; }
    public required double Length { get; init; }
    public required double Radius { get; init; }
    public required double Angle { get; init; }
    public required string ProductCode { get; init; }

    public string DisplayText => $"{ArticleCode} - {Name}";

    public string Description => Type switch
    {
        TrackType.Straight => $"{Length:F0} mm",
        TrackType.Curve => $"R={Radius:F0} mm, {Angle:F0}°",
        TrackType.TurnoutLeft or TrackType.TurnoutRight => $"{Angle:F0}°, R={Radius:F0} mm",
        TrackType.CurvedTurnoutLeft or TrackType.CurvedTurnoutRight => $"R={Radius:F0} mm, {Angle:F0}°",
        TrackType.DoubleCrossover or TrackType.ThreeWay => $"{Angle:F0}°",
        TrackType.Crossing => $"{Angle:F0}°",
        _ => string.Empty
    };
}
