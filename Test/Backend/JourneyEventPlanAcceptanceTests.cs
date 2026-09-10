// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend.Interface;
using Moba.Backend.Manager;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Domain;
using Moq;
using System.Collections.Concurrent;

[TestFixture]
public sealed class JourneyEventPlanAcceptanceTests
{
    [Test]
    public async Task SparsePlan_ExecutesOnlyConfiguredWorkflowsAtTwoThreeAndFiveSinceStart()
    {
        using var fixture = new AcceptanceFixture();
        var signal = fixture.AddWorkflow("Set signal");
        var announcement = fixture.AddWorkflow("Station announcement");
        var changeStop = fixture.AddWorkflow("Change virtual stop");
        var journey = fixture.AddJourney("Oval journey",
            new JourneyEvent { InPort = 1, Count = 2, WorkflowId = signal.Id },
            new JourneyEvent { InPort = 1, Count = 3, WorkflowId = announcement.Id },
            new JourneyEvent { InPort = 1, Count = 5, WorkflowId = changeStop.Id });
        for (var count = 0; count < 42; count++)
        {
            await fixture.RaiseAsync(1);
        }

        await fixture.Manager.StartJourneyAsync(journey);
        Assert.That(fixture.Manager.GetState(journey.Id)!.CurrentEventBases[1], Is.EqualTo(42UL));
        var expectedAfterEachActivation = new[]
        {
            Array.Empty<Guid>(),
            new[] { signal.Id },
            new[] { signal.Id, announcement.Id },
            new[] { signal.Id, announcement.Id },
            new[] { signal.Id, announcement.Id, changeStop.Id }
        };

        foreach (var expected in expectedAfterEachActivation)
        {
            await fixture.RaiseAsync(1);
            Assert.That(fixture.Requests.Select(request => request.Workflow.Id), Is.EqualTo(expected));
        }

        await fixture.RaiseAsync(1);
        Assert.Multiple(() =>
        {
            Assert.That(fixture.Requests, Has.Count.EqualTo(3));
            Assert.That(fixture.Counters.GetSnapshot().Single().Count, Is.EqualTo(48UL));
            Assert.That(fixture.Manager.GetState(journey.Id)!.IsActive, Is.True);
        });
    }

    [Test]
    public async Task ThreeParallelJourneys_UseIndependentPortsAndStartValuesWithoutResettingEachOther()
    {
        using var fixture = new AcceptanceFixture();
        var workflow = fixture.AddWorkflow("Parallel track event");
        var first = fixture.AddJourney("Track one", new JourneyEvent { InPort = 1, Count = 2, WorkflowId = workflow.Id });
        var second = fixture.AddJourney("Track two", new JourneyEvent { InPort = 2, Count = 1, WorkflowId = workflow.Id });
        var third = fixture.AddJourney("Track three", new JourneyEvent { InPort = 3, Count = 3, WorkflowId = workflow.Id });
        foreach (var port in new[] { 1, 2, 2, 3, 2, 1, 2 })
        {
            await fixture.RaiseAsync(port);
        }

        await fixture.Manager.StartJourneyAsync(first);
        await fixture.RaiseAsync(1);
        await fixture.RaiseAsync(2);
        await fixture.Manager.StartJourneyAsync(second);
        await fixture.RaiseAsync(3);
        await fixture.Manager.StartJourneyAsync(third);
        Assert.Multiple(() =>
        {
            Assert.That(fixture.Manager.GetState(first.Id)!.CurrentEventBases[1], Is.EqualTo(2UL));
            Assert.That(fixture.Manager.GetState(second.Id)!.CurrentEventBases[2], Is.EqualTo(5UL));
            Assert.That(fixture.Manager.GetState(third.Id)!.CurrentEventBases[3], Is.EqualTo(2UL));
            Assert.That(fixture.Requests, Is.Empty);
        });

        foreach (var port in new[] { 3, 2, 1, 3, 3 })
        {
            await fixture.RaiseAsync(port);
        }

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Requests.Select(request => request.Context.CurrentJourney!.Id),
                Is.EqualTo(new[] { second.Id, first.Id, third.Id }));
            Assert.That(fixture.Counters.GetSnapshot().Select(snapshot => (snapshot.InPort, snapshot.Count)),
                Is.EquivalentTo(new[] { (1U, 4UL), (2U, 6UL), (3U, 5UL) }));
            Assert.That(fixture.Counters.TryResetAll(), Is.False);
        });

        await fixture.Manager.StopJourneyAsync(first);
        await fixture.Manager.StopJourneyAsync(second);
        Assert.That(fixture.Counters.TryResetAll(), Is.False);
        await fixture.Manager.StopJourneyAsync(third);
        Assert.That(fixture.Counters.TryResetAll(), Is.True);
    }

    [Test]
    public async Task ChangingRelevantInPort_DoesNotGateLaterRowsOnTheSwitchWorkflow()
    {
        using var fixture = new AcceptanceFixture();
        var switchTrack = fixture.AddWorkflow("Switch track");
        var announcement = fixture.AddWorkflow("Second track announcement");
        var changeStop = fixture.AddWorkflow("Second track stop");
        var journey = fixture.AddJourney("Connected tracks",
            new JourneyEvent { InPort = 1, Count = 3, WorkflowId = switchTrack.Id },
            new JourneyEvent { InPort = 2, Count = 1, WorkflowId = announcement.Id },
            new JourneyEvent { InPort = 2, Count = 2, WorkflowId = changeStop.Id });
        await fixture.Manager.StartJourneyAsync(journey);

        await fixture.RaiseAsync(2);
        Assert.That(fixture.Requests.Select(request => request.Workflow.Id), Is.EqualTo(new[] { announcement.Id }));
        await fixture.RaiseAsync(1);
        await fixture.RaiseAsync(1);
        Assert.That(fixture.Requests, Has.Count.EqualTo(1));
        await fixture.RaiseAsync(1);
        await fixture.RaiseAsync(2);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Requests.Select(request => request.Workflow.Id),
                Is.EqualTo(new[] { announcement.Id, switchTrack.Id, changeStop.Id }));
            Assert.That(fixture.Requests.Select(request => request.Context.FeedbackInPort),
                Is.EqualTo(new uint?[] { 2, 1, 2 }));
        });
    }

    private sealed class AcceptanceFixture : IDisposable
    {
        private readonly Mock<IZ21> _z21 = new();
        private readonly Project _project = new();
        public InPortCounterService Counters { get; }
        public ObservableJourneyManager Manager { get; }
        public ConcurrentQueue<WorkflowExecutionRequest> Requests { get; } = new();

        public AcceptanceFixture()
        {
            Counters = new InPortCounterService(_z21.Object, new AppSettings { Counter = { UseTimerFilter = false } });
            var workflows = new Mock<IWorkflowService>();
            workflows.Setup(service => service.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()))
                .Returns((WorkflowExecutionRequest request, CancellationToken _) =>
                {
                    Requests.Enqueue(request);
                    return Task.FromResult(new WorkflowExecutionResult
                    {
                        ExecutionId = Guid.NewGuid(),
                        WorkflowId = request.Workflow.Id,
                        SourceCorrelationId = request.SourceCorrelationId,
                        Status = WorkflowExecutionStatus.Succeeded
                    });
                });
            Manager = new ObservableJourneyManager(_z21.Object, _project, workflows.Object, Counters);
        }

        public Workflow AddWorkflow(string name)
        {
            var workflow = new Workflow { Name = name };
            _project.Workflows.Add(workflow);
            return workflow;
        }

        public Journey AddJourney(string name, params JourneyEvent[] events)
        {
            var journey = new Journey { Name = name, EventPlan = new JourneyEventPlan { Events = [.. events] } };
            _project.Journeys.Add(journey);
            return journey;
        }

        public async Task RaiseAsync(int inPort)
        {
            InPortCounterServiceTests.Raise(_z21, inPort);
            await Manager.LastProcessing.WaitAsync(TimeSpan.FromSeconds(5));
        }

        public void Dispose()
        {
            Manager.Dispose();
            Counters.Dispose();
        }
    }

    private sealed class ObservableJourneyManager(IZ21 z21, Project project, IWorkflowService workflows, InPortCounterService counters)
        : JourneyManager(z21, project, workflows, dependencies: new JourneyManagerDependencies { InPortCounterService = counters })
    {
        public Task LastProcessing { get; private set; } = Task.CompletedTask;

        protected override Task ProcessCountedFeedbackAsync(InPortCountedEventArgs args)
        {
            LastProcessing = base.ProcessCountedFeedbackAsync(args);
            return LastProcessing;
        }
    }
}
