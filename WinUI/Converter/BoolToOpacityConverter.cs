// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converts a boolean value to opacity values.
/// Default: true → 1.0, false → 0.5 (dimmed)
/// Parameter format: "TrueValue|FalseValue" (e.g., "1.0|0.5" or "0.45|0.0")
/// 
/// Usage Examples:
/// - Default dimming: {Binding IsEnabled, Converter={StaticResource BoolToOpacityConverter}}
/// - Glow effect: {Binding IsOn, Converter={StaticResource BoolToOpacityConverter}, ConverterParameter="0.45|0.0"}
/// - Custom values: {Binding IsActive, Converter={StaticResource BoolToOpacityConverter}, ConverterParameter="0.8|0.3"}
/// </summary>
public partial class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        var (trueValue, falseValue) = ParseParameter(parameter, 1.0, 0.5);
        return value is bool boolValue && boolValue ? trueValue : falseValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }

    private static (double TrueValue, double FalseValue) ParseParameter(object? parameter, double defaultTrue, double defaultFalse)
    {
        if (parameter is not string paramStr || string.IsNullOrWhiteSpace(paramStr))
        {
            return (defaultTrue, defaultFalse);
        }

        var parts = paramStr.Split('|');
        if (parts.Length != 2)
        {
            return (defaultTrue, defaultFalse);
        }

        var trueValue = double.TryParse(parts[0], out var t) ? t : defaultTrue;
        var falseValue = double.TryParse(parts[1], out var f) ? f : defaultFalse;

        return (trueValue, falseValue);
    }
}
