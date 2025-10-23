namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// Konvertiert Boolean in ein Connection-Status-Icon (Connected/Disconnected)
/// </summary>
public class BoolToConnectionIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isConnected)
        {
            // ✅ Connected: Plug-Icon
            // ❌ Disconnected: PlugDisconnected-Icon
            return isConnected ? "\uE8EB" : "\uF384";
        }

        return "\uF384"; // Disconnected als Default
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
