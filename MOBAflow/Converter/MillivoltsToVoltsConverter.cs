// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;

/// &lt;summary&gt;
/// Converts millivolts (mV) to volts (V) for display.
/// Example: 16000 mV → "16.0" V
/// &lt;/summary&gt;
internal class MillivoltsToVoltsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int millivolts)
        {
            var volts = millivolts / 1000.0;
            return volts.ToString("F1"); // 1 decimal place
        }
        return "0.0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
