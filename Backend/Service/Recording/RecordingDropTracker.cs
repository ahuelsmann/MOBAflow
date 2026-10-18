// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Recording;

/// <summary>
/// Counts entries dropped at ingestion capacity until a gap, limit or fault entry reports them.
/// Not thread-safe: the recording session calls it under its own lock.
/// </summary>
internal sealed class RecordingDropTracker
{
    /// <summary>Gets the number of entries dropped since the session started.</summary>
    public long TotalCount { get; private set; }

    /// <summary>Gets the number of dropped entries that no gap, limit or fault entry has reported yet.</summary>
    public long PendingCount { get; private set; }

    /// <summary>Gets whether dropped entries are waiting to be reported.</summary>
    public bool HasPending => PendingCount > 0;

    /// <summary>Gets the first unreported dropped sequence; 0 when none is pending.</summary>
    public long FirstPendingSequence { get; private set; }

    /// <summary>Gets the last unreported dropped sequence; 0 when none is pending.</summary>
    public long LastPendingSequence { get; private set; }

    /// <summary>Records that the entry with <paramref name="sequence"/> was dropped.</summary>
    public void Track(long sequence)
    {
        if (PendingCount == 0) FirstPendingSequence = sequence;
        LastPendingSequence = sequence;
        PendingCount++;
        TotalCount++;
    }

    /// <summary>Marks the pending drops as reported.</summary>
    public void ClearPending()
    {
        PendingCount = 0;
        FirstPendingSequence = 0;
        LastPendingSequence = 0;
    }

    /// <summary>Forgets all drops for a new session.</summary>
    public void Reset()
    {
        TotalCount = 0;
        ClearPending();
    }

    /// <summary>
    /// Combines the still-queued entries (<paramref name="orderedQueuedSequences"/>, ascending) with the pending
    /// drops into the sequence range that a fault loses.
    /// </summary>
    public (long? FirstLostSequence, long? LastLostSequence, long LostCount) LostRange(IReadOnlyList<long> orderedQueuedSequences)
    {
        ArgumentNullException.ThrowIfNull(orderedQueuedSequences);
        var firstLost = CombineFirstSequence(
            orderedQueuedSequences.Count == 0 ? null : orderedQueuedSequences[0],
            PendingCount == 0 ? null : FirstPendingSequence);
        var lastLost = CombineLastSequence(
            orderedQueuedSequences.Count == 0 ? null : orderedQueuedSequences[^1],
            PendingCount == 0 ? null : LastPendingSequence);
        return (firstLost, lastLost, orderedQueuedSequences.Count + PendingCount);
    }

    private static long? CombineFirstSequence(long? first, long? second)
    {
        if (first is null) return second;
        if (second is null) return first;
        return Math.Min(first.Value, second.Value);
    }

    private static long? CombineLastSequence(long? first, long? second)
    {
        if (first is null) return second;
        if (second is null) return first;
        return Math.Max(first.Value, second.Value);
    }
}
