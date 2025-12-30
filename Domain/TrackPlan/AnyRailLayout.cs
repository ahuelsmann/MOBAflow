// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.TrackPlan;

using System.Globalization;
using System.Xml.Linq;
using System.Diagnostics;

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
    public List<AnyRailEndpoint> Endpoints { get; } = [];
    public List<AnyRailConnection> Connections { get; } = [];

    /// <summary>
    /// Parses an AnyRail XML file asynchronously and returns an AnyRailLayout.
    /// </summary>
    public static async Task<AnyRailLayout> ParseAsync(string xmlPath, CancellationToken cancellationToken = default)
    {
        using var reader = File.OpenText(xmlPath);
        var xdoc = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken).ConfigureAwait(false);
        var layoutEl = xdoc.Root!;
        var layout = new AnyRailLayout
        {
            Width = double.Parse(layoutEl.Attribute("width")?.Value ?? "0", CultureInfo.InvariantCulture),
            Height = double.Parse(layoutEl.Attribute("height")?.Value ?? "0", CultureInfo.InvariantCulture),
            ScaleX = double.Parse(layoutEl.Attribute("scaleX")?.Value ?? "1", CultureInfo.InvariantCulture),
            ScaleY = double.Parse(layoutEl.Attribute("scaleY")?.Value ?? "1", CultureInfo.InvariantCulture),
        };

        // Parse endpoints first (needed for connection resolution)
        var endpointsEl = layoutEl.Element("endpoints");
        if (endpointsEl != null)
        {
            foreach (var ep in endpointsEl.Elements("endpoint"))
            {
                var coord = ParseCoord3(ep.Attribute("coord")?.Value);
                layout.Endpoints.Add(new AnyRailEndpoint
                {
                    Nr = int.Parse(ep.Attribute("nr")?.Value ?? "0", CultureInfo.InvariantCulture),
                    X = coord.X,
                    Y = coord.Y,
                    Direction = int.Parse(ep.Attribute("direction")?.Value ?? "0", CultureInfo.InvariantCulture)
                });
            }
        }

        // Parse parts with their endpoint numbers
        foreach (var p in layoutEl.Element("parts")!.Elements("part"))
        {
            var part = new AnyRailPart
            {
                Id = (string?)p.Attribute("id") ?? string.Empty,
                Type = (string?)p.Attribute("type")
            };

            // Parse endpoint numbers for this part
            var endpointNrsEl = p.Element("endpointNrs");
            if (endpointNrsEl != null)
            {
                foreach (var epNr in endpointNrsEl.Elements("endpointNr"))
                {
                    part.EndpointNrs.Add(int.Parse(epNr.Value, CultureInfo.InvariantCulture));
                }
            }

            // Parse drawing elements
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

        // Parse connections (endpoint to endpoint)
        var connectionsEl = layoutEl.Element("connections");
        if (connectionsEl != null)
        {
            foreach (var conn in connectionsEl.Elements("connection"))
            {
                layout.Connections.Add(new AnyRailConnection
                {
                    Endpoint1 = int.Parse(conn.Attribute("endpoint1")?.Value ?? "0", CultureInfo.InvariantCulture),
                    Endpoint2 = int.Parse(conn.Attribute("endpoint2")?.Value ?? "0", CultureInfo.InvariantCulture)
                });
            }
        }

        return layout;
    }

    /// <summary>
    /// Parses an AnyRail XML file and returns an AnyRailLayout (synchronous wrapper for backward compatibility).
    /// </summary>
    [Obsolete("Use ParseAsync instead for better performance and non-blocking I/O")]
    public static AnyRailLayout Parse(string xmlPath) => ParseAsync(xmlPath).GetAwaiter().GetResult();

    /// <summary>
    /// Converts AnyRail connections (endpoint-to-endpoint) to TrackConnections (segment-to-segment).
    /// </summary>
    public List<TrackConnection> ToTrackConnections()
    {
        var result = new List<TrackConnection>();

        // Build endpoint-to-parts lookup (support multiple parts per endpoint)
        var endpointToParts = new Dictionary<int, List<(string PartId, int EndpointIndex)>>();
        foreach (var part in Parts)
        {
            for (int i = 0; i < part.EndpointNrs.Count; i++)
            {
                var ep = part.EndpointNrs[i];
                if (!endpointToParts.TryGetValue(ep, out var list))
                {
                    list = new List<(string, int)>();
                    endpointToParts[ep] = list;
                }
                endpointToParts[ep].Add((part.Id, i));
            }
        }

        Debug.WriteLine($"AnyRail: Parts={Parts.Count}, Endpoints={Endpoints.Count}, Connections={Connections.Count}");

        // Build endpoint number -> coordinate lookup for fallback matching
        var endpointCoords = Endpoints.ToDictionary(e => e.Nr, e => (X: e.X, Y: e.Y));

        // Convert each connection (endpoint-to-endpoint) to segment-to-segment connections
        foreach (var conn in Connections)
        {
            var has1 = endpointToParts.TryGetValue(conn.Endpoint1, out var list1);
            var has2 = endpointToParts.TryGetValue(conn.Endpoint2, out var list2);

            // If mapping missing for either endpoint, attempt coordinate-based fallback
            if (!has1 || !has2)
            {
                Debug.WriteLine($"⚠️ ToTrackConnections: Missing mapping for endpoints {conn.Endpoint1} or {conn.Endpoint2} - attempting coordinate fallback");

                const double tolerance = 1.0; // units in AnyRail coordinates

                if (!has1)
                {
                    // Try to find parts whose endpoint coordinates are close to the referenced endpoint
                    if (endpointCoords.TryGetValue(conn.Endpoint1, out var coord1))
                    {
                        var matches = new List<(string PartId, int EndpointIndex, double Dist)>();
                        foreach (var part in Parts)
                        {
                            for (int i = 0; i < part.EndpointNrs.Count; i++)
                            {
                                var epNr = part.EndpointNrs[i];
                                if (endpointCoords.TryGetValue(epNr, out var epCoord))
                                {
                                    var dx = epCoord.X - coord1.X;
                                    var dy = epCoord.Y - coord1.Y;
                                    var dist = Math.Sqrt(dx * dx + dy * dy);
                                    if (dist <= tolerance)
                                    {
                                        matches.Add((part.Id, i, dist));
                                    }
                                }
                            }
                        }

                        if (matches.Count > 0)
                        {
                            // Order by distance and use matches as list1
                            list1 = matches.OrderBy(m => m.Dist).Select(m => (m.PartId, m.EndpointIndex)).ToList();
                            has1 = true;
                            Debug.WriteLine($"🔁 ToTrackConnections: Fallback matched endpoint {conn.Endpoint1} to parts: {string.Join(',', list1.Select(x=>$"{x.PartId}.ep{x.EndpointIndex}"))}");
                        }
                        else
                        {
                            Debug.WriteLine($"⚠️ ToTrackConnections: Fallback found no match for endpoint {conn.Endpoint1} at ({coord1.X},{coord1.Y})");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"⚠️ ToTrackConnections: No coordinate found for endpoint {conn.Endpoint1} in endpoints list");
                    }
                }

                if (!has2)
                {
                    if (endpointCoords.TryGetValue(conn.Endpoint2, out var coord2))
                    {
                        var matches = new List<(string PartId, int EndpointIndex, double Dist)>();
                        foreach (var part in Parts)
                        {
                            for (int i = 0; i < part.EndpointNrs.Count; i++)
                            {
                                var epNr = part.EndpointNrs[i];
                                if (endpointCoords.TryGetValue(epNr, out var epCoord))
                                {
                                    var dx = epCoord.X - coord2.X;
                                    var dy = epCoord.Y - coord2.Y;
                                    var dist = Math.Sqrt(dx * dx + dy * dy);
                                    if (dist <= tolerance)
                                    {
                                        matches.Add((part.Id, i, dist));
                                    }
                                }
                            }
                        }

                        if (matches.Count > 0)
                        {
                            list2 = matches.OrderBy(m => m.Dist).Select(m => (m.PartId, m.EndpointIndex)).ToList();
                            has2 = true;
                            Debug.WriteLine($"🔁 ToTrackConnections: Fallback matched endpoint {conn.Endpoint2} to parts: {string.Join(',', list2.Select(x=>$"{x.PartId}.ep{x.EndpointIndex}"))}");
                        }
                        else
                        {
                            Debug.WriteLine($"⚠️ ToTrackConnections: Fallback found no match for endpoint {conn.Endpoint2} at ({coord2.X},{coord2.Y})");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"⚠️ ToTrackConnections: No coordinate found for endpoint {conn.Endpoint2} in endpoints list");
                    }
                }

                // If still missing after fallback, skip this connection
                if (!has1 || !has2)
                {
                    Debug.WriteLine($"⚠️ ToTrackConnections: Skipping connection {conn.Endpoint1} <-> {conn.Endpoint2} after fallback attempts");
                    continue;
                }
            }

            // If multiple parts map to an endpoint, produce all pairwise connections
            foreach (var p1 in list1)
            {
                foreach (var p2 in list2)
                {
                    // Avoid connecting the same part endpoint to itself
                    if (p1.PartId == p2.PartId && p1.EndpointIndex == p2.EndpointIndex)
                        continue;

                    result.Add(new TrackConnection
                    {
                        Segment1Id = p1.PartId,
                        Segment1EndpointIndex = p1.EndpointIndex,
                        Segment2Id = p2.PartId,
                        Segment2EndpointIndex = p2.EndpointIndex
                    });
                    Debug.WriteLine($"🔗 ToTrackConnections: {p1.PartId}.ep{p1.EndpointIndex} <-> {p2.PartId}.ep{p2.EndpointIndex}");
                }
            }
        }

        // Deduplicate identical connections (unordered)
        var unique = new List<TrackConnection>();
        var seen = new HashSet<string>();
        foreach (var c in result)
        {
            // Create an order-independent key
            var key = string.CompareOrdinal(c.Segment1Id, c.Segment2Id) <= 0
                ? $"{c.Segment1Id}:{c.Segment1EndpointIndex}-{c.Segment2Id}:{c.Segment2EndpointIndex}"
                : $"{c.Segment2Id}:{c.Segment2EndpointIndex}-{c.Segment1Id}:{c.Segment1EndpointIndex}";
            if (seen.Add(key))
            {
                unique.Add(c);
            }
        }

        Debug.WriteLine($"ToTrackConnections: converted {unique.Count} connections (raw {result.Count})");
        return unique;
    }

    private static (double X, double Y) ParsePoint(string? s)
    {
        var parts = (s ?? "0,0").Split(',');
        return (double.Parse(parts[0], CultureInfo.InvariantCulture),
                double.Parse(parts[1], CultureInfo.InvariantCulture));
    }

    private static (double X, double Y, double Z) ParseCoord3(string? s)
    {
        var parts = (s ?? "0,0,0").Split(',');
        return (double.Parse(parts[0], CultureInfo.InvariantCulture),
                double.Parse(parts[1], CultureInfo.InvariantCulture),
                parts.Length > 2 ? double.Parse(parts[2], CultureInfo.InvariantCulture) : 0);
    }
}

/// <summary>
/// AnyRail endpoint with coordinates and direction.
/// </summary>
public class AnyRailEndpoint
{
    public int Nr { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public int Direction { get; set; }
}

/// <summary>
/// AnyRail connection between two endpoints.
/// </summary>
public class AnyRailConnection
{
    public int Endpoint1 { get; set; }
    public int Endpoint2 { get; set; }
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
    
    /// <summary>
    /// Endpoint numbers assigned to this part.
    /// Index 0 = first endpoint, Index 1 = second endpoint, etc.
    /// </summary>
    public List<int> EndpointNrs { get; } = [];
    
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

    /// <summary>
    /// Gets all unique endpoints of this part (for connection detection).
    /// </summary>
    /// <returns>List of endpoint coordinates.</returns>
    public List<(double X, double Y)> GetEndpoints()
    {
        var endpoints = new HashSet<(int X, int Y)>();

        foreach (var line in Lines)
        {
            endpoints.Add(((int)Math.Round(line.Pt1.X), (int)Math.Round(line.Pt1.Y)));
            endpoints.Add(((int)Math.Round(line.Pt2.X), (int)Math.Round(line.Pt2.Y)));
        }

        foreach (var arc in Arcs)
        {
            endpoints.Add(((int)Math.Round(arc.Pt1.X), (int)Math.Round(arc.Pt1.Y)));
            endpoints.Add(((int)Math.Round(arc.Pt2.X), (int)Math.Round(arc.Pt2.Y)));
        }

        return endpoints.Select(e => ((double)e.X, (double)e.Y)).ToList();
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
