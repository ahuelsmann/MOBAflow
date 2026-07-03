// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Interface;

using Common.Runtime;

using Domain;

/// <summary>
/// Metadata for a solution snapshot synced from MOBApi and cached on the mobile device.
/// </summary>

public sealed record SolutionSyncMeta(

    DateTimeOffset UpdatedAt,

    string SolutionName,

    string? ActiveProjectName);

/// <summary>
/// Cached MOBAflow solution and optional signal-box runtime state for offline MOBAsmart use.
/// </summary>

public sealed record MobileSolutionCacheEntry(
    Solution Solution,
    SolutionSyncMeta Meta,
    IReadOnlyList<SignalBoxElementRuntimeSnapshot> SignalBoxElements,
    IReadOnlyList<LocomotiveFleetSnapshot> LocomotiveFleet);

/// <summary>
/// Persists MOBAsmart solution and signal-box data locally so the app works without MOBAflow.
/// </summary>

public interface IMobileSolutionStore

{

    /// <summary>
    /// Saves the solution and sync metadata to local storage.
    /// </summary>

    Task SaveAsync(Solution solution, SolutionSyncMeta meta, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the cached signal-box element list without changing the solution payload.
    /// </summary>

    Task SaveSignalBoxElementsAsync(
        IReadOnlyList<SignalBoxElementRuntimeSnapshot> elements,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the cached locomotive fleet without changing the solution payload.
    /// </summary>
    Task SaveLocomotiveFleetAsync(
        IReadOnlyList<LocomotiveFleetSnapshot> fleet,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the cached solution, metadata, signal-box elements, and locomotive fleet when available.
    /// </summary>
    Task<MobileSolutionCacheEntry?> TryLoadAsync(CancellationToken cancellationToken = default);
}

