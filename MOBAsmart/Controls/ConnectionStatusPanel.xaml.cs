// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Controls;

public partial class ConnectionStatusPanel
{
    public ConnectionStatusPanel()
    {
        InitializeComponent();
    }

    public event EventHandler<ToggledEventArgs>? ThemeSwitchToggled;

    public event EventHandler<ToggledEventArgs>? TrackPowerSwitchToggled;

    public Label ThemeIconLabel => ThemeIcon;

    public Switch ThemeSwitchControl => ThemeSwitch;

    public Switch TrackPowerSwitchControl => TrackPowerSwitch;

    public Border ConnectionIndicatorBorder => ConnectionIndicator;

    private void ThemeSwitch_Toggled(object? sender, ToggledEventArgs e) =>
        ThemeSwitchToggled?.Invoke(sender, e);

    private void TrackPowerSwitch_Toggled(object? sender, ToggledEventArgs e) =>
        TrackPowerSwitchToggled?.Invoke(sender, e);
}