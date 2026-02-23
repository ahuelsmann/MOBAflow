// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

/// <summary>
/// Converts a relative photo path ("photos/...") into a BitmapImage that bypasses the image cache.
/// Ensures updated photos (same filename) are reloaded immediately after upload.
/// </summary>
internal partial class PhotoPathToImageConverter : IValueConverter
{
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
        // Mirror the logic from PhotoStorageService.GetStoragePath()
        string basePath;
        var contentRootPath = Environment.GetEnvironmentVariable("ASPNETCORE_CONTENTROOT");
        if (!string.IsNullOrEmpty(contentRootPath))
        {
            basePath = Path.Combine(contentRootPath, "photos");
        }
        else
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            basePath = Path.Combine(localAppData, "MOBAflow", "photos");
        }

        return Path.Combine(basePath, relativePath.Replace("/", "\\"));
    }

    private static string StripQuery(string path)
    {
        var idx = path.IndexOf('?', StringComparison.Ordinal);
        return idx >= 0 ? path[..idx] : path;
    }
}
