// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Sound;

using Common.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moba.Common.Platform;

/// <summary>
/// Factory for creating speaker engines based on configuration.
/// Allows dynamic switching between Piper TTS and Windows SAPI.
/// Uses AppSettings for engine selection to support runtime changes via UI.
/// </summary>
public interface ISpeakerEngineFactory
{
    ISpeakerEngine CreateEngine(string engineName);

    ISpeakerEngine CreateEngineFromOptions();
}

public class SpeakerEngineFactory : ISpeakerEngineFactory
{
    private readonly AppSettings _appSettings;
    private readonly IOptionsMonitor<SpeechOptions> _optionsMonitor;
    private readonly ILogger<PiperSpeechEngine> _piperLogger;
    private readonly ILogger<SystemSpeechEngine> _systemLogger;
    private readonly IReadOnlyList<ISpeakerEngineRegistration> _registrations;

    private readonly IPlatformCapability _platformCapability;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpeakerEngineFactory"/>.
    /// </summary>
    /// <param name="appSettings">Global application settings including speech configuration.</param>
    /// <param name="optionsMonitor">Options monitor for speech configuration.</param>
    /// <param name="piperLogger">Logger instance for the Piper-based speech engine.</param>
    /// <param name="systemLogger">Logger instance for the Windows system speech engine.</param>
    /// <param name="registrations">Optional engine registrations; defaults to Piper and Windows SAPI when supported.</param>
    /// <param name="platformCapability">Optional platform capability probe for Windows-only engines.</param>
    public SpeakerEngineFactory(
        AppSettings appSettings,
        IOptionsMonitor<SpeechOptions> optionsMonitor,
        ILogger<PiperSpeechEngine> piperLogger,
        ILogger<SystemSpeechEngine> systemLogger,
        IEnumerable<ISpeakerEngineRegistration>? registrations = null,
        IPlatformCapability? platformCapability = null)
    {
        _appSettings = appSettings;
        _optionsMonitor = optionsMonitor;
        _piperLogger = piperLogger;
        _systemLogger = systemLogger;
        _platformCapability = platformCapability ?? new RuntimePlatformCapability();
        _registrations = registrations?.ToList() is { Count: > 0 } registeredEngines
            ? registeredEngines
            : CreateDefaultRegistrations();
    }

    /// <summary>
    /// Creates the appropriate speaker engine based on current configuration.
    /// </summary>
    /// <param name="engineName">
    /// Engine id: <see cref="SpeechSpeakerEngineSelection.PiperTts"/> or
    /// <see cref="SpeechSpeakerEngineSelection.SystemSpeech"/>; legacy display strings
    /// <see cref="SpeechSpeakerEngineSelection.PiperDisplayName"/> are still accepted.
    /// </param>
    /// <returns>Configured speaker engine</returns>
    public ISpeakerEngine CreateEngine(string engineName)
    {
        _systemLogger.LogDebug("🔊 [FACTORY] Creating engine for: '{EngineName}'", engineName);

        var registration = _registrations.FirstOrDefault(engine => engine.CanCreate(engineName))
            ?? _registrations.First(engine => engine.IsFallback);

        _systemLogger.LogDebug("🔊 [FACTORY] ✅ Creating {EngineType}", registration.EngineName);
        return registration.Create(CreateCurrentSpeechOptions());
    }

    /// <summary>
    /// Creates the appropriate speaker engine based on current AppSettings.
    /// Uses AppSettings.Speech.SpeakerEngineName which is updated when user changes engine in UI.
    /// </summary>
    public ISpeakerEngine CreateEngineFromOptions()
    {
        // ✅ FIX: Use AppSettings.Speech.SpeakerEngineName instead of SpeechOptions
        // This allows runtime engine switching via UI
        var engineName = _appSettings.Speech.SpeakerEngineName;
        _systemLogger.LogDebug("🔊 [FACTORY] AppSettings.Speech.SpeakerEngineName = '{EngineName}'", engineName);
        return CreateEngine(engineName);
    }

    private SpeechOptions CreateCurrentSpeechOptions()
    {
        var configuredOptions = _optionsMonitor.CurrentValue;
        var settings = _appSettings.Speech;

        return new SpeechOptions
        {
            PiperExecutablePath = settings.PiperExecutablePath,
            PiperModelPath = settings.PiperModelPath,
            PiperConfigPath = settings.PiperConfigPath,
            Rate = settings.Rate,
            Volume = (int)settings.Volume,
            VoiceName = settings.VoiceName,
            SpeakerEngineName = settings.SpeakerEngineName,
            PiperTimeoutSeconds = configuredOptions.PiperTimeoutSeconds,
            EnablePronunciationNormalization = settings.EnablePronunciationNormalization,
            PiperSentenceSilenceSeconds = settings.PiperSentenceSilenceSeconds,
            PronunciationReplacements = settings.PronunciationReplacements
        };
    }

    private sealed class CurrentSpeechOptionsMonitor(SpeechOptions value) : IOptionsMonitor<SpeechOptions>
    {
        public SpeechOptions CurrentValue => value;

        public SpeechOptions Get(string? name)
        {
            return value;
        }

        public IDisposable? OnChange(Action<SpeechOptions, string?> listener)
        {
            return null;
        }
    }

    private IReadOnlyList<ISpeakerEngineRegistration> CreateDefaultRegistrations()
    {
        var registrations = new List<ISpeakerEngineRegistration>();
        var supportsSystemSpeech = _platformCapability.SupportsWindowsSystemSpeech;

        registrations.Add(new PiperSpeakerEngineRegistration(_piperLogger, isFallback: !supportsSystemSpeech));

        if (supportsSystemSpeech)
        {
            registrations.Add(new SystemSpeechEngineRegistration(_systemLogger, _platformCapability));
        }

        return registrations;
    }
}

/// <summary>
/// Creates a speech engine for one registered engine selection.
/// </summary>
public interface ISpeakerEngineRegistration
{
    /// <summary>
    /// Gets the display name used in diagnostics.
    /// </summary>
    string EngineName { get; }

    /// <summary>
    /// Gets whether this registration is used when no specific registration matches.
    /// </summary>
    bool IsFallback { get; }

    /// <summary>
    /// Returns true when this registration handles the configured engine name.
    /// </summary>
    bool CanCreate(string engineName);

    /// <summary>
    /// Creates a configured engine instance.
    /// </summary>
    ISpeakerEngine Create(SpeechOptions options);
}

/// <summary>
/// Registration for local Piper TTS.
/// </summary>
public sealed class PiperSpeakerEngineRegistration(
    ILogger<PiperSpeechEngine> logger,
    bool isFallback = false) : ISpeakerEngineRegistration
{
    public string EngineName => SpeechSpeakerEngineSelection.PiperDisplayName;

    public bool IsFallback => isFallback;

    public bool CanCreate(string engineName) =>
        SpeechSpeakerEngineSelection.ShouldUsePiperTts(engineName);

    public ISpeakerEngine Create(SpeechOptions options) =>
        new PiperSpeechEngine(new CurrentSpeechOptionsMonitor(options), logger);

    private sealed class CurrentSpeechOptionsMonitor(SpeechOptions value) : IOptionsMonitor<SpeechOptions>
    {
        public SpeechOptions CurrentValue => value;

        public SpeechOptions Get(string? name) => value;

        public IDisposable? OnChange(Action<SpeechOptions, string?> listener) => null;
    }
}

/// <summary>
/// Registration for Windows SAPI.
/// </summary>
public sealed class SystemSpeechEngineRegistration(
    ILogger<SystemSpeechEngine> logger,
    IPlatformCapability? platformCapability = null) : ISpeakerEngineRegistration
{
    private readonly IPlatformCapability _platformCapability = platformCapability ?? new RuntimePlatformCapability();

    public string EngineName => SpeechSpeakerEngineSelection.SystemSpeech;

    public bool IsFallback => _platformCapability.SupportsWindowsSystemSpeech;

    public bool CanCreate(string engineName) =>
        _platformCapability.SupportsWindowsSystemSpeech &&
        SpeechSpeakerEngineSelection.ShouldUseSystemSpeech(engineName);

    public ISpeakerEngine Create(SpeechOptions options)
    {
        _ = options;
        return new SystemSpeechEngine(logger);
    }
}
