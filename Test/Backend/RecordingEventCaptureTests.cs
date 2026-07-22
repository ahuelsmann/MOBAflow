// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using global::Moba.Backend;
using global::Moba.Backend.Interface;
using global::Moba.Backend.Service;
using global::Moba.Backend.Service.Recording;
using global::Moba.Common.Configuration;
using global::Moba.Common.Events;
using global::Moba.Common.Recording;
using global::Moba.Common.Runtime;
using global::Moba.SharedUI.Extensions;
using global::Moba.SharedUI.Interface;
using global::Moba.SharedUI.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

internal sealed class RecordingEventCaptureTests
{
    private static readonly DateTimeOffset StartTime =
        new(2026, 7, 21, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task Publish_Should_CaptureBeforeUiDispatchAndForwardOriginalEvent()
    {
        await using var session = CreateSession();
        session.Start(new RecordingSessionStartRequest("Capture", "1.0"));
        var dispatcher = new QueuedUiDispatcher();
        var baseBus = new EventBus(NullLogger<EventBus>.Instance);
        var uiBus = new UiThreadEventBusDecorator(baseBus, dispatcher);
        var recorder = CreateDecorator(uiBus, session);
        FeedbackReceivedEvent? deliveredEvent = null;
        recorder.Subscribe<FeedbackReceivedEvent>(@event => deliveredEvent = @event);
        var publishedEvent = new FeedbackReceivedEvent(17);

        recorder.Publish(publishedEvent);

        Assert.Multiple(() =>
        {
            Assert.That(session.CurrentStatus.EntryCount, Is.EqualTo(2));
            Assert.That(deliveredEvent, Is.Null);
            Assert.That(dispatcher.PendingCount, Is.EqualTo(1));
        });

        dispatcher.Drain();
        var artifact = (await session.StopAsync()).Artifact!;
        var captured = artifact.Entries.Single(entry => entry.TypeKey == "z21.feedback.activated");
        Assert.Multiple(() =>
        {
            Assert.That(deliveredEvent, Is.SameAs(publishedEvent));
            Assert.That(captured.Payload.GetProperty("inPort").GetInt32(), Is.EqualTo(17));
        });
    }

    [Test]
    public async Task Publish_Should_PreserveDuplicateOrderFromOrderedProducer()
    {
        await using var session = CreateSession();
        session.Start(new RecordingSessionStartRequest("Order", "1.0"));
        var recorder = CreateDecorator(new EventBus(NullLogger<EventBus>.Instance), session);
        await using var pipeline = new Z21EventPipeline(recorder, capacity: 16);

        pipeline.TryEnqueue(new FeedbackReceivedEvent(3));
        pipeline.TryEnqueue(new FeedbackReceivedEvent(3));
        pipeline.TryEnqueue(new FeedbackReceivedEvent(9));
        pipeline.TryEnqueue(new Z21ConnectionLostEvent());
        pipeline.TryEnqueue(new Z21ConnectionEstablishedEvent());
        await pipeline.StopAsync(TimeSpan.FromSeconds(1));
        var artifact = (await session.StopAsync()).Artifact!;

        var captured = artifact.Entries.Where(entry => entry.TypeKey == "z21.feedback.activated").ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(captured.Select(entry => entry.Payload.GetProperty("inPort").GetInt32()), Is.EqualTo(new[] { 3, 3, 9 }));
            Assert.That(captured.Select(entry => entry.Sequence), Is.Ordered.And.Unique);
            Assert.That(
                artifact.Entries.Where(entry => entry.Category == "z21").Select(entry => entry.TypeKey),
                Is.EqualTo(new[]
                {
                    "z21.feedback.activated",
                    "z21.feedback.activated",
                    "z21.feedback.activated",
                    "z21.connection.lost",
                    "z21.connection.established"
                }));
        });
    }

    [Test]
    public async Task UnknownEvent_Should_ForwardWithoutReflectiveCapture()
    {
        await using var session = CreateSession();
        session.Start(new RecordingSessionStartRequest("Unknown event", "1.0"));
        var inner = new EventBus(NullLogger<EventBus>.Instance);
        var recorder = CreateDecorator(inner, session);
        var delivered = false;
        recorder.Subscribe<TestEvent>(_ => delivered = true);

        recorder.Publish(new TestEvent());
        var artifact = (await session.StopAsync()).Artifact!;

        Assert.Multiple(() =>
        {
            Assert.That(delivered, Is.True);
            Assert.That(artifact.Entries.Select(entry => entry.TypeKey),
                Is.EqualTo(new[] { "recorder.started", "recorder.completed" }));
        });
    }

    [Test]
    public async Task ConcurrentCaptureOverload_Should_EmitGapWithoutBlockingPublishers()
    {
        const int publishCount = 20_000;
        var options = new RecorderOptions(pendingCapacity: 1, entryLimit: 100_000);
        await using var session = new RecordingSessionService(new FixedTimeProvider(StartTime), options);
        session.Start(new RecordingSessionStartRequest("Capture overload", "1.0"));
        var recorder = CreateDecorator(new EventBus(NullLogger<EventBus>.Instance), session);

        Parallel.For(0, publishCount, index => recorder.Publish(new FeedbackReceivedEvent(index + 1)));
        var artifact = (await session.StopAsync()).Artifact!;

        Assert.Multiple(() =>
        {
            Assert.That(session.CurrentStatus.DroppedEntryCount, Is.GreaterThan(0));
            Assert.That(artifact.Entries, Has.Some.Matches<RecordingEntry>(entry => entry.TypeKey == "recorder.gap"));
            Assert.That(artifact.Entries.Select(entry => entry.Sequence), Is.Ordered.And.Unique);
        });
    }

    [Test]
    public async Task MapperFailure_Should_NotPreventOriginalEventDelivery()
    {
        await using var session = CreateSession();
        session.Start(new RecordingSessionStartRequest("Failure isolation", "1.0"));
        var inner = new EventBus(NullLogger<EventBus>.Instance);
        var registry = new RecordingEventMapperRegistry([new ThrowingMapper()]);
        var recorder = new RecordingEventBusDecorator(
            inner,
            session,
            registry,
            NullLogger<RecordingEventBusDecorator>.Instance);
        var delivered = false;
        recorder.Subscribe<TestEvent>(_ => delivered = true);

        recorder.Publish(new TestEvent());
        var artifact = (await session.StopAsync()).Artifact!;

        Assert.Multiple(() =>
        {
            Assert.That(delivered, Is.True);
            Assert.That(artifact.Entries, Has.None.Matches<RecordingEntry>(entry => entry.TypeKey == "test.throw"));
        });
    }

    [Test]
    public async Task SubscriberFailure_Should_NotPreventCaptureOrLaterSubscriberDelivery()
    {
        await using var session = CreateSession();
        session.Start(new RecordingSessionStartRequest("Subscriber isolation", "1.0"));
        var inner = new EventBus(NullLogger<EventBus>.Instance);
        var recorder = CreateDecorator(inner, session);
        var delivered = false;
        recorder.Subscribe<FeedbackReceivedEvent>(_ => throw new InvalidOperationException("Expected handler failure."));
        recorder.Subscribe<FeedbackReceivedEvent>(_ => delivered = true);

        recorder.Publish(new FeedbackReceivedEvent(4));
        var artifact = (await session.StopAsync()).Artifact!;

        Assert.Multiple(() =>
        {
            Assert.That(delivered, Is.True);
            Assert.That(recorder.HandlerFailureCount, Is.EqualTo(1));
            Assert.That(artifact.Entries.Count(entry => entry.TypeKey == "z21.feedback.activated"), Is.EqualTo(1));
        });
    }

    [Test]
    public void RuntimeMapper_Should_ExcludeBroadSnapshotTextAndCollections()
    {
        var mapper = new RuntimeSnapshotRecordingEventMapper();
        var snapshot = new MobaRuntimeSnapshot
        {
            IsConnected = true,
            IsTrackPowerOn = true,
            StatusText = "sensitive endpoint text",
            SerialNumber = "secret-looking-serial",
            JourneyStates = new Dictionary<Guid, JourneyRuntimeSnapshot>
            {
                [Guid.NewGuid()] = new JourneyRuntimeSnapshot { JourneyId = Guid.NewGuid() }
            }
        };

        var projection = mapper.Map(new RuntimeSnapshotChangedEvent(snapshot));
        var payload = projection.Payload.GetRawText();

        Assert.Multiple(() =>
        {
            Assert.That(payload, Does.Contain("isConnected"));
            Assert.That(payload, Does.Not.Contain(snapshot.StatusText));
            Assert.That(payload, Does.Not.Contain(snapshot.SerialNumber));
            Assert.That(payload, Does.Not.Contain("JourneyStates"));
        });
    }

    [Test]
    public void WorkflowMapper_Should_CaptureCorrelatedLifecycleWithoutFreeFormDetail()
    {
        var mapper = new WorkflowLifecycleRecordingEventMapper();
        var sourceCorrelationId = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var lifecycleEvent = new WorkflowLifecycleEvent
        {
            Kind = WorkflowLifecycleKind.StepFailed,
            SourceCorrelationId = sourceCorrelationId,
            ExecutionId = executionId,
            WorkflowId = workflowId,
            StepId = stepId,
            Sequence = 7,
            Attempt = 2,
            Mode = WorkflowLifecycleMode.Live,
            TimestampUtc = StartTime,
            Elapsed = TimeSpan.FromMilliseconds(125),
            Result = "Failed",
            Detail = "token=must-not-be-recorded"
        };

        var projection = mapper.Map(lifecycleEvent);
        var payload = projection.Payload.GetRawText();

        Assert.Multiple(() =>
        {
            Assert.That(projection.Category, Is.EqualTo("workflow"));
            Assert.That(projection.TypeKey, Is.EqualTo("workflow.lifecycle"));
            Assert.That(projection.Severity, Is.EqualTo("error"));
            Assert.That(projection.CorrelationId, Is.EqualTo(sourceCorrelationId));
            Assert.That(projection.ReplayApplicability, Is.EqualTo(RecordingReplayApplicability.ReplayApplicable));
            Assert.That(projection.EntityReferences, Has.Some.EqualTo(new RecordingEntityReference("execution", executionId)));
            Assert.That(projection.EntityReferences, Has.Some.EqualTo(new RecordingEntityReference("workflow", workflowId)));
            Assert.That(projection.EntityReferences, Has.Some.EqualTo(new RecordingEntityReference("step", stepId)));
            Assert.That(projection.Payload.GetProperty("sourceSequence").GetInt64(), Is.EqualTo(7));
            Assert.That(projection.Payload.GetProperty("elapsedTicks").GetInt64(), Is.EqualTo(TimeSpan.FromMilliseconds(125).Ticks));
            Assert.That(payload, Does.Not.Contain(lifecycleEvent.Detail));
        });
    }

    [Test]
    public void WorkflowMapper_Should_DiscardUnrecognizedResultText()
    {
        var mapper = new WorkflowLifecycleRecordingEventMapper();
        var lifecycleEvent = new WorkflowLifecycleEvent
        {
            Kind = WorkflowLifecycleKind.ConditionDecided,
            SourceCorrelationId = Guid.NewGuid(),
            ExecutionId = Guid.NewGuid(),
            WorkflowId = Guid.NewGuid(),
            Sequence = 1,
            Mode = WorkflowLifecycleMode.DryRun,
            TimestampUtc = StartTime,
            Result = "endpoint=https://private.invalid"
        };

        var projection = mapper.Map(lifecycleEvent);

        Assert.That(projection.Payload.GetProperty("result").ValueKind, Is.EqualTo(System.Text.Json.JsonValueKind.Null));
    }

    [Test]
    public async Task UiRegistration_Should_PlaceRecordingDecoratorOutsideUiDecorator()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton(new AppSettings());
        services.AddSingleton<IUiDispatcher, QueuedUiDispatcher>();
        services.AddEventBusWithUiDispatch();
        services.AddMobaBackendServices();
        await using var provider = services.BuildServiceProvider();

        var eventBus = provider.GetRequiredService<IEventBus>();
        var session = provider.GetRequiredService<IRecordingSessionService>();
        session.Start(new RecordingSessionStartRequest("DI capture", "1.0"));
        eventBus.Publish(new FeedbackReceivedEvent(11));
        eventBus.Publish(new WorkflowLifecycleEvent
        {
            Kind = WorkflowLifecycleKind.WorkflowCompleted,
            SourceCorrelationId = Guid.NewGuid(),
            ExecutionId = Guid.NewGuid(),
            WorkflowId = Guid.NewGuid(),
            Sequence = 1,
            Mode = WorkflowLifecycleMode.Live,
            TimestampUtc = StartTime,
            Result = "Succeeded"
        });
        var artifact = (await session.StopAsync()).Artifact!;
        var serializer = provider.GetRequiredService<RecordingArtifactSerializer>();
        var imported = serializer.Import(serializer.SerializeToUtf8(artifact));

        Assert.Multiple(() =>
        {
            Assert.That(eventBus, Is.InstanceOf<RecordingEventBusDecorator>());
            Assert.That(provider.GetServices<IRecordingEventMapper>().Count(), Is.EqualTo(4));
            Assert.That(imported.IsValid, Is.True);
            Assert.That(
                imported.Artifact!.Entries.Single(entry => entry.TypeKey == "z21.feedback.activated").ReplayApplicability,
                Is.EqualTo(RecordingReplayApplicability.ReplayApplicable));
            Assert.That(
                imported.Artifact.Entries.Single(entry => entry.TypeKey == "workflow.lifecycle").ReplayApplicability,
                Is.EqualTo(RecordingReplayApplicability.ReplayApplicable));
        });
    }

    private static RecordingSessionService CreateSession() =>
        new(new FixedTimeProvider(StartTime));

    private static RecordingEventBusDecorator CreateDecorator(
        IEventBus inner,
        IRecordingSessionService session) =>
        new(
            inner,
            session,
            new RecordingEventMapperRegistry(
            [
                new Z21RecordingEventMapper(),
                new RuntimeSnapshotRecordingEventMapper(),
                new JourneyRecordingEventMapper(),
                new WorkflowLifecycleRecordingEventMapper()
            ]),
            NullLogger<RecordingEventBusDecorator>.Instance);

    private sealed record TestEvent : EventBase;

    private sealed class ThrowingMapper : IRecordingEventMapper
    {
        public IReadOnlyCollection<Type> EventTypes { get; } = [typeof(TestEvent)];

        public RecordingEntryProjection Map(IEvent sourceEvent) =>
            throw new InvalidOperationException("Expected mapper failure.");
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class QueuedUiDispatcher : IUiDispatcher
    {
        private readonly Queue<Action> _pending = [];

        public int PendingCount => _pending.Count;

        public void InvokeOnUi(Action action) => _pending.Enqueue(action);

        public Task InvokeOnUiAsync(Func<Task> asyncAction) => asyncAction();

        public Task<T> InvokeOnUiAsync<T>(Func<Task<T>> asyncFunc) => asyncFunc();

        public void InvokeOnUiHighPriority(Action action) => _pending.Enqueue(action);

        public void InvokeOnUiLowPriority(Action action) => _pending.Enqueue(action);

        public Task InvokeOnUiAsync(Func<Task> asyncAction, UiPriority priority) => asyncAction();

        public void Drain()
        {
            while (_pending.TryDequeue(out var action)) action();
        }
    }
}