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

    [Test]
    public void RecorderOptions_DefaultsProvideBoundedIngestionAndDrain()
    {
        var options = new RecorderOptions();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(options.PendingCapacity, Is.EqualTo(10_000));
            Assert.That(options.EntryLimit, Is.EqualTo(250_000));
            Assert.That(options.EstimatedPayloadByteLimit, Is.EqualTo(67_108_864L));
            Assert.That(options.StopDrainTimeout, Is.EqualTo(TimeSpan.FromSeconds(5)));
        }
    }

    [TestCase(1)]
    [TestCase(RecorderOptions.MaximumPendingCapacity)]
    public void RecorderOptions_AcceptsPendingCapacityBoundaries(int capacity)
    {
        var options = new RecorderOptions(pendingCapacity: capacity);

        Assert.That(options.PendingCapacity, Is.EqualTo(capacity));
    }

    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(RecorderOptions.MaximumPendingCapacity + 1)]
    public void RecorderOptions_RejectsPendingCapacityOutsideBounds(int capacity)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new RecorderOptions(pendingCapacity: capacity));

        Assert.That(exception!.ParamName, Is.EqualTo("pendingCapacity"));
    }

    [TestCase(4)]
    [TestCase(RecordingFormat.DefaultMaxEntries)]
    public void RecorderOptions_AcceptsEntryLimitBoundaries(int limit)
    {
        // Four entries leave room for the start record and the three reserved terminal records.
        var options = new RecorderOptions(entryLimit: limit);

        Assert.That(options.EntryLimit, Is.EqualTo(limit));
    }

    [TestCase(-1)]
    [TestCase(3)]
    [TestCase(RecordingFormat.DefaultMaxEntries + 1)]
    public void RecorderOptions_RejectsEntryLimitOutsideBounds(int limit)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new RecorderOptions(entryLimit: limit));

        Assert.That(exception!.ParamName, Is.EqualTo("entryLimit"));
    }

    [TestCase(1L)]
    [TestCase(RecordingFormat.DefaultMaxArtifactBytes)]
    public void RecorderOptions_AcceptsPayloadByteLimitBoundaries(long limit)
    {
        var options = new RecorderOptions(estimatedPayloadByteLimit: limit);

        Assert.That(options.EstimatedPayloadByteLimit, Is.EqualTo(limit));
    }

    [TestCase(-1L)]
    [TestCase(0L)]
    [TestCase(RecordingFormat.DefaultMaxArtifactBytes + 1)]
    public void RecorderOptions_RejectsPayloadByteLimitOutsideBounds(long limit)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new RecorderOptions(estimatedPayloadByteLimit: limit));

        Assert.That(exception!.ParamName, Is.EqualTo("estimatedPayloadByteLimit"));
    }

    [TestCase(0L)]
    [TestCase(1L)]
    [TestCase(60 * TimeSpan.TicksPerSecond)]
    public void RecorderOptions_AcceptsDrainTimeoutIncludingBothBoundaries(long ticks)
    {
        var timeout = TimeSpan.FromTicks(ticks);

        var options = new RecorderOptions(stopDrainTimeout: timeout);

        Assert.That(options.StopDrainTimeout, Is.EqualTo(timeout));
    }

    [TestCase(-1L)]
    [TestCase(60 * TimeSpan.TicksPerSecond + 1)]
    public void RecorderOptions_RejectsDrainTimeoutOutsideBounds(long ticks)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RecorderOptions(stopDrainTimeout: TimeSpan.FromTicks(ticks)));

        Assert.That(exception!.ParamName, Is.EqualTo("stopDrainTimeout"));
    }

    [Test]
    public void OperationResult_DefaultSuccessIsNotIdempotent()
    {
        var result = RecordingOperationResult.Success();

        AssertSuccess(result, expectedIdempotent: false);
    }

    [Test]
    public void OperationResult_SuccessPreservesIdempotence([Values] bool idempotent)
    {
        var result = RecordingOperationResult.Success(isIdempotent: idempotent);

        AssertSuccess(result, idempotent);
    }

    [TestCase(RecordingFailureCode.InvalidState)]
    [TestCase(RecordingFailureCode.InvalidRequest)]
    [TestCase(RecordingFailureCode.LimitReached)]
    [TestCase(RecordingFailureCode.Cancelled)]
    [TestCase(RecordingFailureCode.DrainTimeout)]
    [TestCase(RecordingFailureCode.InternalError)]
    public void OperationResult_FailureRemainsUnsuccessfulAndPreservesDiagnostics(RecordingFailureCode code)
    {
        const string message = "The requested recording operation could not be completed.";

        var result = RecordingOperationResult.Failure(code, message);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.IsIdempotent, Is.False);
            Assert.That(result.FailureCode, Is.EqualTo(code));
            Assert.That(result.Message, Is.EqualTo(message));
        }
    }

    private static void AssertSuccess(RecordingOperationResult result, bool expectedIdempotent)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.IsIdempotent, Is.EqualTo(expectedIdempotent));
            Assert.That(result.FailureCode, Is.EqualTo(RecordingFailureCode.None));
            Assert.That(result.Message, Is.Empty);
        }
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