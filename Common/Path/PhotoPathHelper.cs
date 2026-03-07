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
