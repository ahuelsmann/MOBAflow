// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBApi.Service;

using System.Text.Json;

/// <summary>
/// Thread-safe in-memory solution cache for MOBApi.
/// </summary>
public sealed class SolutionCache : ISolutionCache
{
    private readonly object _lock = new();
    private SolutionCacheEntry? _entry;
    private HashSet<Guid> _projectIds = [];

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
    public bool ContainsProject(Guid projectId)
    {
        lock (_lock)
        {
            return _projectIds.Contains(projectId);
        }
    }

    /// <inheritdoc />
    public void Set(string json, string? sourcePath = null, string? activeProjectName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        lock (_lock)
        {
            _entry = new SolutionCacheEntry(json, DateTimeOffset.UtcNow, sourcePath, activeProjectName);
            _projectIds = ReadProjectIds(json);
        }
    }

    /// <summary>Reads the project ids of a solution document; property names are matched case-insensitively.</summary>
    private static HashSet<Guid> ReadProjectIds(string json)
    {
        using var document = JsonDocument.Parse(json);
        var projects = document.RootElement.EnumerateObject()
            .FirstOrDefault(property => property.NameEquals("projects") || property.NameEquals("Projects")).Value;
        if (projects.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return
        [
            .. projects.EnumerateArray()
                .Where(project => project.ValueKind == JsonValueKind.Object)
                .Select(project => project.EnumerateObject()
                    .FirstOrDefault(property => property.NameEquals("id") || property.NameEquals("Id")).Value)
                .Where(id => id.ValueKind == JsonValueKind.String && id.TryGetGuid(out _))
                .Select(id => id.GetGuid())
        ];
    }
}
