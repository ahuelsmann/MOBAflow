// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Interface;
using Moba.Common.Configuration;
using Moba.Common.Discovery;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;
using Moq;
namespace Moba.Test.SharedUI;
[TestFixture]
internal sealed class MauiViewModelControlTabTests
{
    [Test]
    public void SetControlTabActive_PublishesFleet_ToTrainControlViewModel()
    {
        var runtimeMock = new Mock<IMobaRuntime>();
        runtimeMock.SetupGet(runtime => runtime.Current).Returns(new MobaRuntimeSnapshot
        {
            LocomotiveFleet =
            [
                new LocomotiveFleetSnapshot
                {
                    LocomotiveId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                    Name = "BR 110 Verkehrsrot",
                    DigitalAddress = 7
                }

            ]
        });
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var trainControl = CreateTrainControlViewModel(eventBus);
        var viewModel = CreateViewModel(mobaRuntime: runtimeMock.Object, eventBus: eventBus);
        viewModel.SetControlTabActive(true);
        Assert.That(trainControl.HasProjectLocomotives, Is.True);
        Assert.That(trainControl.ProjectLocomotives[0].Name, Is.EqualTo("BR 110 Verkehrsrot"));
    }

    [Test]
    public void SetControlTabActive_PublishesFleet_ForEnginesTabLifecycle()
    {
        var runtimeMock = new Mock<IMobaRuntime>();
        runtimeMock.SetupGet(runtime => runtime.Current).Returns(new MobaRuntimeSnapshot
        {
            LocomotiveFleet =
            [
                new LocomotiveFleetSnapshot
                {
                    LocomotiveId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff"),
                    Name = "BR 211",
                    DigitalAddress = 12
                }

            ]
        });
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var trainControl = CreateTrainControlViewModel(eventBus);
        var viewModel = CreateViewModel(mobaRuntime: runtimeMock.Object, eventBus: eventBus);
        viewModel.SetControlTabActive(true);
        Assert.That(trainControl.HasProjectLocomotives, Is.True);
        Assert.That(trainControl.ProjectLocomotives[0].Name, Is.EqualTo("BR 211"));
    }

    [Test]
    public void SetControlTabActive_ExposesFleet_FromSyncedProject()
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
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var trainControl = CreateTrainControlViewModel(eventBus, projectContext);
        var viewModel = CreateViewModel(projectContext, eventBus: eventBus);
        viewModel.SetControlTabActive(true);
        Assert.That(trainControl.HasProjectLocomotives, Is.True);
        Assert.That(trainControl.ProjectLocomotives, Has.Count.EqualTo(1));
        Assert.That(trainControl.ProjectLocomotives[0].Name, Is.EqualTo("BR 110 Verkehrsrot"));
    }

    [Test]
    public void SolutionSyncedEvent_RefreshesFleet_WhenProjectContextUpdates()
    {
        var projectContext = new MobileSolutionContext();
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var trainControl = CreateTrainControlViewModel(eventBus, projectContext);
        var viewModel = CreateViewModel(projectContext, eventBus: eventBus);
        var solution = new Solution
        {
            Projects =
            [
                new Project
                {
                    Name = "myMOBA",
                    Locomotives = [new Locomotive { Name = "BR 218", DigitalAddress = 3 }]
                }

            ]
        };
        projectContext.ApplySolution(solution, "myMOBA");
        eventBus.Publish(new SolutionSyncedEvent(DateTimeOffset.UtcNow, solution.Name, "myMOBA"));
        viewModel.SetControlTabActive(true);
        Assert.That(trainControl.HasProjectLocomotives, Is.True);
        Assert.That(trainControl.ProjectLocomotives[0].Name, Is.EqualTo("BR 218"));
    }

    [Test]
    public void FleetUpdate_KeepsList_WhenEmptySnapshotArrives()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var trainControl = CreateTrainControlViewModel(eventBus);
        var viewModel = CreateViewModel(eventBus: eventBus);
        viewModel.SetControlTabActive(true);
        var fleet =
            new List<LocomotiveFleetSnapshot>
            {
                new()
                {
                    LocomotiveId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                    Name = "BR 110 Verkehrsrot",
                    DigitalAddress = 7
                }
            };
        eventBus.Publish(new LocomotiveFleetUpdatedEvent(fleet));
        Assert.That(trainControl.ProjectLocomotives, Has.Count.EqualTo(1));
        eventBus.Publish(new LocomotiveFleetUpdatedEvent([]));
        Assert.That(trainControl.ProjectLocomotives, Has.Count.EqualTo(1));
        Assert.That(trainControl.ProjectLocomotives[0].Name, Is.EqualTo("BR 110 Verkehrsrot"));
    }

    [Test]
    public void RemoteSnapshot_WithFleet_PopulatesTrainControlList()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var runtimeHubMock = new Mock<IRuntimeHubRemoteClient>();
        var trainControl = CreateTrainControlViewModel(eventBus);
        var viewModel = CreateViewModel(
            eventBus: eventBus,
            runtimeHubRemoteClient: runtimeHubMock.Object);
        viewModel.SetControlTabActive(true);
        eventBus.Publish(new RemoteRuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            LocomotiveFleet =
            [
                new LocomotiveFleetSnapshot
                {
                    LocomotiveId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                    Name = "BR 218",
                    DigitalAddress = 3
                }

            ]
        }));
        Assert.That(trainControl.HasProjectLocomotives, Is.True);
        Assert.That(trainControl.ProjectLocomotives[0].Name, Is.EqualTo("BR 218"));
    }

    [Test]
    public void RemoteSnapshot_DoesNotReplaceProjectFleet_WhenRemoteFleetIsIncomplete()
    {
        var br110Id = Guid.Parse("bb15c10a-5b78-451f-8f2f-2d4e3efa74af");
        var br211Id = Guid.Parse("f8a91b2c-3d4e-5f60-a211-279170442317");
        var projectContext = new MobileSolutionContext();
        projectContext.ApplySolution(new Solution
        {
            Projects =
            [
                new Project
                {
                    Name = "myMOBA",
                    Locomotives =
                    [
                        new Locomotive { Id = br110Id, Name = "BR 110 Verkehrsrot", DigitalAddress = 7 },
                        new Locomotive { Id = br211Id, Name = "BR 211", DigitalAddress = 211 }
                    ]
                }

            ]
        }, "myMOBA");
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var trainControl = CreateTrainControlViewModel(eventBus, projectContext);
        var viewModel = CreateViewModel(projectContext, eventBus: eventBus);
        viewModel.SetControlTabActive(true);
        eventBus.Publish(new RemoteRuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            LocomotiveFleet =
            [
                new LocomotiveFleetSnapshot
                {
                    LocomotiveId = br110Id,
                    Name = "BR 110 Verkehrsrot",
                    DigitalAddress = 7,
                    PhotoPath = "photos/latest/stale.jpg"
                }

            ]
        }));
        Assert.Multiple(() =>
        {
            Assert.That(trainControl.ProjectLocomotives, Has.Count.EqualTo(2));
            Assert.That(trainControl.ProjectLocomotives[0].Name, Is.EqualTo("BR 110 Verkehrsrot"));
            Assert.That(trainControl.ProjectLocomotives[1].Name, Is.EqualTo("BR 211"));
        });
    }

    [Test]
    public void FleetUpdate_UpdatesInPlace_WhenFleetMetadataChanges()
    {
        var locomotiveId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var trainControl = CreateTrainControlViewModel(eventBus);
        var viewModel = CreateViewModel(eventBus: eventBus);
        viewModel.SetControlTabActive(true);
        eventBus.Publish(new LocomotiveFleetUpdatedEvent(
        [
            new LocomotiveFleetSnapshot
            {
                LocomotiveId = locomotiveId,
                Name = "BR 110 Verkehrsrot",
                DigitalAddress = 7
            }

        ]));
        var originalInstance = trainControl.ProjectLocomotives[0];
        eventBus.Publish(new LocomotiveFleetUpdatedEvent(
        [
            new LocomotiveFleetSnapshot
            {
                LocomotiveId = locomotiveId,
                Name = "BR 110 Ocean Blue",
                DigitalAddress = 7
            }

        ]));
        Assert.That(trainControl.ProjectLocomotives, Has.Count.EqualTo(1));
        Assert.That(trainControl.ProjectLocomotives[0], Is.SameAs(originalInstance));
        Assert.That(trainControl.ProjectLocomotives[0].Name, Is.EqualTo("BR 110 Ocean Blue"));
    }

    [Test]
    public void FleetUpdate_DefersUntilControlTabActive()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var trainControl = CreateTrainControlViewModel(eventBus);
        var viewModel = CreateViewModel(eventBus: eventBus);
        viewModel.SetControlTabActive(false);
        viewModel.RestoreCachedLocomotiveFleet(
        [
            new LocomotiveFleetSnapshot
            {
                LocomotiveId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                Name = "BR 218",
                DigitalAddress = 3
            }

        ]);
        viewModel.ApplyBestAvailableLocomotiveFleet();
        Assert.That(trainControl.HasProjectLocomotives, Is.False);
        viewModel.SetControlTabActive(true);
        Assert.That(trainControl.HasProjectLocomotives, Is.True);
        Assert.That(trainControl.ProjectLocomotives[0].Name, Is.EqualTo("BR 218"));
    }

    [Test]
    public void SolutionSyncedEvent_RefreshesFleet_WhenPreferRemoteRuntime()
    {
        var projectContext = new MobileSolutionContext();
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var coordinator = new MobileRuntimeCoordinator(
            new Mock<IMobaRuntime>().Object,
            new Mock<IRuntimeHubRemoteClient>().Object);
        coordinator.SetMobaflowSessionActive(true);
        var trainControl = CreateTrainControlViewModel(eventBus, projectContext);
        var viewModel = CreateViewModel(
            projectContext,
            eventBus: eventBus,
            mobileRuntimeCoordinator: coordinator);
        var solution = new Solution
        {
            Projects =
            [
                new Project
                {
                    Name = "myMOBA",
                    Locomotives = [new Locomotive { Name = "BR 218", DigitalAddress = 3 }]
                }

            ]
        };
        projectContext.ApplySolution(solution, "myMOBA");
        eventBus.Publish(new SolutionSyncedEvent(DateTimeOffset.UtcNow, solution.Name, "myMOBA"));
        viewModel.SetControlTabActive(true);
        Assert.That(trainControl.HasProjectLocomotives, Is.True);
        Assert.That(trainControl.ProjectLocomotives[0].Name, Is.EqualTo("BR 218"));
    }

    [Test]
    public void SetControlTabActive_PrefersProjectFleet_WhenRemoteSessionAndStaleFleetCacheExists()
    {
        var br110Id = Guid.Parse("bb15c10a-5b78-451f-8f2f-2d4e3efa74af");
        var br211Id = Guid.Parse("f8a91b2c-3d4e-5f60-a211-279170442317");
        var projectContext = new MobileSolutionContext();
        projectContext.ApplySolution(new Solution
        {
            Projects =
            [
                new Project
                {
                    Name = "myMOBA",
                    Locomotives =
                    [
                        new Locomotive
                        {
                            Id = br110Id,
                            Name = "BR 110 Verkehrsrot",
                            DigitalAddress = 7,
                            PhotoPath = "photos/locomotives/bb15c10a-5b78-451f-8f2f-2d4e3efa74af.jpg"
                        },
                        new Locomotive
                        {
                            Id = br211Id,
                            Name = "BR 211",
                            DigitalAddress = 211,
                            PhotoPath = "photos/locomotives/f8a91b2c-3d4e-5f60-a211-279170442317.jpg"
                        }

                    ]
                }

            ]
        }, "myMOBA");
        var coordinator = new MobileRuntimeCoordinator(
            new Mock<IMobaRuntime>().Object,
            new Mock<IRuntimeHubRemoteClient>().Object);
        coordinator.SetMobaflowSessionActive(true);
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var trainControl = CreateTrainControlViewModel(eventBus, projectContext);
        var viewModel = CreateViewModel(projectContext, eventBus: eventBus, mobileRuntimeCoordinator: coordinator);
        viewModel.RestoreCachedLocomotiveFleet(
        [
            new LocomotiveFleetSnapshot
            {
                LocomotiveId = br110Id,
                Name = "BR 110 Verkehrsrot",
                DigitalAddress = 7,
                PhotoPath = "photos/latest/stale.jpg"
            }

        ]);
        viewModel.SetControlTabActive(true);
        Assert.Multiple(() =>
        {
            Assert.That(trainControl.ProjectLocomotives, Has.Count.EqualTo(2));
            Assert.That(trainControl.ProjectLocomotives[0].Name, Is.EqualTo("BR 110 Verkehrsrot"));
            Assert.That(trainControl.ProjectLocomotives[0].PhotoPath, Is.EqualTo("photos/locomotives/bb15c10a-5b78-451f-8f2f-2d4e3efa74af.jpg"));
            Assert.That(trainControl.ProjectLocomotives[1].Name, Is.EqualTo("BR 211"));
        });
    }

    [Test]
    public void RemoteSnapshot_ReplacesStaleProjectPhotos_WhenPreferRemoteRuntime()
    {
        var br110Id = Guid.Parse("bb15c10a-5b78-451f-8f2f-2d4e3efa74af");
        var br211Id = Guid.Parse("f8a91b2c-3d4e-5f60-a211-279170442317");
        var projectContext = new MobileSolutionContext();
        projectContext.ApplySolution(new Solution
        {
            Projects =
            [
                new Project
                {
                    Name = "myMOBA",
                    Locomotives =
                    [
                        new Locomotive
                        {
                            Id = br110Id,
                            Name = "BR 110 Verkehrsrot",
                            DigitalAddress = 7,
                            PhotoPath = "photos/locomotives/stale-project.jpg"
                        },
                        new Locomotive
                        {
                            Id = br211Id,
                            Name = "BR 211",
                            DigitalAddress = 211,
                            PhotoPath = "photos/locomotives/stale-project-211.jpg"
                        }

                    ]
                }

            ]
        }, "myMOBA");
        var coordinator = new MobileRuntimeCoordinator(
            new Mock<IMobaRuntime>().Object,
            new Mock<IRuntimeHubRemoteClient>().Object);
        coordinator.SetMobaflowSessionActive(true);
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var trainControl = CreateTrainControlViewModel(eventBus, projectContext);
        var viewModel = CreateViewModel(
            projectContext,
            eventBus: eventBus,
            runtimeHubRemoteClient: new Mock<IRuntimeHubRemoteClient>().Object,
            mobileRuntimeCoordinator: coordinator);
        viewModel.SetControlTabActive(true);
        eventBus.Publish(new RemoteRuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            LocomotiveFleet =
            [
                new LocomotiveFleetSnapshot
                {
                    LocomotiveId = br110Id,
                    Name = "BR 110 Verkehrsrot",
                    DigitalAddress = 7,
                    PhotoPath = "photos/locomotives/bb15c10a-5b78-451f-8f2f-2d4e3efa74af.jpg"
                },
                new LocomotiveFleetSnapshot
                {
                    LocomotiveId = br211Id,
                    Name = "BR 211",
                    DigitalAddress = 211,
                    PhotoPath = "photos/locomotives/f8a91b2c-3d4e-5f60-a211-279170442317.jpg"
                }

            ]
        }));
        Assert.Multiple(() =>
        {
            Assert.That(trainControl.ProjectLocomotives, Has.Count.EqualTo(2));
            Assert.That(trainControl.ProjectLocomotives[0].PhotoPath, Is.EqualTo("photos/locomotives/bb15c10a-5b78-451f-8f2f-2d4e3efa74af.jpg"));
            Assert.That(trainControl.ProjectLocomotives[1].PhotoPath, Is.EqualTo("photos/locomotives/f8a91b2c-3d4e-5f60-a211-279170442317.jpg"));
        });
    }

    [Test]
    public void SetControlTabActive_UsesProjectFleet_WhenRemoteSessionAndFleetCacheEmpty()
    {
        var projectContext = new MobileSolutionContext();
        projectContext.ApplySolution(new Solution
        {
            Projects =
            [
                new Project
                {
                    Name = "myMOBA",
                    Locomotives =
                    [
                        new Locomotive
                        {
                            Name = "BR 110 Verkehrsrot",
                            DigitalAddress = 7,
                            PhotoPath = "photos/latest/test.jpg"
                        }

                    ]
                }

            ]
        }, "myMOBA");
        var coordinator = new MobileRuntimeCoordinator(
            new Mock<IMobaRuntime>().Object,
            new Mock<IRuntimeHubRemoteClient>().Object);
        coordinator.SetMobaflowSessionActive(true);
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var trainControl = CreateTrainControlViewModel(eventBus, projectContext);
        var viewModel = CreateViewModel(projectContext, eventBus: eventBus, mobileRuntimeCoordinator: coordinator);
        viewModel.SetControlTabActive(true);
        Assert.Multiple(() =>
        {
            Assert.That(trainControl.HasProjectLocomotives, Is.True);
            Assert.That(trainControl.ProjectLocomotives[0].Name, Is.EqualTo("BR 110 Verkehrsrot"));
            Assert.That(trainControl.ProjectLocomotives[0].DigitalAddress, Is.EqualTo(7u));
            Assert.That(trainControl.ProjectLocomotives[0].PhotoPath, Is.EqualTo("photos/latest/test.jpg"));
        });
    }

    private static TrainControlViewModel CreateTrainControlViewModel(
        EventBus eventBus,
        IProjectContext? projectContext = null)
    {
        var runtimeMock = new Mock<IMobaRuntime>();
        runtimeMock.SetupGet(runtime => runtime.Current).Returns(MobaRuntimeSnapshot.Empty);
        var settingsMock = new Mock<ISettingsService>();
        settingsMock.Setup(service => service.GetSettings()).Returns(new AppSettings());
        return new TrainControlViewModel(
            runtimeMock.Object,
            settingsMock.Object,
            projectContext ?? new MobileSolutionContext(),
            NullLogger<TrainControlViewModel>.Instance,
            eventBus: eventBus,
            options: new TrainControlViewModelOptions { HybridRuntimeSnapshots = true });
    }

    private static MauiViewModel CreateViewModel(
        IProjectContext? projectContext = null,
        IMobaRuntime? mobaRuntime = null,
        EventBus? eventBus = null,
        IRuntimeHubRemoteClient? runtimeHubRemoteClient = null,
        IMobileRuntimeCoordinator? mobileRuntimeCoordinator = null)
    {
        var runtimeMock = mobaRuntime != null ? null : new Mock<IMobaRuntime>();
        if (runtimeMock != null)
        {
            runtimeMock.SetupGet(runtime => runtime.Current).Returns(MobaRuntimeSnapshot.Empty);
        }

        var uiDispatcherMock = new Mock<IUiDispatcher>();
        uiDispatcherMock
            .Setup(dispatcher => dispatcher.InvokeOnUi(It.IsAny<Action>()))
            .Callback<Action>(action => action());
        uiDispatcherMock
            .Setup(dispatcher => dispatcher.InvokeOnUiLowPriority(It.IsAny<Action>()))
            .Callback<Action>(action => action());
        return new MauiViewModel(
            mobaRuntime ?? runtimeMock!.Object,
            uiDispatcherMock.Object,
            new AppSettings(),
            new Mock<ISettingsService>().Object,
            new Mock<IRestDiscoveryService>().Object,
            new Mock<IZ21DiscoveryService>().Object,
            new Mock<IPhotoUploadService>().Object,
            new Mock<IPhotoCaptureService>().Object,
            new Mock<INetworkProfileChangeNotifier>().Object,
            NullLogger<MauiViewModel>.Instance,
            eventBus ?? new EventBus(NullLogger<EventBus>.Instance),
            projectContext: projectContext,
            runtimeHubRemoteClient: runtimeHubRemoteClient,
            mobileRuntimeCoordinator: mobileRuntimeCoordinator);
    }
}