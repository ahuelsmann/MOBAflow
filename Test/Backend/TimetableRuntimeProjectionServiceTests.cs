// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using global::Moba.Backend.Interface;
using global::Moba.Backend.Service;
using global::Moba.Common.Runtime;
using global::Moba.Domain;

internal sealed class TimetableRuntimeProjectionServiceTests
{
    private static readonly DateTimeOffset OperatingTime = new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task ProjectAsync_Should_RecordArrivalForSingleLiveOwner()
    {
        // Arrange
        var journeyId = Guid.NewGuid();
        var stopId = Guid.NewGuid();
        var serviceDefinition = CreateService(journeyId, stopId);
        var project = new Project { TimetableServices = [serviceDefinition] };
        var operations = new RecordingOperations();
        var projection = new TimetableRuntimeProjectionService(operations);
        var snapshot = CreateSnapshot(journeyId, stopId);

        // Act
        var result = await projection.ProjectAsync(project, snapshot);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.RecordedArrivals, Is.EqualTo(1));
            Assert.That(result.SuppressedJourneyIds, Is.Empty);
            Assert.That(operations.RecordedCallId, Is.EqualTo(serviceDefinition.Calls[0].Id));
        });
    }

    [Test]
    public async Task ProjectAsync_Should_SuppressAmbiguousJourneyOwnership()
    {
        // Arrange
        var journeyId = Guid.NewGuid();
        var stopId = Guid.NewGuid();
        var project = new Project { TimetableServices = [CreateService(journeyId, stopId), CreateService(journeyId, stopId)] };
        var operations = new RecordingOperations();
        var projection = new TimetableRuntimeProjectionService(operations);

        // Act
        var result = await projection.ProjectAsync(project, CreateSnapshot(journeyId, stopId));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.RecordedArrivals, Is.Zero);
            Assert.That(result.SuppressedJourneyIds, Is.EqualTo(new[] { journeyId }));
            Assert.That(operations.RecordedCallId, Is.Null);
        });
    }

    private static TimetableService CreateService(Guid journeyId, Guid stopId) => new()
    {
        ServiceNumber = "S1",
        JourneyId = journeyId,
        ServiceDate = DateOnly.FromDateTime(OperatingTime.Date),
        Calls = [new TimetableCall { JourneyStopId = stopId }]
    };

    private static MobaRuntimeSnapshot CreateSnapshot(Guid journeyId, Guid stopId) => new()
    {
        JourneyStates = new Dictionary<Guid, JourneyRuntimeSnapshot>
        {
            [journeyId] = new JourneyRuntimeSnapshot
            {
                JourneyId = journeyId,
                CurrentStationId = stopId,
                LastFeedbackTime = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Local)
            }
        },
        CreatedAt = OperatingTime
    };

    private sealed class RecordingOperations : ITimetableOperationsService
    {
        public Guid? RecordedCallId { get; private set; }

        public Task<IReadOnlyList<TimetableServiceState>> GetStatesAsync(Guid projectId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TimetableServiceState>>([]);

        public Task<TimetableServiceState> RecordArrivalAsync(Guid projectId, Guid serviceId, Guid callId, DateTimeOffset? occurredAt = null, CancellationToken cancellationToken = default)
        {
            RecordedCallId = callId;
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
