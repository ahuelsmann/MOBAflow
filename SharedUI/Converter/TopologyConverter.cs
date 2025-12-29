// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using System.Collections.Generic;
using System.Linq;
using Moba.Domain.TrackPlan;

namespace Moba.SharedUI.Converter;

/// <summary>
/// Converts AnyRail-imported track layout (with coordinates) to pure topology (ArticleCode + Connections only).
/// After conversion, original coordinates/Lines/Arcs are discarded - renderer calculates everything from topology.
/// </summary>
public static class TopologyConverter
{
    /// <summary>
    /// Convert AnyRail TrackLayout to pure topology.
    /// Strips all coordinates, Lines, Arcs - keeps only ArticleCode, AssignedInPort, Name, Layer.
    /// Connections are preserved (they define the topology graph).
    /// </summary>
    public static TrackLayout ToPureTopology(TrackLayout anyRailLayout)
    {
        return new TrackLayout
        {
            Name = anyRailLayout.Name,
            Description = $"{anyRailLayout.Description} (Pure Topology)",
            TrackSystem = anyRailLayout.TrackSystem,
            
            // Strip coordinates from segments - keep only topology data
            Segments = anyRailLayout.Segments.Select(s => new TrackSegment
            {
                Id = s.Id,
                ArticleCode = s.ArticleCode,
                AssignedInPort = s.AssignedInPort,
                Name = s.Name,
                Layer = s.Layer
                // NO Endpoints, Lines, Arcs - renderer calculates from ArticleCode
            }).ToList(),
            
            // Connections define the topology graph - keep unchanged
            Connections = anyRailLayout.Connections.ToList()
        };
    }

    /// <summary>
    /// Get statistics about the conversion (how much data was stripped).
    /// </summary>
    public static ConversionStats GetConversionStats(TrackLayout before, TrackLayout after)
    {
        var beforeEndpoints = before.Segments.Sum(s => s.Endpoints?.Count ?? 0);
        var beforeLines = before.Segments.Sum(s => s.Lines?.Count ?? 0);
        var beforeArcs = before.Segments.Sum(s => s.Arcs?.Count ?? 0);

        var afterEndpoints = after.Segments.Sum(s => s.Endpoints?.Count ?? 0);
        var afterLines = after.Segments.Sum(s => s.Lines?.Count ?? 0);
        var afterArcs = after.Segments.Sum(s => s.Arcs?.Count ?? 0);

        return new ConversionStats
        {
            SegmentCount = after.Segments.Count,
            ConnectionCount = after.Connections.Count,
            EndpointsRemoved = beforeEndpoints - afterEndpoints,
            LinesRemoved = beforeLines - afterLines,
            ArcsRemoved = beforeArcs - afterArcs
        };
    }
}

/// <summary>
/// Statistics about topology conversion.
/// </summary>
public class ConversionStats
{
    public int SegmentCount { get; set; }
    public int ConnectionCount { get; set; }
    public int EndpointsRemoved { get; set; }
    public int LinesRemoved { get; set; }
    public int ArcsRemoved { get; set; }

    /// <summary>
    /// Get human-readable summary.
    /// </summary>
    public override string ToString()
    {
        return $"Converted {SegmentCount} segments + {ConnectionCount} connections. " +
               $"Removed: {EndpointsRemoved} endpoints, {LinesRemoved} lines, {ArcsRemoved} arcs.";
    }
}
