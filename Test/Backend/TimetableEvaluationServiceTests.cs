// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using global::Moba.Backend.Interface;
using global::Moba.Backend.Service;
using global::Moba.Domain;

internal sealed class TimetableEvaluationServiceTests
{
    private readonly TimetableEvaluationService _service = new();

    [Test]
    public void Evaluate_Should_AcceptAdjacentHalfOpenPlatformOccupancy()
    {
        // Arrange
        var project = CreateProject();
        var first = CreateService(project, new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero), TimeSpan.FromMinutes(10));
        var second = CreateService(project, new DateTimeOffset(2026, 8, 1, 10, 10, 0, TimeSpan.Zero), TimeSpan.FromMinutes(10));
        second.JourneyId = Guid.NewGuid();
        project.Journeys.Add(new Journey { Id = second.JourneyId, Name = "Second", Stations = [new Station { Id = second.Calls[0].JourneyStopId, Name = "Stop" }] });
        project.TimetableServices = [first, second];

        // Act
        var result = _service.Evaluate(project);

        // Assert
        Assert.That(result.Issues, Has.None.Matches<TimetableIssue>(issue => issue.Kind == TimetableIssueKind.PlatformConflict));
    }

    [Test]
    public void Evaluate_Should_ReportPlatformAndJourneyConflicts()
    {
        // Arrange
        var project = CreateProject();
        project.TimetableServices =
        [
            CreateService(project, new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero), TimeSpan.FromMinutes(15)),
            CreateService(project, new DateTimeOffset(2026, 8, 1, 10, 5, 0, TimeSpan.Zero), TimeSpan.FromMinutes(15))
        ];

        // Act
        var result = _service.Evaluate(project);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Issues, Has.Some.Matches<TimetableIssue>(issue => issue.Kind == TimetableIssueKind.PlatformConflict));
            Assert.That(result.Issues, Has.Some.Matches<TimetableIssue>(issue => issue.Kind == TimetableIssueKind.JourneyConflict));
        });
    }

    [Test]
    public void Evaluate_Should_ReportTrainTurnaroundBelowProjectMinimum()
    {
        // Arrange
        var project = CreateProject();
        var train = new Train { Id = Guid.NewGuid(), Name = "Regional set" };
        project.Trains.Add(train);
        project.TimetablePolicy.MinimumTurnaround = TimeSpan.FromMinutes(10);
        var first = CreateService(project, new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero), TimeSpan.FromMinutes(10));
        var second = CreateService(project, new DateTimeOffset(2026, 8, 1, 10, 15, 0, TimeSpan.Zero), TimeSpan.FromMinutes(10));
        first.TrainId = train.Id;
        second.TrainId = train.Id;
        second.JourneyId = Guid.NewGuid();
        project.Journeys.Add(new Journey { Id = second.JourneyId, Name = "Second", Stations = [new Station { Id = second.Calls[0].JourneyStopId, Name = "Stop" }] });
        project.TimetableServices = [first, second];

        // Act
        var result = _service.Evaluate(project);

        // Assert
        Assert.That(result.Issues, Has.Some.Matches<TimetableIssue>(issue => issue.Kind == TimetableIssueKind.TurnaroundConflict));
    }

    [Test]
    public void Evaluate_Should_ReportBrokenMasterDataReferences()
    {
        // Arrange
        var project = CreateProject();
        var service = CreateService(project, DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        service.Calls[0].PlatformId = Guid.NewGuid();
        project.TimetableServices.Add(service);

        // Act
        var result = _service.Evaluate(project);

        // Assert
        Assert.That(result.Issues, Has.Some.Matches<TimetableIssue>(issue => issue.Kind == TimetableIssueKind.InvalidReference));
    }

    [Test]
    public void Evaluate_Should_ReportContradictoryCallOrder()
    {
        // Arrange
        var project = CreateProject();
        var service = CreateService(project, new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero), TimeSpan.FromMinutes(10));
        service.Calls.Add(new TimetableCall
        {
            JourneyStopId = service.Calls[0].JourneyStopId,
            StationId = service.Calls[0].StationId,
            PlatformId = service.Calls[0].PlatformId,
            ScheduledArrival = service.Calls[0].ScheduledArrival.AddMinutes(5),
            ScheduledDeparture = service.Calls[0].ScheduledDeparture.AddMinutes(10)
        });
        project.TimetableServices.Add(service);

        // Act
        var result = _service.Evaluate(project);

        // Assert
        Assert.That(result.Issues, Has.Some.Matches<TimetableIssue>(issue => issue.Kind == TimetableIssueKind.InvalidTimeRange));
    }

    [Test]
    public void Evaluate_Should_RecalculatePlatformConflictAfterSessionReassignment()
    {
        // Arrange
        var project = CreateProject();
        var secondPlatform = new Platform { Id = Guid.NewGuid(), Name = "Platform 2", Number = 2 };
        project.Stations[0].Platforms.Add(secondPlatform);
        var first = CreateService(project, new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero), TimeSpan.FromMinutes(15));
        var second = CreateService(project, new DateTimeOffset(2026, 8, 1, 10, 5, 0, TimeSpan.Zero), TimeSpan.FromMinutes(15));
        project.TimetableServices = [first, second];
        var states = new[]
        {
            new TimetableServiceState
            {
                ServiceId = second.Id,
                Calls = [new TimetableCallState { CallId = second.Calls[0].Id, AssignedPlatformId = secondPlatform.Id }]
            }
        };

        // Act
        var result = _service.Evaluate(project, states);

        // Assert
        Assert.That(result.Issues, Has.None.Matches<TimetableIssue>(issue => issue.Kind == TimetableIssueKind.PlatformConflict));
    }

    [Test]
    public void Evaluate_Should_IgnoreCancelledServiceForResourceConflicts()
    {
        // Arrange
        var project = CreateProject();
        var first = CreateService(project, new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero), TimeSpan.FromMinutes(15));
        var cancelled = CreateService(project, new DateTimeOffset(2026, 8, 1, 10, 5, 0, TimeSpan.Zero), TimeSpan.FromMinutes(15));
        project.TimetableServices = [first, cancelled];
        var states = new[]
        {
            new TimetableServiceState
            {
                ServiceId = cancelled.Id,
                Status = TimetableServiceStatus.Cancelled
            }
        };

        // Act
        var result = _service.Evaluate(project, states);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Issues, Has.None.Matches<TimetableIssue>(issue => issue.Kind == TimetableIssueKind.PlatformConflict));
            Assert.That(result.Issues, Has.None.Matches<TimetableIssue>(issue => issue.Kind == TimetableIssueKind.JourneyConflict));
        });
    }

    [Test]
    public void Evaluate_Should_IgnoreCompletedServiceForResourceConflicts()
    {
        // Arrange
        var project = CreateProject();
        var first = CreateService(project, new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero), TimeSpan.FromMinutes(15));
        var completed = CreateService(project, new DateTimeOffset(2026, 8, 1, 10, 5, 0, TimeSpan.Zero), TimeSpan.FromMinutes(15));
        project.TimetableServices = [first, completed];
        var states = new[]
        {
            new TimetableServiceState
            {
                ServiceId = completed.Id,
                Status = TimetableServiceStatus.Completed
            }
        };

        // Act
        var result = _service.Evaluate(project, states);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Issues, Has.None.Matches<TimetableIssue>(issue => issue.Kind == TimetableIssueKind.PlatformConflict));
            Assert.That(result.Issues, Has.None.Matches<TimetableIssue>(issue => issue.Kind == TimetableIssueKind.JourneyConflict));
        });
    }

    [Test]
    public void Evaluate_Should_ReportDuplicateCallIdentifiers()
    {
        // Arrange
        var project = CreateProject();
        var first = CreateService(project, new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero), TimeSpan.FromMinutes(5));
        var second = CreateService(project, new DateTimeOffset(2026, 8, 1, 11, 0, 0, TimeSpan.Zero), TimeSpan.FromMinutes(5));
        second.Calls[0].Id = first.Calls[0].Id;
        project.TimetableServices = [first, second];

        // Act
        var result = _service.Evaluate(project);

        // Assert
        Assert.That(result.Issues.Count(issue => issue.Kind == TimetableIssueKind.DuplicateIdentifier), Is.EqualTo(2));
    }

    private static Project CreateProject()
    {
        var platform = new Platform { Id = Guid.NewGuid(), Number = 1 };
        return new Project
        {
            Name = "Test",
            Stations = [new Station { Id = Guid.NewGuid(), Name = "Central", Platforms = [platform] }],
            Journeys = [new Journey { Id = Guid.NewGuid(), Name = "Main", Stations = [new Station { Id = Guid.NewGuid(), Name = "Central stop" }] }]
        };
    }

    private static TimetableService CreateService(Project project, DateTimeOffset arrival, TimeSpan dwell)
    {
        var station = project.Stations[0];
        var journey = project.Journeys[0];
        return new TimetableService
        {
            ServiceNumber = Guid.NewGuid().ToString("N"),
            Name = "Test service",
            JourneyId = journey.Id,
            ServiceDate = DateOnly.FromDateTime(arrival.Date),
            Calls =
            [
                new TimetableCall
                {
                    JourneyStopId = journey.Stations[0].Id,
                    StationId = station.Id,
                    PlatformId = station.Platforms[0].Id,
                    ScheduledArrival = arrival,
                    ScheduledDeparture = arrival + dwell
                }
            ]
        };
    }
}
