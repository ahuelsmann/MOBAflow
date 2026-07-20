// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Recording;

using System.Collections.Immutable;

/// <summary>
/// Combines timeline predicates while preserving the source journal order.
/// </summary>
public sealed class RecordingFilter
{
    public RecordingFilter(
        IEnumerable<string>? categories = null,
        IEnumerable<string>? sources = null,
        IEnumerable<string>? severities = null,
        string? entityKind = null,
        Guid? entityId = null,
        string? text = null,
        RecordingReplayApplicability? replayApplicability = null)
    {
        Categories = (categories ?? []).ToImmutableHashSet(StringComparer.Ordinal);
        Sources = (sources ?? []).ToImmutableHashSet(StringComparer.Ordinal);
        Severities = (severities ?? []).ToImmutableHashSet(StringComparer.Ordinal);
        EntityKind = string.IsNullOrWhiteSpace(entityKind) ? null : entityKind;
        EntityId = entityId;
        Text = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        ReplayApplicability = replayApplicability;
    }

    public ImmutableHashSet<string> Categories { get; }

    public ImmutableHashSet<string> Sources { get; }

    public ImmutableHashSet<string> Severities { get; }

    public string? EntityKind { get; }

    public Guid? EntityId { get; }

    public string? Text { get; }

    public RecordingReplayApplicability? ReplayApplicability { get; }

    public IReadOnlyList<RecordingEntry> Apply(IEnumerable<RecordingEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return entries.Where(Matches).ToArray();
    }

    public bool Matches(RecordingEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return MatchesSelection(Categories, entry.Category)
               && MatchesSelection(Sources, entry.Source)
               && MatchesSelection(Severities, entry.Severity)
               && MatchesEntity(entry)
               && (ReplayApplicability is null || entry.ReplayApplicability == ReplayApplicability)
               && (Text is null || entry.DisplayText.Contains(Text, StringComparison.InvariantCultureIgnoreCase));
    }

    private static bool MatchesSelection(ImmutableHashSet<string> selection, string value)
    {
        return selection.Count == 0 || selection.Contains(value);
    }

    private bool MatchesEntity(RecordingEntry entry)
    {
        if (EntityKind is null && EntityId is null)
        {
            return true;
        }

        return entry.EntityReferences.Any(reference =>
            (EntityKind is null || reference.Kind == EntityKind)
            && (EntityId is null || reference.Id == EntityId));
    }
}