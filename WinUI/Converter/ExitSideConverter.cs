namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converter that converts a boolean IsExitOnLeft value to a readable string.
/// True → "Exit Left", False → "Exit Right"
/// Used for displaying platform exit direction in station info.
/// </summary>
public class ExitSideConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isExitOnLeft)
        {
            return isExitOnLeft ? "Exit Left" : "Exit Right";
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
