// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

/// <summary>
/// Displays the current locomotive details and preset quick switches.
/// </summary>
public sealed partial class LocomotiveInfoCardControl : UserControl
{
    /// <summary>
    /// Current locomotive preset name.
    /// </summary>
    public static readonly DependencyProperty CurrentPresetNameProperty =
        DependencyProperty.Register(nameof(CurrentPresetName), typeof(string), typeof(LocomotiveInfoCardControl),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Current locomotive DCC address.
    /// </summary>
    public static readonly DependencyProperty LocoAddressProperty =
        DependencyProperty.Register(nameof(LocoAddress), typeof(int), typeof(LocomotiveInfoCardControl),
            new PropertyMetadata(0));

    /// <summary>
    /// Current locomotive vmax in km/h.
    /// </summary>
    public static readonly DependencyProperty SelectedVmaxProperty =
        DependencyProperty.Register(nameof(SelectedVmax), typeof(int), typeof(LocomotiveInfoCardControl),
            new PropertyMetadata(0));

    /// <summary>
    /// Preset 1 display name.
    /// </summary>
    public static readonly DependencyProperty Preset1NameProperty =
        DependencyProperty.Register(nameof(Preset1Name), typeof(string), typeof(LocomotiveInfoCardControl),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Preset 2 display name.
    /// </summary>
    public static readonly DependencyProperty Preset2NameProperty =
        DependencyProperty.Register(nameof(Preset2Name), typeof(string), typeof(LocomotiveInfoCardControl),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Preset 3 display name.
    /// </summary>
    public static readonly DependencyProperty Preset3NameProperty =
        DependencyProperty.Register(nameof(Preset3Name), typeof(string), typeof(LocomotiveInfoCardControl),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Command to select preset 1.
    /// </summary>
    public static readonly DependencyProperty Preset1CommandProperty =
        DependencyProperty.Register(nameof(Preset1Command), typeof(ICommand), typeof(LocomotiveInfoCardControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Command to select preset 2.
    /// </summary>
    public static readonly DependencyProperty Preset2CommandProperty =
        DependencyProperty.Register(nameof(Preset2Command), typeof(ICommand), typeof(LocomotiveInfoCardControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Command to select preset 3.
    /// </summary>
    public static readonly DependencyProperty Preset3CommandProperty =
        DependencyProperty.Register(nameof(Preset3Command), typeof(ICommand), typeof(LocomotiveInfoCardControl),
            new PropertyMetadata(null));

    public LocomotiveInfoCardControl()
    {
        InitializeComponent();
    }

    public string CurrentPresetName
    {
        get => (string)GetValue(CurrentPresetNameProperty);
        set => SetValue(CurrentPresetNameProperty, value);
    }

    public int LocoAddress
    {
        get => (int)GetValue(LocoAddressProperty);
        set => SetValue(LocoAddressProperty, value);
    }

    public int SelectedVmax
    {
        get => (int)GetValue(SelectedVmaxProperty);
        set => SetValue(SelectedVmaxProperty, value);
    }

    public string Preset1Name
    {
        get => (string)GetValue(Preset1NameProperty);
        set => SetValue(Preset1NameProperty, value);
    }

    public string Preset2Name
    {
        get => (string)GetValue(Preset2NameProperty);
        set => SetValue(Preset2NameProperty, value);
    }

    public string Preset3Name
    {
        get => (string)GetValue(Preset3NameProperty);
        set => SetValue(Preset3NameProperty, value);
    }

    public ICommand? Preset1Command
    {
        get => (ICommand?)GetValue(Preset1CommandProperty);
        set => SetValue(Preset1CommandProperty, value);
    }

    public ICommand? Preset2Command
    {
        get => (ICommand?)GetValue(Preset2CommandProperty);
        set => SetValue(Preset2CommandProperty, value);
    }

    public ICommand? Preset3Command
    {
        get => (ICommand?)GetValue(Preset3CommandProperty);
        set => SetValue(Preset3CommandProperty, value);
    }
}
