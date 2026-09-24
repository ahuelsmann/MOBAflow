// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Events;
using Moba.Domain;
using Moba.Domain.Enum;
using Moq;

[TestFixture]
internal sealed class WorkflowSequenceExecutionTests
{
    [Test]
    public async Task FailedActionTracePreservesElapsedTime()
    {
        var clock = new MOBAdisplay.ManualTimeProvider();
        var trace = new WorkflowTraceStore();
        var executor = new Executor((_, _, _) =>
        {
            clock.Advance(TimeSpan.FromMilliseconds(275));
            throw new InvalidOperationException("Failure after some work");
        });
        var service = new WorkflowService(executor, new WorkflowServiceDependencies
        {
            Validator = new WorkflowValidator(), EffectPlanner = new WorkflowEffectPlanner(),
            TraceStore = trace, TimeProvider = clock
        });

        await service.ExecuteAsync(Request(new Workflow { Actions = [Command("Fail")] })).ConfigureAwait(false);

        Assert.That(trace.GetEntries().Single(entry => entry.Kind == WorkflowLifecycleKind.StepFailed).Elapsed,
            Is.EqualTo(TimeSpan.FromMilliseconds(275)));
    }

    [TestCase("{\"type\":\"ChangeJourneyStop\"}")]
    [TestCase("{\"id\":\"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee\",\"type\":\"ChangeJourneyStop\"}")]
    [TestCase("{\"id\":\"not-a-guid\",\"type\":\"ChangeJourneyStop\",\"changeJourneyStop\":{}}")]
    [TestCase("{\"type\":\"ChangeJourneyStop\",\"changeJourneyStop\":{}}")]
    public async Task InvalidPersistedAction_RemainsRejectedAfterRuntimeJsonClone(string actionJson)
    {
        var workflow = System.Text.Json.JsonSerializer.Deserialize<Workflow>(
            "{\"actions\":[" + actionJson + "]}", JsonOptions.Default)
            ?? throw new InvalidOperationException("Test workflow could not be loaded.");
        var clone = System.Text.Json.JsonSerializer.Deserialize<Workflow>(
            System.Text.Json.JsonSerializer.Serialize(workflow, JsonOptions.Default), JsonOptions.Default)
            ?? throw new InvalidOperationException("Test workflow could not be cloned.");
        var executor = new Mock<IActionExecutor>(MockBehavior.Strict);

        var result = await new WorkflowService(executor.Object).ExecuteAsync(Request(clone)).ConfigureAwait(false);

        Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.NotStarted));
        Assert.That(result.ValidationIssues, Is.Not.Empty);
        executor.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ListOrderIsAuthoritativeAndEachActionIsAwaited()
    {
        var first = Command("First", 9);
        var second = Command("Second", 1);
        var workflow = new Workflow { Actions = [first, second] };
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executor = new Executor((action, _, _) => action == first ? release.Task : Task.CompletedTask);
        var run = new WorkflowService(executor).ExecuteAsync(Request(workflow));
        Assert.That(executor.Executed, Is.EqualTo(new[] { first.Id }));
        release.SetResult();
        var result = await run.WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(executor.Executed, Is.EqualTo(new[] { first.Id, second.Id }));
            Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.Succeeded));
        }
    }

    [Test]
    public async Task DelayAfterActionUsesInjectedClockBeforeNextAction()
    {
        var clock = new MOBAdisplay.ManualTimeProvider();
        var first = Command("First");
        first.DelayAfterMs = 200;
        var second = Command("Second");
        var executor = new Executor();
        var run = new WorkflowService(executor, clock).ExecuteAsync(Request(new Workflow { Actions = [first, second] }));
        Assert.That(clock.ScheduledTimerCount, Is.EqualTo(1));
        clock.Advance(TimeSpan.FromMilliseconds(199));
        Assert.That(executor.Executed, Is.EqualTo(new[] { first.Id }));
        clock.Advance(TimeSpan.FromMilliseconds(1));
        await run.WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
        Assert.That(executor.Executed, Is.EqualTo(new[] { first.Id, second.Id }));
    }

    [Test]
    public async Task DryRunPlansInOrderWithoutHandlersOrTimers()
    {
        var clock = new MOBAdisplay.ManualTimeProvider();
        var first = Command("First");
        first.DelayAfterMs = int.MaxValue;
        var workflow = new Workflow { Actions = [first, new WorkflowAction { Type = ActionType.Audio, Audio = new() { FilePath = "not-opened.wav" } }] };
        var executor = new Mock<IActionExecutor>(MockBehavior.Strict);
        var result = await new WorkflowService(executor.Object, clock).ExecuteAsync(Request(workflow) with { Mode = WorkflowRunMode.DryRun }).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.Succeeded));
            Assert.That(result.PlannedEffects.Select(effect => effect.ActionType), Is.EqualTo(new[] { ActionType.Command, ActionType.Audio }));
            Assert.That(clock.ScheduledTimerCount, Is.Zero);
        }
        executor.VerifyNoOtherCalls();
    }

    [Test]
    public async Task FirstFailureStopsFollowingActionsAndPublishesOneTerminalEvent()
    {
        var trace = new WorkflowTraceStore();
        var first = Command("First");
        var executor = new Executor((_, _, _) => throw new InvalidOperationException("private detail"));
        var result = await CreateService(executor, trace).ExecuteAsync(Request(new Workflow { Actions = [first, Command("Never")] })).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.Failed));
            Assert.That(trace.GetEntries()[^1].Result, Is.EqualTo(nameof(WorkflowExecutionStatus.Failed)));
            Assert.That(executor.Executed, Is.EqualTo(new[] { first.Id }));
            Assert.That(result.FailureDetail, Does.Not.Contain("private detail"));
            Assert.That(trace.GetEntries().Count(entry => entry.Kind == WorkflowLifecycleKind.WorkflowFailed), Is.EqualTo(1));
            Assert.That(trace.GetEntries().Any(entry => entry.Kind == WorkflowLifecycleKind.WorkflowCompleted), Is.False);
        }
    }

    [Test]
    public async Task CancellationDuringActionStopsSequenceAndPublishesOneTerminalEvent()
    {
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var trace = new WorkflowTraceStore();
        var executor = new Executor((_, _, token) =>
        {
            entered.SetResult();
            return Task.Delay(Timeout.InfiniteTimeSpan, token);
        });
        var run = CreateService(executor, trace).ExecuteAsync(Request(new Workflow { Actions = [Command("Wait"), Command("Never")] }), cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
        await cancellation.CancelAsync().ConfigureAwait(false);
        var result = await run.WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.Cancelled));
            Assert.That(executor.Executed, Has.Count.EqualTo(1));
            Assert.That(trace.GetEntries().Count(entry => entry.Kind == WorkflowLifecycleKind.WorkflowCancelled), Is.EqualTo(1));
        }
    }

    [Test]
    public async Task ConcurrentCallsReceiveIndependentContextsAndTheirOwnSourceEvents()
    {
        var bothEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = 0;
        var contexts = new System.Collections.Concurrent.ConcurrentBag<ActionExecutionContext>();
        var workflow = new Workflow { Actions = [Command("First"), Command("Second")] };
        var firstStation = new Station { Name = "North" };
        var secondStation = new Station { Name = "South" };
        var firstEvent = new FeedbackReceivedEvent(7);
        var secondEvent = new Z21TrackPowerChangedEvent(true);
        var firstContext = new ActionExecutionContext { Z21 = Mock.Of<IZ21>(), SourceEvent = firstEvent, CurrentStation = firstStation };
        var secondContext = new ActionExecutionContext { Z21 = Mock.Of<IZ21>(), SourceEvent = secondEvent, CurrentStation = secondStation };
        var executor = new Executor(async (action, context, token) =>
        {
            if (action == workflow.Actions[0])
            {
                contexts.Add(context);
                if (Interlocked.Increment(ref count) == 2) bothEntered.SetResult();
                await bothEntered.Task.WaitAsync(TimeSpan.FromSeconds(3), token).ConfigureAwait(false);
            }
            Assert.That(context.CurrentStation, Is.SameAs(ReferenceEquals(context.SourceEvent, firstEvent) ? firstStation : secondStation));
        });
        var service = new WorkflowService(executor);
        var results = await Task.WhenAll(service.ExecuteAsync(Request(workflow, firstContext)), service.ExecuteAsync(Request(workflow, secondContext))).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(results.All(result => result.Status == WorkflowExecutionStatus.Succeeded), Is.True);
            Assert.That(contexts.Distinct().Count(), Is.EqualTo(2));
            Assert.That(contexts, Does.Not.Contain(firstContext).And.Not.Contain(secondContext));
            Assert.That(contexts.Select(context => context.SourceEvent), Is.EquivalentTo(new IEvent[] { firstEvent, secondEvent }));
        }
    }

    [Test]
    public async Task StopChangeIsVisibleToLaterActionsInTheSameInvocation()
    {
        var first = new Station { Name = "First" };
        var next = new Station { Name = "Next" };
        var journey = new Journey { Stations = [first, next] };
        var state = new JourneySessionState { JourneyId = journey.Id, CurrentPos = 0, CurrentStationId = first.Id, IsActive = true };
        var context = new ActionExecutionContext { Z21 = Mock.Of<IZ21>(), CurrentJourney = journey, CurrentJourneySessionState = state, CurrentStation = first };
        var change = new WorkflowAction { Type = ActionType.ChangeJourneyStop, ChangeJourneyStop = new() };
        Station? observed = null;
        var executor = new Executor(async (action, invocation, token) =>
        {
            if (action == change)
                await new ChangeJourneyStopWorkflowActionHandler().ExecuteAsync(action, invocation, token).ConfigureAwait(false);
            else observed = invocation.CurrentStation;
        });
        var result = await new WorkflowService(executor).ExecuteAsync(Request(new Workflow { Actions = [change, Command("Observe")] }, context)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.Succeeded));
            Assert.That(observed, Is.SameAs(next));
            Assert.That(context.CurrentStation, Is.SameAs(first));
            Assert.That(state.CurrentStationId, Is.EqualTo(next.Id));
        }
    }

    [Test]
    public async Task UnfinishedOtherWorkflowDoesNotBlockValidInvocation()
    {
        var workflow = new Workflow { Actions = [Command("Run")] };
        var request = Request(workflow);
        request.Project.Workflows.Add(new Workflow());
        var result = await new WorkflowService(new Executor()).ExecuteAsync(request).ConfigureAwait(false);
        Assert.That(result.Status, Is.EqualTo(WorkflowExecutionStatus.Succeeded));
    }

    [Test]
    public async Task MissingWorkflowIsRejectedWithoutCallingHandlers()
    {
        var request = Request(new Workflow { Actions = [Command("Run")] });
        request.Project.Workflows.Clear();
        var executor = new Mock<IActionExecutor>(MockBehavior.Strict);
        var result = await new WorkflowService(executor.Object).ExecuteAsync(request).ConfigureAwait(false);
        Assert.That(result.ValidationIssues.Select(issue => issue.Code), Does.Contain(WorkflowValidationCodes.MissingWorkflow));
        executor.VerifyNoOtherCalls();
    }

    [Test]
    public void ContextFactoryPreservesInvocationDataWithoutSharingTheContainer()
    {
        var journey = new Journey();
        var station = new Station();
        var platform = new Platform();
        var session = new JourneySessionState { JourneyId = journey.Id };
        var sourceEvent = new FeedbackReceivedEvent(12);
        var eventId = Guid.NewGuid();
        var factory = new ActionExecutionContextFactory(new ActionExecutionContext { Z21 = Mock.Of<IZ21>() });
        var context = factory.Create(new ActionExecutionContextState
        {
            SourceEvent = sourceEvent, SourceEventDefinitionId = eventId,
            CurrentJourney = journey, CurrentStation = station, CurrentPlatform = platform,
            CurrentJourneySessionState = session, CurrentStationIndex = 2,
            JourneyTemplateText = "Regional", FeedbackInPort = 12
        });
        var invocation = new ActionExecutionContextFactory(context).CreateForExecution();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(invocation, Is.Not.SameAs(context));
            Assert.That(invocation.Z21, Is.SameAs(context.Z21));
            Assert.That(invocation.SourceEvent, Is.SameAs(sourceEvent));
            Assert.That(invocation.SourceEventDefinitionId, Is.EqualTo(eventId));
            Assert.That(invocation.CurrentJourney, Is.SameAs(journey));
            Assert.That(invocation.CurrentStation, Is.SameAs(station));
            Assert.That(invocation.CurrentPlatform, Is.SameAs(platform));
            Assert.That(invocation.CurrentJourneySessionState, Is.SameAs(session));
            Assert.That(invocation.CurrentStationIndex, Is.EqualTo(2));
            Assert.That(invocation.JourneyTemplateText, Is.EqualTo("Regional"));
            Assert.That(invocation.FeedbackInPort, Is.EqualTo(12));
        }
    }

    private static WorkflowAction Command(string name, uint number = 1) => new()
    {
        Name = name, Number = number, Type = ActionType.Command, Command = new() { BytesBase64 = "AQID" }
    };

    private static WorkflowExecutionRequest Request(Workflow workflow, ActionExecutionContext? context = null) => new()
    {
        Workflow = workflow,
        Project = new Project { Workflows = [workflow] },
        Context = context ?? new ActionExecutionContext { Z21 = Mock.Of<IZ21>() }
    };

    private static WorkflowService CreateService(IActionExecutor executor, IWorkflowTraceStore trace) => new(executor, new WorkflowServiceDependencies
    {
        Validator = new WorkflowValidator(), EffectPlanner = new WorkflowEffectPlanner(), TraceStore = trace, TimeProvider = TimeProvider.System
    });

    private sealed class Executor(Func<WorkflowAction, ActionExecutionContext, CancellationToken, Task>? execute = null) : IActionExecutor
    {
        public System.Collections.Concurrent.ConcurrentQueue<Guid> Executed { get; } = new();
        public Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context) => ExecuteAsync(action, context, CancellationToken.None);
        public Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context, CancellationToken cancellationToken)
        {
            Executed.Enqueue(action.Id);
            return execute?.Invoke(action, context, cancellationToken) ?? Task.CompletedTask;
        }
    }
}
