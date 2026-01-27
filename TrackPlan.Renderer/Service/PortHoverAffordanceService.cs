// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Service;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Provides hover affordance feedback for ports and interactive track elements.
/// 
/// Implements the "Affordances" principle from neuroscience: users learn faster
/// when UI elements clearly communicate what's interactive. This service manages
/// visual feedback when hovering over ports and draggable tracks.
/// 
/// Neuroscience reference: Affordances (Gibson) - perceived properties that suggest
/// how an object should be used. Visual feedback (opacity, scale, glow) teaches users
/// "this element is interactive."
/// </summary>
public interface IPortHoverAffordanceService
{
    /// <summary>
    /// Highlight a port element (increase opacity, show glow, scale up).
    /// Called when mouse hovers over a port.
    /// </summary>
    /// <param name="portId">The port ID being hovered</param>
    /// <param name="trackId">The track ID containing the port</param>
    Task HighlightPortAsync(Guid trackId, string portId);

    /// <summary>
    /// Clear port highlight (fade out glow, return to normal state).
    /// Called when mouse leaves a port.
    /// </summary>
    /// <param name="trackId">The track ID containing the port</param>
    /// <param name="portId">The port ID being un-hovered</param>
    Task UnhighlightPortAsync(Guid trackId, string portId);

    /// <summary>
    /// Highlight a track as draggable (show subtle outline/shadow).
    /// Called when hovering over a track that can be selected and dragged.
    /// </summary>
    /// <param name="trackId">The track ID being hovered</param>
    Task HighlightDraggableTrackAsync(Guid trackId);

    /// <summary>
    /// Clear track draggable highlight.
    /// </summary>
    /// <param name="trackId">The track ID being un-hovered</param>
    Task UnhighlightDraggableTrackAsync(Guid trackId);

    /// <summary>
    /// Get current highlight state of a port.
    /// </summary>
    /// <param name="trackId">The track ID</param>
    /// <param name="portId">The port ID</param>
    /// <returns>True if port is currently highlighted</returns>
    bool IsPortHighlighted(Guid trackId, string portId);

    /// <summary>
    /// Get current hover state for a track.
    /// </summary>
    /// <param name="trackId">The track ID</param>
    /// <returns>True if track is currently hovered/highlighted</returns>
    bool IsTrackHovered(Guid trackId);

    /// <summary>
    /// Get set of currently highlighted ports.
    /// </summary>
    IReadOnlySet<(Guid TrackId, string PortId)> HighlightedPorts { get; }

    /// <summary>
    /// Get set of currently hovered tracks.
    /// </summary>
    IReadOnlySet<Guid> HoveredTracks { get; }

    /// <summary>
    /// Clear all highlights (typically on application blur or mode change).
    /// </summary>
    Task ClearAllHighlightsAsync();
}

/// <summary>
/// Default implementation of IPortHoverAffordanceService.
/// Manages hover state for ports and tracks.
/// </summary>
public sealed class DefaultPortHoverAffordanceService : IPortHoverAffordanceService
{
    private readonly HashSet<(Guid TrackId, string PortId)> _highlightedPorts = new();
    private readonly HashSet<Guid> _hoveredTracks = new();
    private readonly object _stateLock = new();

    public IReadOnlySet<(Guid TrackId, string PortId)> HighlightedPorts
    {
        get
        {
            lock (_stateLock)
            {
                return _highlightedPorts;
            }
        }
    }

    public IReadOnlySet<Guid> HoveredTracks
    {
        get
        {
            lock (_stateLock)
            {
                return _hoveredTracks;
            }
        }
    }

    /// <inheritdoc/>
    public async Task HighlightPortAsync(Guid trackId, string portId)
    {
        lock (_stateLock)
        {
            _highlightedPorts.Add((trackId, portId));
        }

        // Simulate animation delay (would use Composition animations in real implementation)
        await Task.Delay(0);
    }

    /// <inheritdoc/>
    public async Task UnhighlightPortAsync(Guid trackId, string portId)
    {
        lock (_stateLock)
        {
            _highlightedPorts.Remove((trackId, portId));
        }

        await Task.Delay(0);
    }

    /// <inheritdoc/>
    public async Task HighlightDraggableTrackAsync(Guid trackId)
    {
        lock (_stateLock)
        {
            _hoveredTracks.Add(trackId);
        }

        await Task.Delay(0);
    }

    /// <inheritdoc/>
    public async Task UnhighlightDraggableTrackAsync(Guid trackId)
    {
        lock (_stateLock)
        {
            _hoveredTracks.Remove(trackId);
        }

        await Task.Delay(0);
    }

    /// <inheritdoc/>
    public bool IsPortHighlighted(Guid trackId, string portId)
    {
        lock (_stateLock)
        {
            return _highlightedPorts.Contains((trackId, portId));
        }
    }

    /// <inheritdoc/>
    public bool IsTrackHovered(Guid trackId)
    {
        lock (_stateLock)
        {
            return _hoveredTracks.Contains(trackId);
        }
    }

    /// <inheritdoc/>
    public async Task ClearAllHighlightsAsync()
    {
        lock (_stateLock)
        {
            _highlightedPorts.Clear();
            _hoveredTracks.Clear();
        }

        await Task.Delay(0);
    }
}

/// <summary>
/// No-op implementation - disables hover affordances.
/// Used when feature is disabled or for testing.
/// </summary>
public sealed class NoOpPortHoverAffordanceService : IPortHoverAffordanceService
{
    public IReadOnlySet<(Guid TrackId, string PortId)> HighlightedPorts => new HashSet<(Guid, string)>();
    public IReadOnlySet<Guid> HoveredTracks => new HashSet<Guid>();

    public Task HighlightPortAsync(Guid trackId, string portId) => Task.CompletedTask;
    public Task UnhighlightPortAsync(Guid trackId, string portId) => Task.CompletedTask;
    public Task HighlightDraggableTrackAsync(Guid trackId) => Task.CompletedTask;
    public Task UnhighlightDraggableTrackAsync(Guid trackId) => Task.CompletedTask;
    public bool IsPortHighlighted(Guid trackId, string portId) => false;
    public bool IsTrackHovered(Guid trackId) => false;
    public Task ClearAllHighlightsAsync() => Task.CompletedTask;
}
