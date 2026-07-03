// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

using Moba.WinUI.View;

public partial class ResourceKeyToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var element = parameter as FrameworkElement ?? ThemeResourceResolver.GetDefaultThemeRoot();
        if (value is string resourceKey)
        {
            return ThemeResourceResolver.ResolveBrush(element, resourceKey, Colors.Black);
        }

        return ThemeResourceResolver.ResolveBrush(element, "TextFillColorPrimaryBrush", Colors.Black);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
