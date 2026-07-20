// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using System.Text.Json;

using Domain;

using Interface;

/// <summary>Stores timetable operating sessions as project-scoped JSON files.</summary>
public sealed class FileTimetableStateStore : ITimetableStateStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _directory;

    /// <summary>Initializes a store in the current user's local application data directory.</summary>
    public FileTimetableStateStore()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MOBAflow", "Timetable"))
    {
    }

    /// <summary>Initializes a store in an explicit directory.</summary>
    public FileTimetableStateStore(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("A storage directory is required.", nameof(directory));
        _directory = directory;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimetableServiceState>> LoadAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var path = GetPath(projectId);
        if (!File.Exists(path)) return [];

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        return await JsonSerializer.DeserializeAsync<List<TimetableServiceState>>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false) ?? [];
    }

    /// <inheritdoc />
    public async Task SaveAsync(Guid projectId, IReadOnlyCollection<TimetableServiceState> states, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(states);
        Directory.CreateDirectory(_directory);
        var path = GetPath(projectId);
        var temporaryPath = path + ".tmp";

        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(stream, states, SerializerOptions, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporaryPath, path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private string GetPath(Guid projectId) => Path.Combine(_directory, $"{projectId:N}.json");
}

/// <summary>Serializes and persists idempotent manual timetable operations.</summary>
public sealed class TimetableOperationsService : ITimetableOperationsService, IDisposable
{
    private readonly ITimetableStateStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Initializes a new timetable operations service.</summary>
    public TimetableOperationsService(ITimetableStateStore store, TimeProvider timeProvider)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TimetableServiceState>> GetStatesAsync(Guid projectId, CancellationToken cancellationToken = default)
        => _store.LoadAsync(projectId, cancellationToken);

    /// <inheritdoc />
    public Task<TimetableServiceState> HoldAsync(Guid projectId, Guid serviceId, DateTimeOffset heldUntil, string reason, CancellationToken cancellationToken = default)
    {
        if (heldUntil <= _timeProvider.GetUtcNow()) throw new ArgumentOutOfRangeException(nameof(heldUntil), "Hold end must be in the future.");
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A hold reason is required.", nameof(reason));
        return MutateAsync(projectId, serviceId, state =>
        {
            EnsureNonTerminal(state);
            state.Status = TimetableServiceStatus.Held;
            state.HeldUntil = heldUntil;
            state.HoldReason = reason.Trim();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TimetableServiceState> CancelAsync(Guid projectId, Guid serviceId, CancellationToken cancellationToken = default)
        => MutateAsync(projectId, serviceId, state =>
        {
            if (state.Status == TimetableServiceStatus.Completed) throw new InvalidOperationException("A completed service cannot be cancelled.");
            state.Status = TimetableServiceStatus.Cancelled;
            state.HeldUntil = null;
            state.HoldReason = null;
        }, cancellationToken);

    /// <inheritdoc />
    public Task<TimetableServiceState> ReleaseAsync(Guid projectId, Guid serviceId, CancellationToken cancellationToken = default)
        => MutateAsync(projectId, serviceId, state =>
        {
            if (state.Status != TimetableServiceStatus.Held) throw new InvalidOperationException("Only a held service can be released.");
            state.Status = state.Calls.Any(call => call.ActualArrival is not null || call.ActualDeparture is not null)
                ? TimetableServiceStatus.Running
                : TimetableServiceStatus.Scheduled;
            state.HeldUntil = null;
            state.HoldReason = null;
        }, cancellationToken);

    /// <inheritdoc />
    public Task<TimetableServiceState> CompleteAsync(Guid projectId, Guid serviceId, CancellationToken cancellationToken = default)
        => MutateAsync(projectId, serviceId, state =>
        {
            if (state.Status == TimetableServiceStatus.Cancelled) throw new InvalidOperationException("A cancelled service cannot be completed.");
            state.Status = TimetableServiceStatus.Completed;
            state.HeldUntil = null;
            state.HoldReason = null;
        }, cancellationToken);

    /// <inheritdoc />
    public Task<TimetableServiceState> ReassignTrainAsync(Guid projectId, Guid serviceId, Guid trainId, CancellationToken cancellationToken = default)
        => MutateAsync(projectId, serviceId, state =>
        {
            EnsureNonTerminal(state);
            state.AssignedTrainId = trainId;
        }, cancellationToken);

    /// <inheritdoc />
    public Task<TimetableServiceState> ReassignJourneyAsync(Guid projectId, Guid serviceId, Guid journeyId, CancellationToken cancellationToken = default)
        => MutateAsync(projectId, serviceId, state =>
        {
            EnsureNonTerminal(state);
            state.AssignedJourneyId = journeyId;
        }, cancellationToken);

    /// <inheritdoc />
    public Task<TimetableServiceState> ReassignPlatformAsync(Guid projectId, Guid serviceId, Guid callId, Guid platformId, CancellationToken cancellationToken = default)
        => MutateAsync(projectId, serviceId, state =>
        {
            EnsureNonTerminal(state);
            var call = GetOrAddCallState(state, callId);
            call.AssignedPlatformId = platformId;
        }, cancellationToken);

    /// <inheritdoc />
    public Task<TimetableServiceState> RecordArrivalAsync(Guid projectId, Guid serviceId, Guid callId, DateTimeOffset? occurredAt = null, CancellationToken cancellationToken = default)
        => MutateCallAsync(projectId, serviceId, callId, call => call.ActualArrival ??= occurredAt ?? _timeProvider.GetUtcNow(), cancellationToken);

    /// <inheritdoc />
    public Task<TimetableServiceState> RecordDepartureAsync(Guid projectId, Guid serviceId, Guid callId, DateTimeOffset? occurredAt = null, CancellationToken cancellationToken = default)
        => MutateCallAsync(projectId, serviceId, callId, call => call.ActualDeparture ??= occurredAt ?? _timeProvider.GetUtcNow(), cancellationToken);

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    private Task<TimetableServiceState> MutateCallAsync(Guid projectId, Guid serviceId, Guid callId, Action<TimetableCallState> mutation, CancellationToken cancellationToken)
        => MutateAsync(projectId, serviceId, state =>
        {
            EnsureNonTerminal(state);
            state.Status = TimetableServiceStatus.Running;
            var call = GetOrAddCallState(state, callId);
            mutation(call);
            if (call.ActualArrival is DateTimeOffset arrival
                && call.ActualDeparture is DateTimeOffset departure
                && departure < arrival)
                throw new InvalidOperationException("Actual departure cannot be earlier than actual arrival.");
        }, cancellationToken);

    private static TimetableCallState GetOrAddCallState(TimetableServiceState state, Guid callId)
    {
        var call = state.Calls.FirstOrDefault(candidate => candidate.CallId == callId);
        if (call is not null) return call;

        call = new TimetableCallState { CallId = callId };
        state.Calls.Add(call);
        return call;
    }

    private async Task<TimetableServiceState> MutateAsync(Guid projectId, Guid serviceId, Action<TimetableServiceState> mutation, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var states = (await _store.LoadAsync(projectId, cancellationToken).ConfigureAwait(false)).ToList();
            var state = states.FirstOrDefault(candidate => candidate.ServiceId == serviceId);
            if (state is null)
            {
                state = new TimetableServiceState { ServiceId = serviceId };
                states.Add(state);
            }

            mutation(state);
            await _store.SaveAsync(projectId, states, cancellationToken).ConfigureAwait(false);
            return state;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static void EnsureNonTerminal(TimetableServiceState state)
    {
        if (state.Status is TimetableServiceStatus.Completed or TimetableServiceStatus.Cancelled)
            throw new InvalidOperationException("A terminal timetable service cannot be changed.");
    }
}
