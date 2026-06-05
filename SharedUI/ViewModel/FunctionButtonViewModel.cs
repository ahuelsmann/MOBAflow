// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// ViewModel for a single locomotive function button (F0–F31) on the Train Control page.
/// Replaces the previously duplicated per-function properties (IsF0On..IsF31On,
/// Function0Glyph..Function31Glyph) with a single reusable item used by a collection.
///
/// What a function actually triggers is decided by the locomotive decoder; MOBAflow only
/// provides the correct symbol per locomotive (sourced from Domain.Locomotive.FunctionSymbols).
/// Platform-neutral: contains no WinUI/MAUI types.
/// </summary>
public sealed partial class FunctionButtonViewModel : ObservableObject
{
    /// <summary>
    /// DCC function index (0 = F0, … 31 = F31). Used as the command parameter.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Display label of the function key (e.g. "F0").
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Fixed backlight accent color (hex, e.g. "#FFD700") used by the WinUI button background.
    /// Migrated from the previously hard-coded XAML converter parameters.
    /// </summary>
    public string BacklightColorHex { get; }

    /// <summary>
    /// Current on/off state of the function.
    /// </summary>
    [ObservableProperty]
    private bool _isOn;

    /// <summary>
    /// SVG asset filename for the button symbol (from FunctionSymbols/defaults). Empty = no symbol.
    /// </summary>
    [ObservableProperty]
    private string _iconAsset = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionButtonViewModel"/> class.
    /// </summary>
    /// <param name="index">DCC function index (0–31).</param>
    /// <param name="backlightColorHex">Fixed backlight accent color as hex string.</param>
    public FunctionButtonViewModel(int index, string backlightColorHex)
    {
        Index = index;
        Label = $"F{index}";
        BacklightColorHex = backlightColorHex;
    }
}
