// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Converters;

using SharedUI.Interface;

using System.Globalization;

/// <summary>
/// Converts a relative photo path into a MAUI <see cref="ImageSource"/> loaded from MOBApi.
/// </summary>
public sealed class RemotePhotoSourceConverter : IValueConverter
{
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

        var uri = _photoUriResolver.TryResolveRemoteUri(path);
        if (string.IsNullOrWhiteSpace(uri))
        {
            return null;
        }

        try
        {
            return ImageSource.FromUri(new Uri(uri, UriKind.Absolute));
        }
        catch (UriFormatException)
        {
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
