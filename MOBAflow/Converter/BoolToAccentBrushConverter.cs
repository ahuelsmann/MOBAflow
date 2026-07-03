// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using Moba.WinUI.View;

using Windows.UI;

/// <summary>
/// Converter that converts a boolean value to a Brush.
/// True → AccentFillColorDefaultBrush, False → TextFillColorSecondaryBrush
/// Pass a <see cref="FrameworkElement"/> as <c>ConverterParameter</c> for runtime theme resolution.
/// </summary>
public partial class BoolToAccentBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var element = parameter as FrameworkElement ?? ThemeResourceResolver.GetDefaultThemeRoot();
        return value is bool { } boolValue && boolValue
            ? ThemeResourceResolver.ResolveBrush(element, "AccentFillColorDefaultBrush", Colors.Blue)
            : ThemeResourceResolver.ResolveBrush(element, "TextFillColorSecondaryBrush", Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
