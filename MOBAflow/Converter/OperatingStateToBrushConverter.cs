// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using Moba.SharedUI.ViewModel;

using Windows.UI;

/// <summary>
/// Converts <see cref="OperatingStateKind"/> values into subtle Fluent-style brushes for the shell status badge.
/// </summary>
internal sealed class OperatingStateToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var state = value is OperatingStateKind operatingState
            ? operatingState
            : OperatingStateKind.Recovering;
        var mode = parameter as string ?? "Foreground";

        return mode.Equals("Background", StringComparison.OrdinalIgnoreCase)
            ? new SolidColorBrush(WithAlpha(GetBaseColor(state), 0x18))
            : new SolidColorBrush(GetBaseColor(state));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();

    private static Color GetBaseColor(OperatingStateKind state) => state switch
    {
        OperatingStateKind.Normal => ColorHelper.FromArgb(0xFF, 0x0F, 0x7B, 0x0F),
        OperatingStateKind.Degraded => ColorHelper.FromArgb(0xFF, 0x9A, 0x67, 0x00),
        OperatingStateKind.FailSafe => ColorHelper.FromArgb(0xFF, 0xC4, 0x2B, 0x1C),
        _ => ColorHelper.FromArgb(0xFF, 0x5F, 0x6A, 0x7D)
    };

    private static Color WithAlpha(Color color, byte alpha)
        => ColorHelper.FromArgb(alpha, color.R, color.G, color.B);
}
