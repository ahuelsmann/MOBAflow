// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Events;
using Moba.Domain;
using Moba.Domain.Enum;

using Mocks;

/// <summary>
/// Unit tests for IActionExecutor interface implementation.
/// Tests workflow action execution logic without hardware dependencies.
/// </summary>
[TestFixture]
internal class ActionExecutorTests
{
    private IActionExecutor _actionExecutor = null!;
    private FakeUdpClientWrapper _fakeUdp = null!;
    private Z21 _z21 = null!;
    private IEventBus _eventBus = null!;
    private ActionExecutionContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        _actionExecutor = new ActionExecutor(); // No AnnouncementService for basic tests
        _fakeUdp = new FakeUdpClientWrapper();
        _eventBus = new EventBus(NullLogger<EventBus>.Instance);
        _z21 = new Z21(_fakeUdp, _eventBus);

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
            Command = new CommandActionPayload
            {
                BytesBase64 = Convert.ToBase64String(commandBytes)
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
    public void ExecuteAsync_WithCommandAction_MissingCommandPayload_ShouldThrow()
    {
        // Arrange
        var action = new WorkflowAction
        {
            Id = Guid.NewGuid(),
            Number = 1,
            Name = "Invalid Command",
            Type = ActionType.Command,
            Command = null
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
            Audio = new AudioActionPayload { FilePath = "test.mp3" }
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
            Type = (ActionType)999
        };

        // Act & Assert
        Assert.ThrowsAsync<NotSupportedException>(async () => await _actionExecutor.ExecuteAsync(action, _context));
    }

    [Test]
    public async Task ExecuteAsync_WithTrainDestinationDisplayAction_ShouldCallDisplayService()
    {
        var displayDeviceId = Guid.NewGuid();
        var displayService = new RecordingTrainDestinationDisplayService();
        var actionExecutor = new ActionExecutor(trainDestinationDisplayService: displayService);
        var action = new WorkflowAction
        {
            Id = Guid.NewGuid(),
            Number = 5,
            Name = "Update Display",
            Type = ActionType.TrainDestinationDisplay,
            TrainDestinationDisplay = new TrainDestinationDisplayActionPayload
            {
                DisplayDeviceId = displayDeviceId
            }
        };

        await actionExecutor.ExecuteAsync(action, _context);

        Assert.That(displayService.Calls, Is.EqualTo(1));
        Assert.That(displayService.LastAction, Is.SameAs(action));
        Assert.That(displayService.LastContext, Is.SameAs(_context));
    }

    private sealed class RecordingTrainDestinationDisplayService : ITrainDestinationDisplayService
    {
        public int Calls { get; private set; }

        public WorkflowAction? LastAction { get; private set; }

        public ActionExecutionContext? LastContext { get; private set; }

        public Task UpdateAsync(WorkflowAction action, ActionExecutionContext context, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            Calls++;
            LastAction = action;
            LastContext = context;
            return Task.CompletedTask;
        }
    }
}
