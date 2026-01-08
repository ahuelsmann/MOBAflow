// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converts an integer index to a boolean indicating if it matches the converter parameter.
/// Used for RadioButton.IsChecked binding with preset selection.
/// </summary>
public class IntToPresetCheckedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is int selectedIndex && parameter is string paramStr && int.TryParse(paramStr, out var targetIndex))
        {
            return selectedIndex == targetIndex;
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        // ConvertBack not used - selection is handled by Commands
        throw new NotImplementedException();
    }
}
