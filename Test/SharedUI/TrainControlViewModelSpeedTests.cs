// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;

using Moq;

[TestFixture]
internal sealed class TrainControlViewModelSpeedTests
{
    [Test]
    public void SpeedKmh_AtMaxStep_UsesGaugeFullScale_NotSelectedVmax()
    {
        var viewModel = CreateTrainControlViewModel();
        viewModel.IsConnected = true;
        viewModel.SelectedVmax = 200;
        viewModel.SpeedSteps = DccSpeedSteps.Steps128;
        viewModel.Speed = TrainControlDccSpeed.GetMaxSpeedStep(DccSpeedSteps.Steps128);

        Assert.That(viewModel.Speed, Is.EqualTo(TrainControlDccSpeed.GetMaxSpeedStep(DccSpeedSteps.Steps128)));
        Assert.That(viewModel.SpeedGaugeMaxKmh, Is.EqualTo(TrainControlDccSpeed.DefaultSpeedGaugeMaxKmh));
        Assert.That(viewModel.SpeedKmh, Is.EqualTo(TrainControlDccSpeed.DefaultSpeedGaugeMaxKmh));
    }

    [Test]
    public void SpeedKmh_AtHalfStep_ReturnsHalfGaugeMax_RegardlessOfSelectedVmax()
    {
        var viewModel = CreateTrainControlViewModel();
        viewModel.IsConnected = true;
        viewModel.SelectedVmax = 160;
        viewModel.SpeedSteps = DccSpeedSteps.Steps128;
        var maxStep = TrainControlDccSpeed.GetMaxSpeedStep(DccSpeedSteps.Steps128);
        viewModel.Speed = maxStep / 2;

        Assert.That(viewModel.Speed, Is.EqualTo(maxStep / 2));
        Assert.That(viewModel.SpeedKmh, Is.EqualTo(TrainControlDccSpeed.DefaultSpeedGaugeMaxKmh / 2));
    }

    private static TrainControlViewModel CreateTrainControlViewModel()
    {
        var runtimeMock = new Mock<IMobaRuntime>();
        runtimeMock.SetupGet(runtime => runtime.Current).Returns(MobaRuntimeSnapshot.Empty);

        var settingsServiceMock = new Mock<ISettingsService>();
        settingsServiceMock.Setup(service => service.GetSettings()).Returns(new AppSettings());
        settingsServiceMock.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);

        return new TrainControlViewModel(
            runtimeMock.Object,
            settingsServiceMock.Object,
            null,
            NullLogger<TrainControlViewModel>.Instance,
            null,
            new EventBus(NullLogger<EventBus>.Instance));
    }
}
