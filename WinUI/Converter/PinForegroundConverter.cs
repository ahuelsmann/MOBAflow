// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

/// <summary>
/// Keeps pin foreground styling consistent with theme resources.
/// </summary>
public sealed class PinForegroundConverter : IValueConverter
{
    /// <summary>
    /// Required for XAML bindings that need a theme-based brush for pin state.
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool { } isPinned && isPinned
            ? Application.Current.Resources["AccentFillColorDefaultBrush"] as Brush
                ?? new SolidColorBrush(Colors.Blue)
            : Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush
                ?? new SolidColorBrush(Colors.Gray);
    }

    /// <summary>
    /// Not supported because pin state binding is one-way in the UI.
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
