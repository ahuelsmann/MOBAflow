// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.TrackPlan;

/// <summary>
/// Represents a complete track layout with all segments.
/// Contains factory methods for creating standard layouts.
/// </summary>
public class TrackLayout
{
    /// <summary>
    /// Unique identifier for this layout.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Display name for this layout.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the layout.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Track system used (e.g., "Piko A-Gleis", "Tillig Elite").
    /// </summary>
    public string TrackSystem { get; set; } = "Piko A-Gleis";

    /// <summary>
    /// Scale (e.g., "H0", "N", "TT").
    /// </summary>
    public string Scale { get; set; } = "H0";

    /// <summary>
    /// Canvas width for normalized coordinates.
    /// </summary>
    public double CanvasWidth { get; set; } = 1000;

    /// <summary>
    /// Canvas height for normalized coordinates.
    /// </summary>
    public double CanvasHeight { get; set; } = 600;

    /// <summary>
    /// All track segments in this layout.
    /// </summary>
    public List<TrackSegment> Segments { get; set; } = [];

    // ============================================
    // PIKO A-GLEIS SPECIFICATIONS (from PDF)
    // ============================================
    // Curves: 6 pieces = 180° semicircle (30° each)
    //   R1 (55211): 360.0 mm
    //   R2 (55212): 421.9 mm
    //   R3 (55213): 483.8 mm
    //
    // Track spacing: 61.9 mm
    //
    // Straights:
    //   G239 (55200): 239 mm
    //   G231 (55201): 231 mm
    //   G119 (55203): 119 mm
    //   G62  (55202): 62 mm
    //
    // Switches:
    //   WL (55220): 239 mm, 15° angle
    //   WR (55221): 239 mm, 15° angle
    //   W3 (55224): 239 mm, 3-way switch
    //   DKW (55226): 239 mm, double crossover

    /// <summary>
    /// Creates the Hundeknochen (dogbone) layout based on AnyRail plan.
    /// Real dimensions: 266cm x 110cm (2660mm x 1100mm)
    /// Upper station: 4 tracks, Lower station: 3 tracks
    /// </summary>
    public static TrackLayout CreateHundeknochenMittelstadt()
    {
        const double canvasWidth = 1000.0;
        const double canvasHeight = 420.0;

        var layout = new TrackLayout
        {
            Name = "Hundeknochen Mittelstadt",
            Description = "Dogbone layout 266cm x 110cm, 4 upper tracks, 3 lower tracks",
            TrackSystem = "Piko A-Gleis",
            Scale = "H0",
            CanvasWidth = canvasWidth,
            CanvasHeight = canvasHeight
        };

        // ============================================
        // GEOMETRY CONSTANTS
        // ============================================
        double centerY = canvasHeight / 2; // 210

        // Curve radii (proportional to real Piko radii)
        const double r1 = 65;   // R1 = 360mm (innermost)
        const double r2 = 88;   // R2 = 422mm
        const double r3 = 110;  // R3 = 484mm (outermost)
        const double trackSpacing = 23; // ~62mm scaled

        // Curve center positions
        const double leftCurveX = 140;
        const double rightCurveX = 860;

        // G62 extension length
        const double g62Extension = 12;

        // ============================================
        // TRACK Y-POSITIONS
        // ============================================
        // Upper tracks (4 tracks, from outer to inner)
        double track1Y = centerY - r3;                      // Outermost upper
        double track2Y = centerY - r2;
        double track3Y = centerY - r1;
        double track4Y = centerY - r1 + trackSpacing;       // Innermost upper (no curve)

        // Lower tracks (3 tracks, from inner to outer)
        double track5Y = centerY + r1;                      // Innermost lower
        double track6Y = centerY + r2;
        double track7Y = centerY + r3;                      // Outermost lower

        // ============================================
        // LEFT CURVES (180° = 6 x 30°)
        // From outer to inner: R3, R2, R1
        // ============================================
        AddSemicircle(layout, "L-R3", "R3", "Outer", leftCurveX, centerY, r3, isLeft: true);
        AddSemicircle(layout, "L-R2", "R2", "Middle", leftCurveX, centerY, r2, isLeft: true);
        AddSemicircle(layout, "L-R1", "R1", "Inner", leftCurveX, centerY, r1, isLeft: true);

        // ============================================
        // RIGHT CURVES (180° = 6 x 30°)
        // ============================================
        AddSemicircle(layout, "R-R3", "R3", "Outer", rightCurveX, centerY, r3, isLeft: false);
        AddSemicircle(layout, "R-R2", "R2", "Middle", rightCurveX, centerY, r2, isLeft: false);
        AddSemicircle(layout, "R-R1", "R1", "Inner", rightCurveX, centerY, r1, isLeft: false);

        // ============================================
        // G62 EXTENSIONS (outer ring only)
        // ============================================
        // Left side
        AddG62Extension(layout, "G62-L-U", track1Y, leftCurveX - g62Extension, leftCurveX);
        AddG62Extension(layout, "G62-L-L", track7Y, leftCurveX - g62Extension, leftCurveX);
        // Right side
        AddG62Extension(layout, "G62-R-U", track1Y, rightCurveX, rightCurveX + g62Extension);
        AddG62Extension(layout, "G62-R-L", track7Y, rightCurveX, rightCurveX + g62Extension);

        // ============================================
        // UPPER STATION TRACKS (from AnyRail screenshot)
        // ============================================

        // Track 1 (outermost): R3 - WR - G231 - G231 - G231 - G239 - G231 - WL - R3
        AddStraightTrack(layout, "U1", "1", track1Y, leftCurveX, rightCurveX,
            ["WR", "G231", "G231", "G231", "G239", "G231", "WL"]);

        // Track 2: R2 - G231 - W3 - WR - G231 - G231 - W3 - G231 - R2
        AddStraightTrack(layout, "U2", "2", track2Y, leftCurveX, rightCurveX,
            ["G231", "W3", "WR", "G231", "G231", "W3", "G231"]);

        // Track 3: R1 - WL - G231 - G231 - DKW - G231 - G231 - WR - R1
        AddStraightTrack(layout, "U3", "3", track3Y, leftCurveX, rightCurveX,
            ["WL", "G231", "G231", "DKW", "G231", "G231", "WR"]);

        // Track 4 (innermost, no curves): G62 - G231 - G231 - G62 - G119 - G239 - WR - G119 - G62 - G231 - G62
        AddStraightTrack(layout, "U4", "4", track4Y, leftCurveX, rightCurveX,
            ["G62", "G231", "G231", "G62", "G119", "G239", "WR", "G119", "G62", "G231", "G62"]);

        // ============================================
        // LOWER STATION TRACKS (from AnyRail screenshot)
        // ============================================

        // Track 5 (innermost): R1 - G231 - G239 - G231 - G239 - G231 - G239 - G231 - R1
        AddStraightTrack(layout, "L1", "5", track5Y, leftCurveX, rightCurveX,
            ["G231", "G239", "G231", "G239", "G231", "G239", "G231"]);

        // Track 6: R2 - G231 - G239 - G231 - G239 - G231 - G239 - G231 - R2
        AddStraightTrack(layout, "L2", "6", track6Y, leftCurveX, rightCurveX,
            ["G231", "G239", "G231", "G239", "G231", "G239", "G231"]);

        // Track 7 (outermost): R3 - G231 - G239 - G231 - G239 - G231 - G239 - G231 - R3
        AddStraightTrack(layout, "L3", "7", track7Y, leftCurveX, rightCurveX,
            ["G231", "G239", "G231", "G239", "G231", "G239", "G231"]);

        return layout;
    }

    /// <summary>
    /// Adds a G62 extension piece (straight segment).
    /// </summary>
    private static void AddG62Extension(TrackLayout layout, string id, double y, double startX, double endX)
    {
        layout.Segments.Add(new TrackSegment
        {
            Id = id,
            Name = $"G62 Extension",
            Type = TrackSegmentType.Straight,
            ArticleCode = "G62",
            Layer = "Extension",
            PathData = $"M {startX:F0},{y:F0} L {endX:F0},{y:F0}",
            CenterX = (startX + endX) / 2,
            CenterY = y
        });
    }

    /// <summary>
    /// Adds a 180° semicircle as 6 individual 30° arc segments.
    /// </summary>
    private static void AddSemicircle(
        TrackLayout layout,
        string curveId,
        string radiusName,
        string layer,
        double centerX,
        double centerY,
        double radius,
        bool isLeft)
    {
        const int segmentCount = 6;
        const double anglePerSegment = 30.0;

        for (int i = 0; i < segmentCount; i++)
        {
            double startAngle, endAngle;

            if (isLeft)
            {
                // Left curves: 90° → -90° (top to bottom, counterclockwise in math coords)
                startAngle = 90 - i * anglePerSegment;
                endAngle = startAngle - anglePerSegment;
            }
            else
            {
                // Right curves: 90° → 270° (top to bottom, clockwise in math coords)
                startAngle = 90 + i * anglePerSegment;
                endAngle = startAngle + anglePerSegment;
            }

            double startRad = startAngle * Math.PI / 180;
            double endRad = endAngle * Math.PI / 180;

            // Canvas coordinates (Y inverted)
            double x1 = centerX + radius * Math.Cos(startRad);
            double y1 = centerY - radius * Math.Sin(startRad);
            double x2 = centerX + radius * Math.Cos(endRad);
            double y2 = centerY - radius * Math.Sin(endRad);

            // SVG arc sweep flag
            int sweepFlag = isLeft ? 1 : 0;

            layout.Segments.Add(new TrackSegment
            {
                Id = $"CURVE-{curveId}-{i + 1:D2}",
                Name = $"{(isLeft ? "Left" : "Right")} {radiusName} #{i + 1}",
                Type = TrackSegmentType.Curve,
                ArticleCode = radiusName,
                Layer = layer,
                PathData = $"M {x1:F1},{y1:F1} A {radius:F1},{radius:F1} 0 0 {sweepFlag} {x2:F1},{y2:F1}",
                CenterX = (x1 + x2) / 2,
                CenterY = (y1 + y2) / 2
            });
        }
    }

    /// <summary>
    /// Adds straight track segments based on piece names from AnyRail.
    /// </summary>
    private static void AddStraightTrack(
        TrackLayout layout,
        string trackId,
        string trackNumber,
        double y,
        double startX,
        double endX,
        string[] pieceNames)
    {
        // Piko piece lengths in mm
        var lengths = new Dictionary<string, double>
        {
            ["G239"] = 239,
            ["G231"] = 231,
            ["G119"] = 119,
            ["G62"] = 62,
            ["WL"] = 239,
            ["WR"] = 239,
            ["W3"] = 239,
            ["DKW"] = 239,
        };

        // Calculate total length
        double totalLength = 0;
        foreach (var name in pieceNames)
        {
            totalLength += lengths.GetValueOrDefault(name, 231);
        }

        // Scale to fit
        double availableLength = endX - startX;
        double scaleFactor = availableLength / totalLength;

        double currentX = startX;
        for (int i = 0; i < pieceNames.Length; i++)
        {
            string name = pieceNames[i];
            double pieceLength = lengths.GetValueOrDefault(name, 231) * scaleFactor;
            double nextX = currentX + pieceLength;

            var segmentType = name switch
            {
                "WL" or "WR" => TrackSegmentType.Switch,
                "W3" => TrackSegmentType.ThreeWaySwitch,
                "DKW" => TrackSegmentType.DoubleCrossover,
                _ => TrackSegmentType.Straight
            };

            string pathData = BuildPathData(name, segmentType, currentX, nextX, y, pieceLength);

            layout.Segments.Add(new TrackSegment
            {
                Id = $"ST-{trackId}-{i + 1:D2}",
                Name = $"Track {trackNumber} {name} #{i + 1}",
                Type = segmentType,
                ArticleCode = name,
                Layer = "Station",
                TrackNumber = trackNumber,
                PathData = pathData,
                CenterX = (currentX + nextX) / 2,
                CenterY = y
            });

            currentX = nextX;
        }
    }

    /// <summary>
    /// Builds SVG path data for different track piece types.
    /// </summary>
    private static string BuildPathData(string pieceName, TrackSegmentType type, double x1, double x2, double y, double length)
    {
        return type switch
        {
            TrackSegmentType.Switch => BuildSwitchPath(pieceName, x1, x2, y),
            TrackSegmentType.ThreeWaySwitch => BuildThreeWaySwitchPath(x1, x2, y),
            TrackSegmentType.DoubleCrossover => BuildDKWPath(x1, x2, y, length),
            _ => $"M {x1:F0},{y:F0} L {x2:F0},{y:F0}"
        };
    }

    private static string BuildSwitchPath(string name, double x1, double x2, double y)
    {
        double midX = (x1 + x2) / 2;
        double divergeY = name == "WL" ? y + 12 : y - 12;
        return $"M {x1:F0},{y:F0} L {x2:F0},{y:F0} M {midX:F0},{y:F0} L {x2:F0},{divergeY:F0}";
    }

    private static string BuildThreeWaySwitchPath(double x1, double x2, double y)
    {
        double midX = (x1 + x2) / 2;
        return $"M {x1:F0},{y:F0} L {x2:F0},{y:F0} " +
               $"M {midX:F0},{y:F0} L {x2:F0},{y - 12:F0} " +
               $"M {midX:F0},{y:F0} L {x2:F0},{y + 12:F0}";
    }

    private static string BuildDKWPath(double x1, double x2, double y, double length)
    {
        double offset = length * 0.15;
        return $"M {x1:F0},{y:F0} L {x2:F0},{y:F0} " +
               $"M {x1 + offset:F0},{y:F0} L {x2 - offset:F0},{y + 12:F0} " +
               $"M {x1 + offset:F0},{y + 12:F0} L {x2 - offset:F0},{y:F0}";
    }
}
