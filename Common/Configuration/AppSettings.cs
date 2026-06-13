// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Configuration;

/// <summary>
/// Application-wide settings loaded from appsettings.json.
/// Settings are independent of Solution and persisted globally.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Gets or sets settings for the Z21 digital command station.
    /// </summary>
    public Z21Settings Z21 { get; set; } = new();

    /// <summary>
    /// Gets or sets REST API server connection settings.
    /// </summary>
    public RestApiSettings RestApi { get; set; } = new();

    /// <summary>
    /// Gets or sets text-to-speech synthesis configuration.
    /// </summary>
    public SpeechSettings Speech { get; set; } = new();

    /// <summary>
    /// Gets or sets general application behavior settings.
    /// </summary>
    public ApplicationSettings Application { get; set; } = new();

    /// <summary>
    /// Gets or sets counter/statistics feature configuration.
    /// </summary>
    public CounterSettings Counter { get; set; } = new();

    /// <summary>
    /// Gets or sets health check configuration.
    /// </summary>
    public HealthCheckSettings HealthCheck { get; set; } = new();
    /// <summary>
    /// Train Control settings including locomotive presets for quick switching.
    /// </summary>
    public TrainControlSettings TrainControl { get; set; } = new();
    /// <summary>
    /// Feature toggles for experimental/preview features (WinUI only).
    /// This setting is optional - if not present in appsettings.json, defaults to new FeatureToggleSettings().
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

    /// <summary>
    /// Signal Box / Viessmann Multiplex signal settings.
    /// Optional; if not present in appsettings.json, default values are used.
    /// </summary>
    public SignalBoxSettings SignalBox { get; set; } = new();

    /// <summary>
    /// Gets the configured Piper executable path (convenience property).
    /// </summary>
    public string PiperExecutablePath => string.IsNullOrEmpty(Speech.PiperExecutablePath) ? string.Empty : Speech.PiperExecutablePath;

    /// <summary>
    /// Gets the configured Piper model path (convenience property).
    /// </summary>
    public string PiperModelPath => string.IsNullOrEmpty(Speech.PiperModelPath) ? string.Empty : Speech.PiperModelPath;
}
