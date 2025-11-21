#pragma warning disable CA1416 // Platform-specific API - tests may use Windows-only sound APIs

using Moq;
using Moba.Sound;

namespace Moba.Test.Backend;

/// <summary>
/// Unit tests for Action classes (Audio, Command, Announcement).
/// Tests action execution and context handling.
/// </summary>
[TestFixture]
public class ActionTests
{
    #region Audio Action Tests

    [Test]
    public async Task Audio_ExecutesSuccessfully_WithValidFile()
    {
        // Arrange
        var soundPlayerMock = new Mock<ISoundPlayer>();
        var audio = new Audio("test.wav") { Name = "Test Audio" };
        var context = new ActionExecutionContext
        {
            SoundPlayer = soundPlayerMock.Object
        };

        // Act
        await audio.ExecuteAsync(context);

        // Assert
        soundPlayerMock.Verify(sp => sp.Play("test.wav"), Times.Once);
    }

    [Test]
    public async Task Audio_DoesNotExecute_WhenFileIsNull()
    {
        // Arrange
        var soundPlayerMock = new Mock<ISoundPlayer>();
        var audio = new Audio(null!) { Name = "Null Audio" };
        var context = new ActionExecutionContext
        {
            SoundPlayer = soundPlayerMock.Object
        };

        // Act
        await audio.ExecuteAsync(context);

        // Assert
        soundPlayerMock.Verify(sp => sp.Play(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Audio_DoesNotExecute_WhenFileIsEmpty()
    {
        // Arrange
        var soundPlayerMock = new Mock<ISoundPlayer>();
        var audio = new Audio(string.Empty) { Name = "Empty Audio" };
        var context = new ActionExecutionContext
        {
            SoundPlayer = soundPlayerMock.Object
        };

        // Act
        await audio.ExecuteAsync(context);

        // Assert
        soundPlayerMock.Verify(sp => sp.Play(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Audio_DoesNotCrash_WhenSoundPlayerIsNull()
    {
        // Arrange
        var audio = new Audio("test.wav");
        var context = new ActionExecutionContext
        {
            SoundPlayer = null
        };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await audio.ExecuteAsync(context));
    }

    [Test]
    public void Audio_Type_IsSound()
    {
        // Arrange
        var audio = new Audio("test.wav");

        // Assert
        Assert.That(audio.Type, Is.EqualTo(Moba.Backend.Model.Enum.ActionType.Sound));
    }

    #endregion

    #region Command Action Tests

    [Test]
    public async Task Command_ExecutesSuccessfully_WithValidBytes()
    {
        // Arrange
        var z21Mock = new Mock<Moba.Backend.Interface.IZ21>();
        var commandBytes = new byte[] { 0x01, 0x02, 0x03 };
        var command = new Command(commandBytes) { Name = "Test Command" };
        var context = new ActionExecutionContext
        {
            Z21 = z21Mock.Object // Direct assignment, no cast needed
        };

        // Act
        await command.ExecuteAsync(context);

        // Assert
        z21Mock.Verify(z => z.SendCommandAsync(commandBytes), Times.Once);
    }

    [Test]
    public async Task Command_DoesNotExecute_WhenBytesAreNull()
    {
        // Arrange
        var z21Mock = new Mock<Moba.Backend.Interface.IZ21>();
        var command = new Command(null!) { Name = "Null Command" };
        var context = new ActionExecutionContext
        {
            Z21 = z21Mock.Object // Direct assignment, no cast needed
        };

        // Act
        await command.ExecuteAsync(context);

        // Assert
        z21Mock.Verify(z => z.SendCommandAsync(It.IsAny<byte[]>()), Times.Never);
    }

    [Test]
    public async Task Command_DoesNotExecute_WhenBytesAreEmpty()
    {
        // Arrange
        var z21Mock = new Mock<Moba.Backend.Interface.IZ21>();
        var command = new Command(Array.Empty<byte>()) { Name = "Empty Command" };
        var context = new ActionExecutionContext
        {
            Z21 = z21Mock.Object // Direct assignment, no cast needed
        };

        // Act
        await command.ExecuteAsync(context);

        // Assert
        z21Mock.Verify(z => z.SendCommandAsync(It.IsAny<byte[]>()), Times.Never);
    }

    [Test]
    public async Task Command_DoesNotCrash_WhenZ21IsNull()
    {
        // Arrange
        var command = new Command([0x01]);
        var context = new ActionExecutionContext
        {
            Z21 = null
        };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await command.ExecuteAsync(context));
    }

    [Test]
    public void Command_Type_IsCommand()
    {
        // Arrange
        var command = new Command([0x01]);

        // Assert
        Assert.That(command.Type, Is.EqualTo(Moba.Backend.Model.Enum.ActionType.Command));
    }

    #endregion

    #region Announcement Action Tests

    [Test]
    public async Task Announcement_ExecutesSuccessfully_WithValidText()
    {
        // Arrange
        var speakerMock = new Mock<ISpeakerEngine>();
        var announcement = new Announcement("Hello World") { Name = "Test Announcement" };
        var context = new ActionExecutionContext
        {
            SpeakerEngine = speakerMock.Object
        };

        // Act
        await announcement.ExecuteAsync(context);

        // Assert
        speakerMock.Verify(sp => sp.AnnouncementAsync("Hello World", null), Times.Once);
    }

    [Test]
    public async Task Announcement_DoesNotExecute_WhenTextIsNull()
    {
        // Arrange
        var speakerMock = new Mock<ISpeakerEngine>();
        var announcement = new Announcement(null!) { Name = "Null Announcement" };
        var context = new ActionExecutionContext
        {
            SpeakerEngine = speakerMock.Object
        };

        // Act
        await announcement.ExecuteAsync(context);

        // Assert
        speakerMock.Verify(sp => sp.AnnouncementAsync(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Test]
    public async Task Announcement_DoesNotExecute_WhenTextIsEmpty()
    {
        // Arrange
        var speakerMock = new Mock<ISpeakerEngine>();
        var announcement = new Announcement(string.Empty) { Name = "Empty Announcement" };
        var context = new ActionExecutionContext
        {
            SpeakerEngine = speakerMock.Object
        };

        // Act
        await announcement.ExecuteAsync(context);

        // Assert
        speakerMock.Verify(sp => sp.AnnouncementAsync(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Test]
    public async Task Announcement_DoesNotCrash_WhenSpeakerEngineIsNull()
    {
        // Arrange
        var announcement = new Announcement("Test");
        var context = new ActionExecutionContext
        {
            SpeakerEngine = null
        };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await announcement.ExecuteAsync(context));
    }

    [Test]
    public async Task Announcement_UsesJourneyTemplateText_WhenAvailable()
    {
        // Arrange
        var speakerMock = new Mock<ISpeakerEngine>();
        var announcement = new Announcement("Default Text") { Name = "Template Announcement" };
        var station = new Station { Name = "Berlin Hbf", IsExitOnLeft = true };
        var context = new ActionExecutionContext
        {
            SpeakerEngine = speakerMock.Object,
            JourneyTemplateText = "Train arriving at {StationName}",
            CurrentStation = station
        };

        // Act
        await announcement.ExecuteAsync(context);

        // Assert
        speakerMock.Verify(sp => 
            sp.AnnouncementAsync("Train arriving at Berlin Hbf", null), 
            Times.Once);
    }

    [Test]
    public async Task Announcement_ReplacesStationName_Placeholder()
    {
        // Arrange
        var speakerMock = new Mock<ISpeakerEngine>();
        var announcement = new Announcement("Station: {StationName}") { Name = "Station Announcement" };
        var station = new Station { Name = "München Hbf" };
        var context = new ActionExecutionContext
        {
            SpeakerEngine = speakerMock.Object,
            JourneyTemplateText = "Station: {StationName}",
            CurrentStation = station
        };

        // Act
        await announcement.ExecuteAsync(context);

        // Assert
        speakerMock.Verify(sp => 
            sp.AnnouncementAsync("Station: München Hbf", null), 
            Times.Once);
    }

    [Test]
    public async Task Announcement_ReplacesExitDirection_Placeholder()
    {
        // Arrange
        var speakerMock = new Mock<ISpeakerEngine>();
        var announcement = new Announcement("Exit on {StationIsExitOnLeft}") { Name = "Exit Announcement" };
        
        var stationLeft = new Station { Name = "Test", IsExitOnLeft = true };
        var stationRight = new Station { Name = "Test", IsExitOnLeft = false };

        var contextLeft = new ActionExecutionContext
        {
            SpeakerEngine = speakerMock.Object,
            JourneyTemplateText = "Exit on {StationIsExitOnLeft}",
            CurrentStation = stationLeft
        };

        var contextRight = new ActionExecutionContext
        {
            SpeakerEngine = speakerMock.Object,
            JourneyTemplateText = "Exit on {StationIsExitOnLeft}",
            CurrentStation = stationRight
        };

        // Act
        await announcement.ExecuteAsync(contextLeft);
        await announcement.ExecuteAsync(contextRight);

        // Assert
        speakerMock.Verify(sp => sp.AnnouncementAsync("Exit on links", null), Times.Once);
        speakerMock.Verify(sp => sp.AnnouncementAsync("Exit on rechts", null), Times.Once);
    }

    [Test]
    public async Task Announcement_ReplacesMultiplePlaceholders()
    {
        // Arrange
        var speakerMock = new Mock<ISpeakerEngine>();
        var announcement = new Announcement("Complex Template") { Name = "Complex Announcement" };
        var station = new Station { Name = "Hamburg", IsExitOnLeft = false };
        var context = new ActionExecutionContext
        {
            SpeakerEngine = speakerMock.Object,
            JourneyTemplateText = "Arriving at {StationName}. Exit {StationIsExitOnLeft}.",
            CurrentStation = station
        };

        // Act
        await announcement.ExecuteAsync(context);

        // Assert
        speakerMock.Verify(sp => 
            sp.AnnouncementAsync("Arriving at Hamburg. Exit rechts.", null), 
            Times.Once);
    }

    [Test]
    public void Announcement_Type_IsAnnouncement()
    {
        // Arrange
        var announcement = new Announcement("Test");

        // Assert
        Assert.That(announcement.Type, Is.EqualTo(Moba.Backend.Model.Enum.ActionType.Announcement));
    }

    #endregion

    #region Base Action Tests

    [Test]
    public void Action_NameProperty_IsSettable()
    {
        // Arrange
        var command = new Command([0x01]);

        // Act
        command.Name = "Custom Name";

        // Assert
        Assert.That(command.Name, Is.EqualTo("Custom Name"));
    }

    [Test]
    public void Audio_InitializesWithDefaultName()
    {
        // Arrange & Act
        var audio = new Audio("test.wav");

        // Assert
        Assert.That(audio.Name, Is.EqualTo("New Wave Output"));
    }

    [Test]
    public void Command_InitializesWithDefaultName()
    {
        // Arrange & Act
        var command = new Command([0x01]);

        // Assert
        Assert.That(command.Name, Is.EqualTo("New Command"));
    }

    [Test]
    public void Announcement_InitializesWithDefaultName()
    {
        // Arrange & Act
        var announcement = new Announcement("Test");

        // Assert
        Assert.That(announcement.Name, Is.EqualTo("New Announcement"));
    }

    #endregion
}
