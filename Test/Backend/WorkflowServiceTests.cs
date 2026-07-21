// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Domain;
using Moba.Domain.Enum;

using Moq;

/// <summary>Compatibility-boundary tests for callers that supply a workflow and context directly.</summary>
[TestFixture]
internal sealed class WorkflowServiceTests
{
    [Test]
    public async Task ExecuteAsync_GraphWorkflow_UsesWorkflow2Executor()
    {
        var executor = new Mock<IActionExecutor>();
        executor
            .Setup(value => value.ExecuteAsync(
                It.IsAny<WorkflowAction>(),
                It.IsAny<ActionExecutionContext>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = new WorkflowService(executor.Object);
        var workflow = CreateWorkflow();
        var project = new Project { Workflows = [workflow] };

        await service.ExecuteAsync(
            workflow,
            new ActionExecutionContext { Z21 = Mock.Of<IZ21>(), CurrentProject = project });

        executor.Verify(value => value.ExecuteAsync(
            It.IsAny<WorkflowAction>(),
            It.IsAny<ActionExecutionContext>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void ExecuteAsync_InvalidGraph_ThrowsWithoutCallingActionExecutor()
    {
        var executor = new Mock<IActionExecutor>(MockBehavior.Strict);
        var service = new WorkflowService(executor.Object);
        var workflow = new Workflow { EntryStepId = Guid.NewGuid(), Steps = [] };
        var project = new Project { Workflows = [workflow] };

        Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(
            workflow,
            new ActionExecutionContext { Z21 = Mock.Of<IZ21>(), CurrentProject = project }));
        executor.VerifyNoOtherCalls();
    }

    [Test]
    public void ExecuteAsync_PreCancelledToken_ThrowsCancellationWithoutCallingActionExecutor()
    {
        var executor = new Mock<IActionExecutor>(MockBehavior.Strict);
        var service = new WorkflowService(executor.Object);
        var workflow = CreateWorkflow();
        var project = new Project { Workflows = [workflow] };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.CatchAsync<OperationCanceledException>(() => service.ExecuteAsync(
            workflow,
            new ActionExecutionContext { Z21 = Mock.Of<IZ21>(), CurrentProject = project },
            default,
            cancellation.Token));
        executor.VerifyNoOtherCalls();
    }

    private static Workflow CreateWorkflow()
    {
        var actionId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();
        return new Workflow
        {
            EntryStepId = actionId,
            Steps =
            [
                new WorkflowActionStep
                {
                    Id = actionId,
                    NextStepId = terminalId,
                    Action = new WorkflowAction
                    {
                        Type = ActionType.Command,
                        Command = new CommandActionPayload { BytesBase64 = "AQID" }
                    }
                },
                new WorkflowTerminateStep { Id = terminalId }
            ]
        };
    }
}
