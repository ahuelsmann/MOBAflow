using Moba.Domain.TrackPlan;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Moba.Domain;

/// <summary>
/// Converts AnyRail connections (with absolute endpoint coordinates) to Pure Topology connections (with library-based endpoint indices).
/// </summary>
public static class AnyRailConnectionConverter
{
    /// <summary>
    /// Convert AnyRail segments (with Endpoints) to Pure Topology connections (with TrackGeometryLibrary indices).
    /// </summary>
    public static List<TrackConnection> ConvertConnections(List<TrackSegment> anyRailSegments)
    {
        var connections = new List<TrackConnection>();
        var processed = new HashSet<(string, string)>(); // Track processed pairs to avoid duplicates

        foreach (var seg1 in anyRailSegments)
        {
            if (seg1.Endpoints == null || seg1.Endpoints.Count == 0) continue;

            for (int ep1Index = 0; ep1Index < seg1.Endpoints.Count; ep1Index++)
            {
                var ep1 = seg1.Endpoints[ep1Index];

                foreach (var seg2 in anyRailSegments)
                {
                    if (seg1.Id == seg2.Id) continue; // Skip self
                    if (seg2.Endpoints == null || seg2.Endpoints.Count == 0) continue;

                    // Check if already processed (avoid duplicates A→B and B→A)
                    var pairKey = seg1.Id.CompareTo(seg2.Id) < 0
                        ? (seg1.Id, seg2.Id)
                        : (seg2.Id, seg1.Id);
                    if (processed.Contains(pairKey)) continue;

                    for (int ep2Index = 0; ep2Index < seg2.Endpoints.Count; ep2Index++)
                    {
                        var ep2 = seg2.Endpoints[ep2Index];

                        // Check if endpoints match (same coordinates within tolerance)
                        if (Math.Abs(ep1.X - ep2.X) < 0.1 && Math.Abs(ep1.Y - ep2.Y) < 0.1)
                        {
                            connections.Add(new TrackConnection
                            {
                                Segment1Id = seg1.Id,
                                Segment1EndpointIndex = ep1Index,
                                Segment2Id = seg2.Id,
                                Segment2EndpointIndex = ep2Index
                            });

                            processed.Add(pairKey);
                            goto NextSegment1Endpoint; // Found match, move to next endpoint
                        }
                    }
                }

                NextSegment1Endpoint:;
            }
        }

        return connections;
    }
}
