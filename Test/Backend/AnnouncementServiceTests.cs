// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging;
using Moba.Backend.Service;
using Moq;

/// <summary>
/// Unit tests for AnnouncementService announcement text generation.
/// Tests template placeholder replacement without speaker dependency.
/// </summary>
[TestFixture]
public class AnnouncementServiceTests
{
    private AnnouncementService _announcementService = null!;
    private Mock<ILogger<AnnouncementService>> _mockLogger = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<AnnouncementService>>();
        _announcementService = new AnnouncementService(null, _mockLogger.Object);
    }

    [Test]
    public void GenerateAnnouncementText_WithStationName_ShouldReplacePlaceholder()
    {
        // Arrange
        var journey = new Journey
        {
            Id = Guid.NewGuid(),
            Name = "Express",
            Text = "Nächster Halt: {StationName}"
        };
        var station = new Station { Name = "Central Station" };

        // Act
        var result = _announcementService.GenerateAnnouncementText(journey, station, 1);

        // Assert
        Assert.That(result, Does.Contain("Central Station"), "Should contain station name");
        Assert.That(result, Does.Not.Contain("{StationName}"), "Placeholder should be replaced");
    }

    [Test]
    public void GenerateAnnouncementText_WithExitDirectionLeft_ShouldReturnLinks()
    {
        // Arrange
        var journey = new Journey
        {
            Text = "Ausgang {ExitDirection}"
        };
        var station = new Station { IsExitOnLeft = true };

        // Act
        var result = _announcementService.GenerateAnnouncementText(journey, station, 1);

        // Assert
        Assert.That(result, Does.Contain("links"), "Should return 'links' for left exit");
    }

    [Test]
    public void GenerateAnnouncementText_WithExitDirectionRight_ShouldReturnRechts()
    {
        // Arrange
        var journey = new Journey
        {
            Text = "Ausgang {ExitDirection}"
        };
        var station = new Station { IsExitOnLeft = false };

        // Act
        var result = _announcementService.GenerateAnnouncementText(journey, station, 1);

        // Assert
        Assert.That(result, Does.Contain("rechts"), "Should return 'rechts' for right exit");
    }

    [Test]
    public void GenerateAnnouncementText_WithStationNumber_ShouldReplaceWithIndex()
    {
        // Arrange
        var journey = new Journey
        {
            Text = "Station Nummer {StationNumber}"
        };
        var station = new Station { Name = "Test" };

        // Act
        var result = _announcementService.GenerateAnnouncementText(journey, station, 3);

        // Assert
        Assert.That(result, Does.Contain("3"), "Should contain ordinal position");
    }

    [Test]
    public void GenerateAnnouncementText_WithTrackNumber_ShouldReplaceWithTrackValue()
    {
        // Arrange
        var journey = new Journey
        {
            Text = "Gleis {TrackNumber}"
        };
        var station = new Station { Track = 5 };

        // Act
        var result = _announcementService.GenerateAnnouncementText(journey, station, 1);

        // Assert
        Assert.That(result, Does.Contain("5"), "Should contain track number");
    }

    [Test]
    public void GenerateAnnouncementText_WithTrackNumberNull_ShouldNotFail()
    {
        // Arrange
        var journey = new Journey
        {
            Text = "Gleis {TrackNumber}"
        };
        var station = new Station { Track = null };

        // Act
        var result = _announcementService.GenerateAnnouncementText(journey, station, 1);

        // Assert
        Assert.That(result, Is.Not.Null, "Should not throw for null track");
    }

    [Test]
    public void GenerateAnnouncementText_WithMultiplePlaceholders_ShouldReplaceAll()
    {
        // Arrange
        var journey = new Journey
        {
            Text = "Willkommen in {StationName}, Gleis {TrackNumber}, Ausgang {ExitDirection}"
        };
        var station = new Station 
        { 
            Name = "Berlin Central",
            Track = 7,
            IsExitOnLeft = false
        };

        // Act
        var result = _announcementService.GenerateAnnouncementText(journey, station, 1);

        // Assert
        Assert.That(result, Does.Contain("Berlin Central"));
        Assert.That(result, Does.Contain("7"));
        Assert.That(result, Does.Contain("rechts"));
        Assert.That(result, Does.Not.Contain("{"));
    }

    [Test]
    public void GenerateAnnouncementText_WithEmptyTemplate_ShouldReturnEmpty()
    {
        // Arrange
        var journey = new Journey { Text = "" };
        var station = new Station { Name = "Test" };

        // Act
        var result = _announcementService.GenerateAnnouncementText(journey, station, 1);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GenerateAnnouncementText_WithNoPlaceholders_ShouldReturnUnchanged()
    {
        // Arrange
        var journey = new Journey { Text = "Willkommen!" };
        var station = new Station { Name = "Test" };

        // Act
        var result = _announcementService.GenerateAnnouncementText(journey, station, 1);

        // Assert
        Assert.That(result, Is.EqualTo("Willkommen!"));
    }

    [Test]
    public void GenerateAnnouncementText_WithStationIsExitOnLeftLeft_ShouldReturnLinks()
    {
        // Arrange
        var journey = new Journey
        {
            Text = "{StationIsExitOnLeft}"
        };
        var station = new Station { IsExitOnLeft = true };

        // Act
        var result = _announcementService.GenerateAnnouncementText(journey, station, 1);

        // Assert
        Assert.That(result, Does.Contain("links"), "Should contain 'links' for left exit");
    }

    [Test]
    public void GenerateAnnouncementText_WithStationIsExitOnLeftRight_ShouldReturnRechts()
    {
        // Arrange
        var journey = new Journey
        {
            Text = "{StationIsExitOnLeft}"
        };
        var station = new Station { IsExitOnLeft = false };

        // Act
        var result = _announcementService.GenerateAnnouncementText(journey, station, 1);

        // Assert
        Assert.That(result, Does.Contain("rechts"), "Should contain 'rechts' for right exit");
    }
}
