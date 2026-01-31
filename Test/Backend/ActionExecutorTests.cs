// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Domain.Enum;
using Moba.Backend.Service;
using Mocks;

/// <summary>
/// Unit tests for IActionExecutor interface implementation.
/// Tests workflow action execution logic without hardware dependencies.
/// </summary>
[TestFixture]
public class ActionExecutorTests
{
    private IActionExecutor _actionExecutor = null!;
    private FakeUdpClientWrapper _fakeUdp = null!;
    private Z21 _z21 = null!;
    private ActionExecutionContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        _actionExecutor = new ActionExecutor(); // No AnnouncementService for basic tests
        _fakeUdp = new FakeUdpClientWrapper();
        _z21 = new Z21(_fakeUdp);

        _context = new ActionExecutionContext
        {
            Z21 = _z21
        };
    }

    [TearDown]
    public void TearDown()
    {
        _z21.Dispose();
        _fakeUdp.Dispose();
    }

    [Test]
    public async Task ExecuteAsync_WithCommandAction_ShouldSendZ21Command()
    {
        // Arrange
        var commandBytes = new byte[] { 0x40, 0x00, 0x00, 0x00 };
        var action = new WorkflowAction
        {
            Id = Guid.NewGuid(),
            Number = 1,
            Name = "Test Command",
            Type = ActionType.Command,
            Parameters = new Dictionary<string, object>
            {
                { "Bytes", Convert.ToBase64String(commandBytes) }
            }
        };

        // Act
        await _actionExecutor.ExecuteAsync(action, _context);

        // Assert
        Assert.That(_fakeUdp.SentPayloads, Is.Not.Empty, "At least one packet should have been sent");

        var lastPacket = _fakeUdp.SentPayloads[^1];
        Assert.That(lastPacket, Has.Length.GreaterThanOrEqualTo(4), "Packet should have at least 4 bytes");
    }

    [Test]
    public void ExecuteAsync_WithCommandAction_MissingParameters_ShouldThrow()
    {
        // Arrange
        var action = new WorkflowAction
        {
            Id = Guid.NewGuid(),
            Number = 1,
            Name = "Invalid Command",
            Type = ActionType.Command,
            Parameters = null
        };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () => await _actionExecutor.ExecuteAsync(action, _context));
    }

    [Test]
    public Task ExecuteAsync_WithAudioAction_WithoutSoundPlayer_ShouldThrow()
    {
        // Arrange - Context without SoundPlayer
        var action = new WorkflowAction
        {
            Id = Guid.NewGuid(),
            Number = 2,
            Name = "Test Audio",
            Type = ActionType.Audio,
            Parameters = new Dictionary<string, object>
            {
                { "AudioFile", "test.mp3" }
            }
        };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () => await _actionExecutor.ExecuteAsync(action, _context));
        return Task.CompletedTask;
    }

    [Test]
    public void ExecuteAsync_WithUnsupportedActionType_ShouldThrow()
    {
        // Arrange
        var action = new WorkflowAction
        {
            Id = Guid.NewGuid(),
            Number = 4,
            Name = "Unsupported Action",
            Type = (ActionType)999,
            Parameters = []
        };

        // Act & Assert
        Assert.ThrowsAsync<NotSupportedException>(async () => await _actionExecutor.ExecuteAsync(action, _context));
    }
}
