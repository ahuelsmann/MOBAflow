// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service.TrackPlan;

using Domain;

/// <summary>
/// Application boundary for layout mutations. UI hosts call this service instead of mutating
/// persistence objects or renderer models directly.
/// </summary>
public sealed class LayoutService
{
    public void Place(Layout layout, TrackInstance instance)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(instance);
        layout.AddTrack(instance);
    }

    public bool Remove(Layout layout, Guid trackId)
    {
        ArgumentNullException.ThrowIfNull(layout);
        return layout.RemoveTrack(trackId);
    }

    public void Move(Layout layout, Guid trackId, double x, double y, double rotationDegrees)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (!layout.TryGetTrack(trackId, out var current))
            throw new KeyNotFoundException($"Track instance '{trackId}' does not exist.");

        layout.ReplaceTrack(current with { X = x, Y = y, RotationDegrees = rotationDegrees });
    }

    public void AssignFeedback(Layout layout, Guid trackId, int? inPort)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (!layout.TryGetTrack(trackId, out var current))
            throw new KeyNotFoundException($"Track instance '{trackId}' does not exist.");

        layout.ReplaceTrack(current with { FeedbackInPort = inPort });
    }

    public void Connect(Layout layout, Connection connection)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(connection);
        layout.Connect(connection);
    }
}
