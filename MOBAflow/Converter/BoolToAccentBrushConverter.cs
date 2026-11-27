// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBAflow.Converter;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using Moba.MOBAflow.View;

using Windows.UI;

/// <summary>
/// Converter that converts a boolean value to a Brush.
/// True → <c>AccentFillColorDefaultBrush</c>, False → <c>TextFillColorSecondaryBrush</c>.
/// Pass a <see cref="FrameworkElement"/> as <c>ConverterParameter</c> for runtime theme resolution.
/// </summary>
public partial class BoolToAccentBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var element = parameter as FrameworkElement ?? ThemeResourceResolver.GetDefaultThemeRoot();
        var isTrue = value is bool { } boolValue && boolValue;
        return ThemeResourceResolver.ResolveBrush(
            element,
            ResolveResourceKey(isTrue),
            isTrue ? Colors.Blue : Colors.Gray);
    }

    internal static string ResolveResourceKey(bool isTrue) =>
        isTrue ? "AccentFillColorDefaultBrush" : "TextFillColorSecondaryBrush";

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
