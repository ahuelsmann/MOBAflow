// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Backend.Interface;
using Moba.Contracts.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;
using Moba.Test.Helpers;
using Moq;

[TestFixture]
internal sealed class JourneyMapViewModelTests
{
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
        var projectViewModel = new ProjectViewModel(project, TestViewModelServices.Default);
        var session = CreateSession();
        session.SelectedProject = projectViewModel;
        session.SelectedJourney = projectViewModel.Journeys.Single();
        var viewModel = new JourneyMapViewModel(session);

        Assert.That(viewModel.RouteStations.Select(station => station.Name), Is.EqualTo(new[] { "Bielefeld", "Herford" }));
        Assert.That(viewModel.ProgressText, Is.EqualTo("Station 1 of 2"));
    }

    private static SolutionSession CreateSession()
    {
        var runtimeMock = new Mock<IMobaRuntime>();
        var dispatcherMock = new Mock<IUiDispatcher>();
        dispatcherMock
            .Setup(dispatcher => dispatcher.InvokeOnUi(It.IsAny<Action>()))
            .Callback<Action>(action => action());
        runtimeMock.Setup(runtime => runtime.Current).Returns(new MobaRuntimeSnapshot());

        return TestSolutionSessions.Create(new Solution(), dispatcherMock.Object, runtimeMock.Object);
    }
}
