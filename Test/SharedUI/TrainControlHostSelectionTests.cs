// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;

using Moq;

namespace Moba.Test.SharedUI;

[TestFixture]
internal sealed class TrainControlHostSelectionTests
{
    [Test]
    public void WinUiAndMauiHosts_PersistIndependentLocomotiveSelections()
    {
        var locomotiveAId = Guid.NewGuid();
        var locomotiveBId = Guid.NewGuid();
        var solution = new Solution
        {
            Projects =
            [
                new Project
                {
                    Name = "myMOBA",
                    Locomotives =
                    [
                        new Locomotive { Id = locomotiveAId, Name = "BR 110", DigitalAddress = 7 },
                        new Locomotive { Id = locomotiveBId, Name = "BR 218", DigitalAddress = 3 }
                    ]
                }
            ]
        };
        var projectContext = new MobileSolutionContext();
        projectContext.ApplySolution(solution, "myMOBA");

        var settings = new AppSettings();
        var settingsServiceMock = new Mock<ISettingsService>();
        settingsServiceMock.Setup(service => service.GetSettings()).Returns(() => settings);
        settingsServiceMock
            .Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>()))
            .Returns<AppSettings>(saved =>
            {
                settings.TrainControl = saved.TrainControl;
                settings.WinUiTrainControlHost = saved.WinUiTrainControlHost;
                settings.MauiTrainControlHost = saved.MauiTrainControlHost;
                return Task.CompletedTask;
            });

        var runtimeMock = new Mock<IMobaRuntime>();
        runtimeMock.SetupGet(runtime => runtime.Current).Returns(MobaRuntimeSnapshot.Empty);
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);

        var winUiViewModel = new TrainControlViewModel(
            runtimeMock.Object,
            settingsServiceMock.Object,
            projectContext,
            eventBus: eventBus,
            options: new TrainControlViewModelOptions { Host = TrainControlHost.WinUi });
        winUiViewModel.RefreshLocomotiveList();
        winUiViewModel.SelectProjectLocomotiveCommand.Execute(winUiViewModel.ProjectLocomotives[0]);

        var mauiViewModel = new TrainControlViewModel(
            runtimeMock.Object,
            settingsServiceMock.Object,
            projectContext,
            eventBus: eventBus,
            options: new TrainControlViewModelOptions { Host = TrainControlHost.Maui });
        mauiViewModel.RefreshLocomotiveList();
        mauiViewModel.SelectProjectLocomotiveCommand.Execute(mauiViewModel.ProjectLocomotives[1]);

        Assert.Multiple(() =>
        {
            Assert.That(settings.WinUiTrainControlHost.SelectedLocomotiveFromProjectId, Is.EqualTo(locomotiveAId));
            Assert.That(settings.MauiTrainControlHost.SelectedLocomotiveFromProjectId, Is.EqualTo(locomotiveBId));
            Assert.That(winUiViewModel.SelectedLocomotiveFromProject?.Model.Id, Is.EqualTo(locomotiveAId));
            Assert.That(mauiViewModel.SelectedLocomotiveFromProject?.Model.Id, Is.EqualTo(locomotiveBId));
        });

        winUiViewModel.Dispose();
        mauiViewModel.Dispose();
    }
}
