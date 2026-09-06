// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

[TestFixture]
internal sealed class TrackPlanDocumentMapperTests
{
    [Test]
    public void TrackPlanDocument_Should_UseLegacyCompatibleDefaults()
    {
        var document = new TrackPlanDocument();

        Assert.Multiple(() =>
        {
            Assert.That(document.Version, Is.EqualTo(1));
            Assert.That(document.LibraryId, Is.EqualTo(TrackPlanDocumentMapper.LegacyLibraryId));
            Assert.That(document.OffsetX, Is.Null);
            Assert.That(document.OffsetY, Is.Null);
            Assert.That(document.ZoomFactor, Is.Null);
            Assert.That(document.Segments, Is.Empty);
            Assert.That(document.Connections, Is.Empty);
        });
    }

    [Test]
    public void ToLayout_Should_RejectNullDocument()
    {
        Assert.That(
            () => TrackPlanDocumentMapper.ToLayout(null!),
            Throws.ArgumentNullException);
    }

    [Test]
    public void ToLayout_Should_MapLegacyDefaultsOverridesAndConnections()
    {
        // Arrange
        var firstId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var secondId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var document = new TrackPlanDocument
        {
            Version = 1,
            LibraryId = " ",
            Segments =
            [
                new TrackPlanSegment
                {
                    Id = firstId,
                    Code = "G231",
                    X = 12,
                    Y = 34,
                    RotationDegrees = 15,
                    InPort = 7
                },
                new TrackPlanSegment
                {
                    Id = secondId,
                    LibraryId = "custom-library",
                    Code = "Curve",
                    X = 56,
                    Y = 78,
                    RotationDegrees = 90
                }
            ],
            Connections =
            [
                new TrackPlanConnection
                {
                    SourceSegment = firstId,
                    SourcePort = "PortB",
                    TargetSegment = secondId,
                    TargetPort = "PortA"
                }
            ]
        };

        // Act
        var layout = TrackPlanDocumentMapper.ToLayout(document);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(layout.Tracks, Has.Count.EqualTo(2));
            Assert.That(
                layout.Tracks,
                Does.Contain(new TrackInstance(firstId, TrackPlanDocumentMapper.LegacyLibraryId, "G231", 12, 34, 15, 7)));
            Assert.That(
                layout.Tracks,
                Does.Contain(new TrackInstance(secondId, "custom-library", "Curve", 56, 78, 90)));
            Assert.That(
                layout.Connections,
                Is.EqualTo([new Connection(firstId, "PortB", secondId, "PortA")]));
        });
    }

    [Test]
    public void ToDocument_Should_RejectNullLayout()
    {
        Assert.That(
            () => TrackPlanDocumentMapper.ToDocument(null!),
            Throws.ArgumentNullException);
    }

    [Test]
    public void ToDocument_Should_MapEmptyLayoutAndViewport()
    {
        // Arrange
        var layout = new Layout();

        // Act
        var document = TrackPlanDocumentMapper.ToDocument(layout, 10, 20, 1.5);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(document.Version, Is.EqualTo(2));
            Assert.That(document.LibraryId, Is.Empty);
            Assert.That(document.OffsetX, Is.EqualTo(10));
            Assert.That(document.OffsetY, Is.EqualTo(20));
            Assert.That(document.ZoomFactor, Is.EqualTo(1.5));
            Assert.That(document.Segments, Is.Empty);
            Assert.That(document.Connections, Is.Empty);
        });
    }

    [Test]
    public void ToDocument_Should_UseSharedLibraryAndSortSegmentsById()
    {
        // Arrange
        var firstId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var secondId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var layout = new Layout();
        layout.AddTrack(new TrackInstance(secondId, "piko-a", "Curve", 50, 60, 90));
        layout.AddTrack(new TrackInstance(firstId, "PIKO-A", "G231", 10, 20, 15, 8));
        layout.Connect(new Connection(firstId, "PortB", secondId, "PortA"));

        // Act
        var document = TrackPlanDocumentMapper.ToDocument(layout);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(document.LibraryId, Is.EqualTo("PIKO-A"));
            Assert.That(document.Segments.Select(segment => segment.Id), Is.EqualTo([firstId, secondId]));
            Assert.That(document.Segments, Has.All.Property(nameof(TrackPlanSegment.LibraryId)).Null);
            Assert.That(document.Segments[0].Code, Is.EqualTo("G231"));
            Assert.That(document.Segments[0].X, Is.EqualTo(10));
            Assert.That(document.Segments[0].Y, Is.EqualTo(20));
            Assert.That(document.Segments[0].RotationDegrees, Is.EqualTo(15));
            Assert.That(document.Segments[0].InPort, Is.EqualTo(8));
            Assert.That(document.Connections, Has.Count.EqualTo(1));
            Assert.That(document.Connections[0].SourceSegment, Is.EqualTo(firstId));
            Assert.That(document.Connections[0].SourcePort, Is.EqualTo("PortB"));
            Assert.That(document.Connections[0].TargetSegment, Is.EqualTo(secondId));
            Assert.That(document.Connections[0].TargetPort, Is.EqualTo("PortA"));
        });
    }

    [Test]
    public void ToDocument_Should_PersistPerTrackLibrariesForMixedLayout()
    {
        // Arrange
        var first = new TrackInstance(Guid.NewGuid(), "piko-a", "G231", 0, 0, 0);
        var second = new TrackInstance(Guid.NewGuid(), "custom", "Curve", 10, 20, 30);
        var layout = new Layout();
        layout.AddTrack(first);
        layout.AddTrack(second);

        // Act
        var document = TrackPlanDocumentMapper.ToDocument(layout);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(document.LibraryId, Is.Empty);
            Assert.That(
                document.Segments.Single(segment => segment.Id == first.Id).LibraryId,
                Is.EqualTo("piko-a"));
            Assert.That(
                document.Segments.Single(segment => segment.Id == second.Id).LibraryId,
                Is.EqualTo("custom"));
        });
    }
}
