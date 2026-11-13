// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Test.Helpers;
using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Contracts.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.TrainControl;
using Moba.SharedUI.ViewModel;

using Moq;

[TestFixture]
internal sealed partial class TrainControlViewModelCleanupTests
{
    [Test]
    public void Dispose_UnsubscribesFromEventBusSnapshots()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateTrainControlViewModel(eventBus: eventBus);

        eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot { IsConnected = true }));
        Assert.That(viewModel.IsConnected, Is.True);

        viewModel.Dispose();

        Assert.That(eventBus.GetSubscriberCount<RuntimeSnapshotChangedEvent>(), Is.EqualTo(0));

        eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot { IsConnected = false }));
        Assert.That(viewModel.IsConnected, Is.True);
    }

    [Test]
    public void SelectedJourneyChange_DetachesOldJourneyPropertyChangedHandler()
    {
        var session = CreateSession();
        var journeyA = CreateJourneyViewModel("A", currentPos: 0, stationCount: 3);
        var journeyB = CreateJourneyViewModel("B", currentPos: 2, stationCount: 3);
        session.SelectedJourney = journeyA;
        using var viewModel = CreateTrainControlViewModel(projectContext: session);

        journeyA.UpdateFromSessionState(new JourneySessionState { JourneyId = journeyA.Id, CurrentPos = 1 });
        Assert.That(viewModel.CurrentStationIndex, Is.EqualTo(1));

        session.SelectedJourney = journeyB;
        Assert.That(viewModel.CurrentStationIndex, Is.EqualTo(2));

        journeyA.UpdateFromSessionState(new JourneySessionState { JourneyId = journeyA.Id, CurrentPos = 0 });
        Assert.That(viewModel.CurrentStationIndex, Is.EqualTo(2));
    }

    private static TrainControlViewModel CreateTrainControlViewModel(
        SolutionSession? projectContext = null,
        IEventBus? eventBus = null)
    {
        var runtimeMock = new Mock<IMobaRuntime>();
        runtimeMock.SetupGet(runtime => runtime.Current).Returns(MobaRuntimeSnapshot.Empty);
        runtimeMock.Setup(runtime => runtime.RequestLocomotiveInfoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        runtimeMock.Setup(runtime => runtime.SetAllLocomotiveFunctionsOffAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var settingsServiceMock = new Mock<ISettingsService>();
        settingsServiceMock.Setup(service => service.GetSettings()).Returns(new AppSettings());
        settingsServiceMock.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);

        return new TrainControlViewModel(
            runtimeMock.Object,
            new LocalRuntimeCommandGateway(runtimeMock.Object),
            settingsServiceMock.Object,
            (IProjectContext?)projectContext ?? new MobileSolutionContext(TestViewModelServices.Default),
            NullLogger<TrainControlViewModel>.Instance,
            ImmediateUiDispatcher.Instance,
            eventBus ?? new EventBus(NullLogger<EventBus>.Instance),
            new NullFunctionAppearancePicker());
    }

    private static SolutionSession CreateSession()
    {
        var runtimeMock = new Mock<IMobaRuntime>();
        runtimeMock.SetupGet(runtime => runtime.Current).Returns(MobaRuntimeSnapshot.Empty);

        var uiDispatcherMock = new Mock<IUiDispatcher>();
        uiDispatcherMock
            .Setup(dispatcher => dispatcher.InvokeOnUi(It.IsAny<Action>()))
            .Callback<Action>(action => action());

        return TestSolutionSessions.Create(new Solution(), uiDispatcherMock.Object, runtimeMock.Object);
    }

    private static JourneyViewModel CreateJourneyViewModel(string name, int currentPos, int stationCount)
    {
        var journey = new Journey { Name = name };
        for (var i = 0; i < stationCount; i++)
        {
            journey.Stations.Add(new Station { Name = $"{name}{i + 1}" });
        }

        return new JourneyViewModel(
            journey,
            new Project(),
            new JourneySessionState { JourneyId = journey.Id, CurrentPos = currentPos });
    }
}