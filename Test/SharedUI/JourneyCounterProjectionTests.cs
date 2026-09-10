// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moq;

/// <summary>Checks the desktop/shared projection against authoritative runtime snapshots and command routing.</summary>
[TestFixture]
public sealed class JourneyCounterProjectionTests
{
    [Test]
    public async Task CounterSnapshot_PreservesUnsignedPrecisionAndTimingInDisplayedStatistics()
    {
        var feedbackTime = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);
        await using var fixture = new ProjectionFixture(initial: new MobaRuntimeSnapshot
        {
            InPortCounters = [new InPortCounterSnapshot(7, ulong.MaxValue, feedbackTime, TimeSpan.FromSeconds(20))]
        });

        var statistic = fixture.ViewModel.Statistics.Single(item => item.InPort == 7);

        Assert.Multiple(() =>
        {
            Assert.That(statistic.Count, Is.EqualTo(ulong.MaxValue));
            Assert.That(statistic.LapCountFormatted, Does.StartWith("18446744073709551615/"));
            Assert.That(statistic.LastFeedbackTime, Is.EqualTo(feedbackTime.UtcDateTime));
            Assert.That(statistic.LastLapTime, Is.EqualTo(TimeSpan.FromSeconds(20)));
            Assert.That(statistic.HasReceivedFirstLap, Is.True);
        });
    }

    [Test]
    public async Task StatisticsReinitializationAndProjectSelection_KeepApplicationCounterValues()
    {
        await using var fixture = new ProjectionFixture(initial: new MobaRuntimeSnapshot
        {
            InPortCounters = [new InPortCounterSnapshot(1, 42, null, null), new InPortCounterSnapshot(7, 19, null, null)]
        });
        fixture.ViewModel.InitializeStatisticsFromFeedbackPoints();
        fixture.ViewModel.SelectedProject = new ProjectViewModel(new Project { Name = "Another layout" });
        fixture.Settings.Counter.CountOfFeedbackPoints = 1;
        fixture.ViewModel.InitializeStatisticsFromFeedbackPoints();

        Assert.Multiple(() =>
        {
            Assert.That(fixture.ViewModel.Statistics.Single(item => item.InPort == 1).Count, Is.EqualTo(42));
            Assert.That(fixture.ViewModel.Statistics.Single(item => item.InPort == 7).Count, Is.EqualTo(19));
        });
        fixture.Gateway.Verify(gateway => gateway.ResetInPortCountersAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ResetPermission_UpdatesCommandAndWaitsForAuthoritativeResetSnapshot()
    {
        await using var fixture = new ProjectionFixture();
        var changes = 0;
        fixture.ViewModel.ResetCountersCommand.CanExecuteChanged += (_, _) => changes++;
        fixture.Publish(new MobaRuntimeSnapshot
        {
            CanResetInPortCounters = false,
            InPortCounters = [new InPortCounterSnapshot(1, 18, null, null)]
        });
        Assert.That(fixture.ViewModel.ResetCountersCommand.CanExecute(null), Is.False);

        fixture.Publish(new MobaRuntimeSnapshot
        {
            CanResetInPortCounters = true,
            InPortCounters = [new InPortCounterSnapshot(1, 18, null, null)]
        });
        Assert.That(fixture.ViewModel.ResetCountersCommand.CanExecute(null), Is.True);
        await fixture.ViewModel.ResetCountersCommand.ExecuteAsync(null);
        Assert.That(fixture.ViewModel.Statistics.Single(item => item.InPort == 1).Count, Is.EqualTo(18));
        fixture.Publish(new MobaRuntimeSnapshot { CanResetInPortCounters = true });

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.GreaterThanOrEqualTo(2));
            Assert.That(fixture.ViewModel.Statistics.Single(item => item.InPort == 1).Count, Is.Zero);
            Assert.That(fixture.ViewModel.JourneyCommandStatus, Is.EqualTo("InPort counters reset."));
        });
        fixture.Gateway.Verify(gateway => gateway.ResetInPortCountersAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task StartAndStop_RouteSelectedJourneyAndLockTheEventEditorWhileRunning()
    {
        var journey = CreateJourney("Regional");
        await using var fixture = new ProjectionFixture(new Solution { Projects = [new Project { Journeys = [journey] }] });
        using var editor = new EventManagerViewModel(fixture.ViewModel);
        var calls = new List<string>();
        fixture.Runtime.Setup(runtime => runtime.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("activate"))
            .Returns(Task.CompletedTask);
        fixture.Gateway.Setup(gateway => gateway.StartJourneyAsync(journey.Id, It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                calls.Add("start");
                fixture.Publish(JourneySnapshot(journey.Id, true));
            })
            .Returns(Task.CompletedTask);
        fixture.Gateway.Setup(gateway => gateway.StopJourneyAsync(journey.Id, It.IsAny<CancellationToken>()))
            .Callback(() => fixture.Publish(JourneySnapshot(journey.Id, false)))
            .Returns(Task.CompletedTask);

        Assert.That(fixture.ViewModel.StartJourneyCommand.CanExecute(null), Is.True);
        await fixture.ViewModel.StartJourneyCommand.ExecuteAsync(null);
        editor.Events.Single().Count = 9;
        Assert.Multiple(() =>
        {
            Assert.That(calls, Is.EqualTo(new[] { "activate", "start" }));
            Assert.That(fixture.ViewModel.SelectedJourney!.IsRunning, Is.True);
            Assert.That(fixture.ViewModel.StartJourneyCommand.CanExecute(null), Is.False);
            Assert.That(fixture.ViewModel.StopJourneyCommand.CanExecute(null), Is.True);
            Assert.That(editor.CanEdit, Is.False);
            Assert.That(journey.EventPlan!.Events.Single().Count, Is.EqualTo(2));
        });

        await fixture.ViewModel.StopJourneyCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.ViewModel.SelectedJourney!.IsRunning, Is.False);
            Assert.That(fixture.ViewModel.StartJourneyCommand.CanExecute(null), Is.True);
            Assert.That(fixture.ViewModel.StopJourneyCommand.CanExecute(null), Is.False);
            Assert.That(editor.CanEdit, Is.True);
        });
        fixture.Gateway.Verify(gateway => gateway.StartJourneyAsync(journey.Id, It.IsAny<CancellationToken>()), Times.Once);
        fixture.Gateway.Verify(gateway => gateway.StopJourneyAsync(journey.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OtherRunningJourney_KeepsSelectedStoppedPlanReadOnly()
    {
        var running = CreateJourney("Running");
        var selected = CreateJourney("Selected");
        await using var fixture = new ProjectionFixture(new Solution
        {
            Projects = [new Project { Journeys = [running, selected] }]
        });
        using var editor = new EventManagerViewModel(fixture.ViewModel);
        fixture.Publish(JourneySnapshot(running.Id, true));
        fixture.ViewModel.SelectedJourney = fixture.ViewModel.SelectedProject!.Journeys.Single(item => item.Id == selected.Id);

        editor.AddEventCommand.Execute(null);
        editor.Events.Single().Count = 8;

        Assert.Multiple(() =>
        {
            Assert.That(fixture.ViewModel.SelectedJourney.IsRunning, Is.False);
            Assert.That(fixture.ViewModel.IsAnyEventPlanRunning, Is.True);
            Assert.That(editor.CanEdit, Is.False);
            Assert.That(selected.EventPlan!.Events, Has.Count.EqualTo(1));
            Assert.That(selected.EventPlan.Events.Single().Count, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task ExistingRuntimeSnapshot_ProjectsRunningStateWhenSolutionWrappersAreCreated()
    {
        var journey = CreateJourney("Already running");
        await using var fixture = new ProjectionFixture(
            new Solution { Projects = [new Project { Journeys = [journey] }] }, JourneySnapshot(journey.Id, true));

        Assert.Multiple(() =>
        {
            Assert.That(fixture.ViewModel.SelectedJourney!.IsRunning, Is.True);
            Assert.That(fixture.ViewModel.SelectedJourney.IsEventPlanRunning, Is.True);
            Assert.That(fixture.ViewModel.StartJourneyCommand.CanExecute(null), Is.False);
            Assert.That(fixture.ViewModel.StopJourneyCommand.CanExecute(null), Is.True);
            Assert.That(fixture.ViewModel.ResetCountersCommand.CanExecute(null), Is.False);
        });
    }

    [Test]
    public async Task FailedStart_ReportsFailureWithoutInventingRunningState()
    {
        var journey = CreateJourney("Unavailable");
        await using var fixture = new ProjectionFixture(new Solution { Projects = [new Project { Journeys = [journey] }] });
        fixture.Gateway.Setup(gateway => gateway.StartJourneyAsync(journey.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Runtime is unavailable."));

        await fixture.ViewModel.StartJourneyCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.ViewModel.JourneyCommandStatus, Is.EqualTo("Runtime is unavailable."));
            Assert.That(fixture.ViewModel.SelectedJourney!.IsRunning, Is.False);
            Assert.That(fixture.ViewModel.StartJourneyCommand.CanExecute(null), Is.True);
        });
    }

    private static Journey CreateJourney(string name) => new()
    {
        Name = name,
        EventPlan = new JourneyEventPlan { Events = [new() { InPort = 1, Count = 2 }] }
    };

    private static MobaRuntimeSnapshot JourneySnapshot(Guid id, bool active) => new()
    {
        CanResetInPortCounters = !active,
        JourneyStates = new Dictionary<Guid, JourneyRuntimeSnapshot>
        {
            [id] = new() { JourneyId = id, IsActive = active, IsEventPlan = true }
        }
    };

    private sealed class ProjectionFixture : IAsyncDisposable
    {
        private readonly EventBus _eventBus = new(NullLogger<EventBus>.Instance);
        private MobaRuntimeSnapshot _snapshot;

        public ProjectionFixture(Solution? solution = null, MobaRuntimeSnapshot? initial = null)
        {
            _snapshot = initial ?? MobaRuntimeSnapshot.Empty;
            Runtime.SetupGet(runtime => runtime.Current).Returns(() => _snapshot);
            Runtime.Setup(runtime => runtime.GetTrafficPackets()).Returns(Array.Empty<Z21TrafficPacket>());
            Runtime.Setup(runtime => runtime.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Runtime.Setup(runtime => runtime.CheckpointUsageAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            Runtime.Setup(runtime => runtime.DisconnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            Runtime.Setup(runtime => runtime.SetActiveTrainAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Gateway.Setup(gateway => gateway.ResetInPortCountersAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var dispatcher = new Mock<IUiDispatcher>();
            dispatcher.Setup(candidate => candidate.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
            var io = new Mock<IIoService>();
            io.Setup(service => service.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()))
                .ReturnsAsync((true, "projection-test.json", null));
            io.Setup(service => service.SaveAsAsync(It.IsAny<Solution>())).ReturnsAsync((true, "projection-test.json", null));
            Settings.Counter.CountOfFeedbackPoints = 2;
            ViewModel = new MainWindowViewModel(new LayoutColumnWidthsViewModel(), Runtime.Object, _eventBus,
                dispatcher.Object, Settings, solution ?? new Solution { Projects = [new Project()] },
                new ActionExecutionContext { Z21 = Mock.Of<IZ21>() }, NullLogger<MainWindowViewModel>.Instance,
                io.Object, runtimeCommandGateway: Gateway.Object);
        }

        public Mock<IMobaRuntime> Runtime { get; } = new();
        public Mock<IRuntimeCommandGateway> Gateway { get; } = new();
        public MainWindowViewModel ViewModel { get; }
        public AppSettings Settings { get; } = new();

        public void Publish(MobaRuntimeSnapshot snapshot)
        {
            _snapshot = snapshot;
            _eventBus.Publish(new RuntimeSnapshotChangedEvent(snapshot));
        }

        public async ValueTask DisposeAsync()
        {
            await ViewModel.PrepareForShutdownAsync();
        }
    }
}
