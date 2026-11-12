// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Converters;

using System.Globalization;

/// <summary>
/// Builds the TalkBack description of a locomotive card from its name, digital address and selection state.
/// The card binds all three, so the description follows in-place fleet updates of the same locomotive.
/// </summary>
public sealed class LocomotiveCardDescriptionConverter : IMultiValueConverter
{
    /// <summary>
    /// Word that starts the description; it stands alone when the locomotive has no name.
    /// </summary>
    public string Subject { get; set; } = "Locomotive";

    /// <summary>
    /// Appended while the card shows the selected locomotive.
    /// </summary>
    public string SelectedSuffix { get; set; } = ", selected";

    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(values);

        var name = values.Length > 0 ? values[0] as string : null;
        var title = string.IsNullOrWhiteSpace(name) ? Subject : $"{Subject} {name}";
        var address = values.Length > 1 && values[1] is uint digitalAddress
            ? string.Create(CultureInfo.InvariantCulture, $", digital address {digitalAddress}")
            : string.Empty;
        var state = values.Length > 2 && values[2] is true ? SelectedSuffix : string.Empty;
        return $"{title}{address}{state}";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException($"{GetType().Name} only builds the card description; it has no reverse.");
    }
}
