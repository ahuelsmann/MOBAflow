// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Common.Path;

using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

using System.Collections.Concurrent;

/// <summary>
/// Converts a relative photo path ("photos/...") into a cached <see cref="BitmapImage"/>.
/// Uses configurable base path when set via SetPhotoBasePath (e.g. from AppSettings); otherwise My Documents\MOBAflow\Photos.
/// Cache invalidation follows the binding version query from <see cref="SharedUI.ViewModel.LocomotiveViewModel.PhotoPathWithVersion"/>.
/// </summary>
public partial class PhotoPathToImageConverter : IValueConverter
{
    private static readonly ConcurrentDictionary<string, BitmapImage> ImageCache = new();
    private static string? _sPhotoBasePath;

    /// <summary>
    /// Sets the base directory for photo resolution (e.g. from Application.PhotoStoragePath).
    /// Call from App startup and when the user changes the path in Settings.
    /// </summary>
    public static void SetPhotoBasePath(string? path)
    {
        var normalized = string.IsNullOrWhiteSpace(path) ? null : path.Trim();
        if (!string.Equals(_sPhotoBasePath, normalized, StringComparison.Ordinal))
        {
            _sPhotoBasePath = normalized;
            ImageCache.Clear();
        }
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

            var cacheKey = BuildCacheKey(absolutePath, path);
            return ImageCache.GetOrAdd(cacheKey, static key =>
            {
                var absolute = key.IndexOf('?', StringComparison.Ordinal) >= 0
                    ? key[..key.IndexOf('?', StringComparison.Ordinal)]
                    : key;
                return new BitmapImage { UriSource = new Uri(absolute) };
            });
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => value;

    private static string BuildCacheKey(string absolutePath, string bindingPath)
    {
        var version = PhotoPathHelper.TryExtractVersionQuery(bindingPath);
        return string.IsNullOrWhiteSpace(version) ? absolutePath : $"{absolutePath}?v={version}";
    }
}
