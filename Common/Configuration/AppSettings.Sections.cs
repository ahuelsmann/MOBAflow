// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Configuration;

using Moba.Domain.Enum;

/// <summary>
/// REST-API server connection settings (MAUI client to MOBApi hosted by MOBAflow).
/// MOBAsmart uses multicast and LAN HTTP discovery; saved IP and recent addresses are probed first.
/// </summary>
public class RestApiSettings
{
    /// <summary>
    /// Current REST-API server IP address (e.g., "192.168.0.34").
    /// Empty until MOBAsmart discovers MOBAflow on the LAN; do not ship a guessed default.
    /// </summary>
    public string CurrentIpAddress { get; set; } = string.Empty;

    /// <summary>
    /// REST-API server port.
    /// </summary>
    public int Port { get; set; } = 5001;

    /// <summary>
    /// When false, MOBAsmart does not discover or connect to MOBAflow (user toggle).
    /// </summary>
    public bool IsConnectionEnabled { get; set; }

    /// <summary>
    /// List of recently used IP addresses.
    /// </summary>
    public List<string> RecentIpAddresses { get; set; } = [];
}

/// <summary>
/// Z21 connection behaviour. The Z21 address belongs to each project (<c>Project.Z21</c>).
/// </summary>
public class Z21Settings
{
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
}

/// <summary>Shared text-to-speech selection and request options.</summary>
public class SpeechSettings
{
    /// <summary>Provider selected for the next announcement.</summary>
    public string ProviderId { get; set; } = "system-speech";
    /// <summary>Maximum retained speech WAV size in MiB; zero disables storage.</summary>
    public int CacheSizeLimitMegabytes { get; set => field = Math.Max(0, value); } = 500;
    /// <summary>Speech rate from -10 to 10.</summary>
    public int Rate { get; set; } = -1;
    /// <summary>Speech volume from 0 to 100.</summary>
    public uint Volume { get; set; } = 90;
    /// <summary>Optional voice name; empty selects the provider default.</summary>
    public string VoiceName { get; set; } = string.Empty;
    /// <summary>Optional language tag used when selecting the default voice.</summary>
    public string Language { get; set; } = string.Empty;
    /// <summary>Text used by the Settings test command.</summary>
    public string TestMessage { get; set; } = "This is a speech synthesis test. Next stop: Central Station.";
    /// <summary>Local Piper synthesis options.</summary>
    public PiperSpeechSettings Piper { get; set; } = new();
    /// <summary>Windows speech synthesis options.</summary>
    public SystemSpeechSettings SystemSpeech { get; set; } = new();
}

/// <summary>Local executable, model and pronunciation options for Piper.</summary>
public class PiperSpeechSettings
{
    /// <summary>Path to the local Piper executable.</summary>
    public string ExecutablePath { get; set => field = value ?? string.Empty; } = string.Empty;
    /// <summary>Path to the selected .onnx voice model.</summary>
    public string ModelPath { get; set => field = value ?? string.Empty; } = string.Empty;
    /// <summary>Optional path to the model configuration.</summary>
    public string ConfigPath { get; set => field = value ?? string.Empty; } = string.Empty;
    /// <summary>Maximum synthesis time in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 30;
    /// <summary>Normalizes station abbreviations before synthesis.</summary>
    public bool EnablePronunciationNormalization { get; set; } = true;
    /// <summary>Pause between sentences in seconds.</summary>
    public double SentenceSilenceSeconds { get; set; }
    /// <summary>Preserves the last WAV in the local diagnostics directory.</summary>
    public bool EnableAudioDiagnostics { get; set; }
    /// <summary>Custom pronunciation replacements.</summary>
    public Dictionary<string, string> PronunciationReplacements { get; set; } = [];
}

/// <summary>Windows speech defaults, independent of a Piper model.</summary>
public class SystemSpeechSettings
{
    /// <summary>Default Windows voice when the shared voice is empty.</summary>
    public string DefaultVoiceName { get; set; } = string.Empty;
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
    /// Auto-load last opened solution on startup.
    /// </summary>
    public bool AutoLoadLastSolution { get; set; } = true;

    /// <summary>
    /// Path to last opened solution file.
    /// </summary>
    public string LastSolutionPath { get; set; } = string.Empty;

    /// <summary>
    /// Automatically start the MOBApi REST host alongside WinUI (see RestApiProcessService).
    /// </summary>
    public bool AutoStartWebApp { get; set; } = true;

    /// <summary>
    /// Base folder for MOBAflow photos (locomotives, wagons, uploads from MAUI).
    /// When null or empty, defaults to My Documents\MOBAflow\Photos.
    /// Can be set to a custom path (e.g. OneDrive folder) so photos are stored in the cloud.
    /// </summary>
    public string PhotoStoragePath { get; set; } = string.Empty;
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
    public int CountOfFeedbackPoints
    {
        get => _countOfFeedbackPoints;
        set
        {
            // Clamp instead of throwing so a hand-edited settings file cannot fail the whole settings load.
            value = Math.Max(value, 0);
            if (_countOfFeedbackPoints == value) return;
            _countOfFeedbackPoints = value;
            FeedbackPointsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private int _countOfFeedbackPoints;

    /// <summary>Raised when the configured local input inventory changes.</summary>
    public event EventHandler? FeedbackPointsChanged;

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
/// Settings for the Signal Box page and Viessmann Multiplex signals (e.g. 5229).
/// Allows separate polarity inversion (Activate bit) per DCC address for the 4 consecutive addresses.
/// </summary>
public class SignalBoxSettings
{
    /// <summary>Invert polarity for address 1 (base address + 0, e.g. 201).</summary>
    public bool InvertPolarityOffset0 { get; set; }

    /// <summary>Invert polarity for address 2 (base address + 1, e.g. 202).</summary>
    public bool InvertPolarityOffset1 { get; set; }

    /// <summary>Invert polarity for address 3 (base address + 2, e.g. 203).</summary>
    public bool InvertPolarityOffset2 { get; set; }

    /// <summary>Invert polarity for address 4 (base address + 3, e.g. 204).</summary>
    public bool InvertPolarityOffset3 { get; set; }

    /// <summary>
    /// Gets whether polarity inversion is enabled for the given address offset.
    /// </summary>
    /// <param name="addressOffset">Offset relative to base address (0..3).</param>
    /// <returns>True when inversion is enabled for the offset; otherwise false.</returns>
    public bool GetInvertPolarityForOffset(int addressOffset)
    {
        return addressOffset switch
        {
            0 => InvertPolarityOffset0,
            1 => InvertPolarityOffset1,
            2 => InvertPolarityOffset2,
            3 => InvertPolarityOffset3,
            _ => false
        };
    }
}
