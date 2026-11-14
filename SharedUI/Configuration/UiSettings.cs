// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Configuration;

using Common.Configuration;

using TrainControl;

/// <summary>
/// Settings root of both apps: the neutral <see cref="AppSettings"/> sections plus the UI sections that only
/// shared ViewModels and the hosts read. Hosts register one instance under both types, so Backend keeps
/// receiving <see cref="AppSettings"/> and appsettings.json keeps a single file with unchanged section names.
/// </summary>
public class UiSettings : AppSettings
{
    /// <summary>
    /// MOBAflow-only train control UI state (e.g. active locomotive picker selection).
    /// </summary>
    public TrainControlHostSettings WinUiTrainControlHost { get; set; } = new();

    /// <summary>
    /// MOBAsmart-only train control UI state (e.g. active locomotive picker selection).
    /// </summary>
    public TrainControlHostSettings MauiTrainControlHost { get; set; } = new();

    /// <summary>
    /// Page availability and badge labels (MOBAflow navigation). Missing pages use registry defaults.
    /// </summary>
    public FeatureToggleSettings FeatureToggles { get; set; } = new();

    /// <summary>
    /// Layout settings for UI panels and splitters (persisted per user).
    /// </summary>
    public LayoutSettings Layout { get; set; } = new();

    /// <summary>
    /// Display page settings (ESP display transport/UI preferences).
    /// </summary>
    public DisplaySettings Display { get; set; } = new();
}
