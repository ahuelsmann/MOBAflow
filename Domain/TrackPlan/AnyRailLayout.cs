// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.TrackPlan;

using System.Globalization;
using System.Text;
using System.Xml.Linq;

/// <summary>
/// Represents a track layout imported from AnyRail XML format.
/// </summary>
public class AnyRailLayout
{
    public double Width { get; set; }
    public double Height { get; set; }
    public double ScaleX { get; set; }
    public double ScaleY { get; set; }
    public List<AnyRailPart> Parts { get; } = [];

    /// <summary>
    /// Parses an AnyRail XML file and returns an AnyRailLayout.
    /// </summary>
    /// <param name="xmlPath">Path to the AnyRail XML file.</param>
    /// <returns>Parsed AnyRailLayout instance.</returns>
    public static AnyRailLayout Parse(string xmlPath)
    {
        var xdoc = XDocument.Load(xmlPath);
        var layoutEl = xdoc.Root!;
        var layout = new AnyRailLayout
        {
            Width = double.Parse(layoutEl.Attribute("width")?.Value ?? "0", CultureInfo.InvariantCulture),
            Height = double.Parse(layoutEl.Attribute("height")?.Value ?? "0", CultureInfo.InvariantCulture),
            ScaleX = double.Parse(layoutEl.Attribute("scaleX")?.Value ?? "1", CultureInfo.InvariantCulture),
            ScaleY = double.Parse(layoutEl.Attribute("scaleY")?.Value ?? "1", CultureInfo.InvariantCulture),
        };

        foreach (var p in layoutEl.Element("parts")!.Elements("part"))
        {
            var part = new AnyRailPart
            {
                Id = (string?)p.Attribute("id") ?? string.Empty,
                Type = (string?)p.Attribute("type")
            };
            var drawing = p.Element("drawing");
            if (drawing != null)
            {
                foreach (var ln in drawing.Elements("line"))
                    part.Lines.Add(new AnyRailLine
                    {
                        Pt1 = ParsePoint(ln.Attribute("pt1")?.Value),
                        Pt2 = ParsePoint(ln.Attribute("pt2")?.Value)
                    });

                foreach (var arc in drawing.Elements("arc"))
                    part.Arcs.Add(new AnyRailArc
                    {
                        Pt1 = ParsePoint(arc.Attribute("pt1")?.Value),
                        Pt2 = ParsePoint(arc.Attribute("pt2")?.Value),
                        Direction = int.Parse(arc.Attribute("direction")?.Value ?? "0", CultureInfo.InvariantCulture),
                        Angle = double.Parse(arc.Attribute("angle")?.Value ?? "0", CultureInfo.InvariantCulture),
                        Radius = double.Parse(arc.Attribute("radius")?.Value ?? "0", CultureInfo.InvariantCulture)
                    });
            }
            layout.Parts.Add(part);
        }
        return layout;
    }

    private static (double X, double Y) ParsePoint(string? s)
    {
        var parts = (s ?? "0,0").Split(',');
        return (double.Parse(parts[0], CultureInfo.InvariantCulture),
                double.Parse(parts[1], CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// Represents a single part (track piece) in the AnyRail layout.
/// </summary>
public class AnyRailPart
{
    /// <summary>
    /// Unique identifier from AnyRail (e.g., "401", "68").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    public string? Type { get; set; }
    public List<AnyRailLine> Lines { get; } = [];
    public List<AnyRailArc> Arcs { get; } = [];

    /// <summary>
    /// Derives the track article code (e.g., "R1", "R2", "G231") from the part's geometry.
    /// Based on Piko A-Gleis H0 specifications.
    /// </summary>
    /// <returns>Article code string like "R1", "R2", "R3", "G231", "WL", "WR", etc.</returns>
    public string GetArticleCode()
    {
        return Type switch
        {
            "Straight" => GetStraightArticleCode(),
            "Curve" => GetCurveArticleCode(),
            "RightRegularTurnout" => "WR",
            "LeftRegularTurnout" => "WL",
            "ThreewayTurnout" => "DWW",
            "DoubleSlipswitch" => "DKW",
            _ => Type ?? "Unknown"
        };
    }

    private string GetStraightArticleCode()
    {
        // Calculate length from first line
        if (Lines.Count == 0)
            return "G";

        var line = Lines[0];
        var length = Math.Sqrt(
            Math.Pow(line.Pt2.X - line.Pt1.X, 2) +
            Math.Pow(line.Pt2.Y - line.Pt1.Y, 2));

        // Piko A-Gleis lengths (approximate values from XML coordinates)
        // G231 = 231mm → ~350 units in XML
        // G119 = 119mm → ~180 units
        // G62 = 62mm → ~94 units
        return length switch
        {
            < 100 => "G62",
            < 200 => "G119",
            _ => "G231"
        };
    }

    private string GetCurveArticleCode()
    {
        // Determine curve radius from first arc
        if (Arcs.Count == 0)
            return "R";

        var radius = Arcs[0].Radius;

        // Piko A-Gleis curve radii (values from XML)
        // R1 ≈ 545 units (360mm real)
        // R2 ≈ 638 units (422mm real)
        // R3 ≈ 732 units (484mm real)
        // R9 ≈ 1374 units (908mm real - used in turnouts)
        return radius switch
        {
            < 600 => "R1",
            < 700 => "R2",
            < 800 => "R3",
            < 1000 => "R4",
            _ => "R9"
        };
    }

    /// <summary>
    /// Determines the TrackSegmentType based on the part's AnyRail type and geometry.
    /// Maps AnyRail types to Piko A-Gleis track segment types.
    /// </summary>
    /// <returns>Corresponding TrackSegmentType.</returns>
    public TrackSegmentType GetTrackSegmentType()
    {
        return Type switch
        {
            "Straight" => TrackSegmentType.Straight,
            "Curve" => TrackSegmentType.Curve,
            "RightRegularTurnout" => TrackSegmentType.TurnoutRight,
            "LeftRegularTurnout" => TrackSegmentType.TurnoutLeft,
            "ThreewayTurnout" => TrackSegmentType.ThreeWaySwitch,
            "DoubleSlipswitch" => TrackSegmentType.DoubleCrossover,
            // Curved turnouts (determined by geometry if not explicit in type)
            _ => DetermineTypeFromGeometry()
        };
    }

    /// <summary>
    /// Fallback: Determine track type from geometry (lines vs arcs).
    /// </summary>
    private TrackSegmentType DetermineTypeFromGeometry()
    {
        // If mostly arcs, it's a curve
        if (Arcs.Count > Lines.Count)
            return TrackSegmentType.Curve;

        // Otherwise assume straight
        return TrackSegmentType.Straight;
    }

    /// <summary>
    /// Generates SVG path data string for this part's drawing elements.
    /// </summary>
    /// <returns>SVG path data string (M, L, A commands).</returns>
    public string ToPathData()
    {
        var sb = new StringBuilder();

        // Process lines
        // Note: XAML Path uses space-separated coordinates: "M x y L x y"
        foreach (var line in Lines)
        {
            sb.Append(CultureInfo.InvariantCulture, $"M {line.Pt1.X:F0} {line.Pt1.Y:F0} ");
            sb.Append(CultureInfo.InvariantCulture, $"L {line.Pt2.X:F0} {line.Pt2.Y:F0} ");
        }

        // Process arcs (SVG arc: A rx ry x-axis-rotation large-arc-flag sweep-flag x y)
        foreach (var arc in Arcs)
        {
            sb.Append(CultureInfo.InvariantCulture, $"M {arc.Pt1.X:F0} {arc.Pt1.Y:F0} ");
            
            // Determine arc flags based on direction and angle
            // large-arc-flag: 1 if angle > 180°, else 0
            var largeArc = Math.Abs(arc.Angle) > 180 ? 1 : 0;
            
            // sweep-flag: Determines arc direction (0 = counter-clockwise, 1 = clockwise)
            // AnyRail direction is in degrees (0-360)
            // For directions 0-180: sweep = 0 (counter-clockwise, curves outward for top)
            // For directions 180-360: sweep = 0 as well (curves outward for bottom)
            // The sweep flag should be 0 for all curves to curve outward from the center
            var sweep = 0;
            
            // SVG arc format: A rx ry x-axis-rotation large-arc-flag sweep-flag x y
            sb.Append(CultureInfo.InvariantCulture, $"A {arc.Radius:F0} {arc.Radius:F0} 0 {largeArc} {sweep} {arc.Pt2.X:F0} {arc.Pt2.Y:F0} ");
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Calculates the center point of this part for label placement.
    /// </summary>
    /// <returns>Center coordinates (X, Y).</returns>
    public (double X, double Y) GetCenter()
    {
        var allPoints = new List<(double X, double Y)>();

        foreach (var line in Lines)
        {
            allPoints.Add(line.Pt1);
            allPoints.Add(line.Pt2);
        }

        foreach (var arc in Arcs)
        {
            allPoints.Add(arc.Pt1);
            allPoints.Add(arc.Pt2);
        }

        if (allPoints.Count == 0)
            return (0, 0);

        var avgX = allPoints.Average(p => p.X);
        var avgY = allPoints.Average(p => p.Y);
        return (avgX, avgY);
    }
}

/// <summary>
/// Represents a straight line segment in an AnyRail part drawing.
/// </summary>
public class AnyRailLine
{
    public (double X, double Y) Pt1 { get; set; }
    public (double X, double Y) Pt2 { get; set; }
}

/// <summary>
/// Represents an arc segment in an AnyRail part drawing.
/// </summary>
public class AnyRailArc
{
    public (double X, double Y) Pt1 { get; set; }
    public (double X, double Y) Pt2 { get; set; }
    public int Direction { get; set; }
    public double Angle { get; set; }
    public double Radius { get; set; }
}
