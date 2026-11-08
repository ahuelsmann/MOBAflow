// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Test.Helpers;
using Moba.Backend.Interface;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Contracts.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moq;
using System.Windows.Input;

/// <summary>
/// Checks the shell's journey projection against authoritative runtime snapshots and command routing.
/// The counter projection is covered by <see cref="InPortCounterViewModelTests"/>.
/// </summary>
[TestFixture]
public sealed partial class JourneyCounterProjectionTests
{
    [Test]
    public async Task SettingsSpeechSelectionRefreshesShellStatusAndDetachesOnShutdown()
    {
        var fixture = new ProjectionFixture();
        await using var fixtureLifetime = fixture.ConfigureAwait(false);
        fixture.ViewModel.UpdateHealthStatus("Ready");
        var updates = 0;
        fixture.ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.SpeechStatusDisplayText)) updates++;
        };
        fixture.ViewModel.SettingsPage.SelectedSpeechProviderId = "piper";
        Assert.That(fixture.ViewModel.SpeechStatusDisplayText, Is.EqualTo("Speech: Initializing..."));

        await fixture.ViewModel.PrepareForShutdownAsync().ConfigureAwait(false);
        fixture.ViewModel.SettingsPage.SelectedSpeechProviderId = "system-speech";
        Assert.That(updates, Is.EqualTo(1));
    }

    [Test]
    public async Task ActiveFlag_IsPersistedOnTheJourneyAndRefreshesTheRuntimeSnapshot()
    {
        var journey = CreateJourney("Regional");
        await using var fixture = new ProjectionFixture(new Solution { Projects = [new Project { Journeys = [journey] }] });
        fixture.Runtime.Invocations.Clear();

        fixture.ViewModel.SelectedJourney!.IsActive = true;

        Assert.That(journey.IsActive, Is.True);
        fixture.Runtime.Verify(runtime => runtime.UpdateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
        fixture.Runtime.Verify(runtime => runtime.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task EventEdits_StayEditableForActiveJourneysAndRefreshTheRuntimeSnapshot()
    {
        var journey = CreateJourney("Regional");
        journey.IsActive = true;
        await using var fixture = new ProjectionFixture(new Solution { Projects = [new Project { Journeys = [journey] }] });
        using var editor = new EventManagerViewModel(fixture.ViewModel.SolutionSession,
            fixture.ViewModel.StationsPage.WorkflowLibrary, fixture.ViewModel.JourneysPage, Mock.Of<ICommand>());
        fixture.Runtime.Invocations.Clear();

        editor.Events.Single().Count = 9;

        Assert.Multiple(() =>
        {
            Assert.That(editor.CanEdit, Is.True);
            Assert.That(journey.EventPlan.Events.Single().Count, Is.EqualTo(9));
        });
        fixture.Runtime.Verify(runtime => runtime.UpdateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
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

        fixture.Runtime.Verify(runtime => runtime.UpdateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
        journeys[1].IsActive = true;
        fixture.Runtime.Verify(runtime => runtime.UpdateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once,
            "An unselected journey is not observed, so it causes no further update.");
    }

    [Test]
    public async Task MainWindowSelectionCommitsThePreviousDraftToItsRuntime()
    {
        var first = CreateJourney("First");
        var second = CreateJourney("Second");
        var fixture = new ProjectionFixture(new Solution { Projects = [new Project { Journeys = [first, second] }] });
        await using var fixtureLifetime = fixture.ConfigureAwait(false);
        using var editor = new EventManagerViewModel(fixture.ViewModel.SolutionSession,
            fixture.ViewModel.StationsPage.WorkflowLibrary, fixture.ViewModel.JourneysPage, Mock.Of<ICommand>());
        fixture.ViewModel.CurrentSolutionPath = "projection-test.json";
        editor.Events.Single().CountText = "25";
        fixture.Runtime.Invocations.Clear();
        fixture.Io.Invocations.Clear();

        fixture.ViewModel.SelectedJourney = fixture.ViewModel.SelectedProject!.Journeys[1];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.EventPlan.Events.Single().Count, Is.EqualTo(25));
            fixture.Runtime.Verify(runtime => runtime.UpdateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
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
        private readonly TestRollingStockPages _rollingStockPages = new();
        private readonly EventBus _eventBus = new(NullLogger<EventBus>.Instance);
        private MobaRuntimeSnapshot _snapshot;

        public ProjectionFixture(Solution? solution = null, MobaRuntimeSnapshot? initial = null)
        {
            _snapshot = initial ?? MobaRuntimeSnapshot.Empty;
            Runtime.SetupGet(runtime => runtime.Current).Returns(() => _snapshot);
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
            var locomotiveSession1 = TestSolutionSessions.Create(solution ?? new Solution { Projects = [new Project()] }, dispatcher.Object, Runtime.Object, io.Object);
            var stationEventBus1 = _eventBus;
            ViewModel = new MainWindowViewModel(
                new LayoutColumnWidthsViewModel(),
                Runtime.Object,
                Runtime.Object,
                Gateway.Object,
                stationEventBus1,
                TestSettingsPages.Create(Settings, dispatcher.Object),
                _rollingStockPages.GetTrainsPage(locomotiveSession1, Runtime.Object, io.Object),
                _rollingStockPages.GetStationsPage(locomotiveSession1, Runtime.Object, stationEventBus1),
                _rollingStockPages.GetJourneysPage(locomotiveSession1, Gateway.Object, dispatcher.Object),
                locomotiveSession1,
                NullLogger<MainWindowViewModel>.Instance);
        }

        public Mock<IMobaRuntime> Runtime { get; } = new();
        public Mock<ISolutionIoService> Io { get; } = TestFileServices.CreateMock();
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
            _rollingStockPages.Dispose();
        }
    }
}
