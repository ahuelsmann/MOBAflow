// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;

/// <summary>
/// Converts SVG path data string to WinUI Geometry object.
/// Used for rendering track segments from PathData strings.
/// </summary>
public class PathDataToGeometryConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is not string pathData || string.IsNullOrWhiteSpace(pathData))
        {
            return null;
        }

        try
        {
            // Use WinUI's XamlBindingHelper to parse SVG path syntax
            return XamlBindingHelper.ConvertValue(typeof(Geometry), pathData);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ PathDataToGeometryConverter failed for: {pathData}\n   Error: {ex.Message}");
            return null; // Return null for invalid path data
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException("PathDataToGeometryConverter only supports one-way binding");
    }
}
