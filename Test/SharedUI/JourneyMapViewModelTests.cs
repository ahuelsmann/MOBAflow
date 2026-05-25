// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;

using Microsoft.Extensions.Logging;

using Moq;

[TestFixture]
internal class JourneyMapViewModelTests
{
    [Test]
    public void RouteStations_ExcludesVirtualStations()
    {
        var journey = new Journey
        {
            Stations =
            [
                new Station { Name = "Event1", IsVirtual = true },
                new Station { Name = "Bielefeld" },
                new Station { Name = "Event2", IsVirtual = true },
                new Station { Name = "Herford" }
            ]
        };
        var project = new Project { Journeys = [journey] };
        var projectViewModel = new ProjectViewModel(project);
        var mainViewModel = CreateMainWindowViewModel();
        mainViewModel.SelectedProject = projectViewModel;
        mainViewModel.SelectedJourney = projectViewModel.Journeys.Single();
        var viewModel = new JourneyMapViewModel(mainViewModel);

        Assert.That(viewModel.RouteStations.Select(station => station.Name), Is.EqualTo(new[] { "Bielefeld", "Herford" }));
        Assert.That(viewModel.ProgressText, Is.EqualTo("Station 0 of 2"));
    }

    private static MainWindowViewModel CreateMainWindowViewModel()
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

        return new MainWindowViewModel(
            new LayoutColumnWidthsViewModel(),
            runtimeMock.Object,
            eventBusMock.Object,
            dispatcherMock.Object,
            new AppSettings(),
            new Solution(),
            new ActionExecutionContext
            {
                Z21 = new Mock<IZ21>().Object
            },
            loggerMock.Object);
    }
}
