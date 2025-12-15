namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converter that formats a track number for display.
/// Converts numeric track value to a readable string like "Track 1".
/// </summary>
public class TrackNumberConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int trackNumber && trackNumber > 0)
        {
            return $"Track {trackNumber}";
        }

        if (value is uint uTrackNumber && uTrackNumber > 0)
        {
            return $"Track {uTrackNumber}";
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
