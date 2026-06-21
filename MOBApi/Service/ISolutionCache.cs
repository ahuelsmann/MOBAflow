// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBApi.Service;

/// <summary>
/// In-memory cache for the current MOBAflow solution JSON pushed from WinUI.
/// </summary>
public interface ISolutionCache
{
    /// <summary>
    /// Tries to read the cached solution snapshot.
    /// </summary>
    bool TryGet(out SolutionCacheEntry entry);

    /// <summary>
    /// Stores validated solution JSON in the cache.
    /// </summary>
    void Set(string json, string? sourcePath = null, string? activeProjectName = null);
}

/// <summary>
/// Cached solution payload served to MOBAsmart clients.
/// </summary>
public sealed record SolutionCacheEntry(
    string Json,
    DateTimeOffset UpdatedAt,
    string? SourcePath,
    string? ActiveProjectName = null);