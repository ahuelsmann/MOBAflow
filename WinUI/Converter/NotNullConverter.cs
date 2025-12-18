// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converter that returns Visible when value is not null, Collapsed otherwise.
/// Used to show content when data is available.
/// </summary>
public class NotNullConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
