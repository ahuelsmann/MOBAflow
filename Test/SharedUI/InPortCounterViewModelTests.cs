// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging;
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
/// Feedback-point counter projection: authoritative snapshots, feedback-point count and project changes,
/// counter settings persistence, the reset-all command and detaching on dispose.
/// </summary>
[TestFixture]
internal sealed partial class InPortCounterViewModelTests : IDisposable
{
    private readonly List<InPortCounterViewModel> _viewModels = [];
    private EventBus _eventBus = null!;
    private Mock<IMobaRuntime> _runtime = null!;
    private Mock<IRuntimeCommandGateway> _gateway = null!;
    private Mock<ISettingsService> _persistence = null!;
    private Mock<IUiDispatcher> _dispatcher = null!;
    private AppSettings _settings = null!;
    private SettingsPageViewModel _settingsPage = null!;
    private SolutionSession _session = null!;
    private MobaRuntimeSnapshot _snapshot = MobaRuntimeSnapshot.Empty;

    [SetUp]
    public void SetUp()
    {
        _snapshot = MobaRuntimeSnapshot.Empty;
        _eventBus = new EventBus(NullLogger<EventBus>.Instance);
        _runtime = new Mock<IMobaRuntime>();
        _runtime.SetupGet(runtime => runtime.Current).Returns(() => _snapshot);
        _runtime.Setup(runtime => runtime.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _gateway = new Mock<IRuntimeCommandGateway>();
        _gateway.Setup(gateway => gateway.ResetInPortCountersAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dispatcher = new Mock<IUiDispatcher>();
        _dispatcher.Setup(dispatcher => dispatcher.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        _settings = new AppSettings();
        _settings.Counter.CountOfFeedbackPoints = 7;
        _settingsPage = TestSettingsPages.Create(_settings, _dispatcher.Object);
        _persistence = new Mock<ISettingsService>();
        _persistence.Setup(service => service.GetSettings()).Returns(_settings);
        _persistence.Setup(service => service.SaveSettingsAsync(_settings)).Returns(Task.CompletedTask);
        _session = TestSolutionSessions.Create(new Solution { Projects = [new Project()] }, _dispatcher.Object, _runtime.Object);
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

    private InPortCounterViewModel CreateViewModel(ILogger<InPortCounterViewModel>? logger = null)
    {
        var viewModel = new InPortCounterViewModel(_session, _runtime.Object, _eventBus, _gateway.Object, _settingsPage,
            _persistence.Object, logger ?? NullLogger<InPortCounterViewModel>.Instance);
        _viewModels.Add(viewModel);
        return viewModel;
    }

    private void Publish(MobaRuntimeSnapshot snapshot)
    {
        _snapshot = snapshot;
        _eventBus.Publish(new RuntimeSnapshotChangedEvent(snapshot));
    }

    private static MobaRuntimeSnapshot Counters(params InPortCounterSnapshot[] counters) =>
        new() { InPortCounters = counters };

    [Test]
    public void Constructor_RequiresEveryDependency()
    {
        var logger = NullLogger<InPortCounterViewModel>.Instance;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => new InPortCounterViewModel(null!, _runtime.Object, _eventBus, _gateway.Object, _settingsPage, _persistence.Object, logger),
                Throws.ArgumentNullException);
            Assert.That(() => new InPortCounterViewModel(_session, null!, _eventBus, _gateway.Object, _settingsPage, _persistence.Object, logger),
                Throws.ArgumentNullException);
            Assert.That(() => new InPortCounterViewModel(_session, _runtime.Object, null!, _gateway.Object, _settingsPage, _persistence.Object, logger),
                Throws.ArgumentNullException);
            Assert.That(() => new InPortCounterViewModel(_session, _runtime.Object, _eventBus, null!, _settingsPage, _persistence.Object, logger),
                Throws.ArgumentNullException);
            Assert.That(() => new InPortCounterViewModel(_session, _runtime.Object, _eventBus, _gateway.Object, null!, _persistence.Object, logger),
                Throws.ArgumentNullException);
            Assert.That(() => new InPortCounterViewModel(_session, _runtime.Object, _eventBus, _gateway.Object, _settingsPage, null!, logger),
                Throws.ArgumentNullException);
            Assert.That(() => new InPortCounterViewModel(_session, _runtime.Object, _eventBus, _gateway.Object, _settingsPage, _persistence.Object, null!),
                Throws.ArgumentNullException);
        }
    }

    [Test]
    public void CounterSnapshot_PreservesUnsignedPrecisionAndTimingInDisplayedStatistics()
    {
        var feedbackTime = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);
        _snapshot = Counters(new InPortCounterSnapshot(7, ulong.MaxValue, feedbackTime, TimeSpan.FromSeconds(20)));

        var statistic = CreateViewModel().Statistics.Single(item => item.InPort == 7);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(statistic.Count, Is.EqualTo(ulong.MaxValue));
            Assert.That(statistic.LapCountFormatted, Does.StartWith("18446744073709551615/"));
            Assert.That(statistic.LastFeedbackTime, Is.EqualTo(feedbackTime.UtcDateTime));
            Assert.That(statistic.LastLapTime, Is.EqualTo(TimeSpan.FromSeconds(20)));
            Assert.That(statistic.HasReceivedFirstLap, Is.True);
        }
    }

    [Test]
    public void StatisticsReinitializationAndProjectSelection_KeepApplicationCounterValues()
    {
        _snapshot = Counters(new InPortCounterSnapshot(1, 42, null, null), new InPortCounterSnapshot(7, 19, null, null));
        var viewModel = CreateViewModel();
        var replacements = 0;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(InPortCounterViewModel.Statistics)) replacements++;
        };

        // A project change rebuilds the statistics from the configured count and keeps the runtime's counters.
        _settings.Counter.CountOfFeedbackPoints = 1;
        _session.SelectedProject = new ProjectViewModel(new Project { Name = "Another layout" }, TestViewModelServices.Default);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(replacements, Is.EqualTo(1));
            Assert.That(viewModel.Statistics.Single(item => item.InPort == 1).Count, Is.EqualTo(42));
            Assert.That(viewModel.Statistics.Any(item => item.InPort == 7), Is.False);
        }

        // Counter updates mutate the statistics in place: no collection change reaches a bound list.
        var collectionChanges = 0;
        viewModel.Statistics.CollectionChanged += (_, _) => collectionChanges++;
        Publish(Counters(new InPortCounterSnapshot(1, 43, null, null), new InPortCounterSnapshot(7, 19, null, null)));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(collectionChanges, Is.Zero);
            Assert.That(viewModel.Statistics.Single(item => item.InPort == 1).Count, Is.EqualTo(43));
        }

        _settings.Counter.CountOfFeedbackPoints = 7;
        viewModel.InitializeStatisticsFromFeedbackPoints();
        Assert.That(viewModel.Statistics.Single(item => item.InPort == 7).Count, Is.EqualTo(19));
        _gateway.Verify(gateway => gateway.ResetInPortCountersAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void ClearedFeedbackPointCountKeepsConfiguredInputs()
    {
        var viewModel = CreateViewModel();

        // A cleared NumberBox reports NaN; converting it would remove every saved counter.
        _settingsPage.CountOfFeedbackPoints = double.NaN;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_settings.Counter.CountOfFeedbackPoints, Is.EqualTo(7));
            Assert.That(_settingsPage.CountOfFeedbackPoints, Is.EqualTo(7));
            Assert.That(viewModel.Statistics, Has.Count.EqualTo(7));
        }
    }

    [Test]
    public void SettingsFeedbackCountUpdatesStatisticsAndDetachesOnDispose()
    {
        var viewModel = CreateViewModel();
        _settingsPage.CountOfFeedbackPoints = 3;
        Assert.That(viewModel.Statistics, Has.Count.EqualTo(3));

        viewModel.Dispose();
        _settingsPage.CountOfFeedbackPoints = 1;
        Assert.That(viewModel.Statistics, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task ResetCounters_IsAlwaysAvailableAndWaitsForAuthoritativeResetSnapshot()
    {
        var viewModel = CreateViewModel();
        Publish(Counters(new InPortCounterSnapshot(1, 18, null, null)));

        Assert.That(viewModel.ResetCountersCommand.CanExecute(null), Is.True);
        await viewModel.ResetCountersCommand.ExecuteAsync(null).ConfigureAwait(false);
        Assert.That(viewModel.Statistics.Single(item => item.InPort == 1).Count, Is.EqualTo(18));
        Publish(MobaRuntimeSnapshot.Empty);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.Statistics.Single(item => item.InPort == 1).Count, Is.Zero);
            Assert.That(viewModel.CounterCommandStatus, Is.EqualTo("InPort counters reset."));
        }
        _gateway.Verify(gateway => gateway.ResetInPortCountersAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ResetCounters_FailureBecomesTheCommandStatusAndIsLogged()
    {
        var logged = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var logger = CreateWarningLogger(logged);
        var failure = new InvalidOperationException("Z21 of the selected project is offline");
        _gateway.Setup(gateway => gateway.ResetInPortCountersAsync(It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        var viewModel = CreateViewModel(logger.Object);

        await viewModel.ResetCountersCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.CounterCommandStatus, Is.EqualTo(failure.Message));
            Assert.That(await logged.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false), Is.SameAs(failure));
        }
    }

    [Test]
    public void PersistenceError_FollowsTheRuntimeSnapshot()
    {
        var viewModel = CreateViewModel();
        Assert.That(viewModel.CounterPersistenceError, Is.Empty);

        Publish(new MobaRuntimeSnapshot { InPortCounterPersistenceError = "Counter file is read-only" });
        Assert.That(viewModel.CounterPersistenceError, Is.EqualTo("Counter file is read-only"));

        Publish(MobaRuntimeSnapshot.Empty);
        Assert.That(viewModel.CounterPersistenceError, Is.Empty);
    }

    [Test]
    public void CounterSettings_StartFromSavedValuesAndPersistEveryChange()
    {
        _settings.Counter.TargetLapCount = 4;
        _settings.Counter.UseTimerFilter = true;
        _settings.Counter.TimerIntervalSeconds = 2.5;
        var viewModel = CreateViewModel();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.GlobalTargetLapCount, Is.EqualTo(4));
            Assert.That(viewModel.UseTimerFilter, Is.True);
            Assert.That(viewModel.TimerIntervalSeconds, Is.EqualTo(2.5));
            Assert.That(viewModel.Statistics.Select(statistic => statistic.TargetLapCount), Is.All.EqualTo(4));
            _persistence.Verify(service => service.SaveSettingsAsync(_settings), Times.Never);
        }
        var notifications = new List<string?>();
        viewModel.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        viewModel.GlobalTargetLapCount = 6;
        viewModel.UseTimerFilter = false;
        viewModel.TimerIntervalSeconds = 9;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_settings.Counter.TargetLapCount, Is.EqualTo(6));
            Assert.That(_settings.Counter.UseTimerFilter, Is.False);
            Assert.That(_settings.Counter.TimerIntervalSeconds, Is.EqualTo(9));
            Assert.That(viewModel.Statistics.Select(statistic => statistic.TargetLapCount), Is.All.EqualTo(6));
            Assert.That(notifications, Is.EqualTo(new[]
            {
                nameof(InPortCounterViewModel.GlobalTargetLapCount),
                nameof(InPortCounterViewModel.UseTimerFilter),
                nameof(InPortCounterViewModel.TimerIntervalSeconds)
            }).AsCollection);
            _persistence.Verify(service => service.SaveSettingsAsync(_settings), Times.Exactly(3));
        }
    }

    [Test]
    public void CounterSettings_UnchangedValueIsNeitherNotifiedNorSaved()
    {
        var viewModel = CreateViewModel();
        var notifications = new List<string?>();
        viewModel.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        viewModel.GlobalTargetLapCount = _settings.Counter.TargetLapCount;
        viewModel.UseTimerFilter = _settings.Counter.UseTimerFilter;
        viewModel.TimerIntervalSeconds = _settings.Counter.TimerIntervalSeconds;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(notifications, Is.Empty);
            _persistence.Verify(service => service.SaveSettingsAsync(_settings), Times.Never);
        }
    }

    [Test]
    public async Task CounterSettings_PersistenceFailureIsLoggedAndKeepsTheNewState()
    {
        var logged = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var logger = CreateWarningLogger(logged);
        var failure = new IOException("settings file locked");
        _persistence.Setup(service => service.SaveSettingsAsync(_settings)).Returns(Task.FromException(failure));
        var viewModel = CreateViewModel(logger.Object);

        viewModel.GlobalTargetLapCount = 12;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.GlobalTargetLapCount, Is.EqualTo(12));
            Assert.That(_settings.Counter.TargetLapCount, Is.EqualTo(12));
            Assert.That(await logged.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false), Is.SameAs(failure));
        }
    }

    [Test]
    public void Dispose_DetachesTheRuntimeSettingsAndSessionSubscriptions()
    {
        var viewModel = CreateViewModel();
        Publish(Counters(new InPortCounterSnapshot(1, 5, null, null)));
        Assert.That(viewModel.Statistics.Single(item => item.InPort == 1).Count, Is.EqualTo(5));

        viewModel.Dispose();

        Assert.That(_eventBus.GetSubscriberCount<RuntimeSnapshotChangedEvent>(), Is.Zero);
        Publish(Counters(new InPortCounterSnapshot(1, 6, null, null)));
        _settings.Counter.CountOfFeedbackPoints = 2;
        _session.SelectedProject = new ProjectViewModel(new Project(), TestViewModelServices.Default);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.Statistics.Single(item => item.InPort == 1).Count, Is.EqualTo(5));
            Assert.That(viewModel.Statistics, Has.Count.EqualTo(7));
        }
    }

    private static Mock<ILogger<InPortCounterViewModel>> CreateWarningLogger(TaskCompletionSource<Exception?> logged)
    {
        var logger = new Mock<ILogger<InPortCounterViewModel>>();
        logger.Setup(value => value.IsEnabled(LogLevel.Warning)).Returns(true);
        logger.Setup(value => value.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation => logged.TrySetResult((Exception?)invocation.Arguments[3])));
        return logger;
    }
}
