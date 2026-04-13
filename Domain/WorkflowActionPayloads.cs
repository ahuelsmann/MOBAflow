// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Domain;

using Moba.Domain.Enum;

using System.Text.Json.Serialization;

/// <summary>
/// Typed payload for <see cref="ActionType.Command"/> workflow actions (Z21 / DCC bytes and optional UI fields).
/// </summary>
public sealed class CommandActionPayload
{
    /// <summary>
    /// Raw command bytes encoded as Base64 (execution uses this when present).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BytesBase64 { get; set; }

    /// <summary>
    /// Optional DCC address used by the UI when encoding bytes from properties.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Address { get; set; }

    /// <summary>
    /// Optional DCC speed (0–127) for UI encoding.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Speed { get; set; }

    /// <summary>
    /// Optional direction label (e.g. Forward / Backward) for UI encoding.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Direction { get; set; }
}

/// <summary>
/// Typed payload for <see cref="ActionType.Audio"/> workflow actions.
/// </summary>
public sealed class AudioActionPayload
{
    /// <summary>
    /// Path to the audio file (absolute or relative).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FilePath { get; set; }
}

/// <summary>
/// Typed payload for <see cref="ActionType.Announcement"/> workflow actions (TTS metadata; execution may use journey templates).
/// </summary>
public sealed class AnnouncementActionPayload
{
    /// <summary>
    /// Optional announcement text or template fragment.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    /// <summary>
    /// Optional voice name for cognitive speech.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VoiceName { get; set; }

    /// <summary>
    /// Optional speech rate adjustment.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Rate { get; set; }
}

/// <summary>
/// Typed payload for <see cref="ActionType.ExecutePowerShellScript"/> (reserved for future execution support).
/// </summary>
public sealed class PowerShellActionPayload
{
    /// <summary>
    /// Path to a script file, when used.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ScriptPath { get; set; }

    /// <summary>
    /// Optional arguments passed to the script.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Arguments { get; set; }
}
