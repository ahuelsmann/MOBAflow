// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Interface;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Contracts.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;
using Moba.Test.Helpers;
using Moq;

/// <summary>
/// Signal Box page state: ordered editor changes through the project refresh and the command gateway, the runtime
/// signal-box projection onto the edited plan and the persisted panel expansion.
/// </summary>
[TestFixture]
internal sealed class SignalBoxPageViewModelTests : IDisposable
{
    private static readonly string[] UpdateThenAspect = ["update", "aspect"];

    private Project _project = null!;
    private SbSignal _signal = null!;
    private Mock<IMobaRuntime> _runtime = null!;
    private Mock<IRuntimeCommandGateway> _gateway = null!;
    private EventBus _eventBus = null!;
    private SolutionSession _session = null!;
    private AppSettings _settings = null!;
    private Mock<ISettingsService> _persistence = null!;
    private SignalBoxPageViewModel _page = null!;

    [SetUp]
    public void SetUp()
    {
        _signal = new SbSignal { SignalAspect = Enum.GetValues<SignalAspect>()[0] };
        _project = new Project { SignalBoxPlan = new SignalBoxPlan { Elements = [_signal] } };
        _runtime = new Mock<IMobaRuntime>();
        _runtime.SetupGet(value => value.Current).Returns(MobaRuntimeSnapshot.Empty);
        _gateway = new Mock<IRuntimeCommandGateway>();
        _eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(value => value.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        _session = TestSolutionSessions.Create(new Solution { Projects = [_project] }, dispatcher.Object, _runtime.Object);
        _settings = new AppSettings();
        _persistence = new Mock<ISettingsService>();
        _persistence.Setup(value => value.GetSettings()).Returns(_settings);
        _persistence.Setup(value => value.SaveSettingsAsync(_settings)).Returns(Task.CompletedTask);
        _page = CreatePage();
    }

    [TearDown]
    public void Dispose()
    {
        _page.Dispose();
        GC.SuppressFinalize(this);
    }

    private SignalBoxPageViewModel CreatePage() => new(_session, _runtime.Object, _gateway.Object, _runtime.Object, _eventBus,
        TestLayouts.Create(_persistence.Object));

    [Test]
    public void Constructor_RequiresEveryDependency()
    {
        var layout = TestLayouts.Create(_persistence.Object);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => new SignalBoxPageViewModel(null!, _runtime.Object, _gateway.Object, _runtime.Object, _eventBus, layout),
                Throws.ArgumentNullException);
            Assert.That(() => new SignalBoxPageViewModel(_session, null!, _gateway.Object, _runtime.Object, _eventBus, layout),
                Throws.ArgumentNullException);
            Assert.That(() => new SignalBoxPageViewModel(_session, _runtime.Object, null!, _runtime.Object, _eventBus, layout),
                Throws.ArgumentNullException);
            Assert.That(() => new SignalBoxPageViewModel(_session, _runtime.Object, _gateway.Object, null!, _eventBus, layout),
                Throws.ArgumentNullException);
            Assert.That(() => new SignalBoxPageViewModel(_session, _runtime.Object, _gateway.Object, _runtime.Object, null!, layout),
                Throws.ArgumentNullException);
            Assert.That(() => new SignalBoxPageViewModel(_session, _runtime.Object, _gateway.Object, _runtime.Object, _eventBus, null!),
                Throws.ArgumentNullException);
        }
    }

    [Test]
    public async Task ApplySignalBoxElementChange_WithPersistence_RefreshesRuntimeSnapshotBeforeSendingAspect()
    {
        var calls = new List<string>();
        _runtime.Setup(value => value.UpdateProjectAsync(_project, It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("update"))
            .Returns(Task.CompletedTask);
        _gateway.Setup(value => value.SetSignalAspectAsync(_signal.Id, _signal.SignalAspect, It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("aspect"))
            .Returns(Task.CompletedTask);
        _runtime.Invocations.Clear();

        await _page.ApplySignalBoxElementChangeAsync(_signal, requiresPersistence: true, requiresSignalCommand: true)
            .ConfigureAwait(false);

        Assert.That(calls, Is.EqualTo(UpdateThenAspect));
        _runtime.Verify(value => value.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never,
            "A signal-box change must not re-activate the project.");
    }

    [Test]
    public async Task ApplySignalBoxElementChange_AspectOnly_SendsAspectWithoutUpdatingRuntime()
    {
        _runtime.Invocations.Clear();

        await _page.ApplySignalBoxElementChangeAsync(_signal, requiresPersistence: false, requiresSignalCommand: true)
            .ConfigureAwait(false);

        _gateway.Verify(value => value.SetSignalAspectAsync(_signal.Id, _signal.SignalAspect, It.IsAny<CancellationToken>()), Times.Once);
        _runtime.Verify(value => value.UpdateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ApplySignalBoxElementChange_AspectWaitsForPendingConfigurationUpdate()
    {
        var update = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _runtime.Setup(value => value.UpdateProjectAsync(_project, It.IsAny<CancellationToken>())).Returns(update.Task);

        var configurationChange = _page.ApplySignalBoxElementChangeAsync(_signal, requiresPersistence: true, requiresSignalCommand: false);
        var aspectChange = _page.ApplySignalBoxElementChangeAsync(_signal, requiresPersistence: false, requiresSignalCommand: true);
        _gateway.Verify(value => value.SetSignalAspectAsync(It.IsAny<Guid>(), It.IsAny<SignalAspect>(), It.IsAny<CancellationToken>()), Times.Never);

        update.SetResult();
        await Task.WhenAll(configurationChange, aspectChange).ConfigureAwait(false);

        _gateway.Verify(value => value.SetSignalAspectAsync(_signal.Id, _signal.SignalAspect, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void RuntimeSnapshot_ProjectsSignalStateOntoTheSelectedPlanAndNotifiesThePage()
    {
        var target = Enum.GetValues<SignalAspect>()[^1];
        var notified = 0;
        _page.SignalBoxRuntimeStateChanged += (_, _) => notified++;

        _eventBus.Publish(new RuntimeSnapshotChangedEvent(SnapshotWithAspect(target)));
        _eventBus.Publish(new RuntimeSnapshotChangedEvent(SnapshotWithAspect(target)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_signal.SignalAspect, Is.EqualTo(target));
            Assert.That(notified, Is.EqualTo(1), "an unchanged plan is not announced again");
        }
    }

    [Test]
    public void Constructor_AppliesTheCurrentRuntimeSnapshot()
    {
        var target = Enum.GetValues<SignalAspect>()[^1];
        _runtime.SetupGet(value => value.Current).Returns(SnapshotWithAspect(target));

        using var page = CreatePage();

        Assert.That(_signal.SignalAspect, Is.EqualTo(target));
    }

    [Test]
    public void Dispose_StopsTheRuntimeProjectionAndSessionForwarding()
    {
        var notifications = new List<string?>();
        _page.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        var initial = _signal.SignalAspect;

        _page.Dispose();
        _eventBus.Publish(new RuntimeSnapshotChangedEvent(SnapshotWithAspect(Enum.GetValues<SignalAspect>()[^1])));
        _session.SelectedProject = null;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_signal.SignalAspect, Is.EqualTo(initial));
            Assert.That(notifications, Is.Empty);
        }
    }

    [Test]
    public void Page_ForwardsTheSessionSelectionAndSaveStatus()
    {
        var notifications = new List<string?>();
        _page.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        var loaded = 0;
        _page.SolutionLoaded += (_, _) => loaded++;

        _session.SelectedProject = null;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_page.SelectedProject, Is.Null);
            Assert.That(_page.SolutionSaveStatusText, Is.EqualTo(_session.SolutionSaveStatusText));
            Assert.That(notifications, Does.Contain(nameof(SignalBoxPageViewModel.SelectedProject)));
            Assert.That(loaded, Is.Zero);
        }
    }

    [Test]
    public void Layout_DefaultsAreExpandedAndFlagsWriteTheirOwnKeys()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_page.IsSignalBoxToolboxExpanded, Is.True);
            Assert.That(_page.IsSignalBoxPropertiesExpanded, Is.True);
        }

        _page.IsSignalBoxToolboxExpanded = false;
        _page.IsSignalBoxToolboxExpanded = false;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_settings.Layout.SignalBoxPage.IsToolboxExpanded, Is.False);
            Assert.That(_settings.Layout.SignalBoxPage.IsPropertiesExpanded, Is.True);
        }
        _persistence.Verify(value => value.SaveSettingsAsync(_settings), Times.Once);
    }

    [Test]
    public void OpenSignalBoxWorkbench_ExpandsAndPersistsTheProperties()
    {
        _settings.Layout.SignalBoxPage.IsPropertiesExpanded = false;
        using var page = CreatePage();
        Assert.That(page.IsSignalBoxPropertiesExpanded, Is.False);

        page.OpenSignalBoxWorkbenchCommand.Execute(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(page.IsSignalBoxPropertiesExpanded, Is.True);
            Assert.That(_settings.Layout.SignalBoxPage.IsPropertiesExpanded, Is.True);
        }
    }

    private MobaRuntimeSnapshot SnapshotWithAspect(SignalAspect aspect) => new()
    {
        SignalBoxElements = [new SignalBoxElementRuntimeSnapshot { ElementId = _signal.Id, SignalAspect = aspect }]
    };
}
