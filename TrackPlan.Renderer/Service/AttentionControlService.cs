// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Service;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Provides attention control for neuroscience-based UX design.
/// 
/// Implements the "Chunking" principle: Users can focus better when irrelevant information
/// is visually reduced. This service dims non-selected tracks during drag operations,
/// helping the brain maintain focus on the task (connecting specific tracks).
/// 
/// Neuroscience reference: Cognitive Load Theory - reducing extraneous load improves
/// working memory utilization for the relevant task.
/// </summary>
public interface IAttentionControlService
{
    /// <summary>
    /// Dim all tracks except the specified selection.
    /// Non-selected tracks become semi-transparent (0.3 opacity).
    /// Selected tracks remain fully visible (1.0 opacity).
    /// </summary>
    /// <param name="selectedTrackIds">Track IDs to keep visible. Pass empty list to dim all.</param>
    /// <param name="dimOpacity">Opacity for non-selected tracks (default 0.3). Valid: 0.0-1.0</param>
    /// <param name="cancellationToken">Optional cancellation token for animation</param>
    Task DimIrrelevantTracksAsync(
        IReadOnlyList<Guid> selectedTrackIds,
        float dimOpacity = 0.3f,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restore all tracks to full visibility (1.0 opacity).
    /// Called when drag ends or selection is cleared.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token for animation</param>
    Task RestoreAllTracksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current opacity of a specific track.
    /// Useful for rendering systems to apply opacity correctly.
    /// </summary>
    /// <param name="trackId">The track ID to query</param>
    /// <returns>Current opacity (0.0-1.0)</returns>
    float GetTrackOpacity(Guid trackId);

    /// <summary>
    /// Check if attention control is currently active (dimming non-selected tracks).
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Get set of currently-dimmed track IDs.
    /// </summary>
    IReadOnlySet<Guid> DimmedTracks { get; }
}

/// <summary>
/// Default implementation of IAttentionControlService.
/// Manages track opacity state during attention control.
/// </summary>
public sealed class DefaultAttentionControlService : IAttentionControlService
{
    private readonly Dictionary<Guid, float> _trackOpacities = new();
    private readonly object _opacityLock = new();
    private float _defaultDimOpacity = 0.3f;
    private HashSet<Guid> _dimmedTracks = new();
    private bool _isActive;

    /// <inheritdoc/>
    public bool IsActive => _isActive;

    /// <inheritdoc/>
    public IReadOnlySet<Guid> DimmedTracks => _dimmedTracks;

    /// <summary>
    /// Initialize with known track IDs (for pre-allocation).
    /// Optional - helps avoid allocations during rendering.
    /// </summary>
    public DefaultAttentionControlService(IEnumerable<Guid>? knownTrackIds = null)
    {
        if (knownTrackIds != null)
        {
            foreach (var id in knownTrackIds)
            {
                _trackOpacities[id] = 1.0f;
            }
        }
    }

    /// <inheritdoc/>
    public async Task DimIrrelevantTracksAsync(
        IReadOnlyList<Guid> selectedTrackIds,
        float dimOpacity = 0.3f,
        CancellationToken cancellationToken = default)
    {
        // Validate opacity range
        dimOpacity = Math.Clamp(dimOpacity, 0.0f, 1.0f);

        lock (_opacityLock)
        {
            // Update opacity map
            _defaultDimOpacity = dimOpacity;
            _dimmedTracks.Clear();

            var selectedSet = new HashSet<Guid>(selectedTrackIds);

            // Apply dimming in single pass
            foreach (var trackId in _trackOpacities.Keys.ToList())
            {
                if (selectedSet.Contains(trackId))
                {
                    _trackOpacities[trackId] = 1.0f;  // Selected: full opacity
                }
                else
                {
                    _trackOpacities[trackId] = dimOpacity;  // Non-selected: dimmed
                    _dimmedTracks.Add(trackId);
                }
            }

            _isActive = _dimmedTracks.Count > 0;
        }

        // Simulate animation delay (optional smooth fade)
        // In real implementation, this could use Composition animations
        await Task.Delay(0, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RestoreAllTracksAsync(CancellationToken cancellationToken = default)
    {
        lock (_opacityLock)
        {
            // Restore all to full opacity
            foreach (var trackId in _trackOpacities.Keys.ToList())
            {
                _trackOpacities[trackId] = 1.0f;
            }

            _dimmedTracks.Clear();
            _isActive = false;
        }

        await Task.Delay(0, cancellationToken);
    }

    /// <inheritdoc/>
    public float GetTrackOpacity(Guid trackId)
    {
        lock (_opacityLock)
        {
            return _trackOpacities.GetValueOrDefault(trackId, 1.0f);  // Default: fully visible
        }
    }

    /// <summary>
    /// Register a track ID for opacity tracking.
    /// Call this when new tracks are added to the graph.
    /// </summary>
    public void RegisterTrack(Guid trackId)
    {
        lock (_opacityLock)
        {
            if (!_trackOpacities.ContainsKey(trackId))
            {
                _trackOpacities[trackId] = 1.0f;
            }
        }
    }

    /// <summary>
    /// Unregister a track ID (e.g., when tracks are deleted).
    /// </summary>
    public void UnregisterTrack(Guid trackId)
    {
        lock (_opacityLock)
        {
            _trackOpacities.Remove(trackId);
            _dimmedTracks.Remove(trackId);
        }
    }

    /// <summary>
    /// Clear all tracked state (e.g., on graph reset).
    /// </summary>
    public void Clear()
    {
        lock (_opacityLock)
        {
            _trackOpacities.Clear();
            _dimmedTracks.Clear();
            _isActive = false;
        }
    }

    /// <summary>
    /// Get all tracked tracks with their current opacities.
    /// Useful for debugging or diagnostics.
    /// </summary>
    public IReadOnlyDictionary<Guid, float> GetAllOpacities()
    {
        lock (_opacityLock)
        {
            return _trackOpacities.ToDictionary(x => x.Key, x => x.Value);
        }
    }
}

/// <summary>
/// No-op implementation - disables attention control.
/// Used when feature is disabled or for testing.
/// </summary>
public sealed class NoOpAttentionControlService : IAttentionControlService
{
    public bool IsActive => false;
    public IReadOnlySet<Guid> DimmedTracks => new HashSet<Guid>();

    public Task DimIrrelevantTracksAsync(
        IReadOnlyList<Guid> selectedTrackIds,
        float dimOpacity = 0.3f,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RestoreAllTracksAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public float GetTrackOpacity(Guid trackId) => 1.0f;  // Always full opacity
}
