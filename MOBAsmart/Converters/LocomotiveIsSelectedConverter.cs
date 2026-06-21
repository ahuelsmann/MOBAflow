// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Converters;

using SharedUI.ViewModel;

using System.Globalization;

/// <summary>
/// Returns true when the bound locomotive matches the converter parameter locomotive.
/// </summary>
public sealed class LocomotiveIsSelectedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is LocomotiveViewModel selected
               && parameter is LocomotiveViewModel candidate
               && selected.Model.Id == candidate.Model.Id;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
