// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using Moba.Common.Display;

using Windows.UI;

/// <summary>
/// Converts function on-state and accent hex to theme-aware backlight brushes.
/// </summary>
public partial class BoolToBacklightBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        bool isOn = value is bool b && b;
        var hexColor = parameter as string;
        return CreateBrush(isOn, hexColor);
    }

    /// <summary>
    /// Creates the backlight brush for a function button.
    /// </summary>
    public static Brush CreateBrush(bool isOn, string? hexColor)
    {
        if (string.IsNullOrEmpty(hexColor))
        {
            hexColor = "#808080";
        }

        var theme = GetAppearanceTheme();
        var cacheKey = $"{theme}:{(isOn ? '1' : '0')}:{hexColor}";
        return BrushCache.GetOrAdd(cacheKey, () => CreateBrushUncached(isOn, hexColor, theme));
    }

    public static Color CreatePrimaryTextColor(bool isOn, string? hexColor)
        => ToWinColor(FunctionBacklightColor.Resolve(isOn, hexColor, GetAppearanceTheme()).PrimaryTextArgb);

    public static Color CreateSecondaryTextColor(bool isOn, string? hexColor)
        => ToWinColor(FunctionBacklightColor.Resolve(isOn, hexColor, GetAppearanceTheme()).SecondaryTextArgb);

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }

    private static SolidColorBrush CreateBrushUncached(
        bool isOn,
        string hexColor,
        FunctionBacklightColor.AppearanceTheme theme)
    {
        var appearance = FunctionBacklightColor.Resolve(isOn, hexColor, theme);
        return new SolidColorBrush(ToWinColor(appearance.BackgroundArgb));
    }

    private static FunctionBacklightColor.AppearanceTheme GetAppearanceTheme()
    {
        return Application.Current?.RequestedTheme == ApplicationTheme.Light
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
