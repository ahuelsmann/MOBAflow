// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Configuration;

/// <summary>
/// Neutral application-wide settings loaded from appsettings.json and read by Backend, speech and the hosts.
/// Settings are independent of Solution and persisted globally. UI sections live in the SharedUI settings root.
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
    /// Shared train control settings (speed ramp, DCC steps, locomotive series).
    /// The locomotive picker selection is host UI state in the SharedUI settings root.
    /// </summary>
    public TrainControlSettings TrainControl { get; set; } = new();

    /// <summary>
    /// Signal Box / Viessmann Multiplex signal settings.
    /// Optional; if not present in appsettings.json, default values are used.
    /// </summary>
    public SignalBoxSettings SignalBox { get; set; } = new();

}