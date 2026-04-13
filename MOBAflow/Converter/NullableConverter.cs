// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converter that converts null/not-null values to Visibility.
/// - Default: null → Collapsed, not-null → Visible
/// - Parameter="Invert": null → Visible, not-null → Collapsed
/// 
/// Usage Examples:
/// - Show content when data available: {Binding Data, Converter={StaticResource NullableConverter}}
/// - Show empty state when no data: {Binding Data, Converter={StaticResource NullableConverter}, ConverterParameter=Invert}
/// </summary>
public partial class NullableConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        var invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);
        var isNull = value == null;

        return invert ? isNull ? Visibility.Visible : Visibility.Collapsed : isNull ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
