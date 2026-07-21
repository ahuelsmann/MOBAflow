// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Common.Runtime;

using Domain;
using Domain.Enum;

using Microsoft.Extensions.Logging;

/// <summary>
/// Attributes monotonic runtime duration and completed journey runs to stable vehicle identifiers.
/// The tracker is fed synchronously from the backend runtime and never depends on EventBus delivery.
/// </summary>
public sealed class VehicleUsageRuntimeTracker
{
    private readonly object _lock = new();
    private readonly TimeProvider _timeProvider;
    private readonly IVehicleUsageCheckpointStore _checkpointStore;
    private readonly ILogger? _logger;
    private readonly Dictionary<Guid, long> _pendingOperatingTicks = [];
    private readonly HashSet<Guid> _completedJourneyRuns = [];

    private Project? _runtimeProject;
    private Guid? _activeTrainId;
    private long _lastTimestamp;
    private bool _hasTimestamp;
    private bool _isConnected;
    private bool _isTrackPowerOn;
    private bool _isEmergencyStopActive;
    private bool _isShortCircuitActive;
    private bool _isProgrammingModeActive;
    private Dictionary<int, LocomotiveRuntimeSnapshot> _locomotiveStates = [];
    private long _rejectedUpdates;
    private long _duplicateJourneyCompletions;
    private long _recoveredVehicles;
    private long _completedCheckpoints;
    private long _checkpointFailures;
    private DateTimeOffset? _lastCheckpointAt;

    public VehicleUsageRuntimeTracker(
        TimeProvider timeProvider,
        IVehicleUsageCheckpointStore checkpointStore,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(checkpointStore);
        _timeProvider = timeProvider;
        _checkpointStore = checkpointStore;
        _logger = logger;
    }

    public void Activate(Project runtimeProject)
    {
        ArgumentNullException.ThrowIfNull(runtimeProject);

        lock (_lock)
        {
            _runtimeProject = runtimeProject;
            _pendingOperatingTicks.Clear();
            _completedJourneyRuns.Clear();
            _activeTrainId = _activeTrainId is { } trainId
                             && runtimeProject.Trains.Any(train => train.Id == trainId)
                ? trainId
                : null;
            _hasTimestamp = true;
            _lastTimestamp = _timeProvider.GetTimestamp();

            VehicleUsageCheckpointState? recovered = null;
            try
            {
                recovered = _checkpointStore.Load(runtimeProject.Id);
            }
            catch (Exception ex)
            {
                _checkpointFailures++;
                _logger?.LogWarning(ex, "Loading vehicle usage checkpoint failed for project {ProjectId}", runtimeProject.Id);
            }

            if (recovered == null)
            {
                return;
            }

            _completedJourneyRuns.UnionWith(recovered.CompletedJourneyRuns);
            foreach (var (vehicleId, checkpoint) in recovered.Vehicles)
            {
                var runtimeUsage = FindUsage(runtimeProject, vehicleId, create: true);
                if (runtimeUsage == null)
                {
                    _rejectedUpdates++;
                    continue;
                }

                var operatingSeconds = Math.Max(
                    checkpoint.OperatingSeconds,
                    runtimeUsage.TrackedOperatingSeconds);
                var completedTrips = Math.Max(
                    checkpoint.CompletedTrips,
                    runtimeUsage.TrackedCompletedTrips);

                if (operatingSeconds > runtimeUsage.TrackedOperatingSeconds
                    || completedTrips > runtimeUsage.TrackedCompletedTrips)
                {
                    _recoveredVehicles++;
                }

                runtimeUsage.TrackedOperatingSeconds = operatingSeconds;
                runtimeUsage.TrackedCompletedTrips = completedTrips;
            }
        }
    }

    public void UpdateRuntimeState(
        bool isConnected,
        bool isTrackPowerOn,
        bool isEmergencyStopActive,
        bool isShortCircuitActive,
        bool isProgrammingModeActive,
        IReadOnlyDictionary<int, LocomotiveRuntimeSnapshot> locomotiveStates)
    {
        ArgumentNullException.ThrowIfNull(locomotiveStates);

        lock (_lock)
        {
            SettleElapsedTime();
            _isConnected = isConnected;
            _isTrackPowerOn = isTrackPowerOn;
            _isEmergencyStopActive = isEmergencyStopActive;
            _isShortCircuitActive = isShortCircuitActive;
            _isProgrammingModeActive = isProgrammingModeActive;
            _locomotiveStates = locomotiveStates.ToDictionary(pair => pair.Key, pair => pair.Value);
        }
    }

    public bool SetActiveTrain(Guid? trainId)
    {
        lock (_lock)
        {
            SettleElapsedTime();
            if (trainId.HasValue && _runtimeProject?.Trains.All(train => train.Id != trainId.Value) != false)
            {
                _rejectedUpdates++;
                return false;
            }

            _activeTrainId = trainId;
            return true;
        }
    }

    public bool RecordJourneyCompleted(Guid journeyRunId)
    {
        lock (_lock)
        {
            SettleElapsedTime();
            if (journeyRunId == Guid.Empty || _runtimeProject == null)
            {
                _rejectedUpdates++;
                return false;
            }

            if (!_completedJourneyRuns.Add(journeyRunId))
            {
                _duplicateJourneyCompletions++;
                return false;
            }

            var train = _activeTrainId.HasValue
                ? _runtimeProject.Trains.FirstOrDefault(candidate => candidate.Id == _activeTrainId.Value)
                : null;
            if (train == null)
            {
                _rejectedUpdates++;
                return CheckpointCore();
            }

            foreach (var vehicleId in train.Vehicles.Select(vehicle => vehicle.VehicleId).Distinct())
            {
                var runtimeUsage = FindUsage(_runtimeProject, vehicleId, create: false);
                if (runtimeUsage == null)
                {
                    _rejectedUpdates++;
                    continue;
                }

                runtimeUsage.TrackedCompletedTrips++;
            }

            return CheckpointCore();
        }
    }

    public bool Checkpoint()
    {
        lock (_lock)
        {
            SettleElapsedTime();
            return CheckpointCore();
        }
    }

    public (Guid? ActiveTrainId,
        IReadOnlyDictionary<Guid, VehicleUsageRuntimeSnapshot> Usage,
        VehicleUsageRuntimeDiagnosticsSnapshot Diagnostics) GetSnapshot()
    {
        lock (_lock)
        {
            var operatingVehicleIds = GetOperatingVehicleIds();
            var usage = new Dictionary<Guid, VehicleUsageRuntimeSnapshot>();
            if (_runtimeProject != null)
            {
                foreach (var vehicle in EnumerateVehicles(_runtimeProject))
                {
                    var pendingSeconds = _pendingOperatingTicks.GetValueOrDefault(vehicle.Id) / TimeSpan.TicksPerSecond;
                    usage[vehicle.Id] = new VehicleUsageRuntimeSnapshot
                    {
                        VehicleId = vehicle.Id,
                        VehicleKind = vehicle.Kind,
                        TrackedOperatingSeconds = checked(vehicle.Usage.TrackedOperatingSeconds + pendingSeconds),
                        TrackedCompletedTrips = vehicle.Usage.TrackedCompletedTrips,
                        IsOperating = operatingVehicleIds.Contains(vehicle.Id)
                    };
                }
            }

            return (
                _activeTrainId,
                usage,
                new VehicleUsageRuntimeDiagnosticsSnapshot
                {
                    RejectedUpdates = _rejectedUpdates,
                    DuplicateJourneyCompletions = _duplicateJourneyCompletions,
                    RecoveredVehicles = _recoveredVehicles,
                    CompletedCheckpoints = _completedCheckpoints,
                    CheckpointFailures = _checkpointFailures,
                    LastCheckpointAt = _lastCheckpointAt
                });
        }
    }

    private void SettleElapsedTime()
    {
        var now = _timeProvider.GetTimestamp();
        if (!_hasTimestamp)
        {
            _hasTimestamp = true;
            _lastTimestamp = now;
            return;
        }

        var elapsed = _timeProvider.GetElapsedTime(_lastTimestamp, now);
        _lastTimestamp = now;
        if (elapsed <= TimeSpan.Zero || _runtimeProject == null)
        {
            return;
        }

        foreach (var vehicleId in GetOperatingVehicleIds())
        {
            _pendingOperatingTicks[vehicleId] = checked(
                _pendingOperatingTicks.GetValueOrDefault(vehicleId) + elapsed.Ticks);
        }
    }

    private HashSet<Guid> GetOperatingVehicleIds()
    {
        var result = new HashSet<Guid>();
        if (_runtimeProject == null
            || !_isConnected
            || !_isTrackPowerOn
            || _isEmergencyStopActive
            || _isShortCircuitActive
            || _isProgrammingModeActive)
        {
            return result;
        }

        foreach (var locomotive in _runtimeProject.Locomotives)
        {
            if (locomotive.DigitalAddress is { } address
                && _locomotiveStates.TryGetValue(checked((int)address), out var state)
                && state.Speed > 0)
            {
                result.Add(locomotive.Id);
            }
        }

        var activeTrain = _activeTrainId.HasValue
            ? _runtimeProject.Trains.FirstOrDefault(train => train.Id == _activeTrainId.Value)
            : null;
        if (activeTrain == null
            || !activeTrain.Vehicles.Any(vehicle =>
                vehicle.VehicleKind == TrainVehicleKind.Locomotive && result.Contains(vehicle.VehicleId)))
        {
            return result;
        }

        foreach (var vehicle in activeTrain.Vehicles)
        {
            if (vehicle.VehicleKind != TrainVehicleKind.Locomotive
                && FindUsage(_runtimeProject, vehicle.VehicleId, create: false) != null)
            {
                result.Add(vehicle.VehicleId);
            }
        }

        return result;
    }

    private bool CheckpointCore()
    {
        if (_runtimeProject == null)
        {
            return false;
        }

        foreach (var (vehicleId, pendingTicks) in _pendingOperatingTicks.ToArray())
        {
            var wholeSeconds = pendingTicks / TimeSpan.TicksPerSecond;
            if (wholeSeconds <= 0)
            {
                continue;
            }

            var runtimeUsage = FindUsage(_runtimeProject, vehicleId, create: false);
            if (runtimeUsage == null)
            {
                _pendingOperatingTicks.Remove(vehicleId);
                _rejectedUpdates++;
                continue;
            }

            runtimeUsage.TrackedOperatingSeconds = checked(runtimeUsage.TrackedOperatingSeconds + wholeSeconds);
            _pendingOperatingTicks[vehicleId] = pendingTicks % TimeSpan.TicksPerSecond;
        }

        var updatedAt = _timeProvider.GetUtcNow();
        var state = new VehicleUsageCheckpointState
        {
            ProjectId = _runtimeProject.Id,
            UpdatedAt = updatedAt,
            Vehicles = EnumerateVehicles(_runtimeProject).ToDictionary(
                vehicle => vehicle.Id,
                vehicle => new VehicleUsageCheckpoint(
                    vehicle.Usage.TrackedOperatingSeconds,
                    vehicle.Usage.TrackedCompletedTrips)),
            CompletedJourneyRuns = [.. _completedJourneyRuns]
        };

        try
        {
            _checkpointStore.Save(state);
            _completedCheckpoints++;
            _lastCheckpointAt = updatedAt;
            return true;
        }
        catch (Exception ex)
        {
            _checkpointFailures++;
            _logger?.LogWarning(ex, "Persisting vehicle usage checkpoint failed for project {ProjectId}", _runtimeProject.Id);
            return false;
        }
    }

    private static IEnumerable<VehicleReference> EnumerateVehicles(Project project)
    {
        foreach (var locomotive in project.Locomotives)
        {
            locomotive.Usage ??= new VehicleUsageData();
            yield return new VehicleReference(locomotive.Id, TrainVehicleKind.Locomotive, locomotive.Usage);
        }

        foreach (var wagon in project.PassengerWagons)
        {
            wagon.Usage ??= new VehicleUsageData();
            yield return new VehicleReference(wagon.Id, TrainVehicleKind.PassengerWagon, wagon.Usage);
        }

        foreach (var wagon in project.GoodsWagons)
        {
            wagon.Usage ??= new VehicleUsageData();
            yield return new VehicleReference(wagon.Id, TrainVehicleKind.GoodsWagon, wagon.Usage);
        }
    }

    private static VehicleUsageData? FindUsage(Project project, Guid vehicleId, bool create)
    {
        var locomotive = project.Locomotives.FirstOrDefault(vehicle => vehicle.Id == vehicleId);
        if (locomotive != null)
        {
            return create ? locomotive.Usage ??= new VehicleUsageData() : locomotive.Usage;
        }

        var passengerWagon = project.PassengerWagons.FirstOrDefault(vehicle => vehicle.Id == vehicleId);
        if (passengerWagon != null)
        {
            return create ? passengerWagon.Usage ??= new VehicleUsageData() : passengerWagon.Usage;
        }

        var goodsWagon = project.GoodsWagons.FirstOrDefault(vehicle => vehicle.Id == vehicleId);
        if (goodsWagon != null)
        {
            return create ? goodsWagon.Usage ??= new VehicleUsageData() : goodsWagon.Usage;
        }

        return null;
    }

    private sealed record VehicleReference(Guid Id, TrainVehicleKind Kind, VehicleUsageData Usage);
}
