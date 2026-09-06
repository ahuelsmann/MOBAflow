// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Converters;

using SharedUI.Interface;

using System.Collections.Concurrent;
using System.Globalization;

/// <summary>
/// Converts a relative photo path into a MAUI <see cref="ImageSource"/> loaded from MOBApi.
/// </summary>
public sealed class RemotePhotoSourceConverter : IValueConverter
{
    private static readonly ConcurrentDictionary<string, ImageSource> ImageCache = new();
    private readonly IPhotoUriResolver _photoUriResolver;

    public RemotePhotoSourceConverter(IPhotoUriResolver photoUriResolver)
    {
        ArgumentNullException.ThrowIfNull(photoUriResolver);
        _photoUriResolver = photoUriResolver;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return ImageCache.GetOrAdd(path, relativePath => new StreamImageSource
        {
            Stream = async cancellationToken =>
                await _photoUriResolver
                    .OpenReadAsync(relativePath, cancellationToken)
                    .ConfigureAwait(false)
                ?? Stream.Null
        });
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}