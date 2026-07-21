// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;

using Moq;

[TestFixture]
internal sealed class TimetablePageViewModelTests
{
    private static readonly DateTimeOffset OperatingTime = new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task RefreshAsync_Should_PopulateBoardCallsAndIssues()
    {
        // Arrange
        var project = CreateProject();
        var service = project.TimetableServices.Single();
        var call = service.Calls.Single();
        var state = new TimetableServiceState
        {
            ServiceId = service.Id,
            Status = TimetableServiceStatus.Running,
            Calls =
            [
                new TimetableCallState
                {
                    CallId = call.Id,
                    ActualArrival = OperatingTime.AddMinutes(4),
                    ActualDeparture = OperatingTime.AddMinutes(6)
                }
            ]
        };
        var operations = new RecordingOperations(state);
        var issue = new TimetableIssue(TimetableIssueKind.PlatformConflict, service.Id, Guid.NewGuid(), "Platform overlap");
        using var context = CreateContext(project, operations, new TimetableEvaluationResult([issue]), TimeSpan.FromMinutes(4));

        // Act
        await context.ViewModel.RefreshAsync();
        context.ViewModel.SelectedService = context.ViewModel.Services.Single();

        // Assert
        var row = context.ViewModel.SelectedService;
        var callRow = context.ViewModel.Calls.Single();
        var issueRow = context.ViewModel.Issues.Single();
        Assert.Multiple(() =>
        {
            Assert.That(context.ViewModel.HasProject, Is.True);
            Assert.That(context.ViewModel.ValidationSummary, Is.EqualTo("1 validation issues or conflicts"));
            Assert.That(context.ViewModel.StatusText, Is.EqualTo("Timetable refreshed"));
            Assert.That(row.Id, Is.EqualTo(service.Id));
            Assert.That(row.EffectiveTrainId, Is.EqualTo(service.TrainId));
            Assert.That(row.ServiceDateText, Is.EqualTo("2026-08-01"));
            Assert.That(row.JourneyName, Is.EqualTo("Main line"));
            Assert.That(row.TrainName, Is.EqualTo("Regional set"));
            Assert.That(row.Status, Is.EqualTo("Running"));
            Assert.That(row.Delay, Is.EqualTo(TimeSpan.FromMinutes(4)));
            Assert.That(row.DelayText, Is.EqualTo("+4 min"));
            Assert.That(row.BoardStatus, Is.EqualTo("Delayed"));
            Assert.That(row.Schedule, Is.EqualTo("10:00 - 10:05"));
            Assert.That(row.ProgressText, Is.EqualTo("No live progress"));
            Assert.That(callRow.Id, Is.EqualTo(call.Id));
            Assert.That(callRow.StationName, Is.EqualTo("Central"));
            Assert.That(callRow.PlatformName, Is.EqualTo("Platform 1"));
            Assert.That(callRow.ScheduledArrival, Is.EqualTo("10:00"));
            Assert.That(callRow.ScheduledDeparture, Is.EqualTo("10:05"));
            Assert.That(callRow.ActualArrival, Is.EqualTo("10:04:00"));
            Assert.That(callRow.ActualDeparture, Is.EqualTo("10:06:00"));
            Assert.That(issueRow.Kind, Is.EqualTo("PlatformConflict"));
            Assert.That(issueRow.Message, Is.EqualTo("Platform overlap"));
            Assert.That(issueRow.Reference, Does.Contain(service.Id.ToString()));
        });

        row.ServiceNumber = "R 101";
        row.Name = "Renamed service";
        Assert.Multiple(() =>
        {
            Assert.That(service.ServiceNumber, Is.EqualTo("R 101"));
            Assert.That(service.Name, Is.EqualTo("Renamed service"));
        });
    }

    [Test]
    public async Task Filters_Should_FocusByTextStationTrainAndTimeWindow()
    {
        // Arrange
        var project = CreateProject();
        var secondStation = new Station
        {
            Id = Guid.NewGuid(),
            Name = "Harbor",
            Platforms = [new Platform { Id = Guid.NewGuid(), Name = "Harbor platform", Number = 2 }]
        };
        var secondTrain = new Train { Id = Guid.NewGuid(), Name = "Freight set" };
        var secondJourneyStop = new Station { Id = Guid.NewGuid(), Name = "Harbor stop" };
        var secondJourney = new Journey { Id = Guid.NewGuid(), Name = "Branch line", Stations = [secondJourneyStop] };
        project.Stations.Add(secondStation);
        project.Trains.Add(secondTrain);
        project.Journeys.Add(secondJourney);
        project.TimetableServices.Add(new TimetableService
        {
            ServiceNumber = "F200",
            Name = "Freight",
            JourneyId = secondJourney.Id,
            TrainId = secondTrain.Id,
            ServiceDate = DateOnly.FromDateTime(OperatingTime.Date),
            Calls =
            [
                new TimetableCall
                {
                    JourneyStopId = secondJourneyStop.Id,
                    StationId = secondStation.Id,
                    PlatformId = secondStation.Platforms[0].Id,
                    ScheduledArrival = OperatingTime.AddHours(8),
                    ScheduledDeparture = OperatingTime.AddHours(8).AddMinutes(5)
                }
            ]
        });
        using var context = CreateContext(project, new RecordingOperations(), now: OperatingTime);
        await context.ViewModel.RefreshAsync();

        // Act + Assert
        context.ViewModel.FilterText = "Express";
        Assert.That(context.ViewModel.Services.Select(row => row.ServiceNumber), Is.EqualTo(new[] { "R100" }));

        context.ViewModel.SelectedFocus = "Station";
        context.ViewModel.FilterText = "Harbor";
        Assert.That(context.ViewModel.Services.Select(row => row.ServiceNumber), Is.EqualTo(new[] { "F200" }));

        context.ViewModel.SelectedFocus = "Train";
        context.ViewModel.FilterText = "Regional";
        Assert.That(context.ViewModel.Services.Select(row => row.ServiceNumber), Is.EqualTo(new[] { "R100" }));

        context.ViewModel.SelectedFocus = "Time window";
        context.ViewModel.FilterText = string.Empty;
        context.ViewModel.TimeWindowHours = 2;
        Assert.That(context.ViewModel.Services.Select(row => row.ServiceNumber), Is.EqualTo(new[] { "R100" }));

        context.ViewModel.TimeWindowHours = double.NaN;
        Assert.That(context.ViewModel.Services.Select(row => row.ServiceNumber), Is.EqualTo(new[] { "R100" }));
    }

    [Test]
    public async Task AddAndDeleteCommands_Should_UpdateDefinitionCollection()
    {
        // Arrange
        var project = CreateProject();
        using var context = CreateContext(project, new RecordingOperations(), now: OperatingTime);

        // Act
        await context.ViewModel.AddServiceCommand.ExecuteAsync(null);
        var added = context.ViewModel.SelectedService;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(project.TimetableServices, Has.Count.EqualTo(2));
            Assert.That(added, Is.Not.Null);
            Assert.That(added!.ServiceNumber, Is.EqualTo("S002"));
            Assert.That(added.Name, Is.EqualTo("New service"));
            Assert.That(context.ViewModel.StatusText, Is.EqualTo("Service added"));
        });

        await context.ViewModel.DeleteSelectedServiceCommand.ExecuteAsync(null);
        Assert.Multiple(() =>
        {
            Assert.That(project.TimetableServices, Has.Count.EqualTo(1));
            Assert.That(context.ViewModel.StatusText, Is.EqualTo("Service deleted"));
        });
    }

    [Test]
    public async Task AddServiceCommand_Should_ExplainMissingPrerequisites()
    {
        // Arrange
        using var context = CreateContext(new Project { Name = "Empty" }, new RecordingOperations());

        // Act
        await context.ViewModel.AddServiceCommand.ExecuteAsync(null);

        // Assert
        Assert.That(context.ViewModel.StatusText, Is.EqualTo("Create a journey stop and a station platform before adding a service."));
    }

    [Test]
    public async Task DispatcherCommands_Should_ForwardAndRefreshOperatingDecisions()
    {
        // Arrange
        var project = CreateProject();
        var secondTrain = new Train { Id = Guid.NewGuid(), Name = "Reserve set" };
        var secondJourney = new Journey { Id = Guid.NewGuid(), Name = "Relief line", Stations = [new Station { Id = Guid.NewGuid(), Name = "Relief stop" }] };
        var secondPlatform = new Platform { Id = Guid.NewGuid(), Name = "Platform 2", Number = 2 };
        project.Trains.Add(secondTrain);
        project.Journeys.Add(secondJourney);
        project.Stations[0].Platforms.Add(secondPlatform);
        var operations = new RecordingOperations();
        using var context = CreateContext(project, operations, now: OperatingTime);
        await context.ViewModel.RefreshAsync();
        SelectFirstServiceAndCall(context.ViewModel);
        var originalArrival = context.ViewModel.SelectedCall!.Model.ScheduledArrival;

        // Act + Assert
        await context.ViewModel.HoldSelectedServiceCommand.ExecuteAsync(null);
        Assert.That(context.ViewModel.StatusText, Is.EqualTo("Service held for five minutes"));

        await context.ViewModel.ReleaseSelectedServiceCommand.ExecuteAsync(null);
        await context.ViewModel.ReassignSelectedTrainCommand.ExecuteAsync(null);
        SelectFirstServiceAndCall(context.ViewModel);
        await context.ViewModel.ReassignSelectedPlatformCommand.ExecuteAsync(null);
        SelectFirstServiceAndCall(context.ViewModel);
        Assert.Multiple(() =>
        {
            Assert.That(context.ViewModel.RecordArrivalCommand.CanExecute(null), Is.True);
            Assert.That(context.ViewModel.RecordDepartureCommand.CanExecute(null), Is.False);
        });
        await context.ViewModel.RecordArrivalCommand.ExecuteAsync(null);
        SelectFirstServiceAndCall(context.ViewModel);
        Assert.Multiple(() =>
        {
            Assert.That(context.ViewModel.RecordArrivalCommand.CanExecute(null), Is.False);
            Assert.That(context.ViewModel.RecordDepartureCommand.CanExecute(null), Is.True);
        });
        await context.ViewModel.RecordDepartureCommand.ExecuteAsync(null);
        SelectFirstServiceAndCall(context.ViewModel);
        Assert.That(context.ViewModel.RecordDepartureCommand.CanExecute(null), Is.False);
        await context.ViewModel.ShiftSelectedCallEarlierCommand.ExecuteAsync(null);
        Assert.That(context.ViewModel.SelectedCall!.Model.ScheduledArrival, Is.EqualTo(originalArrival.AddMinutes(-5)));
        await context.ViewModel.ShiftSelectedCallLaterCommand.ExecuteAsync(null);
        await context.ViewModel.ReassignSelectedJourneyCommand.ExecuteAsync(null);
        SelectFirstServiceAndCall(context.ViewModel);
        await context.ViewModel.SaveDefinitionCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(context.ViewModel.SelectedService!.State!.AssignedTrainId, Is.EqualTo(secondTrain.Id));
            Assert.That(context.ViewModel.SelectedService.State.AssignedJourneyId, Is.EqualTo(secondJourney.Id));
            Assert.That(context.ViewModel.SelectedCall!.State!.AssignedPlatformId, Is.EqualTo(secondPlatform.Id));
            Assert.That(context.ViewModel.SelectedCall.State.ActualArrival, Is.EqualTo(OperatingTime));
            Assert.That(context.ViewModel.SelectedCall.State.ActualDeparture, Is.EqualTo(OperatingTime));
            Assert.That(context.ViewModel.SelectedCall.Model.ScheduledArrival, Is.EqualTo(originalArrival));
            Assert.That(context.ViewModel.StatusText, Is.EqualTo("Timetable definition saved"));
        });

        await context.ViewModel.CompleteSelectedServiceCommand.ExecuteAsync(null);
        Assert.That(context.ViewModel.SelectedService!.Status, Is.EqualTo("Completed"));
    }

    [Test]
    public async Task CancelCommand_Should_ExposeTerminalStatus()
    {
        // Arrange
        var project = CreateProject();
        using var context = CreateContext(project, new RecordingOperations());
        await context.ViewModel.RefreshAsync();
        context.ViewModel.SelectedService = context.ViewModel.Services.Single();

        // Act
        await context.ViewModel.CancelSelectedServiceCommand.ExecuteAsync(null);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(context.ViewModel.SelectedService!.Status, Is.EqualTo("Cancelled"));
            Assert.That(context.ViewModel.SelectedService.BoardStatus, Is.EqualTo("Cancelled"));
            Assert.That(context.ViewModel.StatusText, Is.EqualTo("Service cancelled"));
        });
    }

    [Test]
    public async Task StationReachedEvent_Should_ProjectRefreshAndPreserveSelection()
    {
        // Arrange
        var project = CreateProject();
        var projection = new RecordingProjectionService
        {
            Result = new TimetableProjectionResult(0, [project.Journeys[0].Id])
        };
        using var context = CreateContext(project, new RecordingOperations(), projection: projection);
        await context.ViewModel.RefreshAsync();
        SelectFirstServiceAndCall(context.ViewModel);
        var selectedServiceId = context.ViewModel.SelectedService!.Id;
        var selectedCallId = context.ViewModel.SelectedCall!.Id;

        // Act
        context.EventBus.Publish(new JourneyStationReachedEvent(
            project.Id,
            project.Journeys[0].Id,
            Guid.NewGuid(),
            project.Journeys[0].Stations[0].Id,
            OperatingTime));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(projection.CallCount, Is.EqualTo(1));
            Assert.That(context.ViewModel.SelectedService!.Id, Is.EqualTo(selectedServiceId));
            Assert.That(context.ViewModel.SelectedCall!.Id, Is.EqualTo(selectedCallId));
            Assert.That(context.ViewModel.StatusText, Does.StartWith("Live projection suppressed"));
        });
    }

    [Test]
    public async Task RuntimeSnapshotEvent_Should_UpdateProgressWithoutProjecting()
    {
        // Arrange
        var project = CreateProject();
        var projection = new RecordingProjectionService();
        using var context = CreateContext(project, new RecordingOperations(), projection: projection);
        await context.ViewModel.RefreshAsync();

        // Act
        context.EventBus.Publish(new RuntimeSnapshotChangedEvent(new MobaRuntimeSnapshot
        {
            JourneyStates = new Dictionary<Guid, JourneyRuntimeSnapshot>
            {
                [project.Journeys[0].Id] = new JourneyRuntimeSnapshot
                {
                    JourneyId = project.Journeys[0].Id,
                    CurrentStationName = "Central stop",
                    CurrentFeedbackIndex = 2
                }
            }
        }));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(context.ViewModel.Services.Single().ProgressText, Is.EqualTo("Central stop (step 3)"));
            Assert.That(projection.CallCount, Is.Zero);
        });
    }

    [Test]
    public void Dispose_Should_UnsubscribeExactlyOnce()
    {
        // Arrange
        var project = CreateProject();
        var context = CreateContext(project, new RecordingOperations());
        var runtimeSubscriptionsBefore = context.EventBus.GetSubscriberCount<RuntimeSnapshotChangedEvent>();
        var stationSubscriptionsBefore = context.EventBus.GetSubscriberCount<JourneyStationReachedEvent>();

        // Act
        context.ViewModel.Dispose();
        context.ViewModel.Dispose();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(context.EventBus.GetSubscriberCount<RuntimeSnapshotChangedEvent>(), Is.EqualTo(runtimeSubscriptionsBefore - 1));
            Assert.That(context.EventBus.GetSubscriberCount<JourneyStationReachedEvent>(), Is.EqualTo(stationSubscriptionsBefore - 1));
        });
    }

    [Test]
    public async Task RefreshAsync_Should_ExplainMissingProject()
    {
        // Arrange
        using var context = CreateContext(CreateProject(), new RecordingOperations());
        context.MainWindow.SelectedProject = null;

        // Act
        await context.ViewModel.RefreshAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(context.ViewModel.HasProject, Is.False);
            Assert.That(context.ViewModel.Services, Is.Empty);
            Assert.That(context.ViewModel.ValidationSummary, Is.EqualTo("Select a project to view its timetable."));
        });
    }

    private static TimetableTestContext CreateContext(
        Project project,
        RecordingOperations operations,
        TimetableEvaluationResult? evaluationResult = null,
        TimeSpan? delay = null,
        DateTimeOffset? now = null,
        RecordingProjectionService? projection = null)
    {
        var runtime = new Mock<IMobaRuntime>();
        var dispatcher = new Mock<IUiDispatcher>();
        var eventBus = new EventBus(Mock.Of<ILogger<EventBus>>());
        dispatcher.Setup(value => value.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        runtime.Setup(value => value.Current).Returns(MobaRuntimeSnapshot.Empty);
        runtime.Setup(value => value.GetTrafficPackets()).Returns([]);

        var mainWindow = new MainWindowViewModel(
            new LayoutColumnWidthsViewModel(),
            runtime.Object,
            eventBus,
            dispatcher.Object,
            new AppSettings(),
            new Solution { Projects = [project] },
            new ActionExecutionContext { Z21 = Mock.Of<IZ21>() },
            Mock.Of<ILogger<MainWindowViewModel>>());
        var evaluation = new StubEvaluationService(evaluationResult ?? new TimetableEvaluationResult([]));
        var timing = new StubTimingService(delay ?? TimeSpan.Zero);
        var runtimeProjection = projection ?? new RecordingProjectionService();
        var viewModel = new TimetablePageViewModel(
            mainWindow,
            evaluation,
            operations,
            timing,
            runtimeProjection,
            eventBus,
            Mock.Of<ILogger<TimetablePageViewModel>>(),
            new FixedTimeProvider(now ?? OperatingTime));
        return new TimetableTestContext(viewModel, mainWindow, eventBus);
    }

    private static Project CreateProject()
    {
        var platform = new Platform { Id = Guid.NewGuid(), Name = "Platform 1", Number = 1 };
        var station = new Station { Id = Guid.NewGuid(), Name = "Central", Platforms = [platform] };
        var journeyStop = new Station { Id = Guid.NewGuid(), Name = "Central stop" };
        var journey = new Journey { Id = Guid.NewGuid(), Name = "Main line", Stations = [journeyStop] };
        var train = new Train { Id = Guid.NewGuid(), Name = "Regional set" };
        return new Project
        {
            Id = Guid.NewGuid(),
            Name = "Timetable test",
            Stations = [station],
            Journeys = [journey],
            Trains = [train],
            TimetableServices =
            [
                new TimetableService
                {
                    ServiceNumber = "R100",
                    Name = "Regional Express",
                    JourneyId = journey.Id,
                    TrainId = train.Id,
                    ServiceDate = DateOnly.FromDateTime(OperatingTime.Date),
                    Calls =
                    [
                        new TimetableCall
                        {
                            JourneyStopId = journeyStop.Id,
                            StationId = station.Id,
                            PlatformId = platform.Id,
                            ScheduledArrival = OperatingTime,
                            ScheduledDeparture = OperatingTime.AddMinutes(5)
                        }
                    ]
                }
            ]
        };
    }

    private static void SelectFirstServiceAndCall(TimetablePageViewModel viewModel)
    {
        viewModel.SelectedService = viewModel.Services.First();
        viewModel.SelectedCall = viewModel.Calls.First();
    }

    private sealed record TimetableTestContext(
        TimetablePageViewModel ViewModel,
        MainWindowViewModel MainWindow,
        EventBus EventBus) : IDisposable
    {
        public void Dispose() => ViewModel.Dispose();
    }

    private sealed class StubEvaluationService(TimetableEvaluationResult result) : ITimetableEvaluationService
    {
        public TimetableEvaluationResult Evaluate(Project project, IReadOnlyCollection<TimetableServiceState>? states = null)
        {
            _ = project;
            _ = states;
            return result;
        }
    }

    private sealed class StubTimingService(TimeSpan delay) : ITimetableTimingService
    {
        public TimeSpan CalculateDelay(TimetableCall call, TimetableCallState? state)
        {
            _ = call;
            _ = state;
            return delay;
        }
    }

    private sealed class RecordingProjectionService : ITimetableRuntimeProjectionService
    {
        public int CallCount { get; private set; }

        public TimetableProjectionResult Result { get; init; } = new(0, []);

        public Task<TimetableProjectionResult> ProjectAsync(Project project, JourneyStationReachedEvent transition, CancellationToken cancellationToken = default)
        {
            _ = project;
            _ = transition;
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            return Task.FromResult(Result);
        }
    }

    private sealed class RecordingOperations(params TimetableServiceState[] initialStates) : ITimetableOperationsService
    {
        private readonly List<TimetableServiceState> _states = [.. initialStates];

        public Task<IReadOnlyList<TimetableServiceState>> GetStatesAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            _ = projectId;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<TimetableServiceState>>(_states);
        }

        public Task<TimetableServiceState> HoldAsync(Guid projectId, Guid serviceId, DateTimeOffset heldUntil, string reason, CancellationToken cancellationToken = default)
            => UpdateAsync(projectId, serviceId, cancellationToken, state =>
            {
                state.Status = TimetableServiceStatus.Held;
                state.HeldUntil = heldUntil;
                state.HoldReason = reason;
            });

        public Task<TimetableServiceState> ReleaseAsync(Guid projectId, Guid serviceId, CancellationToken cancellationToken = default)
            => UpdateAsync(projectId, serviceId, cancellationToken, state =>
            {
                state.Status = TimetableServiceStatus.Scheduled;
                state.HeldUntil = null;
                state.HoldReason = null;
            });

        public Task<TimetableServiceState> CancelAsync(Guid projectId, Guid serviceId, CancellationToken cancellationToken = default)
            => UpdateAsync(projectId, serviceId, cancellationToken, state => state.Status = TimetableServiceStatus.Cancelled);

        public Task<TimetableServiceState> CompleteAsync(Guid projectId, Guid serviceId, CancellationToken cancellationToken = default)
            => UpdateAsync(projectId, serviceId, cancellationToken, state => state.Status = TimetableServiceStatus.Completed);

        public Task<TimetableServiceState> ReassignTrainAsync(Guid projectId, Guid serviceId, Guid trainId, CancellationToken cancellationToken = default)
            => UpdateAsync(projectId, serviceId, cancellationToken, state => state.AssignedTrainId = trainId);

        public Task<TimetableServiceState> ReassignJourneyAsync(Guid projectId, Guid serviceId, Guid journeyId, CancellationToken cancellationToken = default)
            => UpdateAsync(projectId, serviceId, cancellationToken, state => state.AssignedJourneyId = journeyId);

        public Task<TimetableServiceState> ReassignPlatformAsync(Guid projectId, Guid serviceId, Guid callId, Guid platformId, CancellationToken cancellationToken = default)
            => UpdateAsync(projectId, serviceId, cancellationToken, state => GetCallState(state, callId).AssignedPlatformId = platformId);

        public Task<TimetableServiceState> RecordArrivalAsync(Guid projectId, Guid serviceId, Guid callId, DateTimeOffset? occurredAt = null, CancellationToken cancellationToken = default)
            => UpdateAsync(projectId, serviceId, cancellationToken, state =>
            {
                state.Status = TimetableServiceStatus.Running;
                GetCallState(state, callId).ActualArrival = occurredAt ?? OperatingTime;
            });

        public Task<TimetableServiceState> RecordDepartureAsync(Guid projectId, Guid serviceId, Guid callId, DateTimeOffset? occurredAt = null, CancellationToken cancellationToken = default)
            => UpdateAsync(projectId, serviceId, cancellationToken, state =>
            {
                state.Status = TimetableServiceStatus.Running;
                GetCallState(state, callId).ActualDeparture = occurredAt ?? OperatingTime;
            });

        private Task<TimetableServiceState> UpdateAsync(
            Guid projectId,
            Guid serviceId,
            CancellationToken cancellationToken,
            Action<TimetableServiceState> update)
        {
            _ = projectId;
            cancellationToken.ThrowIfCancellationRequested();
            var state = _states.FirstOrDefault(candidate => candidate.ServiceId == serviceId);
            if (state is null)
            {
                state = new TimetableServiceState { ServiceId = serviceId };
                _states.Add(state);
            }

            update(state);
            return Task.FromResult(state);
        }

        private static TimetableCallState GetCallState(TimetableServiceState state, Guid callId)
        {
            var callState = state.Calls.FirstOrDefault(candidate => candidate.CallId == callId);
            if (callState is not null) return callState;
            callState = new TimetableCallState { CallId = callId };
            state.Calls.Add(callState);
            return callState;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
