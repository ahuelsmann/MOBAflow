// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Common.Path;

using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

/// <summary>
/// Converts a relative photo path ("photos/...") into a BitmapImage that bypasses the image cache.
/// Uses configurable base path when set via SetPhotoBasePath (e.g. from AppSettings); otherwise My Documents\MOBAflow\Photos.
/// </summary>
public partial class PhotoPathToImageConverter : IValueConverter
{
    private static string? _sPhotoBasePath;

    /// <summary>
    /// Sets the base directory for photo resolution (e.g. from Application.PhotoStoragePath).
    /// Call from App startup and when the user changes the path in Settings.
    /// </summary>
    public static void SetPhotoBasePath(string? path)
    {
        _sPhotoBasePath = string.IsNullOrWhiteSpace(path) ? null : path.Trim();
    }

    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            var absolutePath = PhotoPathHelper.TryResolveExistingPhotoFullPath(_sPhotoBasePath, path);
            if (string.IsNullOrWhiteSpace(absolutePath))
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
}