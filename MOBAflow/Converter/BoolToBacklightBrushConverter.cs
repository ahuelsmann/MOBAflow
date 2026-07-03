// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using Moba.Common.Display;
using Moba.WinUI.View;

using Windows.UI;

/// <summary>
/// Converts function on-state and accent hex to theme-aware backlight brushes.
/// Pass a <see cref="FrameworkElement"/> as <c>ConverterParameter</c> when the app theme
/// is toggled via <c>RequestedTheme</c> on a root element.
/// </summary>
public partial class BoolToBacklightBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        bool isOn = value is bool b && b;
        FrameworkElement? themeElement = null;
        string? hexColor = null;

        switch (parameter)
        {
            case FrameworkElement element:
                themeElement = element;
                break;
            case string hex:
                hexColor = hex;
                break;
        }

        return CreateBrush(isOn, hexColor, themeElement);
    }

    /// <summary>
    /// Creates the backlight brush for a function button.
    /// </summary>
    public static Brush CreateBrush(bool isOn, string? hexColor, FrameworkElement? themeElement = null)
    {
        if (string.IsNullOrEmpty(hexColor) && themeElement == null)
        {
            hexColor = "#808080";
        }

        var theme = GetAppearanceTheme(themeElement);
        var cacheKey = $"{theme}:{(isOn ? '1' : '0')}:{hexColor ?? string.Empty}";
        return BrushCache.GetOrAdd(cacheKey, () => CreateBrushUncached(isOn, hexColor, theme));
    }

    public static Color CreatePrimaryTextColor(bool isOn, string? hexColor, FrameworkElement? themeElement = null)
        => ToWinColor(FunctionBacklightColor.Resolve(isOn, hexColor, GetAppearanceTheme(themeElement)).PrimaryTextArgb);

    public static Color CreateSecondaryTextColor(bool isOn, string? hexColor, FrameworkElement? themeElement = null)
        => ToWinColor(FunctionBacklightColor.Resolve(isOn, hexColor, GetAppearanceTheme(themeElement)).SecondaryTextArgb);

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }

    private static SolidColorBrush CreateBrushUncached(
        bool isOn,
        string? hexColor,
        FunctionBacklightColor.AppearanceTheme theme)
    {
        var appearance = FunctionBacklightColor.Resolve(isOn, hexColor, theme);
        return new SolidColorBrush(ToWinColor(appearance.BackgroundArgb));
    }

    internal static FunctionBacklightColor.AppearanceTheme GetAppearanceTheme(FrameworkElement? themeElement)
    {
        return ThemeResourceResolver.IsLightTheme(themeElement)
            ? FunctionBacklightColor.AppearanceTheme.Light
            : FunctionBacklightColor.AppearanceTheme.Dark;
    }

    private static Color ToWinColor(uint argb)
    {
        return Color.FromArgb(
            (byte)((argb >> 24) & 0xFF),
            (byte)((argb >> 16) & 0xFF),
            (byte)((argb >> 8) & 0xFF),
            (byte)(argb & 0xFF));
    }
}
