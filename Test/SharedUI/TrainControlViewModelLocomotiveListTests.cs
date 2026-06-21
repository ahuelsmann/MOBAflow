// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.Common.Configuration;
using Moba.Common.Runtime;
using Moba.Common.Events;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;
using Moq;

namespace Moba.Test.SharedUI;

[TestFixture]
internal sealed class TrainControlViewModelLocomotiveListTests
{
    [Test]
    public void RefreshLocomotiveList_PopulatesProjectLocomotives_FromSelectedProject()
    {
        var solution = new Solution
        {
            Projects =
            [
                new Project
                {
                    Name = "myMOBA",
                    Locomotives =
                    [
                        new Locomotive { Name = "BR 110 Verkehrsrot", DigitalAddress = 7 }
                    ]
                }
            ]
        };

        var projectContext = new MobileSolutionContext();
        projectContext.ApplySolution(solution, "myMOBA");

        var viewModel = CreateViewModel(projectContext);
        viewModel.RefreshLocomotiveList();

        Assert.That(viewModel.HasProjectLocomotives, Is.True);
        Assert.That(viewModel.ProjectLocomotives, Has.Count.EqualTo(1));
        Assert.That(viewModel.ProjectLocomotives[0].Name, Is.EqualTo("BR 110 Verkehrsrot"));
        Assert.That(viewModel.ProjectLocomotives[0].DigitalAddress, Is.EqualTo(7u));
    }

    [Test]
    public void RefreshLocomotiveList_UsesFleetOverride_WhenRemoteSnapshotProvidesFleet()
    {
        var fleet =
            new List<LocomotiveFleetSnapshot>
            {
                new()
                {
                    LocomotiveId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                    Name = "BR 218",
                    DigitalAddress = 3
                }
            };

        var viewModel = CreateViewModel(new MobileSolutionContext());
        viewModel.RefreshLocomotiveList(fleet);

        Assert.That(viewModel.HasProjectLocomotives, Is.True);
        Assert.That(viewModel.ProjectLocomotives[0].Name, Is.EqualTo("BR 218"));
        Assert.That(viewModel.ProjectLocomotives[0].DigitalAddress, Is.EqualTo(3u));
    }

    [Test]
    public void RefreshLocomotiveList_ClearsList_WhenProjectHasNoLocomotives()
    {
        var solution = new Solution
        {
            Projects = [new Project { Name = "empty", Locomotives = [] }]
        };

        var projectContext = new MobileSolutionContext();
        projectContext.ApplySolution(solution, "empty");

        var viewModel = CreateViewModel(projectContext);
        viewModel.RefreshLocomotiveList();

        Assert.That(viewModel.HasProjectLocomotives, Is.False);
        Assert.That(viewModel.ProjectLocomotives, Is.Empty);
    }

    [Test]
    public void RefreshLocomotiveList_KeepsList_WhenEmptyFleetOverrideArrives()
    {
        var viewModel = CreateViewModel(new MobileSolutionContext());

        viewModel.RefreshLocomotiveList(
        [
            new LocomotiveFleetSnapshot
            {
                LocomotiveId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                Name = "BR 110 Verkehrsrot",
                DigitalAddress = 7
            }
        ]);

        Assert.That(viewModel.ProjectLocomotives, Has.Count.EqualTo(1));
        viewModel.RefreshLocomotiveList([]);
        Assert.That(viewModel.ProjectLocomotives, Has.Count.EqualTo(1));
    }

    [Test]
    public void RefreshLocomotiveList_UpdatesInPlace_WhenFleetMetadataChanges()
    {
        var locomotiveId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var viewModel = CreateViewModel(new MobileSolutionContext());

        viewModel.RefreshLocomotiveList(
        [
            new LocomotiveFleetSnapshot
            {
                LocomotiveId = locomotiveId,
                Name = "BR 110 Verkehrsrot",
                DigitalAddress = 7
            }
        ]);

        var originalInstance = viewModel.ProjectLocomotives[0];
        viewModel.RefreshLocomotiveList(
        [
            new LocomotiveFleetSnapshot
            {
                LocomotiveId = locomotiveId,
                Name = "BR 110 Ocean Blue",
                DigitalAddress = 7
            }
        ]);

        Assert.That(viewModel.ProjectLocomotives, Has.Count.EqualTo(1));
        Assert.That(viewModel.ProjectLocomotives[0], Is.SameAs(originalInstance));
        Assert.That(viewModel.ProjectLocomotives[0].Name, Is.EqualTo("BR 110 Ocean Blue"));
    }

    private static TrainControlViewModel CreateViewModel(IProjectContext projectContext)
    {
        var runtimeMock = new Mock<IMobaRuntime>();
        runtimeMock.SetupGet(runtime => runtime.Current).Returns(MobaRuntimeSnapshot.Empty);
        var settingsMock = new Mock<ISettingsService>();
        settingsMock.Setup(service => service.GetSettings()).Returns(new AppSettings());

        return new TrainControlViewModel(
            runtimeMock.Object,
            settingsMock.Object,
            projectContext,
            NullLogger<TrainControlViewModel>.Instance,
            eventBus: new EventBus(NullLogger<EventBus>.Instance),
            options: new TrainControlViewModelOptions { PreferProjectLocomotives = true });
    }
}
