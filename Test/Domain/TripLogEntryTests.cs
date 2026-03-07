// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

/// <summary>
/// Unit tests for TripLogEntry domain model.
/// Covers constructor, properties, and Duration calculation.
/// </summary>
[TestFixture]
internal class TripLogEntryTests
{
    [Test]
    public void Constructor_InitializesWithNewGuid()
    {
        var entry = new TripLogEntry();

        Assert.That(entry.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void Constructor_GeneratesUniqueIds()
    {
        var entry1 = new TripLogEntry();
        var entry2 = new TripLogEntry();

        Assert.That(entry1.Id, Is.Not.EqualTo(entry2.Id));
    }

    [Test]
    public void Properties_CanBeSetAndRead()
    {
        var id = Guid.NewGuid();
        var start = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var end = start.AddMinutes(5);

        var entry = new TripLogEntry
        {
            Id = id,
            LocoAddress = 80,
            StartTime = start,
            EndTime = end,
            Speed = 50,
            IsStopSegment = false
        };

        Assert.That(entry.Id, Is.EqualTo(id));
        Assert.That(entry.LocoAddress, Is.EqualTo(80));
        Assert.That(entry.StartTime, Is.EqualTo(start));
        Assert.That(entry.EndTime, Is.EqualTo(end));
        Assert.That(entry.Speed, Is.EqualTo(50));
        Assert.That(entry.IsStopSegment, Is.False);
    }

    [Test]
    public void Duration_ReturnsNull_WhenEndTimeIsNull()
    {
        var entry = new TripLogEntry
        {
            StartTime = DateTime.UtcNow
            // EndTime left null
        };

        Assert.That(entry.Duration, Is.Null);
    }

    [Test]
    public void Duration_ReturnsDifference_WhenEndTimeIsSet()
    {
        var start = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var end = start.AddMinutes(5);

        var entry = new TripLogEntry
        {
            StartTime = start,
            EndTime = end
        };

        Assert.That(entry.Duration, Is.EqualTo(TimeSpan.FromMinutes(5)));
    }

    [Test]
    public void Duration_WithZeroSpan_ReturnsZero()
    {
        var t = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);

        var entry = new TripLogEntry
        {
            StartTime = t,
            EndTime = t
        };

        Assert.That(entry.Duration, Is.EqualTo(TimeSpan.Zero));
    }
}
