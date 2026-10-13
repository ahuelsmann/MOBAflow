// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Common.Configuration;
using Moba.SharedUI.Interface;
using Moba.SharedUI.TrainControl;

using Moq;

/// <summary>
/// Persisted train-control preferences and the per-host locomotive selection.
/// </summary>
[TestFixture]
internal sealed class TrainControlSettingsStoreTests
{
    private AppSettings _settings = null!;
    private Mock<ISettingsService> _settingsService = null!;

    [SetUp]
    public void SetUp()
    {
        _settings = new AppSettings();
        _settingsService = new Mock<ISettingsService>();
        _settingsService.Setup(service => service.GetSettings()).Returns(_settings);
        _settingsService.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
    }

    [Test]
    public void RememberLocomotive_KeepsSeparateSelectionsPerHost()
    {
        var desktopLocomotive = Guid.NewGuid();
        var phoneLocomotive = Guid.NewGuid();
        var desktop = new TrainControlSettingsStore(_settingsService.Object, TrainControlHost.WinUi);
        var phone = new TrainControlSettingsStore(_settingsService.Object, TrainControlHost.Maui);

        desktop.RememberLocomotive(desktopLocomotive);
        phone.RememberLocomotive(phoneLocomotive);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(desktop.SavedLocomotiveId, Is.EqualTo(desktopLocomotive));
            Assert.That(phone.SavedLocomotiveId, Is.EqualTo(phoneLocomotive));
            Assert.That(_settings.WinUiTrainControlHost.SelectedLocomotiveFromProjectId, Is.EqualTo(desktopLocomotive));
        }

        _settingsService.Verify(service => service.SaveSettingsAsync(It.IsAny<AppSettings>()), Times.Never);
    }

    [Test]
    public async Task SaveAsync_WritesRampStepsAndTheSelectionWhenGiven()
    {
        var store = new TrainControlSettingsStore(_settingsService.Object, TrainControlHost.Maui);
        var locomotive = Guid.NewGuid();
        store.RememberLocomotive(locomotive);

        await store.SaveAsync(7.9, 250, DccSpeedSteps.Steps28, selectedLocomotiveId: null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_settings.TrainControl.SpeedRampStepSize, Is.EqualTo(7));
            Assert.That(_settings.TrainControl.SpeedRampIntervalMs, Is.EqualTo(250));
            Assert.That(_settings.TrainControl.SpeedSteps, Is.EqualTo(DccSpeedSteps.Steps28));
            Assert.That(store.SavedLocomotiveId, Is.EqualTo(locomotive));
        }

        _settingsService.Verify(service => service.SaveSettingsAsync(_settings), Times.Once);
    }

    [Test]
    public async Task SaveSeriesAsync_WritesSeriesAndVmax()
    {
        var store = new TrainControlSettingsStore(_settingsService.Object, TrainControlHost.WinUi);

        await store.SaveSeriesAsync("BR 103", 200).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(store.Preferences.SelectedLocoSeries, Is.EqualTo("BR 103"));
            Assert.That(store.Preferences.SelectedVmax, Is.EqualTo(200));
        }
    }

    [Test]
    public void Saves_LogAFailureInsteadOfThrowing()
    {
        _settingsService
            .Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>()))
            .ThrowsAsync(new IOException("Expected test failure"));
        var store = new TrainControlSettingsStore(_settingsService.Object, TrainControlHost.WinUi);

        Assert.That(
            async () =>
            {
                await store.SaveAsync(5, 100, DccSpeedSteps.Steps128, Guid.NewGuid()).ConfigureAwait(false);
                await store.SaveSeriesAsync("ICE 3", 300).ConfigureAwait(false);
            },
            Throws.Nothing);
    }
}
