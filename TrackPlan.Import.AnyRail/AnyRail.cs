// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Import;

using Domain;

using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;

/// <summary>
/// Represents a track layout imported from AnyRail XML format.
/// </summary>
public class AnyRail
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
    public static async Task<AnyRail> ParseAsync(string xmlPath, CancellationToken cancellationToken = default)
    {
        using var reader = File.OpenText(xmlPath);
        var xdoc = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken).ConfigureAwait(false);
        var layoutEl = xdoc.Root!;
        var layout = new AnyRail
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
                            list1 = matches.OrderBy(m => m.Dist).Select(m => (m.PartId, m.EndpointIndex)).ToList();
                            has1 = true;
                            Debug.WriteLine($"🔁 ToTrackConnections: Fallback matched endpoint {conn.Endpoint1} to parts: {string.Join(',', list1.Select(x => $"{x.PartId}.ep{x.EndpointIndex}"))}");
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
                            Debug.WriteLine($"🔁 ToTrackConnections: Fallback matched endpoint {conn.Endpoint2} to parts: {string.Join(',', list2.Select(x => $"{x.PartId}.ep{x.EndpointIndex}"))}");
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
                if (!has1 || !has2 || list1 is null || list2 is null)
                {
                    Debug.WriteLine($"WARN ToTrackConnections: Skipping connection {conn.Endpoint1} <-> {conn.Endpoint2} after fallback attempts");
                    continue;
                }
            }

            var mappedList1 = list1 ?? [];
            var mappedList2 = list2 ?? [];

            // If multiple parts map to an endpoint, produce all pairwise connections
            foreach (var p1 in mappedList1)
            {
                foreach (var p2 in mappedList2)
                {
                    if (p1.PartId == p2.PartId && p1.EndpointIndex == p2.EndpointIndex)
                        continue;

                    result.Add(new TrackConnection
                    {
                        Segment1Id = p1.PartId,
                        Segment1ConnectorIndex = p1.EndpointIndex,
                        Segment2Id = p2.PartId,
                        Segment2ConnectorIndex = p2.EndpointIndex
                    });
                    Debug.WriteLine($"🔗 ToTrackConnections: {p1.PartId}.ep{p1.EndpointIndex} <-> {p2.PartId}.ep{p2.EndpointIndex}");
                }
            }
        }

        var unique = new List<TrackConnection>();
        var seen = new HashSet<string>();
        foreach (var c in result)
        {
            var key = string.CompareOrdinal(c.Segment1Id, c.Segment2Id) <= 0
                ? $"{c.Segment1Id}:{c.Segment1ConnectorIndex}-{c.Segment2Id}:{c.Segment2ConnectorIndex}"
                : $"{c.Segment2Id}:{c.Segment2ConnectorIndex}-{c.Segment1Id}:{c.Segment1ConnectorIndex}";
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

public class AnyRailEndpoint
{
    public int Nr { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public int Direction { get; set; }
}

public class AnyRailConnection
{
    public int Endpoint1 { get; set; }
    public int Endpoint2 { get; set; }
}

public class AnyRailPart
{
    public string Id { get; set; } = string.Empty;
    public string? Type { get; set; }
    public List<int> EndpointNrs { get; } = [];
    public List<AnyRailLine> Lines { get; } = [];
    public List<AnyRailArc> Arcs { get; } = [];

    public double GetX()
    {
        if (Lines.Count > 0)
            return Lines[0].Pt1.X;
        if (Arcs.Count > 0)
            return Arcs[0].Pt1.X;
        return 0;
    }

    public double GetY()
    {
        if (Lines.Count > 0)
            return Lines[0].Pt1.Y;
        if (Arcs.Count > 0)
            return Arcs[0].Pt1.Y;
        return 0;
    }

    public double GetRotation()
    {
        if (Lines.Count > 0)
        {
            var line = Lines[0];
            var dx = line.Pt2.X - line.Pt1.X;
            var dy = line.Pt2.Y - line.Pt1.Y;
            var angleRad = Math.Atan2(dy, dx);
            var angleDeg = angleRad * 180.0 / Math.PI;
            return NormalizeAngle(angleDeg);
        }

        if (Arcs.Count > 0)
        {
            var arc = Arcs[0];
            var dx = arc.Pt2.X - arc.Pt1.X;
            var dy = arc.Pt2.Y - arc.Pt1.Y;
            var angleRad = Math.Atan2(dy, dx);
            var angleDeg = angleRad * 180.0 / Math.PI;
            return NormalizeAngle(angleDeg);
        }

        return 0;
    }

    public string GetArticleCode()
    {
        return Type switch
        {
            "Straight" => GetStraightArticleCode(),
            "Curve" => GetCurveArticleCode(),
            "RightRegularTurnout" => "WR",
            "LeftRegularTurnout" => "WL",
            "ThreewayTurnout" => "W3",
            "DoubleSlipswitch" => "DKW",
            _ => Type ?? "Unknown"
        };
    }

    private string GetStraightArticleCode()
    {
        if (Lines.Count == 0)
            return "G";

        var line = Lines[0];
        var length = Math.Sqrt(
            Math.Pow(line.Pt2.X - line.Pt1.X, 2) +
            Math.Pow(line.Pt2.Y - line.Pt1.Y, 2));

        return length switch
        {
            < 100 => "G62",
            < 200 => "G119",
            _ => "G231"
        };
    }

    private string GetCurveArticleCode()
    {
        if (Arcs.Count == 0)
            return "R";

        var radius = Arcs[0].Radius;
        var angle = Arcs[0].Angle;

        // 15° = R9 (Weichengegenbogen)
        if (Math.Abs(angle - 15) < 2)
            return "R9";

        // AnyRail uses centerline radius, not nominal Piko radius
        if (Math.Abs(radius - 454) < 20) return "R1";   // Piko R1
        if (Math.Abs(radius - 515) < 20) return "R2";   // Piko R2
        if (Math.Abs(radius - 577) < 20) return "R3";   // Piko R3
        if (Math.Abs(radius - 639) < 20) return "R4";   // Piko R4

        // Fallback for unusual radii
        return radius switch
        {
            < 480 => "R1",
            < 540 => "R2",
            < 600 => "R3",
            < 700 => "R4",
            _ => "R9"
        };
    }

    public string ToPathData()
    {
        var sb = new System.Text.StringBuilder();

        foreach (var line in Lines)
        {
            sb.Append($"M {line.Pt1.X},{line.Pt1.Y} L {line.Pt2.X},{line.Pt2.Y} ");
        }

        foreach (var arc in Arcs)
        {
            var sweepFlag = 0;
            var largeArcFlag = arc.Angle > 180 ? 1 : 0;
            sb.Append($"M {arc.Pt1.X},{arc.Pt1.Y} A {arc.Radius},{arc.Radius} 0 {largeArcFlag} {sweepFlag} {arc.Pt2.X},{arc.Pt2.Y} ");
        }

        return sb.ToString().Trim();
    }

    private static double NormalizeAngle(double degrees)
    {
        var result = degrees % 360.0;
        if (result < 0) result += 360.0;
        return result;
    }

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
            _ => DetermineTypeFromGeometry()
        };
    }

    private TrackSegmentType DetermineTypeFromGeometry()
    {
        if (Arcs.Count > Lines.Count)
            return TrackSegmentType.Curve;

        return TrackSegmentType.Straight;
    }

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

public class AnyRailLine
{
    public (double X, double Y) Pt1 { get; set; }
    public (double X, double Y) Pt2 { get; set; }
}

public class AnyRailArc
{
    public (double X, double Y) Pt1 { get; set; }
    public (double X, double Y) Pt2 { get; set; }
    public int Direction { get; set; }
    public double Angle { get; set; }
    public double Radius { get; set; }
}
