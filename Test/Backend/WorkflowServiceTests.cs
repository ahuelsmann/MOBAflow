// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Backend.Service;
using Moba.Common.Events;
using Moba.Domain.Enum;

using Mocks;

/// <summary>
/// Integration tests for WorkflowService with IActionExecutor.
/// Tests end-to-end workflow execution.
/// </summary>
[TestFixture]
public class WorkflowServiceTests
{
    private WorkflowService _workflowService = null!;
    private IActionExecutor _actionExecutor = null!;
    private FakeUdpClientWrapper _fakeUdp = null!;
    private Z21 _z21 = null!;
    private IEventBus _eventBus = null!;
    private ActionExecutionContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        _fakeUdp = new FakeUdpClientWrapper();
        _eventBus = new EventBus();
        _z21 = new Z21(_fakeUdp, _eventBus);
        _actionExecutor = new ActionExecutor();
        _workflowService = new WorkflowService(_actionExecutor);

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
    public Task ExecuteAsync_WithEmptyWorkflow_ShouldNotThrow()
    {
        // Arrange
        var workflow = new Workflow
        {
            Id = Guid.NewGuid(),
            Name = "Empty Workflow",
            Actions = []
        };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _workflowService.ExecuteAsync(workflow, _context));
        return Task.CompletedTask;
    }

    [Test]
    public async Task ExecuteAsync_WithMultipleActions_ShouldExecuteAll()
    {
        // Arrange
        var workflow = new Workflow
        {
            Id = Guid.NewGuid(),
            Name = "Multi-Action Workflow",
            Actions =
            [
                new WorkflowAction
                {
                    Id = Guid.NewGuid(),
                    Number = 1,
                    Name = "Command 1",
                    Type = ActionType.Command,
                    Parameters = new Dictionary<string, object>
                    {
                        { "Bytes", Convert.ToBase64String(new byte[] { 0x01, 0x00, 0x00, 0x00 }) }
                    }
                },
                new WorkflowAction
                {
                    Id = Guid.NewGuid(),
                    Number = 2,
                    Name = "Command 2",
                    Type = ActionType.Command,
                    Parameters = new Dictionary<string, object>
                    {
                        { "Bytes", Convert.ToBase64String(new byte[] { 0x02, 0x00, 0x00, 0x00 }) }
                    }
                }
            ]
        };

        // Act
        await _workflowService.ExecuteAsync(workflow, _context);

        // Assert
        Assert.That(_fakeUdp.SentPayloads, Has.Count.GreaterThanOrEqualTo(2),
            "At least 2 command packets should have been sent");
    }

    [Test]
    public void ExecuteAsync_WithNullWorkflow_ShouldThrow()
    {
        // Arrange
        Workflow? workflow = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
#pragma warning disable CS8604 // Possible null reference argument
            await _workflowService.ExecuteAsync(workflow, _context);
#pragma warning restore CS8604
        });
    }

    [Test]
    public void ExecuteAsync_WithNullContext_ShouldThrow()
    {
        // Arrange
        var workflow = new Workflow
        {
            Id = Guid.NewGuid(),
            Name = "Test Workflow",
            Actions = []
        };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
            await _workflowService.ExecuteAsync(workflow, null);
#pragma warning restore CS8625
        });
    }
}
