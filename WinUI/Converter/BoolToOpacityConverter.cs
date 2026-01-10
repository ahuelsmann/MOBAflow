// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converts a boolean value to opacity (1.0 for true, 0.5 for false).
/// Used to visually dim disabled/inactive controls.
/// </summary>
public partial class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        return value is bool boolValue ? boolValue ? 1.0 : 0.5 : 1.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
