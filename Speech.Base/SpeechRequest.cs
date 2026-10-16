// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.Base;

/// <summary>Text and shared synthesis options captured for a single call.</summary>
public sealed record SpeechRequest(string Text, string VoiceName, string Language, int Rate, uint Volume);
