// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Recording;

using System.Collections.Immutable;

/// <summary>
/// Contains deterministic counts declared by a completed recording artifact.
/// </summary>
public sealed record RecordingSummary(
    int EntryCount,
    int ReplayApplicableEntryCount,
    int DisplayOnlyEntryCount,
    int MarkerCount,
    int NoteCount,
    long FirstSequence,
    long LastSequence,
    long DurationTicks)
{
    public static RecordingSummary Create(IEnumerable<RecordingEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var snapshot = entries as IReadOnlyList<RecordingEntry> ?? entries.ToArray();
        var replayApplicableCount = snapshot.Count(entry => entry.ReplayApplicability == RecordingReplayApplicability.ReplayApplicable);

        return new RecordingSummary(
            snapshot.Count,
            replayApplicableCount,
            snapshot.Count - replayApplicableCount,
            snapshot.Count(entry => entry.TypeKey == "recorder.marker"),
            snapshot.Count(entry => entry.TypeKey == "recorder.note"),
            snapshot.Count == 0 ? 0 : snapshot[0].Sequence,
            snapshot.Count == 0 ? 0 : snapshot[^1].Sequence,
            snapshot.Count == 0 ? 0 : snapshot[^1].Elapsed.Ticks);
    }
}

/// <summary>
/// Represents a completed, immutable recording artifact independent of solution JSON.
/// </summary>
public sealed class RecordingArtifact
{
    public RecordingArtifact(
        RecordingSessionMetadata metadata,
        string sourceApplicationVersion,
        RecordingProjectIdentity? project,
        RecordingArtifactOptions options,
        IEnumerable<RecordingEntry> entries,
        string? entriesSha256 = null)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        SourceApplicationVersion = sourceApplicationVersion ?? throw new ArgumentNullException(nameof(sourceApplicationVersion));
        Project = project;
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Entries = (entries ?? throw new ArgumentNullException(nameof(entries))).ToImmutableArray();
        Summary = RecordingSummary.Create(Entries);
        EntriesSha256 = entriesSha256;
    }

    public RecordingSessionMetadata Metadata { get; }

    public string SourceApplicationVersion { get; }

    public RecordingProjectIdentity? Project { get; }

    public RecordingArtifactOptions Options { get; }

    public ImmutableArray<RecordingEntry> Entries { get; }

    public RecordingSummary Summary { get; }

    /// <summary>
    /// Gets the declared lowercase SHA-256 over canonical entry bytes, excluding nondeterministic session metadata.
    /// </summary>
    public string? EntriesSha256 { get; }
}