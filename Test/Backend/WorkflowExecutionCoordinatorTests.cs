// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Domain;

using Moq;

/// <summary>Verifies the reusable source-ordering and cancellation boundary.</summary>
[TestFixture]
public sealed class WorkflowExecutionCoordinatorTests
{
    [Test]
    public async Task CancelOwner_CancelsDelayedExecutionWithoutAffectingAnotherOwner()
    {
        var service = new Mock<IWorkflowService>();
        service
            .Setup(value => value.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()))
            .Returns<WorkflowExecutionRequest, CancellationToken>((request, _) => Task.FromResult(Succeeded(request)));
        using var coordinator = new WorkflowExecutionCoordinator(service.Object, TimeProvider.System);
        var cancelledRequest = CreateRequest();
        var successfulRequest = CreateRequest();
        var cancelledOwner = Guid.NewGuid();

        var cancelled = coordinator.EnqueueAsync(new QueuedWorkflowExecution
        {
            SourceKey = "feedback:1",
            OwnerId = cancelledOwner,
            Request = cancelledRequest,
            Delay = TimeSpan.FromHours(1)
        });
        coordinator.CancelOwner(cancelledOwner);
        var successful = coordinator.EnqueueAsync(new QueuedWorkflowExecution
        {
            SourceKey = "feedback:2",
            OwnerId = Guid.NewGuid(),
            Request = successfulRequest
        });

        var cancelledResult = await cancelled.WaitAsync(TimeSpan.FromSeconds(1));
        var successfulResult = await successful.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Multiple(() =>
        {
            Assert.That(cancelledResult.Status, Is.EqualTo(WorkflowExecutionStatus.Cancelled));
            Assert.That(successfulResult.Status, Is.EqualTo(WorkflowExecutionStatus.Succeeded));
        });
        service.Verify(value => value.ExecuteAsync(
            It.Is<WorkflowExecutionRequest>(request => request.Workflow.Id == cancelledRequest.Workflow.Id),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task FailedExecution_DoesNotPoisonSameSourceTail()
    {
        var service = new Mock<IWorkflowService>();
        var callCount = 0;
        service
            .Setup(value => value.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()))
            .Returns((WorkflowExecutionRequest request, CancellationToken _) =>
            {
                callCount++;
                return callCount == 1
                    ? Task.FromException<WorkflowExecutionResult>(new InvalidOperationException("Expected test failure"))
                    : Task.FromResult(Succeeded(request));
            });
        using var coordinator = new WorkflowExecutionCoordinator(service.Object, TimeProvider.System);
        var first = coordinator.EnqueueAsync(CreateQueued("feedback:1"));
        var second = coordinator.EnqueueAsync(CreateQueued("feedback:1"));

        Assert.That(async () => await first, Throws.TypeOf<InvalidOperationException>());
        var secondResult = await second.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Multiple(() =>
        {
            Assert.That(secondResult.Status, Is.EqualTo(WorkflowExecutionStatus.Succeeded));
            Assert.That(callCount, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Dispose_CancelsEveryPendingExecution()
    {
        var service = new Mock<IWorkflowService>();
        var coordinator = new WorkflowExecutionCoordinator(service.Object, TimeProvider.System);
        var first = coordinator.EnqueueAsync(CreateQueued("feedback:1", TimeSpan.FromHours(1)));
        var second = coordinator.EnqueueAsync(CreateQueued("feedback:2", TimeSpan.FromHours(1)));

        coordinator.Dispose();

        var results = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(1));
        Assert.That(results.Select(result => result.Status), Is.All.EqualTo(WorkflowExecutionStatus.Cancelled));
        service.Verify(value => value.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static QueuedWorkflowExecution CreateQueued(string sourceKey, TimeSpan delay = default) => new()
    {
        SourceKey = sourceKey,
        OwnerId = Guid.NewGuid(),
        Request = CreateRequest(),
        Delay = delay
    };

    private static WorkflowExecutionRequest CreateRequest()
    {
        var workflow = new Workflow();
        var project = new Project { Workflows = [workflow] };
        return new WorkflowExecutionRequest
        {
            Project = project,
            Workflow = workflow,
            Context = new ActionExecutionContext { Z21 = Mock.Of<IZ21>() },
            SourceCorrelationId = Guid.NewGuid()
        };
    }

    private static WorkflowExecutionResult Succeeded(WorkflowExecutionRequest request) => new()
    {
        ExecutionId = Guid.NewGuid(),
        WorkflowId = request.Workflow.Id,
        SourceCorrelationId = request.SourceCorrelationId,
        Status = WorkflowExecutionStatus.Succeeded
    };
}
