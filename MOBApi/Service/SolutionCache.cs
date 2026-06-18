// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBApi.Service;

/// <summary>
/// Thread-safe in-memory solution cache for MOBApi.
/// </summary>
public sealed class SolutionCache : ISolutionCache
{
    private readonly object _lock = new();
    private SolutionCacheEntry? _entry;

    /// <inheritdoc />
    public bool TryGet(out SolutionCacheEntry entry)
    {
        lock (_lock)
        {
            if (_entry == null)
            {
                entry = null!;
                return false;
            }

            entry = _entry;
            return true;
        }
    }

    /// <inheritdoc />
    public void Set(string json, string? sourcePath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        lock (_lock)
        {
            _entry = new SolutionCacheEntry(json, DateTimeOffset.UtcNow, sourcePath);
        }
    }
}