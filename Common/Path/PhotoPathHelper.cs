// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Path;

using Path = System.IO.Path;

/// <summary>
/// Centralized photo path handling to avoid regressions (e.g. Path.DirectorySeparator vs Path.DirectorySeparatorChar).
/// Used by SharedUI, WinUI IoService, and RestApi for consistent resolution of relative photo paths.
/// </summary>
public static class PhotoPathHelper
{
    private const string PhotosPrefixSlash = "photos/";
    private const string PhotosPrefixBackslash = "photos\\";
    private static string? _solutionDirectory;

    /// <summary>
    /// Sets the directory containing the loaded solution file so relative photo paths can resolve next to it.
    /// </summary>
    public static void SetSolutionDirectory(string? solutionFilePath)
    {
        _solutionDirectory = string.IsNullOrWhiteSpace(solutionFilePath)
            ? null
            : Path.GetDirectoryName(Path.GetFullPath(solutionFilePath));
    }

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
        if (Path.IsPathRooted(trimmed))
            return string.Empty;

        var subPath = trimmed.StartsWith(PhotosPrefixSlash, StringComparison.OrdinalIgnoreCase)
            ? trimmed.Substring(PhotosPrefixSlash.Length)
            : trimmed.StartsWith(PhotosPrefixBackslash, StringComparison.OrdinalIgnoreCase)
                ? trimmed.Substring(PhotosPrefixBackslash.Length)
                : trimmed;

        var normalized = subPath.Replace("\\", "/").TrimStart('/');
        if (string.IsNullOrEmpty(normalized) || normalized.Contains("..", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        return $"photos/{normalized}";
    }

    public static bool TryGetStorageRelativePath(string baseDir, string fullPath, out string? relativePath)
    {
        ArgumentNullException.ThrowIfNull(baseDir);
        ArgumentNullException.ThrowIfNull(fullPath);

        relativePath = null;
        if (string.IsNullOrWhiteSpace(fullPath) || !Path.IsPathRooted(fullPath))
            return false;

        var normalizedBaseDir = Path.GetFullPath(baseDir)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedFullPath = Path.GetFullPath(fullPath);
        var candidate = Path.GetRelativePath(normalizedBaseDir, normalizedFullPath);

        if (candidate.Equals("..", StringComparison.Ordinal)
            || candidate.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || candidate.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal)
            || Path.IsPathRooted(candidate))
        {
            return false;
        }

        relativePath = NormalizeStoredRelativePath(candidate);
        return !string.IsNullOrWhiteSpace(relativePath);
    }

    /// <summary>
    /// Resolves the photo storage root: configured path, else bundled <c>photos/</c> next to the app, else My Documents.
    /// </summary>
    public static string ResolvePhotoBaseDirectory(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath.Trim();
        }

        var bundledBase = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (Directory.Exists(Path.Combine(bundledBase, "photos")))
        {
            return bundledBase;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "MOBAflow",
            "Photos");
    }

    /// <summary>
    /// Resolves a stored relative photo path to an existing file by probing known storage roots.
    /// </summary>
    public static string? TryResolveExistingPhotoFullPath(string? configuredPhotoStoragePath, string? relativePhotoPath)
    {
        if (string.IsNullOrWhiteSpace(relativePhotoPath))
        {
            return null;
        }

        var normalized = NormalizeStoredRelativePath(StripQuery(relativePhotoPath));
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        foreach (var baseDir in EnumeratePhotoBaseDirectoryCandidates(configuredPhotoStoragePath))
        {
            if (TryResolvePhotoFullPathUnderBase(baseDir, normalized, out var existing))
            {
                return existing;
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves a stored relative photo path to a full path that must exist under <paramref name="baseDir"/>.
    /// </summary>
    public static bool TryResolvePhotoFullPathUnderBase(string baseDir, string normalizedRelativePath, out string? fullPath)
    {
        ArgumentNullException.ThrowIfNull(baseDir);
        ArgumentNullException.ThrowIfNull(normalizedRelativePath);

        fullPath = null;
        if (string.IsNullOrWhiteSpace(normalizedRelativePath))
        {
            return false;
        }

        foreach (var candidate in BuildPhotoFullPathCandidates(baseDir, normalizedRelativePath))
        {
            var resolved = Path.GetFullPath(candidate);
            if (!IsPathUnderDirectory(baseDir, resolved) || !File.Exists(resolved))
            {
                continue;
            }

            fullPath = resolved;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Builds the destination full path for a photo upload under the storage root.
    /// </summary>
    public static bool TryBuildPhotoUploadFullPath(
        string baseDir,
        string category,
        Guid entityId,
        string extension,
        out string? fullPath,
        out string? storageRelativePath)
    {
        ArgumentNullException.ThrowIfNull(baseDir);

        fullPath = null;
        storageRelativePath = null;

        string normalizedCategory;
        try
        {
            normalizedCategory = NormalizeCategory(category);
        }
        catch (ArgumentException)
        {
            return false;
        }

        storageRelativePath = ToStorageRelativePath(normalizedCategory, entityId, extension);
        var candidate = Path.GetFullPath(ToFullPath(baseDir, storageRelativePath));
        if (!IsPathUnderDirectory(baseDir, candidate))
        {
            storageRelativePath = null;
            return false;
        }

        fullPath = candidate;
        return true;
    }

    private static bool IsPathUnderDirectory(string baseDir, string fullPath)
    {
        var normalizedBaseDir = Path.GetFullPath(baseDir)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedFullPath = Path.GetFullPath(fullPath);
        var relative = Path.GetRelativePath(normalizedBaseDir, normalizedFullPath);

        return !relative.Equals("..", StringComparison.Ordinal)
               && !relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
               && !relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal)
               && !Path.IsPathRooted(relative);
    }

    private static IEnumerable<string> BuildPhotoFullPathCandidates(string baseDir, string normalizedRelativePath)
    {
        yield return Path.GetFullPath(ToFullPath(baseDir, normalizedRelativePath));

        var subPath = normalizedRelativePath.StartsWith(PhotosPrefixSlash, StringComparison.OrdinalIgnoreCase)
            ? normalizedRelativePath
            : normalizedRelativePath.StartsWith(PhotosPrefixBackslash, StringComparison.OrdinalIgnoreCase)
                ? normalizedRelativePath.Replace('\\', '/')
                : $"{PhotosPrefixSlash}{normalizedRelativePath.TrimStart('/')}";

        yield return Path.GetFullPath(Path.Combine(
            baseDir,
            subPath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static IEnumerable<string> EnumeratePhotoBaseDirectoryCandidates(string? configuredPhotoStoragePath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPhotoStoragePath))
        {
            yield return configuredPhotoStoragePath.Trim();
        }

        if (!string.IsNullOrWhiteSpace(_solutionDirectory))
        {
            yield return _solutionDirectory;
        }

        yield return AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "MOBAflow",
            "Photos");

        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MOBAflow",
            "photos");
    }

    private static string StripQuery(string path)
    {
        var idx = path.IndexOf('?', StringComparison.Ordinal);
        return idx >= 0 ? path[..idx] : path;
    }

    /// <summary>
    /// Extracts the cache-busting version from a photo binding path (e.g. <c>photos/a.jpg?v=3</c>).
    /// </summary>
    public static string? TryExtractVersionQuery(string? relativePhotoPath)
    {
        if (string.IsNullOrWhiteSpace(relativePhotoPath))
        {
            return null;
        }

        var queryIndex = relativePhotoPath.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex < 0)
        {
            return null;
        }

        foreach (var segment in relativePhotoPath[(queryIndex + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment.StartsWith("v=", StringComparison.OrdinalIgnoreCase))
            {
                return segment[2..];
            }
        }

        return null;
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

        var normalized = subPath.Replace("/", Path.DirectorySeparatorChar.ToString());
        return Path.Combine(baseDir, normalized);
    }
}