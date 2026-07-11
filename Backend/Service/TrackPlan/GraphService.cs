// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service.TrackPlan;

using Domain;

/// <summary>Read-only topology queries for a layout.</summary>
public sealed class GraphService
{
    public IReadOnlySet<Guid> GetConnectedGroup(Layout layout, Guid trackId)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (!layout.TryGetTrack(trackId, out _))
            return new HashSet<Guid>();

        var result = new HashSet<Guid> { trackId };
        var pending = new Queue<Guid>(result);
        while (pending.TryDequeue(out var current))
        {
            foreach (var connection in layout.Connections)
            {
                Guid? next = connection.SourceTrackId == current ? connection.TargetTrackId
                    : connection.TargetTrackId == current ? connection.SourceTrackId
                    : null;
                if (next.HasValue && result.Add(next.Value))
                    pending.Enqueue(next.Value);
            }
        }

        return result;
    }

    public IReadOnlyList<IReadOnlySet<Guid>> GetConnectedGroups(Layout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        var remaining = layout.Tracks.Select(track => track.Id).ToHashSet();
        var groups = new List<IReadOnlySet<Guid>>();
        while (remaining.Count > 0)
        {
            var group = GetConnectedGroup(layout, remaining.First());
            groups.Add(group);
            remaining.ExceptWith(group);
        }
        return groups;
    }
}
