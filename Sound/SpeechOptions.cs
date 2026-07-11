// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Sound;

/// <summary>
/// Configuration options for text-to-speech services.
/// Used with IOptions pattern for dependency injection.
/// </summary>
public class SpeechOptions
{
    /// <summary>
    /// Path to the local Piper executable.
    /// Can be set via environment variable PIPER_EXECUTABLE_PATH.
    /// </summary>
    public string? PiperExecutablePath { get; set; }

    /// <summary>
    /// Path to the local Piper voice model (.onnx).
    /// Can be set via environment variable PIPER_MODEL_PATH.
    /// </summary>
    public string? PiperModelPath { get; set; }

    /// <summary>
    /// Optional path to the Piper model configuration (.json).
    /// Can be set via environment variable PIPER_CONFIG_PATH.
    /// </summary>
    public string? PiperConfigPath { get; set; }

    /// <summary>
    /// Speech synthesis rate (-10 to 10).
    /// Negative values = slower, positive values = faster, 0 = normal speed.
    /// Default: -1 (slightly slower than normal)
    /// </summary>
    public int Rate { get; set; } = -1;

    /// <summary>
    /// Speech synthesis volume (0-100).
    /// Default: 90
    /// </summary>
    public int Volume { get; set; } = 90;

    /// <summary>
    /// Voice or model name for speech synthesis.
    /// Piper selects the voice through <see cref="PiperModelPath"/>.
    /// </summary>
    public string? VoiceName { get; set; }

    /// <summary>
    /// Selected speaker engine name (e.g., "Piper TTS", "System Speech (Windows SAPI)").
    /// Determines which TTS engine to use.
    /// </summary>
    public string? SpeakerEngineName { get; set; }

    /// <summary>
    /// Maximum time in seconds to wait for Piper synthesis.
    /// </summary>
    public int PiperTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Enables Piper-specific pronunciation normalization before synthesis.
    /// </summary>
    public bool EnablePronunciationNormalization { get; set; } = true;

    /// <summary>
    /// Pause in seconds between sentences passed to Piper.
    /// </summary>
    public double PiperSentenceSilenceSeconds { get; set; }

    /// <summary>
    /// Keeps the last generated Piper WAV file in the local diagnostics folder.
    /// </summary>
    public bool EnablePiperAudioDiagnostics { get; set; }

    /// <summary>
    /// Custom phrase replacements for difficult station names or words.
    /// </summary>
    public IReadOnlyDictionary<string, string> PronunciationReplacements { get; set; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Gets whether Piper is configured with required local paths.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(PiperExecutablePath) && !string.IsNullOrWhiteSpace(PiperModelPath);
}
