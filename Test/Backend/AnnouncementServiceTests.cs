// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging;

using Moba.Backend.Service;
using Moba.Sound;

using Moq;

/// <summary>
/// Unit tests for AnnouncementService announcement text generation.
/// Tests template placeholder replacement without speaker dependency.
/// </summary>
[TestFixture]
internal class AnnouncementServiceTests
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
        var station = new Station { Platforms = [new Platform { Number = 5 }] };

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
        var station = new Station();

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
            Platforms = [new Platform { Number = 7 }],
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

    [Test]
    public async Task GenerateAndSpeakAnnouncementAsync_CancelledSpeech_IsNotSuppressed()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var speaker = new Mock<ISpeakerEngine>();
        speaker
            .Setup(engine => engine.AnnouncementAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                cancellation.Token))
            .Returns(() =>
            {
                cancellation.Cancel();
                return Task.FromCanceled(cancellation.Token);
            });
        var factory = new Mock<ISpeakerEngineFactory>();
        factory.Setup(value => value.CreateEngineFromOptions()).Returns(speaker.Object);
        var service = new AnnouncementService(factory.Object, _mockLogger.Object);

        // Act & Assert
        await Assert.CatchAsync<OperationCanceledException>(() => service.GenerateAndSpeakAnnouncementAsync(
            "Next stop {StationName}",
            new Station { Name = "Minden" },
            1,
            cancellation.Token)).ConfigureAwait(false);
        speaker.Verify(engine => engine.AnnouncementAsync(
            It.IsAny<string>(),
            null,
            cancellation.Token), Times.Once);
    }

    [Test]
    public async Task GenerateAndSpeakAnnouncementAsync_ForwardsRenderedTextAndCallerToken()
    {
        using var cancellation = new CancellationTokenSource();
        var speaker = new Mock<ISpeakerEngine>();
        speaker.Setup(engine => engine.AnnouncementAsync(
                It.IsAny<string>(), null, cancellation.Token))
            .Returns(Task.CompletedTask);
        var service = CreateSpeakingService(speaker);

        await service.GenerateAndSpeakAnnouncementAsync(
            "Next stop {StationName}, platform {TrackNumber}.",
            new Station { Name = "Central", Platforms = [new Platform { Number = 4 }] },
            1,
            cancellation.Token).ConfigureAwait(false);

        speaker.Verify(engine => engine.AnnouncementAsync(
            "Next stop Central, platform 4.", null, cancellation.Token), Times.Once);
    }

    [TestCase(null)]
    [TestCase("")]
    public async Task GenerateAndSpeakAnnouncementAsync_EmptyTemplate_DoesNotResolveSpeaker(string? template)
    {
        var factory = new Mock<ISpeakerEngineFactory>(MockBehavior.Strict);
        var service = new AnnouncementService(factory.Object, _mockLogger.Object);

        await service.GenerateAndSpeakAnnouncementAsync(template, new Station(), 1).ConfigureAwait(false);

        factory.Verify(value => value.CreateEngineFromOptions(), Times.Never);
    }

    [Test]
    public async Task GenerateAndSpeakAnnouncementAsync_AlreadyCancelled_DoesNotResolveSpeaker()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(false);
        var factory = new Mock<ISpeakerEngineFactory>(MockBehavior.Strict);
        var service = new AnnouncementService(factory.Object, _mockLogger.Object);

        await Assert.CatchAsync<OperationCanceledException>(() => service.GenerateAndSpeakAnnouncementAsync(
            "Next stop Central.", new Station(), 1, cancellation.Token)).ConfigureAwait(false);

        factory.Verify(value => value.CreateEngineFromOptions(), Times.Never);
    }

    [Test]
    public async Task GenerateAndSpeakAnnouncementAsync_OverlappingCalls_AreNotQueuedByTheService()
    {
        var firstCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var speaker = new Mock<ISpeakerEngine>();
        speaker.Setup(engine => engine.AnnouncementAsync(
                It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .Returns((string message, string? voice, CancellationToken token) =>
                message == "First" ? firstCompletion.Task : secondCompletion.Task);
        var service = CreateSpeakingService(speaker);

        var first = service.GenerateAndSpeakAnnouncementAsync("First", new Station(), 1);
        var second = service.GenerateAndSpeakAnnouncementAsync("Second", new Station(), 1);
        try
        {
            speaker.Verify(engine => engine.AnnouncementAsync(
                "First", null, CancellationToken.None), Times.Once);
            speaker.Verify(engine => engine.AnnouncementAsync(
                "Second", null, CancellationToken.None), Times.Once);
            secondCompletion.SetResult(true);
            await second.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            Assert.That(first.IsCompleted, Is.False, "The second call can finish while the first is still speaking.");
            firstCompletion.SetResult(true);
            await first.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        }
        finally
        {
            firstCompletion.TrySetResult(true);
            secondCompletion.TrySetResult(true);
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GenerateAndSpeakAnnouncementAsync_SpeakerFailure_RespectsErrorSuppression(bool suppressErrors)
    {
        var failure = new InvalidOperationException("Speaker unavailable.");
        var speaker = new Mock<ISpeakerEngine>();
        speaker.Setup(engine => engine.AnnouncementAsync(
                It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);
        var service = CreateSpeakingService(speaker);

        if (suppressErrors)
        {
            await service.GenerateAndSpeakAnnouncementAsync("Next stop Central.", new Station(), 1)
                .ConfigureAwait(false);
        }
        else
        {
            var result = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.GenerateAndSpeakAnnouncementAsync(
                    "Next stop Central.", new Station(), 1, suppressSpeechErrors: false)).ConfigureAwait(false);
            Assert.That(result, Is.SameAs(failure));
        }

        _mockLogger.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            failure,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    public async Task GenerateAndSpeakAnnouncementAsync_FactoryFailure_IsLoggedAndSuppressedByDefault()
    {
        var failure = new InvalidOperationException("No speaker registration.");
        var factory = new Mock<ISpeakerEngineFactory>();
        factory.Setup(value => value.CreateEngineFromOptions()).Throws(failure);
        var service = new AnnouncementService(factory.Object, _mockLogger.Object);

        await service.GenerateAndSpeakAnnouncementAsync("Next stop Central.", new Station(), 1)
            .ConfigureAwait(false);

        factory.Verify(value => value.CreateEngineFromOptions(), Times.Once);
        _mockLogger.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            failure,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    private AnnouncementService CreateSpeakingService(Mock<ISpeakerEngine> speaker)
    {
        var factory = new Mock<ISpeakerEngineFactory>();
        factory.Setup(value => value.CreateEngineFromOptions()).Returns(speaker.Object);
        return new AnnouncementService(factory.Object, _mockLogger.Object);
    }
}
