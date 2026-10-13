// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.Base;

/// <summary>An available voice and its language tag.</summary>
public sealed record SpeechVoice(string Name, string Language);

/// <summary>Current provider capabilities; an unavailable provider supplies a safe reason.</summary>
public sealed record SpeechCapabilities(
    IReadOnlyList<SpeechVoice> Voices,
    bool SupportsRate,
    bool SupportsVolume,
    bool IsAvailable = true,
    string Reason = "");
