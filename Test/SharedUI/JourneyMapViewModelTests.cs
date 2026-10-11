// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging;
using Moba.Test.Helpers;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;
using Moq;

[TestFixture]
internal sealed class JourneyMapViewModelTests : IDisposable
{
    private readonly TestLocomotivePages _locomotivePages = new();

    [TearDown]
    public void Dispose()
    {
        _locomotivePages.Dispose();
        GC.SuppressFinalize(this);
    }

    [Test]
    public void RouteStations_ReturnsJourneyStations()
    {
        var journey = new Journey
        {
            Stations =
            [
                new Station { Name = "Bielefeld" },
                new Station { Name = "Herford" }
            ]
        };
        var project = new Project { Journeys = [journey] };
        var projectViewModel = new ProjectViewModel(project);
        var mainViewModel = CreateMainWindowViewModel();
        mainViewModel.SelectedProject = projectViewModel;
        mainViewModel.SelectedJourney = projectViewModel.Journeys.Single();
        var viewModel = new JourneyMapViewModel(mainViewModel.SolutionSession);

        Assert.That(viewModel.RouteStations.Select(station => station.Name), Is.EqualTo(new[] { "Bielefeld", "Herford" }));
        Assert.That(viewModel.ProgressText, Is.EqualTo("Station 1 of 2"));
    }

    private MainWindowViewModel CreateMainWindowViewModel()
    {
        var runtimeMock = new Mock<IMobaRuntime>();
        var eventBusMock = new Mock<IEventBus>();
        var dispatcherMock = new Mock<IUiDispatcher>();
        var loggerMock = new Mock<ILogger<MainWindowViewModel>>();
        dispatcherMock
            .Setup(dispatcher => dispatcher.InvokeOnUi(It.IsAny<Action>()))
            .Callback<Action>(action => action());
        runtimeMock.Setup(runtime => runtime.Current).Returns(new MobaRuntimeSnapshot());
        runtimeMock.Setup(runtime => runtime.GetTrafficPackets()).Returns([]);

        var locomotiveSession1 = TestSolutionSessions.Create(new Solution(), dispatcherMock.Object, runtimeMock.Object);
        return new MainWindowViewModel(
            new LayoutColumnWidthsViewModel(),
            runtimeMock.Object,
            runtimeMock.Object,
            runtimeMock.Object,
            new LocalRuntimeCommandGateway(runtimeMock.Object),
            eventBusMock.Object,
            dispatcherMock.Object,
            TestSettingsPages.Create(new AppSettings(), dispatcherMock.Object),
            _locomotivePages.GetPage(locomotiveSession1, null),
            locomotiveSession1,
            new ActionExecutionContext
            {
                Z21 = new Mock<IZ21>().Object
            },
            loggerMock.Object);
    }
}
