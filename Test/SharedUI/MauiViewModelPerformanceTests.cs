// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
//
// Manual Android profiling checklist (device/emulator) — record before/after each optimization phase:
// 1. Cold start: app launch until Counter tab is interactive (stopwatch).
// 2. Tab switch: Counter -> Control, time until slider responds.
// 3. Slider: drag 0->126, confirm no visible frame drops.
// 4. Z21 connected: switch away from Control tab, confirm no UI jank every 5s on Counter.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;

using Moq;

[TestFixture]
internal sealed class MauiViewModelPerformanceTests
{
    private readonly List<MauiViewModel> _createdViewModels = [];

    [TearDown]
    public void TearDown()
    {
        foreach (var viewModel in _createdViewModels)
        {
            viewModel.NotifyApplicationStopping();
        }

        _createdViewModels.Clear();
    }

    [Test]
    public void PauseHeavyUpdates_SkipsSignalBoxRefresh_OnRuntimeSnapshot()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(eventBus);
        viewModel.SetSignalBoxTabActive(true);
        var elementId = Guid.NewGuid();
        PublishSignalBoxSnapshot(eventBus, elementId);

        Assert.That(viewModel.SignalBoxElements, Has.Count.EqualTo(1));

        viewModel.PauseHeavyUpdates();
        PublishSignalBoxSnapshot(eventBus, elementId, name: "Renamed");

        Assert.That(viewModel.SignalBoxElements[0].Name, Is.EqualTo("Signal"));
    }

    [Test]
    public void ResumeHeavyUpdates_AppliesPendingSignalBoxSnapshot()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(eventBus);
        var elementId = Guid.NewGuid();

        viewModel.PauseHeavyUpdates();
        PublishSignalBoxSnapshot(eventBus, elementId, name: "Signal A");

        viewModel.SetSignalBoxTabActive(true);
        viewModel.ResumeHeavyUpdates();

        Assert.That(viewModel.SignalBoxElements, Has.Count.EqualTo(1));
        Assert.That(viewModel.SignalBoxElements[0].Name, Is.EqualTo("Signal A"));
    }

    [Test]
    public void RefreshSignalBoxElements_SameSnapshot_DoesNotReplaceCollectionItems()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(eventBus);
        viewModel.SetSignalBoxTabActive(true);
        var elementId = Guid.NewGuid();

        PublishSignalBoxSnapshot(eventBus, elementId, name: "Signal A");
        var firstReference = viewModel.SignalBoxElements[0];

        PublishSignalBoxSnapshot(eventBus, elementId, name: "Signal A");

        Assert.That(ReferenceEquals(firstReference, viewModel.SignalBoxElements[0]), Is.True);
    }

    [Test]
    public void RefreshSignalBoxElements_UpdatesExistingItem_WhenAspectChanges()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(eventBus);
        viewModel.SetSignalBoxTabActive(true);
        var elementId = Guid.NewGuid();

        PublishSignalBoxSnapshot(eventBus, elementId, aspect: SignalAspect.Hp0);
        var firstReference = viewModel.SignalBoxElements[0];

        PublishSignalBoxSnapshot(eventBus, elementId, aspect: SignalAspect.Ks1);

        Assert.Multiple(() =>
        {
            Assert.That(ReferenceEquals(firstReference, viewModel.SignalBoxElements[0]), Is.True);
            Assert.That(viewModel.SignalBoxElements[0].SelectedSignalAspect, Is.EqualTo(SignalAspect.Ks1));
        });
    }

    private static void PublishSignalBoxSnapshot(
        EventBus eventBus,
        Guid elementId,
        string name = "Signal",
        SignalAspect aspect = SignalAspect.Hp0)
    {
        eventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            SignalBoxElements =
            [
                new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = elementId,
                    Name = name,
                    Kind = SignalBoxElementKind.Signal,
                    X = 1,
                    Y = 2,
                    SignalAspect = aspect
                }
            ]
        }));
    }

    private MauiViewModel CreateViewModel(IEventBus eventBus)
    {
        var mobaRuntimeMock = new Mock<IMobaRuntime>();
        mobaRuntimeMock.SetupGet(runtime => runtime.Current).Returns(MobaRuntimeSnapshot.Empty);

        var uiDispatcherMock = new Mock<IUiDispatcher>();
        uiDispatcherMock
            .Setup(dispatcher => dispatcher.InvokeOnUi(It.IsAny<Action>()))
            .Callback<Action>(action => action());
        uiDispatcherMock
            .Setup(dispatcher => dispatcher.InvokeOnUiAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(asyncAction => asyncAction());

        var viewModel = new MauiViewModel(
            mobaRuntimeMock.Object,
            uiDispatcherMock.Object,
            new AppSettings(),
            CreateSettingsServiceMock().Object,
            new Mock<IRestDiscoveryService>().Object,
            new Mock<IZ21DiscoveryService>().Object,
            new Mock<IPhotoUploadService>().Object,
            new Mock<IPhotoCaptureService>().Object,
            new Mock<INetworkProfileChangeNotifier>().Object,
            NullLogger<MauiViewModel>.Instance,
            eventBus);

        _createdViewModels.Add(viewModel);
        return viewModel;
    }

    private static Mock<ISettingsService> CreateSettingsServiceMock()
    {
        var settingsServiceMock = new Mock<ISettingsService>();
        settingsServiceMock.Setup(service => service.GetSettings()).Returns(new AppSettings());
        settingsServiceMock.Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
        return settingsServiceMock;
    }
}