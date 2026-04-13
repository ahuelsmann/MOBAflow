namespace Moba.WinUI.Converter;

using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converter that converts a boolean value to a FontWeight.
/// True → Bold, False → Normal
/// Used for highlighting current/active items in lists.
/// </summary>
public partial class BoolToFontWeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool boolValue ? boolValue ? FontWeights.Bold : FontWeights.Normal : FontWeights.Normal;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
