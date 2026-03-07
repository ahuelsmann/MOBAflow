// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

/// <summary>
/// Converts a relative photo path ("photos/...") into a BitmapImage that bypasses the image cache.
/// Uses configurable base path when set via SetPhotoBasePath (e.g. from AppSettings); otherwise My Documents\MOBAflow\Photos.
/// </summary>
internal partial class PhotoPathToImageConverter : IValueConverter
{
    private static string? s_photoBasePath;

    /// <summary>
    /// Sets the base directory for photo resolution (e.g. from Application.PhotoStoragePath).
    /// Call from App startup and when the user changes the path in Settings.
    /// </summary>
    public static void SetPhotoBasePath(string? path)
    {
        s_photoBasePath = string.IsNullOrWhiteSpace(path) ? null : path.Trim();
    }

    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            var normalizedPath = StripQuery(path);
            var absolutePath = Path.IsPathRooted(normalizedPath) ? normalizedPath : GetAbsolutePath(normalizedPath);
            if (!File.Exists(absolutePath))
                return null;

            // Add query string to force cache refresh
            var uriWithCacheBust = new Uri(absolutePath + "?" + DateTime.UtcNow.Ticks);
            var bitmap = new BitmapImage
            {
                CreateOptions = BitmapCreateOptions.IgnoreImageCache,
                UriSource = uriWithCacheBust
            };

            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => value;

    private static string GetAbsolutePath(string relativePath)
    {
        var baseDir = !string.IsNullOrWhiteSpace(s_photoBasePath)
            ? s_photoBasePath
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "MOBAflow", "Photos");
        var subPath = relativePath.TrimStart().StartsWith("photos/", StringComparison.OrdinalIgnoreCase)
            ? relativePath.Substring(7)
            : relativePath.TrimStart().StartsWith("photos\\", StringComparison.OrdinalIgnoreCase)
                ? relativePath.Substring(8)
                : relativePath;
        return Path.Combine(baseDir, subPath.Replace("/", "\\"));
    }

    private static string StripQuery(string path)
    {
        var idx = path.IndexOf('?', StringComparison.Ordinal);
        return idx >= 0 ? path[..idx] : path;
    }
}
