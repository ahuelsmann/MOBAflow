// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Service;

using Moba.TrackPlan.Geometry;

using System;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Interface for snap preview providers that optimize performance for large track layouts.
/// Handles lazy-loading, caching, and cancellation of snap preview operations.
/// </summary>
public interface ISnapPreviewProvider
{
    /// <summary>
    /// Requests preview update for a dragging track.
    /// Provides cached or newly computed snap candidates with cancellation support.
    /// Snap candidates are sorted by validity, pointer relevance (if provided), then distance.
    /// </summary>
    /// <param name="draggedEdgeId">The edge being dragged</param>
    /// <param name="draggedPortId">The port on the dragged edge</param>
    /// <param name="worldPortLocation">World space location of the port</param>
    /// <param name="pointerLocationMm">Current mouse pointer location in world space (mm), or null if unavailable. Used to prioritize ports near cursor.</param>
    /// <param name="cancellationToken">Cancellation token for async work</param>
    /// <returns>Snap preview result with candidates and metadata</returns>
    SnapPreviewResult? GetSnapPreview(
        Guid draggedEdgeId,
        string draggedPortId,
        Point2D worldPortLocation,
        Point2D? pointerLocationMm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the snap preview cache (e.g., after topology changes).
    /// </summary>
    void InvalidateCache();

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    void ClearCache();
}

/// <summary>
/// Result of snap preview computation.
/// </summary>
public sealed record SnapPreviewResult(
    Guid DraggedEdgeId,
    string DraggedPortId,
    List<ISnapToConnectService.SnapCandidate> Candidates,
    DateTime ComputedAtUtc,
    bool IsFromCache);

/// <summary>
/// Default implementation of snap preview provider with caching and performance optimization.
/// </summary>
public sealed class DefaultSnapPreviewProvider : ISnapPreviewProvider
{
    private readonly ISnapToConnectService _snapService;
    private readonly int _maxCacheSize;
    private readonly Dictionary<string, SnapPreviewResult> _cache;
    private readonly object _cacheLock = new();

    /// <summary>
    /// Cache key format: "{edgeId}_{portId}_{locationX}_{locationY}"
    /// </summary>
    private static string CreateCacheKey(Guid edgeId, string portId, Point2D location, double snapRadius)
    {
        // Round location to nearest 0.5mm to allow slight variations to use cached result
        var roundedX = Math.Round(location.X / 0.5) * 0.5;
        var roundedY = Math.Round(location.Y / 0.5) * 0.5;
        return $"{edgeId}_{portId}_{roundedX}_{roundedY}_{snapRadius}";
    }

    public DefaultSnapPreviewProvider(
        ISnapToConnectService snapService,
        int maxCacheSize = 100)
    {
        _snapService = snapService ?? throw new ArgumentNullException(nameof(snapService));
        _maxCacheSize = maxCacheSize;
        _cache = new Dictionary<string, SnapPreviewResult>();
    }

    /// <summary>
    /// Gets snap preview with caching support.
    /// </summary>
    public SnapPreviewResult? GetSnapPreview(
        Guid draggedEdgeId,
        string draggedPortId,
        Point2D worldPortLocation,
        Point2D? pointerLocationMm = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(worldPortLocation);

        // Cache key includes pointer position to differentiate snap results
        // Note: pointerLocationMm is rounded to 1mm for caching efficiency
        var cacheKey = CreateCacheKey(
            draggedEdgeId,
            draggedPortId,
            worldPortLocation,
            ISnapToConnectService.DefaultSnapRadiusMm);

        // Append pointer position to cache key if provided
        if (pointerLocationMm.HasValue)
        {
            var ptrX = Math.Round(pointerLocationMm.Value.X / 1.0) * 1.0;
            var ptrY = Math.Round(pointerLocationMm.Value.Y / 1.0) * 1.0;
            cacheKey = $"{cacheKey}_ptr_{ptrX}_{ptrY}";
        }

        // Try to get from cache
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(cacheKey, out var cachedResult))
            {
                return cachedResult;
            }
        }

        // Compute new preview
        try
        {
            // Check cancellation token
            cancellationToken.ThrowIfCancellationRequested();

            var candidates = _snapService.FindSnapCandidates(
                draggedEdgeId,
                draggedPortId,
                worldPortLocation,
                ISnapToConnectService.DefaultSnapRadiusMm,
                pointerLocationMm);

            var result = new SnapPreviewResult(
                DraggedEdgeId: draggedEdgeId,
                DraggedPortId: draggedPortId,
                Candidates: candidates,
                ComputedAtUtc: DateTime.UtcNow,
                IsFromCache: false);

            // Cache the result
            lock (_cacheLock)
            {
                // Implement simple LRU by removing oldest entry if cache is full
                if (_cache.Count >= _maxCacheSize && _cache.Count > 0)
                {
                    var oldestKey = _cache.Keys.First();
                    _cache.Remove(oldestKey);
                }

                _cache[cacheKey] = result;
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    /// <summary>
    /// Invalidates cache entries related to a specific edge.
    /// </summary>
    public void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _cache.Clear();
        }
    }

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _cache.Clear();
        }
    }
}

/// <summary>
/// No-operation snap preview provider (always computes, no caching).
/// Useful for testing or scenarios where caching is not beneficial.
/// </summary>
public sealed class NoOpSnapPreviewProvider : ISnapPreviewProvider
{
    private readonly ISnapToConnectService _snapService;

    public NoOpSnapPreviewProvider(ISnapToConnectService snapService)
    {
        _snapService = snapService ?? throw new ArgumentNullException(nameof(snapService));
    }

    public SnapPreviewResult? GetSnapPreview(
        Guid draggedEdgeId,
        string draggedPortId,
        Point2D worldPortLocation,
        Point2D? pointerLocationMm = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(worldPortLocation);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidates = _snapService.FindSnapCandidates(
                draggedEdgeId,
                draggedPortId,
                worldPortLocation,
                ISnapToConnectService.DefaultSnapRadiusMm,
                pointerLocationMm);

            return new SnapPreviewResult(
                DraggedEdgeId: draggedEdgeId,
                DraggedPortId: draggedPortId,
                Candidates: candidates,
                ComputedAtUtc: DateTime.UtcNow,
                IsFromCache: false);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    public void InvalidateCache() { }

    public void ClearCache() { }
}
