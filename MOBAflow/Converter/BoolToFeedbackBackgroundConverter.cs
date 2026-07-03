// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using Moba.WinUI.View;

/// <summary>
/// Converter that converts a boolean value to a Brush for feedback highlighting.
/// True → Accent color with opacity (feedback), False → Transparent.
/// Pass a <see cref="FrameworkElement"/> as <c>ConverterParameter</c> for runtime theme resolution.
/// </summary>
public partial class BoolToFeedbackBackgroundConverter : IValueConverter
{
    private const byte FeedbackHighlightAlpha = 38;

    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is bool boolValue && boolValue)
        {
            var element = parameter as FrameworkElement ?? ThemeResourceResolver.GetDefaultThemeRoot();
            return ThemeResourceResolver.ResolveAccentWithAlpha(element, FeedbackHighlightAlpha);
        }

        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
