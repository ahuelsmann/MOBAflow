// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Domain;
using Moba.Domain.Enum;

using Moq;

/// <summary>Verifies that the editor's test run plans through the workflow service with the project under edit.</summary>
[TestFixture]
internal sealed class WorkflowDryRunServiceTests
{
    [Test]
    public async Task DryRunAsync_PlansTheWorkflowWithAContextOverTheSharedServices()
    {
        var workflow = new Workflow { Name = "Preview", Actions = [new WorkflowAction { Type = ActionType.Command, Command = new CommandActionPayload() }] };
        var project = new Project { Workflows = [workflow] };
        var z21 = Mock.Of<IZ21>();
        WorkflowExecutionRequest? capturedRequest = null;
        var workflowService = new Mock<IWorkflowService>();
        workflowService
            .Setup(service => service.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowExecutionRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new WorkflowExecutionResult
            {
                ExecutionId = Guid.NewGuid(),
                WorkflowId = workflow.Id,
                SourceCorrelationId = Guid.NewGuid(),
                Status = WorkflowExecutionStatus.Succeeded,
                PlannedEffects = Array.Empty<WorkflowPlannedEffect>()
            });
        var service = new WorkflowDryRunService(
            workflowService.Object,
            new ActionExecutionContextFactory(new ActionExecutionContext { Z21 = z21 }));

        var result = await service.DryRunAsync(project, workflow).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.Succeeded));
            Assert.That(capturedRequest, Is.Not.Null);
            Assert.That(capturedRequest!.Mode, Is.EqualTo(WorkflowRunMode.DryRun));
            Assert.That(capturedRequest.Project, Is.SameAs(project));
            Assert.That(capturedRequest.Workflow, Is.SameAs(workflow));
            Assert.That(capturedRequest.Context.CurrentProject, Is.SameAs(project));
            Assert.That(capturedRequest.Context.Z21, Is.SameAs(z21));
            Assert.That(capturedRequest.SourceCorrelationId, Is.Not.EqualTo(Guid.Empty));
        }
        workflowService.Verify(
            service => service.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void Constructor_RequiresTheWorkflowServiceAndTheContextFactory()
    {
        var factory = new ActionExecutionContextFactory(new ActionExecutionContext { Z21 = Mock.Of<IZ21>() });
        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => new WorkflowDryRunService(null!, factory), Throws.ArgumentNullException);
            Assert.That(() => new WorkflowDryRunService(Mock.Of<IWorkflowService>(), null!), Throws.ArgumentNullException);
        }
    }
}
