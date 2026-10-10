// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Serilog;
using global::Serilog.Events;
using global::Serilog.Parsing;

[TestFixture]
[NonParallelizable]
public class InMemorySinkTests
{
    [SetUp]
    public void SetUp() => InMemorySink.ClearLogs();

    [TearDown]
    public void TearDown() => InMemorySink.ClearLogs();

    [TestCase(LogEventLevel.Verbose, LogSeverity.Debug)]
    [TestCase(LogEventLevel.Debug, LogSeverity.Debug)]
    [TestCase(LogEventLevel.Information, LogSeverity.Info)]
    [TestCase(LogEventLevel.Warning, LogSeverity.Warning)]
    [TestCase(LogEventLevel.Error, LogSeverity.Error)]
    [TestCase(LogEventLevel.Fatal, LogSeverity.Error)]
    [TestCase((LogEventLevel)99, LogSeverity.Info)]
    public void Emit_MapsSeverityAndPreservesDisplayedContent(LogEventLevel level, LogSeverity expected)
    {
        var timestamp = new DateTimeOffset(2026, 10, 9, 12, 34, 56, TimeSpan.Zero).AddMilliseconds(123);
        var logEvent = new LogEvent(timestamp, level, null,
            new MessageTemplateParser().Parse("Train {Number} stopped"),
            [new LogEventProperty("Number", new ScalarValue(42)),
             new LogEventProperty("SourceContext", new ScalarValue("Moba.Backend.JourneyManager"))]);

        new InMemorySink().Emit(logEvent);

        var entry = InMemorySink.GetLogEntries().Single();
        Assert.Multiple(() =>
        {
            Assert.That(entry.Severity, Is.EqualTo(expected));
            Assert.That(entry.Timestamp, Is.EqualTo(timestamp.DateTime));
            Assert.That(entry.TimestampFormatted, Is.EqualTo("12:34:56.123"));
            Assert.That(entry.Source, Is.EqualTo("JourneyManager"));
            Assert.That(entry.Message, Is.EqualTo("Train 42 stopped"));
        });
    }

    [Test]
    public void Emit_WithoutSourceContext_UsesUnknownSource()
    {
        new InMemorySink().Emit(CreateEvent("No source"));
        Assert.That(InMemorySink.GetLogEntries().Single().Source, Is.EqualTo("Unknown"));
    }

    [Test]
    public void Emit_NotifiesSubscriberAfterEntryIsAvailable()
    {
        LogEntry? received = null;
        var wasAvailable = false;
        void OnLogAdded(LogEntry entry)
        {
            received = entry;
            wasAvailable = InMemorySink.GetLogEntries().Contains(entry);
        }

        InMemorySink.LogAdded += OnLogAdded;
        try
        {
            new InMemorySink().Emit(CreateEvent("Notification"));
            Assert.Multiple(() =>
            {
                Assert.That(received, Is.SameAs(InMemorySink.GetLogEntries().Single()));
                Assert.That(wasAvailable, Is.True);
            });
        }
        finally
        {
            InMemorySink.LogAdded -= OnLogAdded;
        }
    }

    [Test]
    public void Emit_KeepsExactlyLatestFiveHundredEntriesNewestFirst()
    {
        var sink = new InMemorySink();
        for (var index = 0; index < 502; index++)
        {
            sink.Emit(CreateEvent($"Entry {index}"));
        }

        Assert.That(InMemorySink.GetLogEntries().Select(entry => entry.Message),
            Is.EqualTo(Enumerable.Range(2, 500).Reverse().Select(index => $"Entry {index}")));
    }

    [Test]
    public void ClearLogs_EmptiesBufferAndAllowsNewEntries()
    {
        var sink = new InMemorySink();
        sink.Emit(CreateEvent("Old"));
        InMemorySink.ClearLogs();
        Assert.That(InMemorySink.GetLogEntries(), Is.Empty);
        sink.Emit(CreateEvent("New"));
        Assert.That(InMemorySink.GetLogEntries().Single().Message, Is.EqualTo("New"));
    }

    [TestCase(LogSeverity.Debug, "🔍")]
    [TestCase(LogSeverity.Info, "ℹ️")]
    [TestCase(LogSeverity.Warning, "⚠️")]
    [TestCase(LogSeverity.Error, "❌")]
    [TestCase((LogSeverity)99, "📝")]
    public void SeverityIcon_IdentifiesDisplayedSeverity(LogSeverity severity, string expected)
    {
        Assert.That(new LogEntry { Severity = severity }.SeverityIcon, Is.EqualTo(expected));
    }

    private static LogEvent CreateEvent(string message) =>
        new(DateTimeOffset.UtcNow, LogEventLevel.Information, null,
            new MessageTemplateParser().Parse(message), []);
}
