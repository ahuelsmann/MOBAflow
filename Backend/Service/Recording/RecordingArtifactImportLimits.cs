// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Recording;

using Moba.Common.Recording;

/// <summary>
/// Applies process-local caps before a recording artifact can allocate its complete object graph.
/// </summary>
public sealed record RecordingArtifactImportLimits(
    int MaxEntries = RecordingFormat.DefaultMaxEntries,
    long MaxArtifactBytes = RecordingFormat.DefaultMaxArtifactBytes)
{
    public void Validate()
    {
        if (MaxEntries <= 0 || MaxEntries > RecordingFormat.DefaultMaxEntries)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxEntries));
        }

        if (MaxArtifactBytes <= 0 || MaxArtifactBytes > RecordingFormat.DefaultMaxArtifactBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxArtifactBytes));
        }
    }
}