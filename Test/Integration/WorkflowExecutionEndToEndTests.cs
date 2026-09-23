// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Integration;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Events;
using Moba.Domain;
using Moba.Domain.Enum;

using Mocks;

/// <summary>End-to-end Workflow 2.0 execution tests through a fake Z21 transport.</summary>
[TestFixture]
internal sealed class WorkflowExecutionEndToEndTests
{
    private FakeUdpClientWrapper _fakeUdp = null!;
    private Z21 _z21 = null!;
    private WorkflowService _workflowService = null!;

    [SetUp]
    public void SetUp()
    {
        _fakeUdp = new FakeUdpClientWrapper();
        _z21 = new Z21(_fakeUdp, new EventBus(NullLogger<EventBus>.Instance));
        _workflowService = new WorkflowService(ActionExecutor.CreateWithDefaultHandlers());
    }

    [TearDown]
    public void TearDown()
    {
        _z21.Dispose();
        _fakeUdp.Dispose();
    }

    [Test]
    public async Task SimpleWorkflow_WithOneCommand_ShouldExecute()
    {
        var workflow = CreateCommandWorkflow([new byte[] { 0x40, 0x00, 0x00, 0x00 }]);

        var result = await ExecuteAsync(workflow);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.Succeeded));
            Assert.That(_fakeUdp.SentPayloads.Count, Is.GreaterThanOrEqualTo(1));
        });
    }

    [Test]
    public async Task ComplexWorkflow_WithMultipleActions_ShouldExecuteSequentially()
    {
        var workflow = CreateCommandWorkflow([new byte[] { 0x01 }, new byte[] { 0x02 }, new byte[] { 0x03 }]);

        var result = await ExecuteAsync(workflow);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.Succeeded));
            Assert.That(_fakeUdp.SentPayloads.Count, Is.GreaterThanOrEqualTo(3));
        });
    }

    [Test]
    public async Task EmptyWorkflow_IsRejectedBeforeExecution()
    {
        var workflow = new Workflow();
        var initialPayloadCount = _fakeUdp.SentPayloads.Count;

        var result = await ExecuteAsync(workflow);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.NotStarted));
            Assert.That(_fakeUdp.SentPayloads, Has.Count.EqualTo(initialPayloadCount));
        });
    }

    [Test]
    public async Task UnsupportedAction_IsRejectedBeforeExecution()
    {
        var workflow = new Workflow { Actions = [new WorkflowAction { Type = (ActionType)999 }] };

        var result = await ExecuteAsync(workflow);

        Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.NotStarted));
    }

    [Test]
    public async Task WorkflowCommandExecution_ShouldUpdateZ21Transport()
    {
        var workflow = CreateCommandWorkflow([new byte[] { 0x21, 0x81, 0x00, 0xA0 }]);
        var initialPayloads = _fakeUdp.SentPayloads.Count;

        var result = await ExecuteAsync(workflow);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.Succeeded));
            Assert.That(_fakeUdp.SentPayloads, Has.Count.GreaterThan(initialPayloads));
        });
    }

    [Test]
    public async Task SequentialActions_PreserveFakeZ21CorrelationChain()
    {
        var trace = new WorkflowTraceStore();
        _workflowService = new WorkflowService(ActionExecutor.CreateWithDefaultHandlers(), new WorkflowServiceDependencies
        {
            Validator = new WorkflowValidator(), EffectPlanner = new WorkflowEffectPlanner(),
            EventBus = new EventBus(NullLogger<EventBus>.Instance), TraceStore = trace, TimeProvider = TimeProvider.System
        });
        var workflow = CreateCommandWorkflow([new byte[] { 1 }, new byte[] { 2 }]);
        var project = new Project { Workflows = [workflow] };
        var sourceId = Guid.NewGuid();
        var result = await _workflowService.ExecuteAsync(new WorkflowExecutionRequest
        {
            Project = project, Workflow = workflow,
            Context = new ActionExecutionContext { Z21 = _z21, SourceEvent = new FeedbackReceivedEvent(5, sourceId) }
        });
        var entries = trace.GetEntries();
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.Succeeded));
            Assert.That(result.SourceCorrelationId, Is.EqualTo(sourceId));
            Assert.That(_fakeUdp.SentPayloads, Is.Not.Empty);
            Assert.That(entries, Is.All.Matches<WorkflowLifecycleEvent>(entry => entry.SourceCorrelationId == sourceId && entry.ExecutionId == result.ExecutionId));
            Assert.That(entries.Select(entry => entry.Sequence), Is.EqualTo(Enumerable.Range(1, entries.Count).Select(value => (long)value)));
            Assert.That(entries.Where(entry => entry.Kind == WorkflowLifecycleKind.StepStarted).Select(entry => entry.StepId), Is.EqualTo(workflow.Actions.Select(action => action.Id)));
            Assert.That(entries.Count(entry => entry.Kind == WorkflowLifecycleKind.WorkflowCompleted), Is.EqualTo(1));
        });
    }

    private async Task<WorkflowExecutionResult> ExecuteAsync(Workflow workflow)
    {
        var project = new Project { Workflows = [workflow] };
        return await _workflowService.ExecuteAsync(new WorkflowExecutionRequest
        {
            Project = project,
            Workflow = workflow,
            Context = new ActionExecutionContext { Z21 = _z21, CurrentProject = project },
            Mode = WorkflowRunMode.Live
        });
    }

    private static Workflow CreateCommandWorkflow(IReadOnlyList<byte[]> commands) => new()
    {
        Actions = commands.Select((bytes, index) => new WorkflowAction
        {
            Name = $"Command {index + 1}", Type = ActionType.Command,
            Command = new CommandActionPayload { BytesBase64 = Convert.ToBase64String(bytes) }
        }).ToList()
    };
}
