// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Sound;

/// <summary>
/// Factory interface for creating/selecting the appropriate ISpeakerEngine
/// based on application settings. This enables runtime switching between
/// SystemSpeechEngine (Windows SAPI) and CognitiveSpeechEngine (Azure).
/// </summary>
public interface ISpeakerEngineFactory
{
    /// <summary>
    /// Gets the currently configured speaker engine based on application settings.
    /// </summary>
    /// <returns>The appropriate ISpeakerEngine implementation</returns>
    ISpeakerEngine GetSpeakerEngine();
}