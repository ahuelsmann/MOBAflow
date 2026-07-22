// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using global::Moba.Backend.Interface;
using global::Moba.Backend.Service;
using global::Moba.Common.Events;
using global::Moba.Domain;

internal sealed class TimetableRuntimeProjectionServiceTests
{
    private static readonly DateTimeOffset OperatingTime = new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task ProjectAsync_Should_RecordArrivalForSingleOwnerAtTransitionTime()
    {
        // Arrange
        var journeyId = Guid.NewGuid();
        var stopId = Guid.NewGuid();
        var serviceDefinition = CreateService(journeyId, stopId, OperatingTime);
        var project = new Project { Id = Guid.NewGuid(), TimetableServices = [serviceDefinition] };
        var operations = new RecordingOperations();
        var projection = new TimetableRuntimeProjectionService(operations);

        // Act
        var result = await projection.ProjectAsync(project, CreateTransition(project, journeyId, stopId));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.RecordedArrivals, Is.EqualTo(1));
            Assert.That(result.SuppressedJourneyIds, Is.Empty);
            Assert.That(operations.RecordedServiceId, Is.EqualTo(serviceDefinition.Id));
            Assert.That(operations.RecordedCallId, Is.EqualTo(serviceDefinition.Calls[0].Id));
            Assert.That(operations.RecordedAt, Is.EqualTo(OperatingTime));
        });
    }

    [Test]
    public async Task ProjectAsync_Should_SuppressEquallyRankedJourneyOwnership()
    {
        // Arrange
        var journeyId = Guid.NewGuid();
        var stopId = Guid.NewGuid();
        var project = new Project
        {
            Id = Guid.NewGuid(),
            TimetableServices =
            [
                CreateService(journeyId, stopId, OperatingTime),
                CreateService(journeyId, stopId, OperatingTime)
            ]
        };
        var operations = new RecordingOperations();
        var projection = new TimetableRuntimeProjectionService(operations);

        // Act
        var result = await projection.ProjectAsync(project, CreateTransition(project, journeyId, stopId));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.RecordedArrivals, Is.Zero);
            Assert.That(result.SuppressedJourneyIds, Is.EqualTo(new[] { journeyId }));
            Assert.That(operations.RecordedCallId, Is.Null);
        });
    }

    [Test]
    public async Task ProjectAsync_Should_SelectScheduleRelevantSequentialOwner()
    {
        // Arrange
        var journeyId = Guid.NewGuid();
        var stopId = Guid.NewGuid();
        var current = CreateService(journeyId, stopId, OperatingTime);
        var later = CreateService(journeyId, stopId, OperatingTime.AddHours(2));
        var project = new Project { Id = Guid.NewGuid(), TimetableServices = [current, later] };
        var operations = new RecordingOperations();
        var projection = new TimetableRuntimeProjectionService(operations);

        // Act
        var result = await projection.ProjectAsync(project, CreateTransition(project, journeyId, stopId));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.RecordedArrivals, Is.EqualTo(1));
            Assert.That(operations.RecordedServiceId, Is.EqualTo(current.Id));
            Assert.That(result.SuppressedJourneyIds, Is.Empty);
        });
    }

    [Test]
    public async Task ProjectAsync_Should_PreferSingleRunningOwnerOverScheduleDistance()
    {
        // Arrange
        var journeyId = Guid.NewGuid();
        var stopId = Guid.NewGuid();
        var scheduled = CreateService(journeyId, stopId, OperatingTime);
        var running = CreateService(journeyId, stopId, OperatingTime.AddHours(2));
        var project = new Project { Id = Guid.NewGuid(), TimetableServices = [scheduled, running] };
        var operations = new RecordingOperations(new TimetableServiceState
        {
            ServiceId = running.Id,
            Status = TimetableServiceStatus.Running
        });
        var projection = new TimetableRuntimeProjectionService(operations);

        // Act
        await projection.ProjectAsync(project, CreateTransition(project, journeyId, stopId));

        // Assert
        Assert.That(operations.RecordedServiceId, Is.EqualTo(running.Id));
    }

    [Test]
    public async Task ProjectAsync_Should_IgnoreTransitionForAnotherProject()
    {
        // Arrange
        var journeyId = Guid.NewGuid();
        var stopId = Guid.NewGuid();
        var project = new Project
        {
            Id = Guid.NewGuid(),
            TimetableServices = [CreateService(journeyId, stopId, OperatingTime)]
        };
        var operations = new RecordingOperations();
        var projection = new TimetableRuntimeProjectionService(operations);
        var transition = new JourneyStationReachedEvent(Guid.NewGuid(), journeyId, Guid.NewGuid(), stopId, OperatingTime);

        // Act
        var result = await projection.ProjectAsync(project, transition);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.RecordedArrivals, Is.Zero);
            Assert.That(operations.RecordedCallId, Is.Null);
        });
    }

    private static TimetableService CreateService(Guid journeyId, Guid stopId, DateTimeOffset arrival) => new()
    {
        ServiceNumber = "S1",
        JourneyId = journeyId,
        ServiceDate = DateOnly.FromDateTime(arrival.Date),
        Calls =
        [
            new TimetableCall
            {
                JourneyStopId = stopId,
                ScheduledArrival = arrival,
                ScheduledDeparture = arrival.AddMinutes(5)
            }
        ]
    };

    private static JourneyStationReachedEvent CreateTransition(Project project, Guid journeyId, Guid stopId)
        => new(project.Id, journeyId, Guid.NewGuid(), stopId, OperatingTime);

    private sealed class RecordingOperations(params TimetableServiceState[] states) : ITimetableOperationsService
    {
        public Guid? RecordedServiceId { get; private set; }

        public Guid? RecordedCallId { get; private set; }

        public DateTimeOffset? RecordedAt { get; private set; }

        public Task<IReadOnlyList<TimetableServiceState>> GetStatesAsync(Guid projectId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TimetableServiceState>>(states);

        public Task<TimetableServiceState> RecordArrivalAsync(Guid projectId, Guid serviceId, Guid callId, DateTimeOffset? occurredAt = null, CancellationToken cancellationToken = default)
        {
            RecordedServiceId = serviceId;
            RecordedCallId = callId;
            RecordedAt = occurredAt;
            return Task.FromResult(new TimetableServiceState { ServiceId = serviceId, Status = TimetableServiceStatus.Running });
        }

        public Task<TimetableServiceState> HoldAsync(Guid projectId, Guid serviceId, DateTimeOffset heldUntil, string reason, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TimetableServiceState> ReleaseAsync(Guid projectId, Guid serviceId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TimetableServiceState> CancelAsync(Guid projectId, Guid serviceId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TimetableServiceState> CompleteAsync(Guid projectId, Guid serviceId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TimetableServiceState> ReassignTrainAsync(Guid projectId, Guid serviceId, Guid trainId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TimetableServiceState> ReassignJourneyAsync(Guid projectId, Guid serviceId, Guid journeyId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TimetableServiceState> ReassignPlatformAsync(Guid projectId, Guid serviceId, Guid callId, Guid platformId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TimetableServiceState> RecordDepartureAsync(Guid projectId, Guid serviceId, Guid callId, DateTimeOffset? occurredAt = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
