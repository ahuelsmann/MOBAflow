// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converts an asset filename (e.g. "scheinwerfer.svg") into a packaged ms-appx URI suitable for
/// <c>BitmapIcon.UriSource</c>. Empty/null input returns null so the bound icon stays blank.
/// </summary>
public sealed partial class AssetNameToUriConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string name || string.IsNullOrWhiteSpace(name))
            return null;

        try
        {
            return new Uri($"ms-appx:///Assets/{name.Trim()}");
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
