// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Converter;

using Domain;

/// <summary>
/// Converts AnyRail-imported track layout (with coordinates) to pure topology (ArticleCode + Connections only).
/// After conversion, original coordinates/Lines/Arcs are discarded - renderer calculates everything from topology.
/// </summary>
public static class TopologyConverter
{
    public static TrackLayout ToPureTopology(TrackLayout anyRailLayout)
    {
        return new TrackLayout
        {
            Name = anyRailLayout.Name,
            Description = $"{anyRailLayout.Description} (Pure Topology)",
            TrackSystem = anyRailLayout.TrackSystem,
            Segments = anyRailLayout.Segments.Select(s => new TrackSegment
            {
                Id = s.Id,
                ArticleCode = s.ArticleCode,
                AssignedInPort = s.AssignedInPort,
                Name = s.Name,
                Layer = s.Layer
            }).ToList(),
            Connections = anyRailLayout.Connections.ToList()
        };
    }

    public static ConversionStats GetConversionStats(TrackLayout before, TrackLayout after)
    {
        return new ConversionStats
        {
            SegmentCount = after.Segments.Count,
            ConnectionCount = after.Connections.Count,
            EndpointsRemoved = 0,
            LinesRemoved = 0,
            ArcsRemoved = 0
        };
    }
}

public class ConversionStats
{
    public int SegmentCount { get; set; }
    public int ConnectionCount { get; set; }
    public int EndpointsRemoved { get; set; }
    public int LinesRemoved { get; set; }
    public int ArcsRemoved { get; set; }

    public override string ToString()
    {
        return $"Converted {SegmentCount} segments + {ConnectionCount} connections. " +
               $"Removed: {EndpointsRemoved} endpoints, {LinesRemoved} lines, {ArcsRemoved} arcs.";
    }
}
