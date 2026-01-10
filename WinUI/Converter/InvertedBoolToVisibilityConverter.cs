// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

/// <summary>
/// Inverts BoolToVisibilityConverter: false → Visible, true → Collapsed.
/// </summary>
public partial class InvertedBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        return value is bool boolValue ? boolValue ? Visibility.Collapsed : Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
