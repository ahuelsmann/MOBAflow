// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Integration;

using Domain.Enum;
using Moba.Backend.Service;
using Mocks;

/// <summary>
/// End-to-end workflow execution tests.
/// Tests complete workflow: Station reached → Workflow trigger → Actions executed.
/// </summary>
[TestFixture]
public class WorkflowExecutionEndToEndTests
{
    private FakeUdpClientWrapper _fakeUdp = null!;
    private Z21 _z21 = null!;
    private ActionExecutor _actionExecutor = null!;
    private WorkflowService _workflowService = null!;
    private ActionExecutionContext _executionContext = null!;

    [SetUp]
    public void SetUp()
    {
        _fakeUdp = new FakeUdpClientWrapper();
        _z21 = new Z21(_fakeUdp);
        _actionExecutor = new ActionExecutor();
        _workflowService = new WorkflowService(_actionExecutor);

        _executionContext = new ActionExecutionContext
        {
            Z21 = _z21
        };
    }

    [TearDown]
    public void TearDown()
    {
        _z21.Dispose();
    }

    [Test]
    public async Task SimpleWorkflow_WithOneCommand_ShouldExecute()
    {
        // Arrange
        var workflow = new Workflow
        {
            Id = Guid.NewGuid(),
            Name = "Simple Workflow",
            Actions =
            [
                new WorkflowAction
                {
                    Id = Guid.NewGuid(),
                    Number = 1,
                    Name = "Stop Train",
                    Type = ActionType.Command,
                    Parameters = new Dictionary<string, object>
                    {
                        { "Bytes", Convert.ToBase64String(new byte[] { 0x40, 0x00, 0x00, 0x00 }) }
                    }
                }
            ]
        };

        // Act
        await _workflowService.ExecuteAsync(workflow, _executionContext);

        // Assert
        Assert.That(_fakeUdp.SentPayloads.Count, Is.GreaterThanOrEqualTo(1), "At least one command should be sent");
    }

    [Test]
    public async Task ComplexWorkflow_WithMultipleActions_ShouldExecuteSequentially()
    {
        // Arrange
        var workflow = new Workflow
        {
            Id = Guid.NewGuid(),
            Name = "Complex Workflow",
            Actions =
            [
                new WorkflowAction
                {
                    Id = Guid.NewGuid(),
                    Number = 1,
                    Name = "Action 1",
                    Type = ActionType.Command,
                    Parameters = new Dictionary<string, object>
                    {
                        { "Bytes", Convert.ToBase64String(new byte[] { 0x01 }) }
                    }
                },
                new WorkflowAction
                {
                    Id = Guid.NewGuid(),
                    Number = 2,
                    Name = "Action 2",
                    Type = ActionType.Audio,
                    Parameters = new Dictionary<string, object>
                    {
                        { "AudioFile", "test.mp3" }
                    }
                },
                new WorkflowAction
                {
                    Id = Guid.NewGuid(),
                    Number = 3,
                    Name = "Action 3",
                    Type = ActionType.Command,
                    Parameters = new Dictionary<string, object>
                    {
                        { "Bytes", Convert.ToBase64String(new byte[] { 0x03 }) }
                    }
                }
            ]
        };

        // Act
        await _workflowService.ExecuteAsync(workflow, _executionContext);

        // Assert
        var sentCommands = _fakeUdp.SentPayloads.Count;
        Assert.That(sentCommands, Is.GreaterThanOrEqualTo(2), "At least 2 commands should be sent (Action 1 + 3)");
    }

    [Test]
    public async Task WorkflowWithoutActions_ShouldComplete()
    {
        // Arrange
        var workflow = new Workflow
        {
            Id = Guid.NewGuid(),
            Name = "Empty Workflow",
            Actions = []
        };

        var initialPayloadCount = _fakeUdp.SentPayloads.Count;

        // Act
        await _workflowService.ExecuteAsync(workflow, _executionContext);

        // Assert
        Assert.That(_fakeUdp.SentPayloads.Count, Is.EqualTo(initialPayloadCount), "No commands should be sent");
    }

    [Test]
    public Task WorkflowExecution_ShouldHandleErrors()
    {
        // Arrange
        var workflow = new Workflow
        {
            Id = Guid.NewGuid(),
            Name = "Error Workflow",
            Actions =
            [
                new WorkflowAction
                {
                    Id = Guid.NewGuid(),
                    Number = 1,
                    Name = "Invalid Action",
                    Type = (ActionType)999, // Invalid type
                    Parameters = new Dictionary<string, object>()
                }
            ]
        };

        // Act & Assert
        // Note: WorkflowService catches exceptions from ActionExecutor and logs them
        // but continues executing remaining actions. No exception is rethrown.
        // This is by design - workflow should be resilient to action failures.
        Assert.DoesNotThrowAsync(async () =>
        {
            await _workflowService.ExecuteAsync(workflow, _executionContext);
        });
        return Task.CompletedTask;
    }

    [Test]
    public async Task WorkflowCommandExecution_ShouldUpdateZ21State()
    {
        // Arrange
        var workflow = new Workflow
        {
            Id = Guid.NewGuid(),
            Name = "Z21 State Workflow",
            Actions =
            [
                new WorkflowAction
                {
                    Id = Guid.NewGuid(),
                    Number = 1,
                    Name = "Set Track Power",
                    Type = ActionType.Command,
                    Parameters = new Dictionary<string, object>
                    {
                        { "Bytes", Convert.ToBase64String(new byte[] { 0x21, 0x81, 0x00, 0xA0 }) }
                    }
                }
            ]
        };

        var initialPayloads = _fakeUdp.SentPayloads.Count;

        // Act
        await _workflowService.ExecuteAsync(workflow, _executionContext);

        // Assert
        Assert.That(_fakeUdp.SentPayloads.Count, Is.GreaterThan(initialPayloads), "Z21 command should be sent");
    }
}
