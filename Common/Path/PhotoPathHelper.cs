// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Path;

/// <summary>
/// Centralized photo path handling to avoid regressions (e.g. Path.DirectorySeparator vs Path.DirectorySeparatorChar).
/// Used by SharedUI, WinUI IoService, and RestApi for consistent resolution of relative photo paths.
/// </summary>
public static class PhotoPathHelper
{
    private const string PhotosPrefixSlash = "photos/";
    private const string PhotosPrefixBackslash = "photos\\";

    public static string NormalizeCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Photo category must not be empty.", nameof(category));

        return category.Trim().ToLowerInvariant() switch
        {
            "locomotives" => "locomotives",
            "passenger-wagons" or "goods-wagons" or "wagons" => "wagons",
            _ => throw new ArgumentException($"Unknown photo category: '{category}'.", nameof(category))
        };
    }

    public static string ToStorageRelativePath(string category, Guid entityId, string extension)
    {
        var normalizedCategory = NormalizeCategory(category);
        var normalizedExtension = string.IsNullOrWhiteSpace(extension)
            ? string.Empty
            : extension.Trim().StartsWith(".", StringComparison.Ordinal)
                ? extension.Trim()
                : $".{extension.Trim()}";

        return $"photos/{normalizedCategory}/{entityId}{normalizedExtension}";
    }

    public static string NormalizeStoredRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        var trimmed = path.Trim();
        if (System.IO.Path.IsPathRooted(trimmed))
            return trimmed;

        var subPath = trimmed.StartsWith(PhotosPrefixSlash, StringComparison.OrdinalIgnoreCase)
            ? trimmed.Substring(PhotosPrefixSlash.Length)
            : trimmed.StartsWith(PhotosPrefixBackslash, StringComparison.OrdinalIgnoreCase)
                ? trimmed.Substring(PhotosPrefixBackslash.Length)
                : trimmed;

        var normalized = subPath.Replace("\\", "/").TrimStart('/');
        return string.IsNullOrEmpty(normalized) ? string.Empty : $"photos/{normalized}";
    }

    public static bool TryGetStorageRelativePath(string baseDir, string fullPath, out string? relativePath)
    {
        ArgumentNullException.ThrowIfNull(baseDir);
        ArgumentNullException.ThrowIfNull(fullPath);

        relativePath = null;
        if (string.IsNullOrWhiteSpace(fullPath) || !System.IO.Path.IsPathRooted(fullPath))
            return false;

        var normalizedBaseDir = System.IO.Path.GetFullPath(baseDir)
            .TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
        var normalizedFullPath = System.IO.Path.GetFullPath(fullPath);
        var candidate = System.IO.Path.GetRelativePath(normalizedBaseDir, normalizedFullPath);

        if (candidate.Equals("..", StringComparison.Ordinal)
            || candidate.StartsWith($"..{System.IO.Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || candidate.StartsWith($"..{System.IO.Path.AltDirectorySeparatorChar}", StringComparison.Ordinal)
            || System.IO.Path.IsPathRooted(candidate))
        {
            return false;
        }

        relativePath = NormalizeStoredRelativePath(candidate);
        return !string.IsNullOrWhiteSpace(relativePath);
    }

    /// <summary>
    /// Combines the photo storage base directory with a relative path (e.g. from API "photos/locomotives/abc.jpg").
    /// Strips leading "photos/" or "photos\" and normalizes forward slashes to the platform separator.
    /// </summary>
    /// <param name="baseDir">Base directory for photos (e.g. from Application.PhotoStoragePath or My Documents).</param>
    /// <param name="relativePath">Relative path, optionally starting with "photos/" or "photos\".</param>
    /// <returns>Full local path using the platform directory separator.</returns>
    public static string ToFullPath(string baseDir, string relativePath)
    {
        ArgumentNullException.ThrowIfNull(baseDir);
        ArgumentNullException.ThrowIfNull(relativePath);

        var trimmed = relativePath.TrimStart();
        var subPath = trimmed.StartsWith(PhotosPrefixSlash, StringComparison.OrdinalIgnoreCase)
            ? trimmed.Substring(PhotosPrefixSlash.Length)
            : trimmed.StartsWith(PhotosPrefixBackslash, StringComparison.OrdinalIgnoreCase)
                ? trimmed.Substring(PhotosPrefixBackslash.Length)
                : relativePath;

        var normalized = subPath.Replace("/", System.IO.Path.DirectorySeparatorChar.ToString());
        return System.IO.Path.Combine(baseDir, normalized);
    }
}
