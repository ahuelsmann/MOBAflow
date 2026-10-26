// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Contracts.Runtime;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.TrainControl;

using Moq;

[TestFixture]
internal sealed class TrainControlViewModelConnectionTests
{
    [Test]
    public void Connecting_RequestsTheLocomotiveSelectedWhileDisconnected()
    {
        var runtime = new Mock<IMobaRuntime>();
        runtime.SetupGet(value => value.Current).Returns(MobaRuntimeSnapshot.Empty);
        runtime.Setup(value => value.RequestLocomotiveInfoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        using var viewModel = CreateTrainControlViewModel(runtime.Object, eventBus);

        viewModel.LocoAddress = 42;
        var requestedWhileDisconnected = runtime.Invocations.Count(call => call.Method.Name == nameof(IMobaRuntime.RequestLocomotiveInfoAsync));
        eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot { IsConnected = true }));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requestedWhileDisconnected, Is.Zero);
            runtime.Verify(value => value.RequestLocomotiveInfoAsync(42, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    private static TrainControlViewModel CreateTrainControlViewModel(IMobaRuntime runtime, IEventBus eventBus)
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(service => service.GetSettings()).Returns(new AppSettings());
        settingsService.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);

        return new TrainControlViewModel(
            runtime,
            new LocalRuntimeCommandGateway(runtime),
            settingsService.Object,
            null,
            NullLogger<TrainControlViewModel>.Instance,
            null,
            eventBus);
    }
}
