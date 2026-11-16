// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Backend.Interface;
using Moba.Common.Events;
using Moba.Contracts.Runtime;
using Moba.Domain;
using Moba.SharedUI.ViewModel;

using Moq;

/// <summary>
/// Dispatcher decisions of the timetable board: which operations a service status allows, how reassignments cycle
/// from the effective (operated) value, how calls and rows are presented and how the board filters by time window and
/// operated train.
/// </summary>
internal sealed partial class TimetablePageViewModelTests
{
    private static readonly string[] PlatformNamesInArrivalOrder = ["7", "Platform 1", "Missing platform"];
    private static readonly string[] FourHourWindow = ["Recent", "Soon"];
    private static readonly string[] SixHourWindow = ["Later", "Recent", "Soon"];
    private static readonly string[] OneHourWindow = ["Recent"];
    private static readonly string[] OnlyLater = ["Later"];

    // Status (null = no operating state), Hold, Release, Cancel, Complete, Change (reassign), RecordArrival, Shift
    [TestCase(null, true, false, true, true, true, true, true)]
    [TestCase(TimetableServiceStatus.Held, true, true, true, true, true, true, true)]
    [TestCase(TimetableServiceStatus.Running, true, false, true, true, true, true, true)]
    [TestCase(TimetableServiceStatus.Completed, false, false, false, true, false, false, false)]
    [TestCase(TimetableServiceStatus.Cancelled, false, false, true, false, false, false, false)]
    public async Task ServiceCommands_FollowTheOperatingStatus(
        TimetableServiceStatus? status,
        bool canHold,
        bool canRelease,
        bool canCancel,
        bool canComplete,
        bool canChange,
        bool canRecordArrival,
        bool canShift)
    {
        var project = CreateProject();
        var service = project.TimetableServices.Single();
        var operations = status is { } current
            ? new RecordingOperations(new TimetableServiceState { ServiceId = service.Id, Status = current })
            : new RecordingOperations();
        using var context = CreateContext(project, operations);
        await context.ViewModel.RefreshAsync().ConfigureAwait(false);

        SelectFirstServiceAndCall(context.ViewModel);

        var viewModel = context.ViewModel;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.HoldSelectedServiceCommand.CanExecute(null), Is.EqualTo(canHold), "Hold");
            Assert.That(viewModel.ReleaseSelectedServiceCommand.CanExecute(null), Is.EqualTo(canRelease), "Release");
            Assert.That(viewModel.CancelSelectedServiceCommand.CanExecute(null), Is.EqualTo(canCancel), "Cancel");
            Assert.That(viewModel.CompleteSelectedServiceCommand.CanExecute(null), Is.EqualTo(canComplete), "Complete");
            Assert.That(viewModel.ReassignSelectedTrainCommand.CanExecute(null), Is.EqualTo(canChange), "Train");
            Assert.That(viewModel.ReassignSelectedJourneyCommand.CanExecute(null), Is.EqualTo(canChange), "Journey");
            Assert.That(viewModel.RecordArrivalCommand.CanExecute(null), Is.EqualTo(canRecordArrival), "Arrival");
            Assert.That(viewModel.ShiftSelectedCallLaterCommand.CanExecute(null), Is.EqualTo(canShift), "Shift");
            Assert.That(viewModel.ReassignSelectedPlatformCommand.CanExecute(null), Is.EqualTo(canShift), "Platform");
            Assert.That(viewModel.SaveDefinitionCommand.CanExecute(null), Is.True, "Save");
        }
    }

    [Test]
    public async Task ServiceCommands_WithoutSelection_AreUnavailable()
    {
        using var context = CreateContext(CreateProject(), new RecordingOperations());
        await context.ViewModel.RefreshAsync().ConfigureAwait(false);

        var viewModel = context.ViewModel;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.SaveDefinitionCommand.CanExecute(null), Is.False);
            Assert.That(viewModel.DeleteSelectedServiceCommand.CanExecute(null), Is.False);
            Assert.That(viewModel.HoldSelectedServiceCommand.CanExecute(null), Is.False);
            Assert.That(viewModel.CancelSelectedServiceCommand.CanExecute(null), Is.False);
            Assert.That(viewModel.CompleteSelectedServiceCommand.CanExecute(null), Is.False);
            Assert.That(viewModel.ReassignSelectedTrainCommand.CanExecute(null), Is.False);
            Assert.That(viewModel.RecordArrivalCommand.CanExecute(null), Is.False);
            Assert.That(viewModel.ShiftSelectedCallEarlierCommand.CanExecute(null), Is.False);
        }
    }

    [Test]
    public async Task ServiceWithoutCallSelection_OffersNoCallOperations()
    {
        using var context = CreateContext(CreateProject(), new RecordingOperations());
        await context.ViewModel.RefreshAsync().ConfigureAwait(false);

        context.ViewModel.SelectedService = context.ViewModel.Services.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.ViewModel.HoldSelectedServiceCommand.CanExecute(null), Is.True);
            Assert.That(context.ViewModel.RecordArrivalCommand.CanExecute(null), Is.False);
            Assert.That(context.ViewModel.ShiftSelectedCallEarlierCommand.CanExecute(null), Is.False);
            Assert.That(context.ViewModel.ReassignSelectedPlatformCommand.CanExecute(null), Is.False);
        }
    }

    [Test]
    public async Task Hold_HoldsTheServiceForFiveMinutesAsOperatorHold()
    {
        var project = CreateProject();
        var operations = new RecordingOperations();
        using var context = CreateContext(project, operations);
        await context.ViewModel.RefreshAsync().ConfigureAwait(false);
        SelectFirstServiceAndCall(context.ViewModel);

        await context.ViewModel.HoldSelectedServiceCommand.ExecuteAsync(null).ConfigureAwait(false);

        var state = (await operations.GetStatesAsync(project.Id).ConfigureAwait(false)).Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.HeldUntil, Is.EqualTo(OperatingTime.AddMinutes(5)));
            Assert.That(state.HoldReason, Is.EqualTo("Operator hold"));
            Assert.That(context.ViewModel.SelectedService?.Id, Is.EqualTo(project.TimetableServices[0].Id));
            Assert.That(context.ViewModel.ReleaseSelectedServiceCommand.CanExecute(null), Is.True);
        }
    }

    [Test]
    public async Task ReassignTrain_CyclesFromTheOperatedTrainAndWrapsAround()
    {
        var project = CreateProject();
        var service = project.TimetableServices.Single();
        var second = new Train { Id = Guid.NewGuid(), Name = "Second set" };
        var third = new Train { Id = Guid.NewGuid(), Name = "Third set" };
        project.Trains.Add(second);
        project.Trains.Add(third);
        var operations = new RecordingOperations(new TimetableServiceState { ServiceId = service.Id, AssignedTrainId = second.Id });
        using var context = CreateContext(project, operations);
        await context.ViewModel.RefreshAsync().ConfigureAwait(false);
        context.ViewModel.SelectedService = context.ViewModel.Services.Single();

        await context.ViewModel.ReassignSelectedTrainCommand.ExecuteAsync(null).ConfigureAwait(false);
        var afterFirst = context.ViewModel.SelectedService?.EffectiveTrainId;
        var firstStatus = context.ViewModel.StatusText;
        await context.ViewModel.ReassignSelectedTrainCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterFirst, Is.EqualTo(third.Id));
            Assert.That(firstStatus, Is.EqualTo("Train reassigned to Third set"));
            Assert.That(context.ViewModel.SelectedService?.EffectiveTrainId, Is.EqualTo(project.Trains[0].Id));
            Assert.That(context.ViewModel.SelectedService?.TrainName, Is.EqualTo("Regional set"));
        }
    }

    [Test]
    public async Task ReassignJourney_CyclesFromTheOperatedJourney()
    {
        var project = CreateProject();
        var service = project.TimetableServices.Single();
        var second = new Journey { Id = Guid.NewGuid(), Name = "Second line" };
        var third = new Journey { Id = Guid.NewGuid(), Name = "Third line" };
        project.Journeys.Add(second);
        project.Journeys.Add(third);
        var operations = new RecordingOperations(new TimetableServiceState { ServiceId = service.Id, AssignedJourneyId = second.Id });
        using var context = CreateContext(project, operations);
        await context.ViewModel.RefreshAsync().ConfigureAwait(false);
        context.ViewModel.SelectedService = context.ViewModel.Services.Single();

        await context.ViewModel.ReassignSelectedJourneyCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.ViewModel.SelectedService?.State?.AssignedJourneyId, Is.EqualTo(third.Id));
            Assert.That(context.ViewModel.SelectedService?.JourneyName, Is.EqualTo("Third line"));
            Assert.That(context.ViewModel.StatusText, Is.EqualTo("Journey reassigned to Third line"));
        }
    }

    [Test]
    public async Task ReassignPlatform_CyclesFromTheOperatedPlatform()
    {
        var project = CreateProject();
        var service = project.TimetableServices.Single();
        var call = service.Calls.Single();
        var second = new Platform { Name = "Platform 2", Number = 2 };
        var third = new Platform { Name = "Platform 3", Number = 3 };
        project.Stations[0].Platforms.Add(second);
        project.Stations[0].Platforms.Add(third);
        var operations = new RecordingOperations(new TimetableServiceState
        {
            ServiceId = service.Id,
            Calls = [new TimetableCallState { CallId = call.Id, AssignedPlatformId = second.Id }]
        });
        using var context = CreateContext(project, operations);
        await context.ViewModel.RefreshAsync().ConfigureAwait(false);
        SelectFirstServiceAndCall(context.ViewModel);
        var operatedPlatform = context.ViewModel.SelectedCall?.PlatformName;

        await context.ViewModel.ReassignSelectedPlatformCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(operatedPlatform, Is.EqualTo("Platform 2"));
            Assert.That(context.ViewModel.SelectedCall?.State?.AssignedPlatformId, Is.EqualTo(third.Id));
            Assert.That(context.ViewModel.SelectedCall?.PlatformName, Is.EqualTo("Platform 3"));
            Assert.That(context.ViewModel.StatusText, Is.EqualTo("Platform reassigned"));
        }
    }

    [Test]
    public async Task ReassignPlatform_AtAStationWithoutPlatforms_ChangesNothing()
    {
        var project = CreateProject();
        project.Stations[0].Platforms.Clear();
        var operations = new RecordingOperations();
        using var context = CreateContext(project, operations);
        await context.ViewModel.RefreshAsync().ConfigureAwait(false);
        SelectFirstServiceAndCall(context.ViewModel);

        await context.ViewModel.ReassignSelectedPlatformCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await operations.GetStatesAsync(project.Id).ConfigureAwait(false), Is.Empty);
            Assert.That(context.ViewModel.StatusText, Is.EqualTo("Timetable refreshed"));
        }
    }

    [Test]
    public async Task SelectedService_ListsItsCallsInArrivalOrderWithFallbackNames()
    {
        var project = CreateProject();
        var service = project.TimetableServices.Single();
        var station = project.Stations[0];
        var unnamedPlatform = new Platform { Name = null, Number = 7 };
        station.Platforms.Add(unnamedPlatform);
        service.Calls.Add(new TimetableCall
        {
            StationId = station.Id,
            PlatformId = unnamedPlatform.Id,
            ScheduledArrival = OperatingTime.AddMinutes(-30),
            ScheduledDeparture = OperatingTime.AddMinutes(-28)
        });
        service.Calls.Add(new TimetableCall
        {
            StationId = Guid.NewGuid(),
            PlatformId = Guid.NewGuid(),
            ScheduledArrival = OperatingTime.AddMinutes(30),
            ScheduledDeparture = OperatingTime.AddMinutes(32)
        });
        using var context = CreateContext(project, new RecordingOperations());
        await context.ViewModel.RefreshAsync().ConfigureAwait(false);

        context.ViewModel.SelectedService = context.ViewModel.Services.Single();

        var calls = context.ViewModel.Calls;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(calls.Select(call => call.Model.ScheduledArrival), Is.Ordered);
            Assert.That(calls.Select(call => call.PlatformName), Is.EqualTo(PlatformNamesInArrivalOrder));
            Assert.That(calls[2].StationName, Is.EqualTo("Missing station"));
            Assert.That(calls[0].ActualArrival, Is.EqualTo("-"));
        }
    }

    [Test]
    public async Task ServiceCountText_CountsVisibleAndAllServices()
    {
        var project = CreateProject();
        using var context = CreateContext(project, new RecordingOperations());
        await context.ViewModel.RefreshAsync().ConfigureAwait(false);
        var one = context.ViewModel.ServiceCountText;
        project.TimetableServices.Add(new TimetableService { ServiceNumber = "R200", Name = "Freight", ServiceDate = DateOnly.FromDateTime(OperatingTime.Date) });
        await context.ViewModel.RefreshAsync().ConfigureAwait(false);
        var two = context.ViewModel.ServiceCountText;

        context.ViewModel.FilterText = "R200";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(one, Is.EqualTo("1 service"));
            Assert.That(two, Is.EqualTo("2 services"));
            Assert.That(context.ViewModel.ServiceCountText, Is.EqualTo("1 of 2 services"));
            Assert.That(context.ViewModel.EmptyStateTitle, Is.EqualTo("No services yet"));
            Assert.That(context.ViewModel.EmptyStateDescription, Does.StartWith("Add your first service"));
        }
    }

    [Test]
    public async Task EmptyState_WithoutProject_AsksToSelectOne()
    {
        using var context = CreateContext(CreateProject(), new RecordingOperations());
        context.MainWindow.SelectedProject = null;
        await context.ViewModel.RefreshAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.ViewModel.EmptyStateTitle, Is.EqualTo("Select a project"));
            Assert.That(context.ViewModel.EmptyStateDescription, Does.StartWith("Select a project in Solution"));
            Assert.That(context.ViewModel.AddServiceCommand.CanExecute(null), Is.False);
        }
    }

    [Test]
    public async Task TimeWindow_ShowsServicesFromOneHourAgoToTheWindowEnd()
    {
        var project = CreateProject();
        project.TimetableServices.Clear();
        AddService(project, "Past", OperatingTime.AddHours(-3));
        AddService(project, "Recent", OperatingTime.AddMinutes(-50));
        AddService(project, "Soon", OperatingTime.AddHours(2));
        AddService(project, "Later", OperatingTime.AddHours(5));
        using var context = CreateContext(project, new RecordingOperations());
        await context.ViewModel.RefreshAsync().ConfigureAwait(false);

        context.ViewModel.SelectedFocus = "Time window";
        var fourHours = Names(context.ViewModel);
        context.ViewModel.TimeWindowHours = 6;
        var sixHours = Names(context.ViewModel);
        context.ViewModel.TimeWindowHours = double.NaN;
        var invalidWindow = Names(context.ViewModel);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.ViewModel.IsTimeWindowFilter, Is.True);
            Assert.That(fourHours, Is.EqualTo(FourHourWindow));
            Assert.That(sixHours, Is.EqualTo(SixHourWindow));
            Assert.That(invalidWindow, Is.EqualTo(OneHourWindow));
        }
    }

    [Test]
    public async Task TimeWindowHours_OutsideTheTimeWindowFocus_KeepsTheBoard()
    {
        var project = CreateProject();
        project.TimetableServices.Clear();
        AddService(project, "Later", OperatingTime.AddHours(5));
        using var context = CreateContext(project, new RecordingOperations());
        await context.ViewModel.RefreshAsync().ConfigureAwait(false);

        context.ViewModel.TimeWindowHours = 1;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.ViewModel.IsTimeWindowFilter, Is.False);
            Assert.That(Names(context.ViewModel), Is.EqualTo(OnlyLater));
        }
    }

    [Test]
    public async Task TrainFocus_MatchesTheOperatedTrainNotTheScheduledOne()
    {
        var project = CreateProject();
        var service = project.TimetableServices.Single();
        var reserve = new Train { Id = Guid.NewGuid(), Name = "Reserve set" };
        project.Trains.Add(reserve);
        var operations = new RecordingOperations(new TimetableServiceState { ServiceId = service.Id, AssignedTrainId = reserve.Id });
        using var context = CreateContext(project, operations);
        await context.ViewModel.RefreshAsync().ConfigureAwait(false);
        context.ViewModel.SelectedFocus = "Train";

        context.ViewModel.FilterText = "regional";
        var scheduledTrain = context.ViewModel.Services.Count;
        context.ViewModel.FilterText = "reserve";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(scheduledTrain, Is.Zero);
            Assert.That(context.ViewModel.Services, Has.Count.EqualTo(1));
            Assert.That(context.ViewModel.SearchPlaceholder, Is.EqualTo("Train name"));
        }
    }

    [Test]
    public async Task StationReached_ForAnotherProject_IsNotProjected()
    {
        var project = CreateProject();
        var projection = new RecordingProjectionService();
        using var context = CreateContext(project, new RecordingOperations(), projection: projection);
        await context.ViewModel.RefreshAsync().ConfigureAwait(false);

        context.EventBus.Publish(new JourneyStationReachedEvent(
            Guid.NewGuid(),
            project.Journeys[0].Id,
            Guid.NewGuid(),
            project.Journeys[0].Stations[0].Id,
            OperatingTime));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(projection.CallCount, Is.Zero);
            Assert.That(context.ViewModel.StatusText, Is.EqualTo("Timetable refreshed"));
        }
    }

    [Test]
    public async Task StationReached_WhenProjectionFails_ReportsTheFailure()
    {
        var project = CreateProject();
        var projection = new Mock<ITimetableRuntimeProjectionService>();
        projection.Setup(service => service.ProjectAsync(It.IsAny<Project>(), It.IsAny<JourneyStationReachedEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("store"));
        using var context = CreateContext(project, new RecordingOperations(), projectionService: projection.Object);
        await context.ViewModel.RefreshAsync().ConfigureAwait(false);

        context.EventBus.Publish(new JourneyStationReachedEvent(
            project.Id,
            project.Journeys[0].Id,
            Guid.NewGuid(),
            project.Journeys[0].Stations[0].Id,
            OperatingTime));

        Assert.That(context.ViewModel.StatusText, Is.EqualTo("Live timetable update failed"));
    }

    [Test]
    public async Task AddService_NumbersItAfterTheExistingServicesAndUsesTheFirstTrain()
    {
        var project = CreateProject();
        using var context = CreateContext(project, new RecordingOperations());
        await context.ViewModel.RefreshAsync().ConfigureAwait(false);

        await context.ViewModel.AddServiceCommand.ExecuteAsync(null).ConfigureAwait(false);

        var added = project.TimetableServices[^1];
        var call = added.Calls.Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(added.ServiceNumber, Is.EqualTo("S002"));
            Assert.That(added.TrainId, Is.EqualTo(project.Trains[0].Id));
            Assert.That(added.JourneyId, Is.EqualTo(project.Journeys[0].Id));
            Assert.That(call.StationId, Is.EqualTo(project.Stations[0].Id));
            Assert.That(call.PlatformId, Is.EqualTo(project.Stations[0].Platforms[0].Id));
            Assert.That(call.ScheduledArrival, Is.GreaterThan(OperatingTime).And.LessThanOrEqualTo(OperatingTime.AddHours(1)));
            Assert.That(call.ScheduledArrival.Minute, Is.Zero);
            Assert.That(call.ScheduledDeparture, Is.EqualTo(call.ScheduledArrival.AddMinutes(2)));
            Assert.That(context.ViewModel.SelectedService?.Id, Is.EqualTo(added.Id));
            Assert.That(context.ViewModel.StatusText, Is.EqualTo("Service added"));
        }
    }

    [Test]
    public async Task AddService_SkipsJourneysWithoutStopsAndStationsWithoutPlatforms()
    {
        var project = CreateProject();
        project.Journeys.Insert(0, new Journey { Id = Guid.NewGuid(), Name = "Empty line" });
        project.Stations.Insert(0, new Station { Id = Guid.NewGuid(), Name = "Halt" });
        project.Trains.Clear();
        using var context = CreateContext(project, new RecordingOperations());
        await context.ViewModel.RefreshAsync().ConfigureAwait(false);

        await context.ViewModel.AddServiceCommand.ExecuteAsync(null).ConfigureAwait(false);

        var added = project.TimetableServices[^1];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(added.JourneyId, Is.EqualTo(project.Journeys[1].Id));
            Assert.That(added.Calls.Single().StationId, Is.EqualTo(project.Stations[1].Id));
            Assert.That(added.TrainId, Is.Null);
        }
    }

    // Status, delay in minutes, expected board status
    [TestCase(null, 0, "Upcoming")]
    [TestCase(null, 3, "Delayed")]
    [TestCase(null, -2, "Upcoming")]
    [TestCase(TimetableServiceStatus.Running, 0, "Active")]
    [TestCase(TimetableServiceStatus.Running, 1, "Delayed")]
    [TestCase(TimetableServiceStatus.Held, 4, "Held")]
    [TestCase(TimetableServiceStatus.Completed, 4, "Completed")]
    [TestCase(TimetableServiceStatus.Cancelled, 4, "Cancelled")]
    public void Row_BoardStatus_CombinesStatusAndDelay(TimetableServiceStatus? status, int delayMinutes, string expected)
    {
        var project = CreateProject();
        var service = project.TimetableServices.Single();
        var state = status is { } current ? new TimetableServiceState { ServiceId = service.Id, Status = current } : null;

        var row = new TimetableServiceRowViewModel(service, state, new StubTimingService(TimeSpan.FromMinutes(delayMinutes)), MobaRuntimeSnapshot.Empty, project);

        Assert.That(row.BoardStatus, Is.EqualTo(expected));
    }

    [Test]
    public void Row_Delay_IsTheLargestCallDelay()
    {
        var project = CreateProject();
        var service = project.TimetableServices.Single();
        AddCall(service, project, OperatingTime.AddMinutes(20));
        AddCall(service, project, OperatingTime.AddMinutes(40));
        var delays = new Queue<TimeSpan>((TimeSpan[])[TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(7), TimeSpan.Zero]);
        var timing = new Mock<ITimetableTimingService>();
        timing.Setup(value => value.CalculateDelay(It.IsAny<TimetableCall>(), It.IsAny<TimetableCallState?>()))
            .Returns(() => delays.Dequeue());

        var row = new TimetableServiceRowViewModel(service, null, timing.Object, MobaRuntimeSnapshot.Empty, project);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(row.Delay, Is.EqualTo(TimeSpan.FromMinutes(7)));
            Assert.That(row.DelayText, Is.EqualTo("+7 min"));
        }
    }

    [Test]
    public void Row_EarlyServiceShowsANegativeDelayAndOnTimeServiceNone()
    {
        var project = CreateProject();
        var service = project.TimetableServices.Single();

        var early = new TimetableServiceRowViewModel(service, null, new StubTimingService(TimeSpan.FromMinutes(-3)), MobaRuntimeSnapshot.Empty, project);
        var onTime = new TimetableServiceRowViewModel(service, null, new StubTimingService(TimeSpan.Zero), MobaRuntimeSnapshot.Empty, project);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(early.DelayText, Is.EqualTo("-3 min"));
            Assert.That(onTime.DelayText, Is.EqualTo("On time"));
        }
    }

    [Test]
    public void Row_Schedule_SpansFromTheEarliestArrivalToTheLatestDeparture()
    {
        var project = CreateProject();
        var service = project.TimetableServices.Single();
        AddCall(service, project, OperatingTime.AddMinutes(-45));
        AddCall(service, project, OperatingTime.AddMinutes(30));

        var row = new TimetableServiceRowViewModel(service, null, new StubTimingService(TimeSpan.Zero), MobaRuntimeSnapshot.Empty, project);
        var empty = new TimetableServiceRowViewModel(new TimetableService(), null, new StubTimingService(TimeSpan.Zero), MobaRuntimeSnapshot.Empty, project);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(row.Schedule, Is.EqualTo("09:15 - 10:32"));
            Assert.That(empty.Schedule, Is.EqualTo("No calls"));
            Assert.That(empty.Delay, Is.EqualTo(TimeSpan.Zero));
        }
    }

    [Test]
    public void Row_Names_FollowTheOperatedAssignmentsAndReportMissingOnes()
    {
        var project = CreateProject();
        var service = project.TimetableServices.Single();
        var reserve = new Train { Id = Guid.NewGuid(), Name = "Reserve set" };
        project.Trains.Add(reserve);
        var assigned = new TimetableServiceState { ServiceId = service.Id, AssignedTrainId = reserve.Id };
        var timing = new StubTimingService(TimeSpan.Zero);

        var operated = new TimetableServiceRowViewModel(service, assigned, timing, MobaRuntimeSnapshot.Empty, project);
        var unassigned = new TimetableServiceRowViewModel(
            new TimetableService { JourneyId = Guid.NewGuid() }, null, timing, MobaRuntimeSnapshot.Empty, project);
        var missingTrain = new TimetableServiceRowViewModel(
            new TimetableService { TrainId = Guid.NewGuid() }, null, timing, MobaRuntimeSnapshot.Empty, project);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(operated.TrainName, Is.EqualTo("Reserve set"));
            Assert.That(operated.EffectiveTrainId, Is.EqualTo(reserve.Id));
            Assert.That(operated.JourneyName, Is.EqualTo("Main line"));
            Assert.That(unassigned.TrainName, Is.EqualTo("No train assigned"));
            Assert.That(unassigned.JourneyName, Is.EqualTo("Missing journey"));
            Assert.That(missingTrain.TrainName, Is.EqualTo("Missing train"));
        }
    }

    private static void AddService(Project project, string name, DateTimeOffset arrival) =>
        project.TimetableServices.Add(new TimetableService
        {
            ServiceNumber = name,
            Name = name,
            ServiceDate = DateOnly.FromDateTime(arrival.Date),
            Calls =
            [
                new TimetableCall
                {
                    StationId = project.Stations[0].Id,
                    PlatformId = project.Stations[0].Platforms[0].Id,
                    ScheduledArrival = arrival,
                    ScheduledDeparture = arrival.AddMinutes(2)
                }
            ]
        });

    private static void AddCall(TimetableService service, Project project, DateTimeOffset arrival) =>
        service.Calls.Add(new TimetableCall
        {
            StationId = project.Stations[0].Id,
            PlatformId = project.Stations[0].Platforms[0].Id,
            ScheduledArrival = arrival,
            ScheduledDeparture = arrival.AddMinutes(2)
        });

    private static string[] Names(TimetablePageViewModel viewModel) => [.. viewModel.Services.Select(service => service.Name)];
}
