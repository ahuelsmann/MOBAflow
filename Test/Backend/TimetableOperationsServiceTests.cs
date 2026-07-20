// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using global::Moba.Backend.Interface;
using global::Moba.Backend.Service;
using global::Moba.Domain;

internal sealed class TimetableOperationsServiceTests
{
    [Test]
    public async Task RecordArrivalAsync_Should_BeIdempotentAndPersistState()
    {
        // Arrange
        var store = new MemoryStore();
        var now = new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
        using var service = new TimetableOperationsService(store, new FixedTimeProvider(now));
        var projectId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var callId = Guid.NewGuid();

        // Act
        await service.RecordArrivalAsync(projectId, serviceId, callId);
        await service.RecordArrivalAsync(projectId, serviceId, callId, now.AddMinutes(1));
        var recovered = await service.GetStatesAsync(projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(recovered, Has.Count.EqualTo(1));
            Assert.That(recovered[0].Status, Is.EqualTo(TimetableServiceStatus.Running));
            Assert.That(recovered[0].Calls.Single().ActualArrival, Is.EqualTo(now));
        });
    }

    [Test]
    public async Task CancelAsync_Should_CreateTerminalIdempotentState()
    {
        // Arrange
        using var service = new TimetableOperationsService(new MemoryStore(), new FixedTimeProvider(DateTimeOffset.UtcNow));
        var projectId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        // Act
        await service.CancelAsync(projectId, serviceId);
        var second = await service.CancelAsync(projectId, serviceId);

        // Assert
        Assert.That(second.Status, Is.EqualTo(TimetableServiceStatus.Cancelled));
    }

    [Test]
    public async Task CompleteAsync_Should_CreateTerminalIdempotentState()
    {
        // Arrange
        using var service = new TimetableOperationsService(new MemoryStore(), new FixedTimeProvider(DateTimeOffset.UtcNow));
        var projectId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        // Act
        await service.CompleteAsync(projectId, serviceId);
        var second = await service.CompleteAsync(projectId, serviceId);

        // Assert
        Assert.That(second.Status, Is.EqualTo(TimetableServiceStatus.Completed));
        Assert.ThrowsAsync<InvalidOperationException>(async () => await service.CancelAsync(projectId, serviceId));
    }

    [Test]
    public async Task HoldAsync_Should_RejectPastDeadline()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
        using var service = new TimetableOperationsService(new MemoryStore(), new FixedTimeProvider(now));

        // Act + Assert
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await service.HoldAsync(Guid.NewGuid(), Guid.NewGuid(), now, "Wait for connection"));
    }

    [Test]
    public async Task ReleaseAndReassignAsync_Should_PersistOperatorDecisions()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
        using var service = new TimetableOperationsService(new MemoryStore(), new FixedTimeProvider(now));
        var projectId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var trainId = Guid.NewGuid();
        var journeyId = Guid.NewGuid();
        var callId = Guid.NewGuid();
        var platformId = Guid.NewGuid();

        // Act
        await service.HoldAsync(projectId, serviceId, now.AddMinutes(5), "Wait for connection");
        await service.ReleaseAsync(projectId, serviceId);
        await service.ReassignTrainAsync(projectId, serviceId, trainId);
        await service.ReassignJourneyAsync(projectId, serviceId, journeyId);
        var state = await service.ReassignPlatformAsync(projectId, serviceId, callId, platformId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(state.Status, Is.EqualTo(TimetableServiceStatus.Scheduled));
            Assert.That(state.AssignedTrainId, Is.EqualTo(trainId));
            Assert.That(state.AssignedJourneyId, Is.EqualTo(journeyId));
            Assert.That(state.Calls.Single().AssignedPlatformId, Is.EqualTo(platformId));
            Assert.That(state.HeldUntil, Is.Null);
        });
    }

    [Test]
    public async Task FileStore_Should_RecoverStateAfterServiceRecreation()
    {
        // Arrange
        var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"timetable-{Guid.NewGuid():N}");
        var projectId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        try
        {
            using (var first = new TimetableOperationsService(new FileTimetableStateStore(directory), new FixedTimeProvider(DateTimeOffset.UtcNow)))
            {
                await first.CancelAsync(projectId, serviceId);
            }

            // Act
            using var second = new TimetableOperationsService(new FileTimetableStateStore(directory), new FixedTimeProvider(DateTimeOffset.UtcNow));
            var recovered = await second.GetStatesAsync(projectId);

            // Assert
            Assert.That(recovered.Single().Status, Is.EqualTo(TimetableServiceStatus.Cancelled));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class MemoryStore : ITimetableStateStore
    {
        private readonly Dictionary<Guid, List<TimetableServiceState>> _states = [];

        public Task<IReadOnlyList<TimetableServiceState>> LoadAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TimetableServiceState> result = _states.TryGetValue(projectId, out var states) ? states : [];
            return Task.FromResult(result);
        }

        public Task SaveAsync(Guid projectId, IReadOnlyCollection<TimetableServiceState> states, CancellationToken cancellationToken = default)
        {
            _states[projectId] = states.ToList();
            return Task.CompletedTask;
        }
    }
}
