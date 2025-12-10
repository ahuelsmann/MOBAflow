// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Configuration;

/// <summary>
/// Application-wide settings loaded from appsettings.json.
/// Settings are independent of Solution and persisted globally.
/// </summary>
public class AppSettings
{
    public Z21Settings Z21 { get; set; } = new();
    public SpeechSettings Speech { get; set; } = new();
    public CityLibrarySettings CityLibrary { get; set; } = new();
    public ApplicationSettings Application { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
    public HealthCheckSettings HealthCheck { get; set; } = new();
}

/// <summary>
/// Z21 digital command station connection settings.
/// </summary>
public class Z21Settings
{
    /// <summary>
    /// Current Z21 IP address.
    /// </summary>
    public string CurrentIpAddress { get; set; } = "192.168.0.111";

    /// <summary>
    /// Default Z21 port.
    /// </summary>
    public string DefaultPort { get; set; } = "21105";

    /// <summary>
    /// List of recently used IP addresses.
    /// </summary>
    public List<string> RecentIpAddresses { get; set; } = [];
}

/// <summary>
/// Azure Speech Synthesis configuration.
/// </summary>
public class SpeechSettings
{
    /// <summary>
    /// Azure Speech Service API Key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Azure Speech Service Region (e.g., "germanywestcentral").
    /// </summary>
    public string Region { get; set; } = "germanywestcentral";

    /// <summary>
    /// Speech synthesis rate (-10 to 10).
    /// </summary>
    public int Rate { get; set; } = -1;

    /// <summary>
    /// Speech synthesis volume (0-100).
    /// </summary>
    public uint Volume { get; set; } = 90;

    /// <summary>
    /// Default speaker engine name.
    /// </summary>
    public string? SpeakerEngineName { get; set; }

    /// <summary>
    /// Default voice name.
    /// </summary>
    public string? VoiceName { get; set; }
}

/// <summary>
/// City library (station master data) configuration.
/// </summary>
public class CityLibrarySettings
{
    /// <summary>
    /// Path to city/station JSON file (e.g., "germany-stations.json").
    /// Supports absolute or relative paths.
    /// </summary>
    public string FilePath { get; set; } = "germany-stations.json";

    /// <summary>
    /// Enable auto-reload when file changes.
    /// </summary>
    public bool AutoReload { get; set; }
}

/// <summary>
/// Application behavior settings.
/// </summary>
public class ApplicationSettings
{
    /// <summary>
    /// Reset window layout (size, position) on startup.
    /// </summary>
    public bool ResetWindowLayoutOnStart { get; set; }

    /// <summary>
    /// Auto-load last opened solution on startup.
    /// </summary>
    public bool AutoLoadLastSolution { get; set; } = true;

    /// <summary>
    /// Path to last opened solution file.
    /// </summary>
    public string? LastSolutionPath { get; set; }
}

/// <summary>
/// Logging configuration.
/// </summary>
public class LoggingSettings
{
    public Dictionary<string, string> LogLevel { get; set; } = new()
    {
        { "Default", "Information" },
        { "Microsoft", "Warning" },
        { "Moba", "Debug" }
    };
}

/// <summary>
/// Health check configuration.
/// </summary>
public class HealthCheckSettings
{
    /// <summary>
    /// Enable Z21 connection health checks.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Health check interval in seconds.
    /// </summary>
    public int IntervalSeconds { get; set; } = 60;
}
