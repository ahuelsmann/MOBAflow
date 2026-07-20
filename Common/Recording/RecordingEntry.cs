// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Recording;

using System.Collections.Immutable;
using System.Text.Json;

/// <summary>
/// Describes whether an imported entry may affect an isolated replay runtime.
/// </summary>
public enum RecordingReplayApplicability
{
    DisplayOnly,
    ReplayApplicable
}

/// <summary>
/// Identifies a domain entity without retaining a mutable domain object.
/// </summary>
public sealed record RecordingEntityReference(string Kind, Guid Id);

/// <summary>
/// Represents one immutable, sequence-bearing journal entry.
/// </summary>
public sealed class RecordingEntry
{
    public RecordingEntry(
        long sequence,
        DateTimeOffset timestampUtc,
        TimeSpan elapsed,
        string category,
        string source,
        string typeKey,
        string severity,
        Guid? correlationId,
        IEnumerable<RecordingEntityReference>? entityReferences,
        JsonElement payload,
        string displayText,
        RecordingReplayApplicability replayApplicability)
    {
        Sequence = sequence;
        TimestampUtc = timestampUtc;
        Elapsed = elapsed;
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Source = source ?? throw new ArgumentNullException(nameof(source));
        TypeKey = typeKey ?? throw new ArgumentNullException(nameof(typeKey));
        Severity = severity ?? throw new ArgumentNullException(nameof(severity));
        CorrelationId = correlationId;
        EntityReferences = (entityReferences ?? [])
            .OrderBy(reference => reference.Kind, StringComparer.Ordinal)
            .ThenBy(reference => reference.Id)
            .ToImmutableArray();
        Payload = payload.Clone();
        DisplayText = displayText ?? throw new ArgumentNullException(nameof(displayText));
        ReplayApplicability = replayApplicability;
    }

    public long Sequence { get; }

    public DateTimeOffset TimestampUtc { get; }

    public TimeSpan Elapsed { get; }

    public string Category { get; }

    public string Source { get; }

    public string TypeKey { get; }

    public string Severity { get; }

    public Guid? CorrelationId { get; }

    public ImmutableArray<RecordingEntityReference> EntityReferences { get; }

    /// <summary>
    /// Gets the mapper-owned allow-listed payload. Arbitrary event, log, exception, file, endpoint, or credential data is prohibited.
    /// </summary>
    public JsonElement Payload { get; }

    /// <summary>
    /// Gets safe mapper-provided text used by the UI and free-text filtering instead of searching raw payload JSON.
    /// </summary>
    public string DisplayText { get; }

    public RecordingReplayApplicability ReplayApplicability { get; }
}