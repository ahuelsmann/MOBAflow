// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Moba.WinUI.View;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

public partial class ResourceKeyToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string resourceKey)
        {
            return ThemeResourceResolver.ResolveBrush(App.MainWindow?.Content as FrameworkElement, resourceKey, Colors.Black);
        }

        return ThemeResourceResolver.ResolveBrush(App.MainWindow?.Content as FrameworkElement, "TextFillColorPrimaryBrush", Colors.Black);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}