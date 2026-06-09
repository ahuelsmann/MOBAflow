// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging;

using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;

using Moq;

/// <summary>
/// Characterization tests for high-risk SharedUI ViewModel behavior.
/// </summary>
[TestFixture]
internal class ViewModelCharacterizationTests
{
    [Test]
    public void TrainControlViewModel_Constructor_ProjectsConnectionStateFromCurrentSnapshot()
    {
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        var settingsServiceMock = CreateSettingsServiceMock();

        var viewModel = new TrainControlViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, eventBus: CreateEventBus());

        Assert.That(viewModel.IsConnected, Is.True);
    }

    [Test]
    public async Task TrainControlViewModel_SpeedChange_SendsDriveCommand()
    {
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        var settingsServiceMock = CreateSettingsServiceMock();
        var viewModel = new TrainControlViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, eventBus: CreateEventBus());

        viewModel.Speed = 12;
        await Task.Delay(100);

        mobaRuntimeMock.Verify(client => client.SetLocomotiveDriveAsync(3, 12, true, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Test]
    public void TrainControlViewModel_SpeedIncrease_WhenDisconnected_IsRejected()
    {
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = false });
        var settingsServiceMock = CreateSettingsServiceMock();
        var viewModel = new TrainControlViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, eventBus: CreateEventBus());

        viewModel.Speed = 12;

        Assert.That(viewModel.Speed, Is.Zero);
        Assert.That(viewModel.IsSpeedControlEnabled, Is.False);
        mobaRuntimeMock.Verify(client => client.SetLocomotiveDriveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task TrainControlViewModel_ToggleFunctionAsync_UpdatesStateAndCallsRuntime()
    {
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        var settingsServiceMock = CreateSettingsServiceMock();
        var viewModel = new TrainControlViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, eventBus: CreateEventBus());

        await viewModel.ToggleFunctionAsync(1);

        Assert.That(viewModel.Functions[1].IsOn, Is.True);
        mobaRuntimeMock.Verify(client => client.SetLocomotiveFunctionAsync(3, 1, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task TrainControlViewModel_TurnOffAllFunctionsAsync_ResetsUiAndCallsRuntime()
    {
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        var settingsServiceMock = CreateSettingsServiceMock();
        var viewModel = new TrainControlViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, eventBus: CreateEventBus());

        await viewModel.ToggleFunctionAsync(0);
        await viewModel.ToggleFunctionAsync(5);
        Assert.That(viewModel.Functions[0].IsOn, Is.True);

        await viewModel.TurnOffAllFunctionsAsync();

        Assert.That(viewModel.Functions.All(f => !f.IsOn), Is.True, "All function buttons should be off");
        mobaRuntimeMock.Verify(client => client.SetAllLocomotiveFunctionsOffAsync(3, It.IsAny<CancellationToken>()), Times.Once);

        // New all-off state is persisted to the currently selected preset.
        Assert.That(Enumerable.Range(0, 32).All(i => !viewModel.CurrentPreset.GetFunction(i)), Is.True, "Preset should store all functions off");
        await Task.Delay(100);
        settingsServiceMock.Verify(service => service.SaveSettingsAsync(It.IsAny<AppSettings>()), Times.AtLeastOnce);
    }

    [Test]
    public void TrainControlViewModel_Timetable_ShowsVirtualEventWithSignalAspect()
    {
        var workflowId = Guid.NewGuid();
        var journey = new Journey
        {
            FirstPos = 1,
            Stations =
            [
                new Station { Name = "Bielefeld", Arrival = new DateTime(2026, 5, 25, 12, 0, 0), Departure = new DateTime(2026, 5, 25, 12, 1, 0) },
                new Station { Name = "Signal Event", IsVirtual = true, WorkflowId = workflowId },
                new Station { Name = "Herford" }
            ]
        };
        var workflow = new Workflow
        {
            Id = workflowId,
            Actions =
            [
                new WorkflowAction
                {
                    Number = 1,
                    Type = ActionType.SelectSignalAspect,
                    SelectSignalAspect = new SelectSignalAspectActionPayload { SignalAspect = SignalAspect.Ks1 }
                }
            ]
        };
        var project = new Project { Journeys = [journey], Workflows = [workflow] };
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        var settingsServiceMock = CreateSettingsServiceMock();
        var mainViewModel = CreateMainWindowViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, new AppSettings());
        mainViewModel.SelectedProject = new ProjectViewModel(project);
        mainViewModel.SelectedJourney = mainViewModel.SelectedProject.Journeys.Single();
        mainViewModel.SelectedJourney.UpdateFromSessionState(new JourneySessionState { JourneyId = journey.Id, CurrentPos = 1 });

        var viewModel = new TrainControlViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, mainViewModel, eventBus: CreateEventBus());

        Assert.That(viewModel.CurrentStationName, Is.EqualTo("Signal Event"));
        Assert.That(viewModel.CurrentStationIsEvent, Is.True);
        Assert.That(viewModel.CurrentStationArrival, Is.EqualTo("\u2014"));
        Assert.That(viewModel.CurrentStationDeparture, Is.EqualTo("\u2014"));
        Assert.That(viewModel.CurrentStationTrack, Is.EqualTo("Signal: Ks1"));
        Assert.That(viewModel.CurrentStationShowsExitDirection, Is.False);
        Assert.That(viewModel.PreviousStationName, Is.EqualTo("Bielefeld"));
        Assert.That(viewModel.NextStationName, Is.EqualTo("Herford"));
    }

    [Test]
    public void TrainControlViewModel_Timetable_ResolvesSignalAspectFromSolutionWhenSelectedProjectDiffers()
    {
        var workflowId = Guid.NewGuid();
        var journey = new Journey
        {
            FirstPos = 0,
            Stations =
            [
                new Station { Name = "Signal Event", IsVirtual = true, WorkflowId = workflowId }
            ]
        };
        var workflow = new Workflow
        {
            Id = workflowId,
            Actions =
            [
                new WorkflowAction
                {
                    Number = 1,
                    Type = ActionType.SelectSignalAspect,
                    SelectSignalAspect = new SelectSignalAspectActionPayload { SignalAspect = SignalAspect.Ks2 }
                }
            ]
        };
        var project = new Project { Name = "Runtime Project", Journeys = [journey], Workflows = [workflow] };
        var otherProject = new Project { Name = "Other Project" };
        var solution = new Solution { Projects = [project, otherProject] };
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        var settingsServiceMock = CreateSettingsServiceMock();
        var mainViewModel = CreateMainWindowViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, new AppSettings(), solution);
        mainViewModel.SelectedProject = mainViewModel.SolutionViewModel?.Projects.Single(viewModel => viewModel.Model == otherProject);
        mainViewModel.SelectedJourney = mainViewModel.SolutionViewModel?.Projects.Single(viewModel => viewModel.Model == project).Journeys.Single();

        var viewModel = new TrainControlViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, mainViewModel, eventBus: CreateEventBus());

        Assert.That(viewModel.CurrentStationTrack, Is.EqualTo("Signal: Ks2"));
    }

    [Test]
    public void MainWindowViewModel_AssignWorkflowToStation_UsesExplicitTargetStation()
    {
        var firstStation = new Station { Name = "A" };
        var secondStation = new Station { Name = "B" };
        var workflow = new Workflow { Name = "Target workflow" };
        var project = new Project
        {
            Journeys =
            [
                new Journey { Stations = [firstStation, secondStation] }
            ],
            Workflows = [workflow]
        };
        var mobaRuntimeMock = CreateMobaRuntimeMock(new MobaRuntimeSnapshot { IsConnected = true });
        var settingsServiceMock = CreateSettingsServiceMock();
        var mainViewModel = CreateMainWindowViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, new AppSettings());
        mainViewModel.SelectedProject = new ProjectViewModel(project);
        mainViewModel.SelectedJourney = mainViewModel.SelectedProject.Journeys.Single();
        mainViewModel.SelectedStation = mainViewModel.SelectedJourney.Stations[0];
        var targetStation = mainViewModel.SelectedJourney.Stations[1];
        var workflowViewModel = mainViewModel.SelectedProject.Workflows.Single();

        mainViewModel.AssignWorkflowToStation(workflowViewModel, targetStation);

        Assert.That(firstStation.WorkflowId, Is.Null);
        Assert.That(secondStation.WorkflowId, Is.EqualTo(workflow.Id));
        Assert.That(mainViewModel.SelectedStation, Is.SameAs(targetStation));
    }

    [Test]
    public void MainWindowViewModel_AutoStartWebApp_SetterPersistsSettings()
    {
        var mobaRuntimeMock = new Mock<IMobaRuntime>();
        mobaRuntimeMock.SetupGet(client => client.Current).Returns(MobaRuntimeSnapshot.Empty);
        mobaRuntimeMock.Setup(client => client.GetTrafficPackets()).Returns(Array.Empty<Z21TrafficPacket>());

        var settings = new AppSettings();
        var settingsServiceMock = CreateSettingsServiceMock(settings);

        var viewModel = CreateMainWindowViewModel(mobaRuntimeMock.Object, settingsServiceMock.Object, settings);
        viewModel.AutoStartWebApp = false;

        Assert.That(settings.Application.AutoStartWebApp, Is.False);
        settingsServiceMock.Verify(service => service.SaveSettingsAsync(settings), Times.AtLeastOnce);
    }

    private static Mock<IMobaRuntime> CreateMobaRuntimeMock(MobaRuntimeSnapshot snapshot)
    {
        var mobaRuntimeMock = new Mock<IMobaRuntime>();
        mobaRuntimeMock.SetupGet(client => client.Current).Returns(snapshot);
        mobaRuntimeMock.Setup(client => client.GetTrafficPackets()).Returns(Array.Empty<Z21TrafficPacket>());
        mobaRuntimeMock.Setup(client => client.SetLocomotiveDriveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mobaRuntimeMock.Setup(client => client.SetLocomotiveFunctionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mobaRuntimeMock.Setup(client => client.SetAllLocomotiveFunctionsOffAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mobaRuntimeMock.Setup(client => client.RequestLocomotiveInfoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mobaRuntimeMock;
    }

    private static Mock<ISettingsService> CreateSettingsServiceMock(AppSettings? settings = null)
    {
        var currentSettings = settings ?? new AppSettings();
        var settingsServiceMock = new Mock<ISettingsService>();
        settingsServiceMock.Setup(service => service.GetSettings()).Returns(currentSettings);
        settingsServiceMock.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        settingsServiceMock.Setup(service => service.LoadSettingsAsync()).Returns(Task.CompletedTask);
        settingsServiceMock.Setup(service => service.ResetToDefaultsAsync()).Returns(Task.CompletedTask);
        return settingsServiceMock;
    }

    private static IEventBus CreateEventBus() => new Mock<IEventBus>().Object;

    private static MainWindowViewModel CreateMainWindowViewModel(
        IMobaRuntime mobaRuntime,
        ISettingsService settingsService,
        AppSettings settings,
        Solution? solution = null)
    {
        var eventBusMock = new Mock<IEventBus>();
        var uiDispatcherMock = new Mock<IUiDispatcher>();
        var loggerMock = new Mock<ILogger<MainWindowViewModel>>();

        uiDispatcherMock
            .Setup(dispatcher => dispatcher.InvokeOnUi(It.IsAny<Action>()))
            .Callback<Action>(action => action());

        return new MainWindowViewModel(
            new LayoutColumnWidthsViewModel(),
            mobaRuntime,
            eventBusMock.Object,
            uiDispatcherMock.Object,
            settings,
            solution ?? new Solution(),
            new ActionExecutionContext
            {
                Z21 = new Mock<IZ21>().Object
            },
            loggerMock.Object,
            settingsService: settingsService);
    }
}
