// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Common;

using Moba.Common.Recording;
using System.Text.Json;

[TestFixture]
[Category("Unit")]
internal sealed class RecordingFilterTests
{
    [Test]
    public void Apply_Should_CombinePredicatesWithoutChangingJournalOrder()
    {
        // Arrange
        var locomotiveId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var entries = new[]
        {
            CreateEntry(30, "runtime", "z21", "warning", "Locomotive stopped", locomotiveId, RecordingReplayApplicability.ReplayApplicable),
            CreateEntry(10, "runtime", "z21", "warning", "Locomotive ready", locomotiveId, RecordingReplayApplicability.ReplayApplicable),
            CreateEntry(
                20,
                "runtime",
                "operator",
                "warning",
                "Locomotive ready",
                locomotiveId,
                RecordingReplayApplicability.ReplayApplicable),
            CreateEntry(
                40,
                "runtime",
                "z21",
                "information",
                "Locomotive ready",
                locomotiveId,
                RecordingReplayApplicability.ReplayApplicable)
        };
        var filter = new RecordingFilter(
            categories: ["runtime"],
            sources: ["z21"],
            severities: ["warning"],
            entityKind: "locomotive",
            entityId: locomotiveId,
            text: "READY",
            replayApplicability: RecordingReplayApplicability.ReplayApplicable);

        // Act
        var filtered = filter.Apply(entries);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(filtered.Select(entry => entry.Sequence), Is.EqualTo(new long[] { 10 }));
            Assert.That(entries.Select(entry => entry.Sequence), Is.EqualTo(new long[] { 30, 10, 20, 40 }));
        });
    }

    [Test]
    public void Apply_Should_SearchOnlySafeDisplayText()
    {
        // Arrange
        using var document = JsonDocument.Parse("""{"internalValue":"hidden-match"}""");
        var entry = new RecordingEntry(
            1,
            DateTimeOffset.UnixEpoch,
            TimeSpan.Zero,
            "runtime",
            "test",
            "runtime.state.changed",
            "information",
            null,
            [],
            document.RootElement,
            "Visible status",
            RecordingReplayApplicability.DisplayOnly);

        // Act
        var filtered = new RecordingFilter(text: "hidden-match").Apply([entry]);

        // Assert
        Assert.That(filtered, Is.Empty);
    }

    private static RecordingEntry CreateEntry(
        long sequence,
        string category,
        string source,
        string severity,
        string displayText,
        Guid entityId,
        RecordingReplayApplicability applicability)
    {
        using var document = JsonDocument.Parse("{}");
        return new RecordingEntry(
            sequence,
            DateTimeOffset.UnixEpoch,
            TimeSpan.FromTicks(sequence),
            category,
            source,
            "runtime.state.changed",
            severity,
            null,
            [new RecordingEntityReference("locomotive", entityId)],
            document.RootElement,
            displayText,
            applicability);
    }
}