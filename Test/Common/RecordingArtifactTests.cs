// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Common;

using Moba.Common.Recording;
using System.Text.Json;

[TestFixture]
[Category("Unit")]
internal sealed class RecordingArtifactTests
{
    [Test]
    public void Constructor_Should_SnapshotEntriesAndComputeSummary()
    {
        // Arrange
        var mutableEntries = new List<RecordingEntry>
        {
            CreateEntry(7, "recorder.marker", RecordingReplayApplicability.DisplayOnly),
            CreateEntry(9, "runtime.state.changed", RecordingReplayApplicability.ReplayApplicable)
        };

        // Act
        var artifact = CreateArtifact(mutableEntries);
        mutableEntries.Clear();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(artifact.Entries, Has.Length.EqualTo(2));
            Assert.That(artifact.Summary.EntryCount, Is.EqualTo(2));
            Assert.That(artifact.Summary.ReplayApplicableEntryCount, Is.EqualTo(1));
            Assert.That(artifact.Summary.DisplayOnlyEntryCount, Is.EqualTo(1));
            Assert.That(artifact.Summary.MarkerCount, Is.EqualTo(1));
            Assert.That(artifact.Summary.FirstSequence, Is.EqualTo(7));
            Assert.That(artifact.Summary.LastSequence, Is.EqualTo(9));
        });
    }

    [Test]
    public void Entry_Should_SortAndSnapshotEntityReferences()
    {
        // Arrange
        var firstId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var secondId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var references = new List<RecordingEntityReference>
        {
            new("signal", secondId),
            new("locomotive", secondId),
            new("locomotive", firstId)
        };

        // Act
        var entry = CreateEntry(1, "runtime.state.changed", RecordingReplayApplicability.DisplayOnly, references);
        references.Clear();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(entry.EntityReferences, Has.Length.EqualTo(3));
            Assert.That(
                entry.EntityReferences.Select(reference => reference.Kind),
                Is.EqualTo(new[] { "locomotive", "locomotive", "signal" }));
            Assert.That(entry.EntityReferences[0].Id, Is.EqualTo(firstId));
        });
    }

    private static RecordingArtifact CreateArtifact(IEnumerable<RecordingEntry> entries)
    {
        return new RecordingArtifact(
            new RecordingSessionMetadata(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                "Artifact test",
                new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
                new DateTimeOffset(2026, 1, 2, 3, 5, 5, TimeSpan.Zero)),
            "1.0.0",
            null,
            new RecordingArtifactOptions(),
            entries);
    }

    private static RecordingEntry CreateEntry(
        long sequence,
        string typeKey,
        RecordingReplayApplicability applicability,
        IEnumerable<RecordingEntityReference>? references = null)
    {
        using var document = JsonDocument.Parse("{}");
        return new RecordingEntry(
            sequence,
            new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            TimeSpan.FromTicks(sequence),
            "runtime",
            "test",
            typeKey,
            "information",
            null,
            references,
            document.RootElement,
            $"Entry {sequence}",
            applicability);
    }
}