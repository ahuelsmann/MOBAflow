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
    public async Task SparsePlan_ExecutesOnlyConfiguredWorkflowsAtTwoThreeAndFiveSinceReset()
    {
        using var fixture = new AcceptanceFixture();
        var signal = fixture.AddWorkflow("Set signal");
        var announcement = fixture.AddWorkflow("Station announcement");
        var changeStop = fixture.AddWorkflow("Change virtual stop");
        fixture.AddJourney("Oval journey",
            new JourneyEvent { InPort = 1, Count = 2, WorkflowId = signal.Id },
            new JourneyEvent { InPort = 1, Count = 3, WorkflowId = announcement.Id },
            new JourneyEvent { InPort = 1, Count = 5, WorkflowId = changeStop.Id });
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
            Assert.That(fixture.Counters.GetSnapshot().Single(counter => counter.InPort == 1).Count, Is.EqualTo(6UL));
        });
    }

    [Test]
    public async Task ThreeParallelJourneys_UseIndependentPortsOfTheSharedSessionCounters()
    {
        using var fixture = new AcceptanceFixture();
        var workflow = fixture.AddWorkflow("Parallel track event");
        var first = fixture.AddJourney("Track one", new JourneyEvent { InPort = 1, Count = 2, WorkflowId = workflow.Id });
        var second = fixture.AddJourney("Track two", new JourneyEvent { InPort = 2, Count = 1, WorkflowId = workflow.Id });
        var third = fixture.AddJourney("Track three", new JourneyEvent { InPort = 3, Count = 3, WorkflowId = workflow.Id });

        foreach (var port in new[] { 3, 2, 1, 3, 1, 3 })
        {
            await fixture.RaiseAsync(port);
        }

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Requests.Select(request => request.Context.CurrentJourney!.Id),
                Is.EqualTo(new[] { second.Id, first.Id, third.Id }));
            Assert.That(fixture.Counters.GetSnapshot().Select(snapshot => (snapshot.InPort, snapshot.Count)),
                Is.EquivalentTo(new[] { (1U, 2UL), (2U, 1UL), (3U, 3UL) }));
        });
    }

    [Test]
    public async Task ChangingRelevantInPort_DoesNotGateLaterRowsOnTheSwitchWorkflow()
    {
        using var fixture = new AcceptanceFixture();
        var switchTrack = fixture.AddWorkflow("Switch track");
        var announcement = fixture.AddWorkflow("Second track announcement");
        var changeStop = fixture.AddWorkflow("Second track stop");
        fixture.AddJourney("Connected tracks",
            new JourneyEvent { InPort = 1, Count = 3, WorkflowId = switchTrack.Id },
            new JourneyEvent { InPort = 2, Count = 1, WorkflowId = announcement.Id },
            new JourneyEvent { InPort = 2, Count = 2, WorkflowId = changeStop.Id });

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
        private readonly Mock<IWorkflowService> _workflows = new();
        private ObservableJourneyManager? _manager;
        public InPortCounterService Counters { get; }

        // Created on first use so that it sees every journey added by the test.
        public ObservableJourneyManager Manager => _manager ??= new ObservableJourneyManager(_z21.Object, _project, _workflows.Object, Counters);
        public ConcurrentQueue<WorkflowExecutionRequest> Requests { get; } = new();

        public AcceptanceFixture()
        {
            Counters = new InPortCounterService(_z21.Object, new AppSettings { Counter = { CountOfFeedbackPoints = 3, UseTimerFilter = false } });
            _workflows.Setup(service => service.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()))
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
        }

        public Workflow AddWorkflow(string name)
        {
            var workflow = new Workflow { Name = name };
            _project.Workflows.Add(workflow);
            return workflow;
        }

        public Journey AddJourney(string name, params JourneyEvent[] events)
        {
            var journey = new Journey { Name = name, IsActive = true, EventPlan = new JourneyEventPlan { Events = [.. events] } };
            _project.Journeys.Add(journey);
            return journey;
        }

        public async Task RaiseAsync(int inPort)
        {
            var manager = Manager;
            InPortCounterServiceTests.Raise(_z21, inPort);
            await manager.LastProcessing.WaitAsync(TimeSpan.FromSeconds(5));
        }

        public void Dispose()
        {
            _manager?.Dispose();
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
