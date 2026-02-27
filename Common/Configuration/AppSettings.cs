// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Configuration;

/// <summary>
/// Application-wide settings loaded from appsettings.json.
/// Settings are independent of Solution and persisted globally.
/// </summary>
public class AppSettings
{
    public Z21Settings Z21 { get; set; } = new();
    public RestApiSettings RestApi { get; set; } = new();
    public SpeechSettings Speech { get; set; } = new();
    public ApplicationSettings Application { get; set; } = new();
    public CounterSettings Counter { get; set; } = new();
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
    /// Signal Box / Viessmann Multiplex-Signal Einstellungen (Stellwerk-Seite).
    /// Optional; wenn nicht in appsettings.json vorhanden, werden Standardwerte verwendet.
    /// </summary>
    public SignalBoxSettings SignalBox { get; set; } = new();

    /// <summary>
    /// Gets Azure Speech Service subscription key (convenience property).
    /// </summary>
    public string AzureSpeechKey => string.IsNullOrEmpty(Speech.Key) ? string.Empty : Speech.Key;

    /// <summary>
    /// Gets Azure Speech Service region (convenience property).
    /// </summary>
    public string AzureSpeechRegion => string.IsNullOrEmpty(Speech.Region) ? string.Empty : Speech.Region;
}

/// <summary>
/// REST-API Server connection settings (for MAUI client connecting to WinUI/WebApp server).
/// Manual IP configuration only - no automatic discovery.
/// </summary>
public class RestApiSettings
{
    /// <summary>
    /// Current REST-API server IP address (e.g., "192.168.0.79").
    /// Used by MAUI app to connect to WebApp REST-API.
    /// Default: 192.168.0.79 (adjust to your PC's actual IP)
    /// </summary>
    public string CurrentIpAddress { get; set; } = "192.168.0.79";

    /// <summary>
    /// REST-API server port.
    /// </summary>
    public int Port { get; set; } = 5001;

    /// <summary>
    /// List of recently used IP addresses.
    /// </summary>
    public List<string> RecentIpAddresses { get; set; } = [];
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
    /// Default: 0 = disabled (recommended - uses Z21 broadcast events only for efficiency).
    /// Set to 1-30 if you want redundant polling in addition to broadcasts.
    /// </summary>
    public int SystemStatePollingIntervalSeconds { get; set; }

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
    /// Azure Speech Service Region.
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
    public string SpeakerEngineName { get; set; } = string.Empty;

    /// <summary>
    /// Default voice name.
    /// </summary>
    public string VoiceName { get; set; } = string.Empty;

    /// <summary>
    /// Test message for speech synthesis (used in Settings test button).
    /// Default: German test message.
    /// </summary>
    public string TestMessage { get; set; } = "Dies ist ein Test der Sprachsynthese. Nächster Halt: Hauptbahnhof.";
}

/// <summary>
/// Application behavior settings.
/// </summary>
public class ApplicationSettings
{
    /// <summary>
    /// Dark mode enabled (true = Dark theme, false = Light theme).
    /// Default: true (Dark mode).
    /// </summary>
    public bool IsDarkMode { get; set; } = true;

    /// <summary>
    /// Follow system theme preference (OS dark/light mode).
    /// When true, IsDarkMode is ignored and OS theme is used instead.
    /// Default: false (manual theme control).
    /// </summary>
    public bool UseSystemTheme { get; set; }

    /// <summary>
    /// Selected skin/theme for theme-enabled pages (TrainControlPage2, SignalBoxPage2).
    /// Values: "Modern", "Classic", "Dark", "EsuCabControl", "RocoZ21", "MaerklinCS"
    /// </summary>
    public string SelectedSkin { get; set; } = "Modern";

    /// <summary>
    /// Auto-load last opened solution on startup.
    /// </summary>
    public bool AutoLoadLastSolution { get; set; } = true;

    /// <summary>
    /// Path to last opened solution file.
    /// </summary>
    public string LastSolutionPath { get; set; } = string.Empty;

    /// <summary>
    /// Automatically start the WebApp (Blazor REST/API) alongside WinUI.
    /// NOTE: Since WinUI now has in-process REST API (Kestrel), this is typically not needed.
    /// </summary>
    public bool AutoStartWebApp { get; set; } = true;
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
    public int CountOfFeedbackPoints { get; set; }

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
    public string OverviewPageLabel { get; set; } = string.Empty;

    /// <summary>
    /// Enable Solution page (Project/Solution management).
    /// </summary>
    public bool IsSolutionPageAvailable { get; set; } = true;
    public string SolutionPageLabel { get; set; } = string.Empty;

    /// <summary>
    /// Enable Settings page (Application configuration).
    /// </summary>
    public bool IsSettingsPageAvailable { get; set; } = true;
    public string SettingsPageLabel { get; set; } = string.Empty;

    // Journey Management (Stable - ENABLED by default)

    /// <summary>
    /// Enable Journeys page (Train journey management with stations).
    /// </summary>
    public bool IsJourneysPageAvailable { get; set; } = true;
    public string JourneysPageLabel { get; set; } = string.Empty;

    /// <summary>
    /// Enable Workflows page (Automation workflows for actions).
    /// </summary>
    public bool IsWorkflowsPageAvailable { get; set; } = true;
    public string WorkflowsPageLabel { get; set; } = string.Empty;

    // Track Management (Testing - ENABLED by default)

    /// <summary>
    /// Enable Track Plan Editor page (Visual track layout designer).
    /// Experimental feature marked as Preview.
    /// </summary>
    public bool IsTrackPlanEditorPageAvailable { get; set; } = true;
    public string TrackPlanEditorPageLabel { get; set; } = "Preview";

    /// <summary>
    /// Enable Signal Box page (Visual signal and turnout control panel).
    /// Experimental feature marked as Preview.
    /// </summary>
    public bool IsSignalBoxPageAvailable { get; set; } = true;
    public string SignalBoxPageLabel { get; set; } = "Preview";

    /// <summary>
    /// Enable Journey Map page (Visual journey path visualization).
    /// Experimental feature marked as Preview.
    /// </summary>
    public bool IsJourneyMapPageAvailable { get; set; } = true;
    public string JourneyMapPageLabel { get; set; } = "Preview";

    // Monitoring (Testing - ENABLED by default)

    /// <summary>
    /// Enable Docking page (Layout management with collapsible panels).
    /// Experimental feature marked as Preview. Hidden by default.
    /// </summary>
    public bool IsDockingPageAvailable { get; set; } = false;
    public string DockingPageLabel { get; set; } = "Preview";

    /// <summary>
    /// Enable Monitor page (Real-time system monitoring and diagnostics).
    /// Experimental feature marked as Preview.
    /// </summary>
    public bool IsMonitorPageAvailable { get; set; } = true;
    public string MonitorPageLabel { get; set; } = "Preview";

    // Train/Rolling Stock Management (Upcoming - ENABLED by default)

    /// <summary>
    /// Enable Locomotives page (Locomotive inventory from project/solution).
    /// Upcoming feature marked as Preview.
    /// </summary>
    public bool IsLocomotivesPageAvailable { get; set; } = true;
    public string LocomotivesPageLabel { get; set; } = "Preview";

    /// <summary>
    /// Enable Trains page (Locomotive and wagon inventory management).
    /// Upcoming feature marked as Preview.
    /// </summary>
    public bool IsTrainsPageAvailable { get; set; } = true;
    public string TrainsPageLabel { get; set; } = "Preview";

    // Train Control (Digital Throttle - ENABLED by default)

    /// <summary>
    /// Enable Train Control page (Digital locomotive throttle for Z21).
    /// New feature marked as Preview until fully tested.
    /// </summary>
    public bool IsTrainControlPageAvailable { get; set; } = true;
    public string TrainControlPageLabel { get; set; } = "Preview";
}

/// <summary>
/// Einstellungen für die Stellwerk-Seite (SignalBox) und Viessmann-Multiplex-Signale (z. B. 5229).
/// Ermöglicht pro DCC-Adresse eine getrennte Polaritätsumkehr (Activate-Bit) für die 4 aufeinanderfolgenden Adressen.
/// </summary>
public class SignalBoxSettings
{
    /// <summary>Polarität umkehren für 1. Adresse (Basisadresse + 0, z. B. 201).</summary>
    public bool InvertPolarityOffset0 { get; set; }

    /// <summary>Polarität umkehren für 2. Adresse (Basisadresse + 1, z. B. 202).</summary>
    public bool InvertPolarityOffset1 { get; set; }

    /// <summary>Polarität umkehren für 3. Adresse (Basisadresse + 2, z. B. 203).</summary>
    public bool InvertPolarityOffset2 { get; set; }

    /// <summary>Polarität umkehren für 4. Adresse (Basisadresse + 3, z. B. 204).</summary>
    public bool InvertPolarityOffset3 { get; set; }
}

/// <summary>
/// Layout settings for UI panels and splitters (persisted per user).
/// </summary>
public class LayoutSettings
{
    /// <summary>
    /// Panel position and width settings for the left side panel.
    /// </summary>
    public SidePanelLayoutSettings LeftPanel { get; set; } = new();

    /// <summary>
    /// Panel position and width settings for the right side panel.
    /// </summary>
    public SidePanelLayoutSettings RightPanel { get; set; } = new();

    /// <summary>
    /// Panel position and size settings for the bottom status/controls panel.
    /// </summary>
    public BottomPanelLayoutSettings BottomPanel { get; set; } = new();

    /// <summary>
    /// Tab visibility settings for application views.
    /// </summary>
    public TabVisibilitySettings TabVisibility { get; set; } = new();

    /// <summary>
    /// JourneysPage-specific layout settings (column widths, panel visibility).
    /// </summary>
    public JourneysPageLayoutSettings JourneysPage { get; set; } = new();
}

/// <summary>
/// Panel position and width settings for the side panels (left/right).
/// </summary>
public class SidePanelLayoutSettings
{
    /// <summary>
    /// Is the panel currently open/visible?
    /// </summary>
    public bool IsOpen { get; set; } = true;

    /// <summary>
    /// Width of the panel in pixels.
    /// </summary>
    public double Width { get; set; } = 300;

    /// <summary>
    /// Persistent layout states for structurally docked panels (e.g., Train Control, Signal Box).
    /// </summary>
    public Dictionary<string, PanelLayoutState> PanelLayoutStates { get; set; } = new();
}

/// <summary>
/// Persistent layout state for a structurally docked panel.
/// </summary>
public abstract class PanelLayoutState
{
    /// <summary>
    /// Is the panel in the collapsed state?
    /// </summary>
    public bool IsCollapsed { get; set; } = false;

    /// <summary>
    /// Additional custom state data as needed by the panel.
    /// </summary>
    public Dictionary<string, object> CustomState { get; set; } = new();
}

/// <summary>
/// Panel position and size settings for the bottom status/controls panel.
/// </summary>
public class BottomPanelLayoutSettings
{
    /// <summary>
    /// Is the panel currently open/visible?
    /// </summary>
    public bool IsOpen { get; set; } = true;

    /// <summary>
    /// Height of the panel in pixels.
    /// </summary>
    public double Height { get; set; } = 150;
}

/// <summary>
/// Tab visibility settings for application views.
/// </summary>
public class TabVisibilitySettings
{
    /// <summary>
    /// Is the Dashboard tab visible?
    /// </summary>
    public bool IsDashboardTabVisible { get; set; } = true;

    /// <summary>
    /// Is the Solutions tab visible?
    /// </summary>
    public bool IsSolutionsTabVisible { get; set; } = true;

    /// <summary>
    /// Is the Settings tab visible?
    /// </summary>
    public bool IsSettingsTabVisible { get; set; } = true;

    /// <summary>
    /// Is the Journeys tab visible?
    /// </summary>
    public bool IsJourneysTabVisible { get; set; } = true;

    /// <summary>
    /// Is the Workflows tab visible?
    /// </summary>
    public bool IsWorkflowsTabVisible { get; set; } = true;

    /// <summary>
    /// Is the Feedback Points tab visible?
    /// </summary>
    public bool IsFeedbackPointsTabVisible { get; set; } = true;

    /// <summary>
    /// Is the Track Plan Editor tab visible?
    /// </summary>
    public bool IsTrackPlanEditorTabVisible { get; set; } = true;

    /// <summary>
    /// Is the Signal Box tab visible?
    /// </summary>
    public bool IsSignalBoxTabVisible { get; set; } = true;

    /// <summary>
    /// Is the Journey Map tab visible?
    /// </summary>
    public bool IsJourneyMapTabVisible { get; set; } = true;

    /// <summary>
    /// Is the Monitor tab visible?
    /// </summary>
    public bool IsMonitorTabVisible { get; set; } = true;

    /// <summary>
    /// Is the Trains tab visible?
    /// </summary>
    public bool IsTrainsTabVisible { get; set; } = true;

    /// <summary>
    /// Is the Train Control tab visible?
    /// </summary>
    public bool IsTrainControlTabVisible { get; set; } = true;
}

/// <summary>
/// JourneysPage-specific layout settings (column widths, panel visibility).
/// </summary>
public class JourneysPageLayoutSettings
{
    /// <summary>
    /// Width of Journeys column (Column 0) in pixels.
    /// </summary>
    public double JourneysColumnWidth { get; set; } = 250;

    /// <summary>
    /// Width of Stations column (Column 2) in pixels.
    /// </summary>
    public double StationsColumnWidth { get; set; } = 250;

    /// <summary>
    /// Is City Library panel expanded?
    /// </summary>
    public bool IsCityLibraryExpanded { get; set; } = true;

    /// <summary>
    /// Is Workflow Library panel expanded?
    /// </summary>
    public bool IsWorkflowLibraryExpanded { get; set; } = true;
}