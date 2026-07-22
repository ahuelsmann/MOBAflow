// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Recording;

/// <summary>
/// Declares the identity and wall-clock boundaries of a recording session.
/// </summary>
public sealed record RecordingSessionMetadata(
    Guid SessionId,
    string Name,
    DateTimeOffset StartedUtc,
    DateTimeOffset? CompletedUtc);

/// <summary>
/// Identifies the optional source project without embedding project data in the artifact.
/// </summary>
public sealed record RecordingProjectIdentity(Guid ProjectId, string Name);

/// <summary>
/// Records bounded session settings needed to interpret the artifact.
/// </summary>
public sealed record RecordingArtifactOptions(
    int EntryLimit = RecordingFormat.DefaultMaxEntries,
    long EstimatedPayloadByteLimit = RecordingFormat.DefaultMaxArtifactBytes);