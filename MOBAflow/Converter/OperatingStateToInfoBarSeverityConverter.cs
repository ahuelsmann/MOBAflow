// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

using Moba.SharedUI.ViewModel;

/// <summary>
/// Converts <see cref="OperatingStateKind"/> values to <see cref="InfoBarSeverity"/>.
/// </summary>
public sealed class OperatingStateToInfoBarSeverityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var state = value is OperatingStateKind operatingState
            ? operatingState
            : OperatingStateKind.Recovering;

        return state switch
        {
            OperatingStateKind.FailSafe => InfoBarSeverity.Error,
            OperatingStateKind.Degraded => InfoBarSeverity.Warning,
            OperatingStateKind.Normal => InfoBarSeverity.Success,
            _ => InfoBarSeverity.Informational
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
