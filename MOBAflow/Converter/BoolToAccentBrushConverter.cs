namespace Moba.WinUI.Converter;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

/// <summary>
/// Converter that converts a boolean value to a Brush.
/// True → AccentFillColorDefaultBrush, False → TextFillColorSecondaryBrush
/// Used for highlighting current/active items with accent color.
/// </summary>
public partial class BoolToAccentBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool { } boolValue && boolValue
            ? Application.Current.Resources["AccentFillColorDefaultBrush"] as Brush
                ?? new SolidColorBrush(Colors.Blue)
            : Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush
            ?? new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
