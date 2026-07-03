// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using Moba.WinUI.View;

using Windows.UI;

/// <summary>
/// Maps locomotive picker selection state to Fluent theme brushes for DataTemplate bindings.
/// Set <see cref="Role"/> on the converter resource (Background, Border, or Foreground).
/// Pass a <see cref="FrameworkElement"/> as <c>ConverterParameter</c> for runtime theme resolution.
/// </summary>
public sealed class BoolToPickerStateBrushConverter : IValueConverter
{
    public string Role { get; set; } = "Background";

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isSelected = value is true;
        var element = parameter as FrameworkElement ?? ThemeResourceResolver.GetDefaultThemeRoot();
        var resourceKey = ResolveResourceKey(Role, isSelected);
        return ThemeResourceResolver.ResolveBrush(element, resourceKey, Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();

    internal static string ResolveResourceKey(string role, bool isSelected) =>
        (role, isSelected) switch
        {
            ("Background", true) => "AccentFillColorDefaultBrush",
            ("Background", false) => "CardBackgroundFillColorDefaultBrush",
            ("Border", true) => "AccentFillColorDefaultBrush",
            ("Border", false) => "CardStrokeColorDefaultBrush",
            ("Foreground", true) => "TextOnAccentFillColorPrimaryBrush",
            _ => "TextFillColorPrimaryBrush"
        };
}
