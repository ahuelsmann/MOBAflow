// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Configuration;

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
    public CounterSettings Counter { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
    public HealthCheckSettings HealthCheck { get; set; } = new();
    /// <summary>
    /// Feature toggles for experimental/preview features (WinUI only).
    /// This setting is optional - if not present in appsettings.json, defaults to new FeatureToggleSettings().
    /// </summary>
    public FeatureToggleSettings? FeatureToggles { get; set; }

    /// <summary>
    /// Gets Azure Speech Service subscription key (convenience property).
    /// </summary>
    public string? AzureSpeechKey => string.IsNullOrEmpty(Speech.Key) ? null : Speech.Key;

    /// <summary>
    /// Gets Azure Speech Service region (convenience property).
    /// </summary>
    public string? AzureSpeechRegion => string.IsNullOrEmpty(Speech.Region) ? null : Speech.Region;
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
    /// Auto-connect retry interval in seconds (how often to attempt reconnection if Z21 is unreachable).
    /// Default: 10 seconds.
    /// </summary>
    public int AutoConnectRetryIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// System state polling interval in seconds (how often to request current, voltage, temperature updates).
    /// Default: 5 seconds. Set to 0 to disable polling (rely only on Z21 broadcast events).
    /// </summary>
    public int SystemStatePollingIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// List of recently used IP addresses.
    /// </summary>
    public List<string> RecentIpAddresses { get; set; } = new();
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
/// Counter/Statistics feature configuration.
/// </summary>
public class CounterSettings
{
    /// <summary>
    /// Number of feedback points (InPorts) in your track layout.
    /// Used to auto-initialize track statistics when no Solution is loaded (default: 0).
    /// Example: If you have 3 feedback points, set this to 3 → Statistics will show InPorts 1, 2, 3.
    /// Note: InPort 0 means "disabled" or "not in use". InPort 1 = Feedback Point 1 (simple 1:1 mapping).
    /// </summary>
    public int CountOfFeedbackPoints { get; set; } = 0;

    /// <summary>
    /// Global target lap count for all tracks (default: 10).
    /// </summary>
    public int TargetLapCount { get; set; } = 10;

    /// <summary>
    /// Enables timer-based filtering to prevent multiple counts from long trains (default: true).
    /// </summary>
    public bool UseTimerFilter { get; set; } = true;

    /// <summary>
    /// Timer filter interval in seconds (prevents duplicate counts within this timeframe, default: 10).
    /// </summary>
    public double TimerIntervalSeconds { get; set; } = 10.0;
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

/// <summary>
/// Feature toggle configuration for controlling page visibility.
/// Used to control which features are available in the application (e.g., for Open Source releases).
/// These settings are transparent - users can enable/disable features by editing appsettings.json.
/// </summary>
public class FeatureToggleSettings
{
    // Core Features (Stable - ENABLED by default)
    
    /// <summary>
    /// Enable Overview page (Dashboard with journey status).
    /// </summary>
    public bool IsOverviewPageAvailable { get; set; } = true;
    public string? OverviewPageLabel { get; set; }

    /// <summary>
    /// Enable Solution page (Project/Solution management).
    /// </summary>
    public bool IsSolutionPageAvailable { get; set; } = true;
    public string? SolutionPageLabel { get; set; }

    /// <summary>
    /// Enable Settings page (Application configuration).
    /// </summary>
    public bool IsSettingsPageAvailable { get; set; } = true;
    public string? SettingsPageLabel { get; set; }

    // Journey Management (Stable - ENABLED by default)
    
    /// <summary>
    /// Enable Journeys page (Train journey management with stations).
    /// </summary>
    public bool IsJourneysPageAvailable { get; set; } = true;
    public string? JourneysPageLabel { get; set; }

    /// <summary>
    /// Enable Workflows page (Automation workflows for actions).
    /// </summary>
    public bool IsWorkflowsPageAvailable { get; set; } = true;
    public string? WorkflowsPageLabel { get; set; }

    // Track Management (Testing - DISABLED by default for Open Source)

    /// <summary>
    /// Enable Feedback Points page (Track sensor configuration).
    /// Experimental feature - disabled by default for Open Source release.
    /// </summary>
    public bool IsFeedbackPointsPageAvailable { get; set; }
    public string? FeedbackPointsPageLabel { get; set; } = "Preview";

    /// <summary>
    /// Enable Track Plan Editor page (Visual track layout designer).
    /// Experimental feature - disabled by default for Open Source release.
    /// </summary>
    public bool IsTrackPlanEditorPageAvailable { get; set; }
    public string? TrackPlanEditorPageLabel { get; set; } = "Preview";

    /// <summary>
    /// Enable Journey Map page (Visual journey path visualization).
    /// Experimental feature - disabled by default for Open Source release.
    /// </summary>
    public bool IsJourneyMapPageAvailable { get; set; }
    public string? JourneyMapPageLabel { get; set; } = "Preview";

    // Monitoring (Testing - DISABLED by default)

    /// <summary>
    /// Enable Monitor page (Real-time system monitoring and diagnostics).
    /// Experimental feature - disabled by default for Open Source release.
    /// </summary>
    public bool IsMonitorPageAvailable { get; set; } = false;
    public string? MonitorPageLabel { get; set; } = "Beta";
}