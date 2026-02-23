// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

/// <summary>
/// Converter that converts a boolean value to a Brush for feedback highlighting.
/// True → Accent color with opacity (feedback), False → Transparent.
/// Uses theme-aware AccentFillColorDefaultBrush for Fluent Design 2 consistency.
/// </summary>
internal partial class BoolToFeedbackBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is bool boolValue && boolValue)
        {
            // Use theme-aware accent color (SystemAccentColor with 15% opacity)
            // This respects user's Windows accent color preference
            var accentBrush = Application.Current.Resources["AccentFillColorDefaultBrush"] as SolidColorBrush;
            if (accentBrush != null)
            {
                var accentColor = accentBrush.Color;
                // Apply 15% opacity for subtle highlight
                return new SolidColorBrush(Color.FromArgb(38, accentColor.R, accentColor.G, accentColor.B));
            }

            // Fallback: Purple accent (if theme resource unavailable)
            return new SolidColorBrush(Color.FromArgb(38, 136, 23, 152)); // #26881798 (Purple with 15% opacity)
        }

        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}

