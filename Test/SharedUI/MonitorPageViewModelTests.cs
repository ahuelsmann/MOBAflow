// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Events;
using Moba.Backend.Interface;
using Moba.Backend.Model;
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
/// Monitor page traffic projection: recorded traffic of the selected runtime, logged packets, connection status,
/// clearing, persisted panel expansion and detaching on dispose.
/// </summary>
[TestFixture]
internal sealed partial class MonitorPageViewModelTests : IDisposable
{
    private readonly List<MonitorPageViewModel> _viewModels = [];
    private EventBus _eventBus = null!;
    private Mock<IMobaRuntime> _runtime = null!;
    private Mock<ISettingsService> _persistence = null!;
    private Mock<IUiDispatcher> _dispatcher = null!;
    private AppSettings _settings = null!;
    private SolutionSession _session = null!;
    private MobaRuntimeSnapshot _snapshot = MobaRuntimeSnapshot.Empty;
    private Z21TrafficPacket[] _recordedPackets = [];

    [SetUp]
    public void SetUp()
    {
        _snapshot = MobaRuntimeSnapshot.Empty;
        _recordedPackets = [];
        _eventBus = new EventBus(NullLogger<EventBus>.Instance);
        _runtime = new Mock<IMobaRuntime>();
        _runtime.SetupGet(runtime => runtime.Current).Returns(() => _snapshot);
        _runtime.Setup(runtime => runtime.GetTrafficPackets()).Returns(() => _recordedPackets);
        _runtime.Setup(runtime => runtime.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _dispatcher = new Mock<IUiDispatcher>();
        _dispatcher.Setup(dispatcher => dispatcher.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        _dispatcher.Setup(dispatcher => dispatcher.InvokeOnUiAsync(It.IsAny<Func<Task>>())).Returns<Func<Task>>(action => action());
        _settings = new AppSettings();
        _persistence = new Mock<ISettingsService>();
        _persistence.Setup(service => service.GetSettings()).Returns(_settings);
        _persistence.Setup(service => service.SaveSettingsAsync(_settings)).Returns(Task.CompletedTask);
        _session = TestSolutionSessions.Create(new Solution { Projects = [CreateProject("192.168.0.111")] }, _dispatcher.Object, _runtime.Object);
    }

    [TearDown]
    public void Dispose()
    {
        foreach (var viewModel in _viewModels)
        {
            viewModel.Dispose();
        }

        _viewModels.Clear();
        GC.SuppressFinalize(this);
    }

    private MonitorPageViewModel CreateViewModel()
    {
        var viewModel = new MonitorPageViewModel(_runtime.Object, _runtime.Object, _eventBus, _session, _persistence.Object,
            _dispatcher.Object, NullLogger<MonitorPageViewModel>.Instance);
        _viewModels.Add(viewModel);
        return viewModel;
    }

    private static Project CreateProject(string z21IpAddress)
    {
        var project = new Project();
        project.Z21.IpAddress = z21IpAddress;
        return project;
    }

    private static MobaRuntimeSnapshot Connected(bool isConnected) => new()
    {
        IsConnected = isConnected,
        HasSeenSuccessfulConnection = isConnected,
        StatusText = isConnected ? "Connected" : "Disconnected"
    };

    [Test]
    public void Constructor_RequiresEveryDependency()
    {
        var logger = NullLogger<MonitorPageViewModel>.Instance;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => new MonitorPageViewModel(null!, _runtime.Object, _eventBus, _session, _persistence.Object, _dispatcher.Object, logger),
                Throws.ArgumentNullException);
            Assert.That(() => new MonitorPageViewModel(_runtime.Object, null!, _eventBus, _session, _persistence.Object, _dispatcher.Object, logger),
                Throws.ArgumentNullException);
            Assert.That(() => new MonitorPageViewModel(_runtime.Object, _runtime.Object, null!, _session, _persistence.Object, _dispatcher.Object, logger),
                Throws.ArgumentNullException);
            Assert.That(() => new MonitorPageViewModel(_runtime.Object, _runtime.Object, _eventBus, null!, _persistence.Object, _dispatcher.Object, logger),
                Throws.ArgumentNullException);
            Assert.That(() => new MonitorPageViewModel(_runtime.Object, _runtime.Object, _eventBus, _session, null!, _dispatcher.Object, logger),
                Throws.ArgumentNullException);
            Assert.That(() => new MonitorPageViewModel(_runtime.Object, _runtime.Object, _eventBus, _session, _persistence.Object, null!, logger),
                Throws.ArgumentNullException);
            Assert.That(() => new MonitorPageViewModel(_runtime.Object, _runtime.Object, _eventBus, _session, _persistence.Object, _dispatcher.Object, null!),
                Throws.ArgumentNullException);
        }
    }

    [Test]
    public void Construction_ShowsTheRecordedTrafficAndTheConnectionOfTheSelectedProject()
    {
        _recordedPackets = [new Z21TrafficPacket(), new Z21TrafficPacket()];
        _snapshot = Connected(true);

        var viewModel = CreateViewModel();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.TrafficPackets, Is.EqualTo(_recordedPackets).AsCollection);
            Assert.That(viewModel.TrafficCount, Is.EqualTo(2));
            Assert.That(viewModel.ConnectionStatus, Is.EqualTo("✅ Connected to 192.168.0.111"));
        }
    }

    [Test]
    public void SelectedRuntimeChange_ReloadsTheTrafficOfTheNewRuntime()
    {
        _recordedPackets = [new Z21TrafficPacket()];
        var viewModel = CreateViewModel();
        var counts = new List<int>();
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MonitorPageViewModel.TrafficCount)) counts.Add(viewModel.TrafficCount);
        };

        _recordedPackets = [new Z21TrafficPacket(), new Z21TrafficPacket(), new Z21TrafficPacket()];
        _eventBus.Publish(new SelectedRuntimeChangedEvent());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.TrafficPackets, Is.EqualTo(_recordedPackets).AsCollection);
            Assert.That(counts, Is.Not.Empty);
            Assert.That(counts[^1], Is.EqualTo(3));
        }
    }

    [Test]
    public void LoggedPackets_AreShownNewestFirstAndCappedAtOneHundred()
    {
        var viewModel = CreateViewModel();
        var packets = Enumerable.Range(0, 101).Select(_ => new Z21TrafficPacket()).ToList();

        foreach (var packet in packets)
        {
            _eventBus.Publish(new Z21TrafficPacketLoggedEvent(packet));
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.TrafficPackets, Has.Count.EqualTo(100));
            Assert.That(viewModel.TrafficCount, Is.EqualTo(100));
            Assert.That(viewModel.TrafficPackets[0], Is.SameAs(packets[^1]));
            Assert.That(viewModel.TrafficPackets[^1], Is.SameAs(packets[1]), "The oldest packet is dropped.");
        }
    }

    [Test]
    public void ConnectionStatus_FollowsTheSnapshotAndTheSelectedProject()
    {
        var viewModel = CreateViewModel();
        var notifications = 0;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MonitorPageViewModel.ConnectionStatus)) notifications++;
        };
        Assert.That(viewModel.ConnectionStatus, Is.EqualTo("❌ Not connected"));

        _eventBus.Publish(new RuntimeSnapshotChangedEvent(Connected(true)));
        Assert.That(viewModel.ConnectionStatus, Is.EqualTo("✅ Connected to 192.168.0.111"));
        _eventBus.Publish(new RuntimeSnapshotChangedEvent(Connected(true)));
        Assert.That(notifications, Is.EqualTo(1), "An unchanged connection state is not notified again.");

        _session.SelectedProject = new ProjectViewModel(CreateProject("10.0.0.5"), TestViewModelServices.Default);
        Assert.That(viewModel.ConnectionStatus, Is.EqualTo("✅ Connected to 10.0.0.5"));

        _eventBus.Publish(new RuntimeSnapshotChangedEvent(Connected(false)));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.ConnectionStatus, Is.EqualTo("❌ Not connected"));
            Assert.That(notifications, Is.EqualTo(3));
        }
    }

    [Test]
    public void ClearTraffic_ClearsTheListAndTheRuntimeMonitor()
    {
        _recordedPackets = [new Z21TrafficPacket()];
        var viewModel = CreateViewModel();
        _eventBus.Publish(new Z21TrafficPacketLoggedEvent(new Z21TrafficPacket()));
        Assert.That(viewModel.TrafficPackets, Has.Count.EqualTo(2));

        viewModel.ClearTrafficCommand.Execute(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.TrafficPackets, Is.Empty);
            Assert.That(viewModel.TrafficCount, Is.Zero);
            _runtime.Verify(runtime => runtime.ClearTrafficMonitor(), Times.Once);
        }
    }

    /// <summary>Each flag reads and writes its own key: the flag under test differs from the other one.</summary>
    [TestCase(nameof(MonitorPageViewModel.IsMonitorTrafficExpanded))]
    [TestCase(nameof(MonitorPageViewModel.IsMonitorActivityLogExpanded))]
    public void Layout_FlagReadsAndWritesItsOwnKey(string flag)
    {
        using var assertions = Assert.EnterMultipleScope();
        var layout = _settings.Layout.MonitorPage;
        layout.IsTrafficExpanded = flag != nameof(MonitorPageViewModel.IsMonitorTrafficExpanded);
        layout.IsActivityLogExpanded = flag != nameof(MonitorPageViewModel.IsMonitorActivityLogExpanded);
        var viewModel = CreateViewModel();
        Assert.That(viewModel.IsMonitorTrafficExpanded, Is.EqualTo(layout.IsTrafficExpanded), "traffic read");
        Assert.That(viewModel.IsMonitorActivityLogExpanded, Is.EqualTo(layout.IsActivityLogExpanded), "activity log read");
        var notifications = new List<string?>();
        viewModel.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        // The cleared flag becomes true; the other one keeps its value.
        if (flag == nameof(MonitorPageViewModel.IsMonitorTrafficExpanded))
            viewModel.IsMonitorTrafficExpanded = true;
        else
            viewModel.IsMonitorActivityLogExpanded = true;

        Assert.That(layout.IsTrafficExpanded, Is.True, "traffic key");
        Assert.That(layout.IsActivityLogExpanded, Is.True, "activity log key");
        Assert.That(notifications, Is.EqualTo(new[] { flag }).AsCollection);
        _persistence.Verify(service => service.SaveSettingsAsync(_settings), Times.Once);
    }

    [Test]
    public void Layout_UnchangedValueIsNeitherNotifiedNorSaved()
    {
        var viewModel = CreateViewModel();
        var notifications = new List<string?>();
        viewModel.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        viewModel.IsMonitorTrafficExpanded = true;
        viewModel.IsMonitorActivityLogExpanded = true;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(notifications, Is.Empty);
            _persistence.Verify(service => service.SaveSettingsAsync(_settings), Times.Never);
        }
    }

    [Test]
    public void Dispose_UnsubscribesAndDropsLatePacketsAndStatusChanges()
    {
        var viewModel = CreateViewModel();
        _eventBus.Publish(new Z21TrafficPacketLoggedEvent(new Z21TrafficPacket()));
        Assert.That(viewModel.TrafficPackets, Has.Count.EqualTo(1));
        var notifications = new List<string?>();
        viewModel.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        viewModel.Dispose();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_eventBus.GetSubscriberCount<Z21TrafficPacketLoggedEvent>(), Is.Zero);
            Assert.That(_eventBus.GetSubscriberCount<SelectedRuntimeChangedEvent>(), Is.Zero);
            Assert.That(_eventBus.GetSubscriberCount<RuntimeSnapshotChangedEvent>(), Is.Zero);
        }
        _eventBus.Publish(new Z21TrafficPacketLoggedEvent(new Z21TrafficPacket()));
        _eventBus.Publish(new RuntimeSnapshotChangedEvent(Connected(true)));
        _session.SelectedProject = new ProjectViewModel(CreateProject("10.0.0.5"), TestViewModelServices.Default);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.TrafficPackets, Has.Count.EqualTo(1));
            Assert.That(viewModel.ConnectionStatus, Is.EqualTo("❌ Not connected"));
            Assert.That(notifications, Is.Empty);
        }
    }
}
