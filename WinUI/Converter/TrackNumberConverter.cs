namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converter that formats a track number for display.
/// Converts numeric track value to a readable string like "Track 1".
/// </summary>
public partial class TrackNumberConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is int { } trackNumber && trackNumber > 0
            ? $"Track {trackNumber}"
            : value is uint { } uTrackNumber && uTrackNumber > 0 ? $"Track {uTrackNumber}" : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
