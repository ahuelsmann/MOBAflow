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

    [Test]
    public void NullInputsAreRejectedAtFilterBoundary()
    {
        var filter = new RecordingFilter();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => filter.Apply(null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("entries"));
            Assert.That(() => filter.Matches(null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("entry"));
        }
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t ")]
    public void EmptyOptionalFiltersLeaveEntriesUnrestricted(string? value)
    {
        var entry = CreateEntry(1, "runtime", "z21", "warning", "Ready", Guid.NewGuid(), RecordingReplayApplicability.DisplayOnly);
        var filter = new RecordingFilter(entityKind: value, text: value);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(filter.EntityKind, Is.Null);
            Assert.That(filter.Text, Is.Null);
            Assert.That(filter.Matches(entry), Is.True);
            Assert.That(filter.Matches(WithReferences(entry, [])), Is.True);
        }
    }

    [TestCase("runtime", "z21", "warning", true)]
    [TestCase("other", "z21", "warning", false)]
    [TestCase("runtime", "other", "warning", false)]
    [TestCase("runtime", "z21", "information", false)]
    [TestCase("RUNTIME", "z21", "warning", false)]
    [TestCase("runtime", "Z21", "warning", false)]
    [TestCase("runtime", "z21", "WARNING", false)]
    public void EverySelectionMustMatchUsingOrdinalComparison(string category, string source, string severity, bool expected)
    {
        var entry = CreateEntry(1, category, source, severity, "Ready", Guid.NewGuid(), RecordingReplayApplicability.DisplayOnly);
        var filter = new RecordingFilter(categories: ["unused", "runtime"], sources: ["unused", "z21"], severities: ["unused", "warning"]);
        Assert.That(filter.Matches(entry), Is.EqualTo(expected));
    }

    [TestCase(RecordingReplayApplicability.DisplayOnly, true)]
    [TestCase(RecordingReplayApplicability.ReplayApplicable, false)]
    public void ReplayApplicabilityIsAnIndependentFilter(RecordingReplayApplicability applicability, bool expected)
    {
        var entry = CreateEntry(1, "runtime", "z21", "warning", "Ready", Guid.NewGuid(), applicability);
        var filter = new RecordingFilter(replayApplicability: RecordingReplayApplicability.DisplayOnly);
        Assert.That(filter.Matches(entry), Is.EqualTo(expected));
    }

    [TestCase("Locomotive READY", true)]
    [TestCase("Stopped", false)]
    public void TextSearchTrimsQueryAndIgnoresCase(string displayText, bool expected)
    {
        var entry = CreateEntry(1, "runtime", "z21", "warning", displayText, Guid.NewGuid(), RecordingReplayApplicability.DisplayOnly);
        var filter = new RecordingFilter(text: "  ready  ");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(filter.Text, Is.EqualTo("ready"));
            Assert.That(filter.Matches(entry), Is.EqualTo(expected));
        }
    }

    [TestCase("Kind", "Match", true)]
    [TestCase("Kind", "WrongKind", false)]
    [TestCase("Kind", "WrongId", true)]
    [TestCase("Id", "Match", true)]
    [TestCase("Id", "WrongKind", true)]
    [TestCase("Id", "WrongId", false)]
    [TestCase("Both", "Match", true)]
    [TestCase("Both", "WrongKind", false)]
    [TestCase("Both", "WrongId", false)]
    [TestCase("Both", "SplitAcrossReferences", false)]
    [TestCase("Both", "Empty", false)]
    public void EntityPredicatesMustMatchOneReference(string selection, string scenario, bool expected)
    {
        var targetId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        RecordingEntityReference[] references = scenario switch
        {
            "WrongKind" => [new("signal", targetId)],
            "WrongId" => [new("locomotive", otherId)],
            "SplitAcrossReferences" => [new("signal", targetId), new("locomotive", otherId)],
            "Empty" => [],
            _ => [new("signal", otherId), new("locomotive", targetId)]
        };
        var entry = CreateEntry(1, "runtime", "z21", "warning", "Ready", targetId, RecordingReplayApplicability.DisplayOnly);
        var filter = new RecordingFilter(
            entityKind: selection == "Id" ? null : "locomotive",
            entityId: selection == "Kind" ? null : targetId);
        Assert.That(filter.Matches(WithReferences(entry, references)), Is.EqualTo(expected));
    }

    [Test]
    public void ApplyPreservesSourceOrderOfMultipleMatches()
    {
        var id = Guid.NewGuid();
        var first = CreateEntry(30, "runtime", "z21", "warning", "Ready", id, RecordingReplayApplicability.DisplayOnly);
        var excluded = CreateEntry(20, "other", "z21", "warning", "Ready", id, RecordingReplayApplicability.DisplayOnly);
        var last = CreateEntry(10, "runtime", "z21", "warning", "Ready", id, RecordingReplayApplicability.DisplayOnly);
        var result = new RecordingFilter(categories: ["runtime"]).Apply([first, excluded, last]);
        Assert.That(result, Is.EqualTo(new[] { first, last }));
    }

    private static RecordingEntry WithReferences(RecordingEntry entry, RecordingEntityReference[] references) => new(
        entry.Sequence, entry.TimestampUtc, entry.Elapsed, entry.Category, entry.Source, entry.TypeKey, entry.Severity,
        entry.CorrelationId, references, entry.Payload, entry.DisplayText, entry.ReplayApplicability);

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