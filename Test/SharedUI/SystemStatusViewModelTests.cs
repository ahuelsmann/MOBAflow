// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moba.Test.Helpers;
using Moq;
using System.ComponentModel;

/// <summary>
/// Status bar and Overview status: speech health and provider switches, post-startup text, REST API status with
/// connected clients, every MOBAsmart synchronization row, the solution-loaded state and detaching on dispose.
/// </summary>
[TestFixture]
internal sealed partial class SystemStatusViewModelTests : IDisposable
{
    private readonly List<SystemStatusViewModel> _viewModels = [];
    private EventBus _eventBus = null!;
    private SettingsPageViewModel _settingsPage = null!;
    private Mock<ISolutionSession> _session = null!;
    private ProjectViewModel? _selectedProject;
    private string? _solutionPath;
    private bool _hasSolution;

    [SetUp]
    public void SetUp()
    {
        _eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(candidate => candidate.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        _settingsPage = TestSettingsPages.Create(new AppSettings(), dispatcher.Object);
        _selectedProject = new ProjectViewModel(new Project { Name = "Main" }, TestViewModelServices.Default);
        _solutionPath = null;
        _hasSolution = true;
        _session = new Mock<ISolutionSession>();
        _session.SetupGet(value => value.SelectedProject).Returns(() => _selectedProject);
        _session.SetupGet(value => value.CurrentSolutionPath).Returns(() => _solutionPath);
        _session.SetupGet(value => value.HasSolution).Returns(() => _hasSolution);
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

    [Test]
    public void ConstructorRejectsMissingDependencies()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.Throws<ArgumentNullException>(() => new SystemStatusViewModel(null!, _session.Object, _settingsPage));
            Assert.Throws<ArgumentNullException>(() => new SystemStatusViewModel(_eventBus, null!, _settingsPage));
            Assert.Throws<ArgumentNullException>(() => new SystemStatusViewModel(_eventBus, _session.Object, null!));
        }
    }

    [Test]
    public void DefaultsShowInitializingSpeechNoRestApiAndNoSynchronization()
    {
        var viewModel = CreateViewModel();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.SpeechHealthStatus, Is.EqualTo("Initializing..."));
            Assert.That(viewModel.SpeechStatusDisplayText, Is.EqualTo("Initializing..."));
            Assert.That(viewModel.SpeechHealthIcon, Is.EqualTo("\uE946"));
            Assert.That(viewModel.SpeechHealthColor, Is.EqualTo("SystemFillColorCautionBrush"));
            Assert.That(viewModel.IsPostStartupInitializationRunning, Is.False);
            Assert.That(viewModel.PostStartupStatusText, Is.Empty);
            Assert.That(viewModel.RestApiStatusText, Is.EqualTo("—"));
            Assert.That(viewModel.RestApiIsReachable, Is.False);
            Assert.That(viewModel.RestApiConnectedClients, Is.Empty);
            Assert.That(viewModel.RestApiConnectedClientsEmpty, Is.True);
            Assert.That(viewModel.RuntimeHubHostStatusText, Is.EqualTo("—"));
            Assert.That(viewModel.HubBroadcastStatusText, Is.EqualTo("—"));
            Assert.That(viewModel.RemoteSnapshotStatusText, Is.EqualTo("—"));
            Assert.That(viewModel.RuntimeSnapshotStatusText, Is.EqualTo("—"));
            Assert.That(viewModel.RestCacheStatusText, Is.EqualTo("—"));
            Assert.That(viewModel.SolutionSyncStatusText, Is.EqualTo("—"));
            Assert.That(viewModel.RuntimeHubHostIsHealthy | viewModel.HubBroadcastIsHealthy | viewModel.RemoteSnapshotIsHealthy
                | viewModel.RuntimeSnapshotIsHealthy | viewModel.RestCacheIsHealthy | viewModel.SolutionSyncIsHealthy, Is.False);
        }
    }

    [TestCase("Ready", "\uE930", "Green", "Speech: Ready")]
    [TestCase("✅ Ready", "\uE930", "Green", "Speech: Ready")]
    [TestCase("Not Configured", "\uE7BA", "SystemFillColorCautionBrush", "Speech: Not Configured")]
    [TestCase("❌ Failed: no voice", "\uE711", "Red", "Speech: Failed: no voice")]
    [TestCase("⏳ Initializing...", "\uE946", "SystemFillColorCautionBrush", "Speech: Initializing...")]
    public void HealthStatusMapsToIconColorAndDisplayText(string status, string icon, string color, string displayText)
    {
        var viewModel = CreateViewModel();
        var notified = new List<string?>();
        viewModel.PropertyChanged += (_, e) => notified.Add(e.PropertyName);

        viewModel.UpdateHealthStatus(status);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.SpeechHealthIcon, Is.EqualTo(icon));
            Assert.That(viewModel.SpeechHealthColor, Is.EqualTo(color));
            Assert.That(viewModel.SpeechHealthStatus, Is.EqualTo(displayText));
            Assert.That(viewModel.SpeechStatusDisplayText, Is.EqualTo(displayText));
            Assert.That(notified, Does.Contain(nameof(SystemStatusViewModel.SpeechStatusDisplayText)));
        }
    }

    [TestCase("Ready")]
    [TestCase("Unavailable")]
    public void ProviderSwitchClearsThePreviousProvidersHealthResult(string previousStatus)
    {
        var viewModel = CreateViewModel();
        viewModel.UpdateHealthStatus(previousStatus);
        _settingsPage.SelectedSpeechProviderId = "piper";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.SpeechHealthStatus, Does.Contain("Initializing"));
            Assert.That(viewModel.SpeechHealthIcon, Is.EqualTo("\uE946"));
            Assert.That(viewModel.SpeechHealthColor, Is.EqualTo("SystemFillColorCautionBrush"));
        }
    }

    [Test]
    public void UnavailableSpeechIsShownAsACompletedWarningInsteadOfInitializing()
    {
        var viewModel = CreateViewModel();
        viewModel.UpdateHealthStatus("Unavailable");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.SpeechHealthIcon, Is.EqualTo("\uE7BA"));
            Assert.That(viewModel.SpeechHealthStatus, Is.EqualTo("Speech: Unavailable"));
        }
    }

    [Test]
    public void SpeechSelectionRefreshesTheDisplayTextOnceAndDetachesOnDispose()
    {
        var viewModel = CreateViewModel();
        viewModel.UpdateHealthStatus("Ready");
        var updates = 0;
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SystemStatusViewModel.SpeechStatusDisplayText)) updates++;
        };
        _settingsPage.SelectedSpeechProviderId = "piper";
        Assert.That(viewModel.SpeechStatusDisplayText, Is.EqualTo("Speech: Initializing..."));

        viewModel.Dispose();
        _settingsPage.SelectedSpeechProviderId = "system-speech";
        Assert.That(updates, Is.EqualTo(1));
    }

    [Test]
    public void PostStartupEventUsesTheDefaultTextWhileRunningAndClearsWhenDone()
    {
        var viewModel = CreateViewModel();

        _eventBus.Publish(new PostStartupStatusEvent(true, string.Empty));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.IsPostStartupInitializationRunning, Is.True);
            Assert.That(viewModel.PostStartupStatusText, Is.EqualTo("Initializing services..."));
        }

        _eventBus.Publish(new PostStartupStatusEvent(true, "Loading plugins"));
        Assert.That(viewModel.PostStartupStatusText, Is.EqualTo("Loading plugins"));

        _eventBus.Publish(new PostStartupStatusEvent(false, string.Empty));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.IsPostStartupInitializationRunning, Is.False);
            Assert.That(viewModel.PostStartupStatusText, Is.Empty);
        }
    }

    [Test]
    public void RestApiStatusEventProjectsTextReachabilityAndClients()
    {
        var viewModel = CreateViewModel();
        var clients = new List<RestApiClientInfo>
        {
            new() { ClientId = "a", DeviceName = "Phone" },
            new() { ClientId = "b", DeviceName = "Tablet" }
        };

        _eventBus.Publish(new RestApiStatusChangedEvent("Running on port 5001", true, clients));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.RestApiStatusText, Is.EqualTo("Running on port 5001"));
            Assert.That(viewModel.RestApiIsReachable, Is.True);
            Assert.That(viewModel.RestApiConnectedClients.Select(client => client.DeviceName), Is.EqualTo(["Phone", "Tablet"]));
            Assert.That(viewModel.RestApiConnectedClientsEmpty, Is.False);
        }

        _eventBus.Publish(new RestApiStatusChangedEvent("Not reachable", false));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.RestApiStatusText, Is.EqualTo("Not reachable"));
            Assert.That(viewModel.RestApiIsReachable, Is.False);
            Assert.That(viewModel.RestApiConnectedClients, Is.Empty);
            Assert.That(viewModel.RestApiConnectedClientsEmpty, Is.True);
        }
    }

    [Test]
    public void FreshSynchronizationDiagnosticsReportEveryRowHealthy()
    {
        var viewModel = CreateViewModel();
        var now = DateTimeOffset.UtcNow;

        _eventBus.Publish(new MobaflowSyncDiagnosticsChangedEvent(new MobaflowSyncDiagnostics
        {
            RestApiReachable = true,
            HostClientConnected = true,
            ServerHasHost = true,
            LastHubPushAt = now,
            LastHubPushSucceeded = true,
            RemoteClientCount = 2,
            SessionOperational = true,
            LocalSnapshotCreatedAt = now,
            LocalSignalBoxElementCount = 3,
            LocalLocomotiveFleetCount = 4,
            RestCacheAvailable = true,
            RestCacheUpdatedAt = now,
            RestCacheSignalBoxElementCount = 3,
            RestCacheLocomotiveFleetCount = 4,
            SolutionAvailable = true,
            SolutionUpdatedAt = now,
            SolutionActiveProjectName = "Main"
        }));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.RuntimeHubHostIsHealthy, Is.True);
            Assert.That(viewModel.RuntimeHubHostStatusText, Is.EqualTo("Connected"));
            Assert.That(viewModel.HubBroadcastIsHealthy, Is.True);
            Assert.That(viewModel.HubBroadcastStatusText, Is.EqualTo("Last push just now"));
            Assert.That(viewModel.RemoteSnapshotIsHealthy, Is.True);
            Assert.That(viewModel.RemoteSnapshotStatusText, Is.EqualTo("2 remote client(s), session active"));
            Assert.That(viewModel.RuntimeSnapshotIsHealthy, Is.True);
            Assert.That(viewModel.RuntimeSnapshotStatusText, Is.EqualTo("Updated just now, 3 signals, 4 locos"));
            Assert.That(viewModel.RestCacheIsHealthy, Is.True);
            Assert.That(viewModel.RestCacheStatusText, Is.EqualTo("Updated just now, 3 signals, 4 locos"));
            Assert.That(viewModel.SolutionSyncIsHealthy, Is.True);
            Assert.That(viewModel.SolutionSyncStatusText, Is.EqualTo("Updated just now, project Main"));
        }
    }

    [Test]
    public void StaleSynchronizationDiagnosticsWithAnUnreachableRestApiReportEveryRowUnhealthy()
    {
        _selectedProject = null;
        var viewModel = CreateViewModel();
        var now = DateTimeOffset.UtcNow;

        _eventBus.Publish(new MobaflowSyncDiagnosticsChangedEvent(new MobaflowSyncDiagnostics
        {
            RestApiReachable = false,
            HostClientConnected = false,
            ServerHasHost = false,
            LastHubPushAt = now.AddMinutes(-2),
            LastHubPushSucceeded = true,
            RemoteClientCount = 0,
            SessionOperational = true,
            LocalSnapshotCreatedAt = now.AddMinutes(-5),
            Z21Connected = true,
            HasActiveProject = true,
            RestCacheAvailable = true,
            RestCacheUpdatedAt = now.AddHours(-2),
            RestCacheSignalBoxElementCount = 1,
            RestCacheLocomotiveFleetCount = 1,
            SolutionAvailable = false
        }));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.RuntimeHubHostIsHealthy, Is.False);
            Assert.That(viewModel.RuntimeHubHostStatusText, Is.EqualTo("REST API unreachable"));
            Assert.That(viewModel.HubBroadcastIsHealthy, Is.False, "A push older than the freshness window is stale.");
            Assert.That(viewModel.HubBroadcastStatusText, Is.EqualTo("Last push 2 min ago"));
            Assert.That(viewModel.RemoteSnapshotIsHealthy, Is.False);
            Assert.That(viewModel.RemoteSnapshotStatusText, Is.EqualTo("No remote clients"));
            Assert.That(viewModel.RuntimeSnapshotIsHealthy, Is.False, "The session has no project, so the event's HasActiveProject is overridden.");
            Assert.That(viewModel.RuntimeSnapshotStatusText, Is.EqualTo("No recent snapshot"));
            Assert.That(viewModel.RestCacheIsHealthy, Is.False, "The cache is older than the local snapshot beyond the tolerance.");
            Assert.That(viewModel.RestCacheStatusText, Is.EqualTo("Updated 2 h ago, 1 signals, 1 locos"));
            Assert.That(viewModel.SolutionSyncIsHealthy, Is.False);
            Assert.That(viewModel.SolutionSyncStatusText, Is.EqualTo("No solution cached"));
        }
    }

    [Test]
    public void SynchronizationTextsExplainTheRemainingUnhealthyReasons()
    {
        var viewModel = CreateViewModel();
        var now = DateTimeOffset.UtcNow;

        _eventBus.Publish(new MobaflowSyncDiagnosticsChangedEvent(new MobaflowSyncDiagnostics
        {
            RestApiReachable = true,
            HostClientConnected = true,
            ServerHasHost = false,
            LastHubPushAt = now,
            LastHubPushSucceeded = false,
            RemoteClientCount = 1,
            SessionOperational = false,
            LocalSnapshotCreatedAt = now.AddMinutes(-10),
            Z21Connected = true,
            RestCacheAvailable = true,
            RestCacheUpdatedAt = null,
            SolutionAvailable = true,
            SolutionUpdatedAt = null
        }));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.RuntimeHubHostStatusText, Is.EqualTo("Host not registered on server"));
            Assert.That(viewModel.HubBroadcastStatusText, Is.EqualTo("Last push failed"));
            Assert.That(viewModel.RemoteSnapshotStatusText, Is.EqualTo("1 remote client(s), session inactive"));
            Assert.That(viewModel.RuntimeSnapshotIsHealthy, Is.True, "A connected Z21 with the session's selected project counts as healthy.");
            Assert.That(viewModel.RuntimeSnapshotStatusText, Does.StartWith("Updated 10 min ago"));
            Assert.That(viewModel.RestCacheIsHealthy, Is.False);
            Assert.That(viewModel.RestCacheStatusText, Is.EqualTo("Cache has no timestamp"));
            Assert.That(viewModel.SolutionSyncIsHealthy, Is.False);
            Assert.That(viewModel.SolutionSyncStatusText, Is.EqualTo("Solution cache has no timestamp"));
        }

        _eventBus.Publish(new MobaflowSyncDiagnosticsChangedEvent(new MobaflowSyncDiagnostics
        {
            RestApiReachable = true,
            HostClientConnected = false,
            LastHubPushAt = null,
            LocalSnapshotCreatedAt = now
        }));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.RuntimeHubHostStatusText, Is.EqualTo("Host client disconnected"));
            Assert.That(viewModel.HubBroadcastStatusText, Is.EqualTo("No snapshot push yet"));
            Assert.That(viewModel.RestCacheStatusText, Is.EqualTo("No snapshot cached"));
        }
    }

    [Test]
    public void SolutionLoadedStatusFollowsTheSessionsPathAndProject()
    {
        _hasSolution = false;
        _selectedProject = null;
        var viewModel = CreateViewModel();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.SolutionLoadedIsHealthy, Is.False);
            Assert.That(viewModel.SolutionLoadedStatusText, Is.EqualTo("No solution loaded"));
        }

        _hasSolution = true;
        _selectedProject = new ProjectViewModel(new Project { Name = "Main" }, TestViewModelServices.Default);
        RaiseSessionChange(nameof(ISolutionSession.SelectedProject));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.SolutionLoadedIsHealthy, Is.False);
            Assert.That(viewModel.SolutionLoadedStatusText, Is.EqualTo("In-memory only, no solution file loaded"));
        }

        _solutionPath = Path.Combine("layouts", "home.json");
        RaiseSessionChange(nameof(ISolutionSession.CurrentSolutionPath));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.SolutionLoadedIsHealthy, Is.True);
            Assert.That(viewModel.SolutionLoadedStatusText, Is.EqualTo("Loaded from home.json, project Main"));
        }

        _selectedProject = null;
        RaiseSessionChange(nameof(ISolutionSession.SelectedProject));
        Assert.That(viewModel.SolutionLoadedStatusText, Is.EqualTo("Loaded from home.json"));

        _solutionPath = Path.Combine("layouts", "club.json");
        RaiseSessionChange(nameof(ISolutionSession.HasUnsavedChanges));
        Assert.That(viewModel.SolutionLoadedStatusText, Is.EqualTo("Loaded from home.json"), "Other session changes do not recompute the row.");
    }

    [Test]
    public void DisposeDetachesTheEventsTheSettingsAndTheSession()
    {
        var viewModel = CreateViewModel();
        viewModel.UpdateHealthStatus("Ready");

        viewModel.Dispose();
        viewModel.Dispose();
        _eventBus.Publish(new RestApiStatusChangedEvent("Running on port 5001", true));
        _eventBus.Publish(new PostStartupStatusEvent(true, "Late"));
        _eventBus.Publish(new MobaflowSyncDiagnosticsChangedEvent(new MobaflowSyncDiagnostics { RestApiReachable = true, HostClientConnected = true, ServerHasHost = true }));
        _settingsPage.SelectedSpeechProviderId = "piper";
        _solutionPath = "late.json";
        RaiseSessionChange(nameof(ISolutionSession.CurrentSolutionPath));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_eventBus.GetSubscriberCount<RestApiStatusChangedEvent>(), Is.Zero);
            Assert.That(_eventBus.GetSubscriberCount<PostStartupStatusEvent>(), Is.Zero);
            Assert.That(_eventBus.GetSubscriberCount<MobaflowSyncDiagnosticsChangedEvent>(), Is.Zero);
            Assert.That(viewModel.RestApiStatusText, Is.EqualTo("—"));
            Assert.That(viewModel.PostStartupStatusText, Is.Empty);
            Assert.That(viewModel.RuntimeHubHostStatusText, Is.EqualTo("—"));
            Assert.That(viewModel.SpeechHealthStatus, Is.EqualTo("Speech: Ready"));
            Assert.That(viewModel.SolutionLoadedStatusText, Is.EqualTo("In-memory only, no solution file loaded"));
        }
    }

    private void RaiseSessionChange(string propertyName) =>
        _session.Raise(value => value.PropertyChanged += null, new PropertyChangedEventArgs(propertyName));

    private SystemStatusViewModel CreateViewModel()
    {
        var viewModel = new SystemStatusViewModel(_eventBus, _session.Object, _settingsPage);
        _viewModels.Add(viewModel);
        return viewModel;
    }
}
