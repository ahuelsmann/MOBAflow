// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Test.Helpers;
using Microsoft.Extensions.Logging;

using Moba.Backend.Service;
using Moba.Speech.Base;

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
        _announcementService = new AnnouncementService(new NullSpeechOutput(), _mockLogger.Object);
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
        using var cancellation = new CancellationTokenSource();
        var output = new Mock<ISpeechOutput>();
        output.Setup(value => value.SpeakAsync(It.IsAny<string>(), cancellation.Token)).Returns(() =>
        {
            cancellation.Cancel();
            return Task.FromCanceled<SpeechResult>(cancellation.Token);
        });
        var service = new AnnouncementService(output.Object, _mockLogger.Object);
        await Assert.CatchAsync<OperationCanceledException>(() => service.GenerateAndSpeakAnnouncementAsync(
            "Next stop {StationName}", new Station { Name = "Minden" }, 1, cancellation.Token)).ConfigureAwait(false);
        output.Verify(value => value.SpeakAsync("Next stop Minden", cancellation.Token), Times.Once);
    }

    [Test]
    public async Task GenerateAndSpeakAnnouncementAsync_ForwardsRenderedTextAndCallerToken()
    {
        using var cancellation = new CancellationTokenSource();
        var output = new Mock<ISpeechOutput>();
        output.Setup(value => value.SpeakAsync(It.IsAny<string>(), cancellation.Token)).ReturnsAsync(SpeechResult.Success([1]));
        var service = new AnnouncementService(output.Object, _mockLogger.Object);
        await service.GenerateAndSpeakAnnouncementAsync("Next stop {StationName}, platform {TrackNumber}.",
            new Station { Name = "Central", Platforms = [new Platform { Number = 4 }] }, 1, cancellation.Token).ConfigureAwait(false);
        output.Verify(value => value.SpeakAsync("Next stop Central, platform 4.", cancellation.Token), Times.Once);
    }

    [TestCase(null)]
    [TestCase("")]
    public async Task GenerateAndSpeakAnnouncementAsync_EmptyTemplate_DoesNotSpeak(string? template)
    {
        var output = new Mock<ISpeechOutput>(MockBehavior.Strict);
        await TestWorkflowServices.Announcements(output.Object).GenerateAndSpeakAnnouncementAsync(template, new Station(), 1).ConfigureAwait(false);
        output.VerifyNoOtherCalls();
    }

    [Test]
    public async Task GenerateAndSpeakAnnouncementAsync_AlreadyCancelled_DoesNotSpeak()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(false);
        var output = new Mock<ISpeechOutput>(MockBehavior.Strict);
        await Assert.CatchAsync<OperationCanceledException>(() => TestWorkflowServices.Announcements(output.Object)
            .GenerateAndSpeakAnnouncementAsync("Test", new Station(), 1, cancellation.Token)).ConfigureAwait(false);
        output.VerifyNoOtherCalls();
    }

    [Test]
    public async Task GenerateAndSpeakAnnouncementAsync_OverlappingCalls_AreNotQueuedByTheService()
    {
        var firstCompletion = new TaskCompletionSource<SpeechResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondCompletion = new TaskCompletionSource<SpeechResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var output = new Mock<ISpeechOutput>();
        output.Setup(value => value.SpeakAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string text, CancellationToken _) => text == "First" ? firstCompletion.Task : secondCompletion.Task);
        var service = TestWorkflowServices.Announcements(output.Object);
        var first = service.GenerateAndSpeakAnnouncementAsync("First", new Station(), 1);
        var second = service.GenerateAndSpeakAnnouncementAsync("Second", new Station(), 1);
        try
        {
            secondCompletion.SetResult(SpeechResult.Success([1]));
            await second.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            Assert.That(first.IsCompleted, Is.False);
            firstCompletion.SetResult(SpeechResult.Success([1]));
            await first.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        }
        finally
        {
            firstCompletion.TrySetResult(SpeechResult.Success([1]));
            secondCompletion.TrySetResult(SpeechResult.Success([1]));
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GenerateAndSpeakAnnouncementAsync_OutputException_RespectsErrorSuppression(bool suppressErrors)
    {
        var failure = new InvalidOperationException("Speech unavailable.");
        var output = new Mock<ISpeechOutput>();
        output.Setup(value => value.SpeakAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        var service = new AnnouncementService(output.Object, _mockLogger.Object);
        if (suppressErrors)
        {
            await service.GenerateAndSpeakAnnouncementAsync("Test", new Station(), 1).ConfigureAwait(false);
        }
        else
        {
            var result = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateAndSpeakAnnouncementAsync(
                "Test", new Station(), 1, suppressSpeechErrors: false)).ConfigureAwait(false);
            Assert.That(result, Is.SameAs(failure));
        }
        _mockLogger.Verify(logger => logger.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), failure,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    public async Task GenerateAndSpeakAnnouncementAsync_FailedResult_IsReportedToSettingsTest()
    {
        var output = new Mock<ISpeechOutput>();
        output.Setup(value => value.SpeakAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SpeechResult.Failure(SpeechFailureKind.Unavailable, "Speech unavailable."));
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => TestWorkflowServices.Announcements(output.Object)
            .GenerateAndSpeakAnnouncementAsync("Test", new Station(), 1, suppressSpeechErrors: false)).ConfigureAwait(false);
        Assert.That(failure!.Message, Is.EqualTo("Speech unavailable."));
    }
}
