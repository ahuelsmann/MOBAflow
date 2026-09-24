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
            Assert.That(fixture.ViewModel.Statistics.Any(item => item.InPort == 7), Is.False);
        });
        var collectionChanges = 0;
        fixture.ViewModel.Statistics.CollectionChanged += (_, _) => collectionChanges++;
        fixture.Publish(new MobaRuntimeSnapshot
        {
            InPortCounters = [new InPortCounterSnapshot(1, 43, null, null), new InPortCounterSnapshot(7, 19, null, null)]
        });
        Assert.That(collectionChanges, Is.Zero);
        fixture.Settings.Counter.CountOfFeedbackPoints = 7;
        fixture.ViewModel.InitializeStatisticsFromFeedbackPoints();
        Assert.That(fixture.ViewModel.Statistics.Single(item => item.InPort == 7).Count, Is.EqualTo(19));
        fixture.Gateway.Verify(gateway => gateway.ResetInPortCountersAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ResetCounters_IsAlwaysAvailableAndWaitsForAuthoritativeResetSnapshot()
    {
        var journey = CreateJourney("Active");
        journey.IsActive = true;
        await using var fixture = new ProjectionFixture(new Solution { Projects = [new Project { Journeys = [journey] }] });
        fixture.Publish(new MobaRuntimeSnapshot { InPortCounters = [new InPortCounterSnapshot(1, 18, null, null)] });

        Assert.That(fixture.ViewModel.ResetCountersCommand.CanExecute(null), Is.True);
        await fixture.ViewModel.ResetCountersCommand.ExecuteAsync(null);
        Assert.That(fixture.ViewModel.Statistics.Single(item => item.InPort == 1).Count, Is.EqualTo(18));
        fixture.Publish(MobaRuntimeSnapshot.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.ViewModel.Statistics.Single(item => item.InPort == 1).Count, Is.Zero);
            Assert.That(fixture.ViewModel.JourneyCommandStatus, Is.EqualTo("InPort counters reset."));
        });
        fixture.Gateway.Verify(gateway => gateway.ResetInPortCountersAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ActiveFlag_IsPersistedOnTheJourneyAndReappliedToTheRuntime()
    {
        var journey = CreateJourney("Regional");
        await using var fixture = new ProjectionFixture(new Solution { Projects = [new Project { Journeys = [journey] }] });
        fixture.Runtime.Invocations.Clear();

        fixture.ViewModel.SelectedJourney!.IsActive = true;

        Assert.That(journey.IsActive, Is.True);
        fixture.Runtime.Verify(runtime => runtime.UpdateJourneyEventsAsync(It.IsAny<Project>(), journey.Id,
            It.IsAny<CancellationToken>()), Times.Once);
        fixture.Runtime.Verify(runtime => runtime.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task EventEdits_StayEditableForActiveJourneysAndAreReappliedToTheRuntime()
    {
        var journey = CreateJourney("Regional");
        journey.IsActive = true;
        await using var fixture = new ProjectionFixture(new Solution { Projects = [new Project { Journeys = [journey] }] });
        using var editor = new EventManagerViewModel(fixture.ViewModel);
        fixture.Runtime.Invocations.Clear();

        editor.Events.Single().Count = 9;

        Assert.Multiple(() =>
        {
            Assert.That(editor.CanEdit, Is.True);
            Assert.That(journey.EventPlan.Events.Single().Count, Is.EqualTo(9));
        });
        fixture.Runtime.Verify(runtime => runtime.UpdateJourneyEventsAsync(It.IsAny<Project>(), journey.Id,
            It.IsAny<CancellationToken>()), Times.Once);
        fixture.Runtime.Verify(runtime => runtime.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ReselectingJourneyDoesNotDuplicateConfigurationUpdates()
    {
        var first = CreateJourney("First");
        var second = CreateJourney("Second");
        var fixture = new ProjectionFixture(new Solution { Projects = [new Project { Journeys = [first, second] }] });
        await using var fixtureLifetime = fixture.ConfigureAwait(false);
        var journeys = fixture.ViewModel.SelectedProject!.Journeys;
        fixture.ViewModel.SelectedJourney = journeys[1];
        fixture.ViewModel.SelectedJourney = journeys[0];
        fixture.ViewModel.SelectedJourney = journeys[1];
        fixture.ViewModel.SelectedJourney = journeys[0];
        fixture.Runtime.Invocations.Clear();

        fixture.ViewModel.SelectedJourney.IsActive = true;

        fixture.Runtime.Verify(runtime => runtime.UpdateJourneyEventsAsync(It.IsAny<Project>(), first.Id,
            It.IsAny<CancellationToken>()), Times.Once);
        journeys[1].IsActive = true;
        fixture.Runtime.Verify(runtime => runtime.UpdateJourneyEventsAsync(It.IsAny<Project>(), second.Id,
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task MainWindowSelectionCommitsThePreviousDraftToItsRuntime()
    {
        var first = CreateJourney("First");
        var second = CreateJourney("Second");
        var fixture = new ProjectionFixture(new Solution { Projects = [new Project { Journeys = [first, second] }] });
        await using var fixtureLifetime = fixture.ConfigureAwait(false);
        using var editor = new EventManagerViewModel(fixture.ViewModel);
        fixture.ViewModel.CurrentSolutionPath = "projection-test.json";
        editor.Events.Single().CountText = "25";
        fixture.Runtime.Invocations.Clear();
        fixture.Io.Invocations.Clear();

        fixture.ViewModel.SelectedJourney = fixture.ViewModel.SelectedProject!.Journeys[1];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.EventPlan.Events.Single().Count, Is.EqualTo(25));
            fixture.Runtime.Verify(runtime => runtime.UpdateJourneyEventsAsync(It.IsAny<Project>(), first.Id,
                It.IsAny<CancellationToken>()), Times.Once);
            fixture.Io.Verify(io => io.SaveAsync(It.IsAny<Solution>(), "projection-test.json"), Times.Once);
        }
    }

    private static Journey CreateJourney(string name) => new()
    {
        Name = name,
        EventPlan = new JourneyEventPlan { Events = [new() { InPort = 1, Count = 2 }] }
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
            Runtime.Setup(runtime => runtime.DisconnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            Gateway.Setup(gateway => gateway.ResetInPortCountersAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var dispatcher = new Mock<IUiDispatcher>();
            dispatcher.Setup(candidate => candidate.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
            var io = Io;
            io.Setup(service => service.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()))
                .ReturnsAsync((true, "projection-test.json", null));
            io.Setup(service => service.SaveAsAsync(It.IsAny<Solution>())).ReturnsAsync((true, "projection-test.json", null));
            Settings.Counter.CountOfFeedbackPoints = 7;
            ViewModel = new MainWindowViewModel(new LayoutColumnWidthsViewModel(), Runtime.Object, _eventBus,
                dispatcher.Object, Settings, solution ?? new Solution { Projects = [new Project()] },
                new ActionExecutionContext { Z21 = Mock.Of<IZ21>() }, NullLogger<MainWindowViewModel>.Instance,
                io.Object, runtimeCommandGateway: Gateway.Object);
        }

        public Mock<IMobaRuntime> Runtime { get; } = new();
        public Mock<IIoService> Io { get; } = new();
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
