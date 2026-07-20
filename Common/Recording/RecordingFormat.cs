// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Recording;

/// <summary>
/// Defines the stable identifiers and defensive limits of the recording artifact format.
/// </summary>
public static class RecordingFormat
{
    public const string FileExtension = ".mobarecording.json";
    public const string Identifier = "mobaflow-recording";
    public const string Version = "1.0";

    public const int MaxJsonDepth = 32;
    public const int MaxSessionNameLength = 128;
    public const int MaxTypeKeyLength = 128;
    public const int MaxKeyLength = 64;
    public const int MaxApplicationVersionLength = 64;
    public const int MaxProjectNameLength = 256;
    public const int MaxDisplayTextLength = 4 * 1024;
    public const int MaxPayloadBytes = 16 * 1024;
    public const int MaxEntityReferencesPerEntry = 64;
    public const int DefaultMaxEntries = 250_000;
    public const long DefaultMaxArtifactBytes = 64L * 1024 * 1024;
}