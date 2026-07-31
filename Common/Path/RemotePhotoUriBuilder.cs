// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Path;

/// <summary>
/// Builds HTTP URIs for photo files served by MOBApi from relative storage paths.
/// </summary>
public static class RemotePhotoUriBuilder
{
    /// <summary>
    /// Returns an HTTP URI for the given relative photo path, or <c>null</c> when inputs are invalid.
    /// </summary>
    public static string? BuildHttpUri(string? serverIp, int serverPort, string? relativePhotoPath)
    {
        if (string.IsNullOrWhiteSpace(serverIp) || serverPort <= 0 || string.IsNullOrWhiteSpace(relativePhotoPath))
        {
            return null;
        }

        var relativeApiPath = BuildRelativeApiPath(relativePhotoPath);
        return relativeApiPath is null
            ? null
            : $"http://{serverIp.Trim()}:{serverPort}/{relativeApiPath}";
    }

    /// <summary>
    /// Returns the relative MOBApi photo path without embedding a server address or credential.
    /// </summary>
    public static string? BuildRelativeApiPath(string? relativePhotoPath)
    {
        if (string.IsNullOrWhiteSpace(relativePhotoPath))
        {
            return null;
        }

        var pathWithoutQuery = StripQuery(relativePhotoPath);
        var normalized = PhotoPathHelper.NormalizeStoredRelativePath(pathWithoutQuery);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var uri = $"api/photos/file?path={Uri.EscapeDataString(normalized)}";
        var version = PhotoPathHelper.TryExtractVersionQuery(relativePhotoPath);
        if (!string.IsNullOrWhiteSpace(version))
        {
            uri += $"&v={Uri.EscapeDataString(version)}";
        }

        return uri;
    }

    private static string StripQuery(string path)
    {
        var queryIndex = path.IndexOf('?', StringComparison.Ordinal);
        return queryIndex >= 0 ? path[..queryIndex] : path;
    }
}
