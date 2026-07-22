// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Common.Configuration;
using Common.Discovery;
using Common.Events;
using Common.Runtime;
using Interface;
using Manager;
using Microsoft.Extensions.Logging;
using System.Threading;

/// <summary>
/// In-process runtime owning Z21 connection state and the active project execution context.
/// Implementation is split across partial files for readability (runtime API, Z21 handlers, auto-connect, helpers, snapshot).
/// </summary>
public sealed partial class MobaRuntimeService : IMobaRuntime, IDisposable
{
    /// <summary>Maximum operating-time loss after an abrupt process termination.</summary>
    public static readonly TimeSpan VehicleUsageCheckpointInterval = TimeSpan.FromSeconds(30);

    private readonly IZ21 _z21;
    private readonly ActionExecutionContextFactory _executionContextFactory;
    private readonly JourneyManagerFactory _journeyManagerFactory;
    private readonly AppSettings _settings;
    private readonly ILogger<MobaRuntimeService> _logger;
    private readonly IEventBus? _eventBus;
    private readonly IZ21DiscoveryService _z21Discovery;
    private readonly IInterlockingRuntime? _interlockingRuntime;
    private readonly TimeProvider _timeProvider;
    private readonly VehicleUsageRuntimeTracker _vehicleUsageTracker;

    private ActiveProjectContext? _activeProjectContext;
    private Timer? _z21AutoConnectTimer;
    private ITimer? _vehicleUsageCheckpointTimer;
    private int _autoConnectAttemptInProgress;
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private bool _started;

    private readonly Dictionary<int, LocomotiveRuntimeSnapshot> _locomotiveStates = [];
    private readonly Dictionary<int, DateTimeOffset> _lastLocomotiveFunctionCommandAt = [];
    private readonly Dictionary<int, DateTimeOffset> _lastLocomotiveDriveCommandAt = [];
    private static readonly TimeSpan LocomotiveFunctionCommandGracePeriod = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan LocomotiveDriveCommandGracePeriod = TimeSpan.FromSeconds(3);

    private bool _isConnected;
    private bool _isTrackPowerOn;
    private bool _isZ21Connecting = true;
    private bool _hasSeenSuccessfulZ21Connection;
    private bool _isManualDisconnectRequested;
    private bool _isEmergencyStopActive;
    private bool _isShortCircuitActive;
    private bool _isProgrammingModeActive;
    private bool _isOperatorAckRequired;

    private string _statusText = "Disconnected";
    private string _serialNumber = "-";
    private string _firmwareVersion = "-";
    private string _hardwareType = "-";
    private string _lastFailSafeReason = "Waiting for the Z21 connection.";
    private DateTimeOffset? _lastFailSafeAt;

    private int _mainCurrent;
    private int _progCurrent;
    private int _filteredMainCurrent;
    private int _temperature;
    private int _supplyVoltage;
    private int _vccVoltage;

    /// <summary>
    /// Initializes a new instance of the <see cref="MobaRuntimeService"/> class.
    /// </summary>
    public MobaRuntimeService(
        IZ21 z21,
        IWorkflowService workflowService,
        ActionExecutionContext executionContext,
        AppSettings settings,
        ILogger<MobaRuntimeService> logger,
        IEventBus? eventBus = null,
        IInterlockingRuntime? interlockingRuntime = null)
        : this(
            z21,
            workflowService,
            new ActionExecutionContextFactory(executionContext),
            settings,
            logger,
            eventBus,
            interlockingRuntime: interlockingRuntime)
    {
    }

    public MobaRuntimeService(
        IZ21 z21,
        IWorkflowService workflowService,
        ActionExecutionContextFactory executionContextFactory,
        AppSettings settings,
        ILogger<MobaRuntimeService> logger,
        IEventBus? eventBus = null,
        JourneyManagerFactory? journeyManagerFactory = null,
        IZ21DiscoveryService? z21Discovery = null,
        IVehicleUsageCheckpointStore? vehicleUsageCheckpointStore = null,
        TimeProvider? timeProvider = null,
        IInterlockingRuntime? interlockingRuntime = null)
    {
        ArgumentNullException.ThrowIfNull(z21);
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(executionContextFactory);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);

        _z21 = z21;
        _executionContextFactory = executionContextFactory;
        _journeyManagerFactory = journeyManagerFactory ?? new JourneyManagerFactory(z21, workflowService);
        _settings = settings;
        _logger = logger;
        _eventBus = eventBus;
        _z21Discovery = z21Discovery ?? new NullZ21DiscoveryService();
        _interlockingRuntime = interlockingRuntime;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _vehicleUsageTracker = new VehicleUsageRuntimeTracker(
            _timeProvider,
            vehicleUsageCheckpointStore ?? new NullVehicleUsageCheckpointStore(),
            logger);

        _z21.OnConnectedChanged += OnZ21ConnectedChanged;
        _z21.OnConnectionLost += OnZ21ConnectionLost;
        _z21.OnSystemStateChanged += OnZ21SystemStateChanged;
        _z21.OnXBusStatusChanged += OnZ21XBusStatusChanged;
        _z21.OnVersionInfoChanged += OnZ21VersionInfoChanged;
        _z21.OnLocoInfoChanged += OnZ21LocomotiveInfoChanged;

        if (_z21.TrafficMonitor != null)
        {
            _z21.TrafficMonitor.PacketLogged += OnTrafficPacketLogged;
        }
    }

    /// <inheritdoc />
    public MobaRuntimeSnapshot Current { get; private set; } = MobaRuntimeSnapshot.Empty;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _startLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_started)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
            PublishSnapshot();
            BeginAutoConnectToZ21();
            _started = true;
        }
        finally
        {
            _startLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _z21.OnConnectedChanged -= OnZ21ConnectedChanged;
        _z21.OnConnectionLost -= OnZ21ConnectionLost;
        _z21.OnSystemStateChanged -= OnZ21SystemStateChanged;
        _z21.OnXBusStatusChanged -= OnZ21XBusStatusChanged;
        _z21.OnVersionInfoChanged -= OnZ21VersionInfoChanged;
        _z21.OnLocoInfoChanged -= OnZ21LocomotiveInfoChanged;

        if (_z21.TrafficMonitor != null)
        {
            _z21.TrafficMonitor.PacketLogged -= OnTrafficPacketLogged;
        }

        StopAutoConnectTimer();
        _vehicleUsageCheckpointTimer?.Dispose();
        _vehicleUsageTracker.Checkpoint();
        ReplaceActiveProjectContext(null);
        _startLock.Dispose();
    }

    private void ReplaceActiveProjectContext(ActiveProjectContext? nextContext)
    {
        if (_activeProjectContext != null)
        {
            _activeProjectContext.JourneyManager.StationChanged -= OnJourneyStationChanged;
            _activeProjectContext.JourneyManager.FeedbackReceived -= OnJourneyRuntimeChanged;
            _activeProjectContext.JourneyManager.JourneyCompleted -= OnJourneyCompleted;
            _activeProjectContext.Dispose();
        }

        _activeProjectContext = nextContext;
    }

    private void PublishSnapshot()
    {
        var snapshot = CreateSnapshot();
        Current = snapshot;
        _eventBus?.Publish(new RuntimeSnapshotChangedEvent(snapshot));
    }

    private MobaRuntimeSnapshot CreateSnapshot()
    {
        var usage = _vehicleUsageTracker.GetSnapshot();
        var telemetry = new MobaRuntimeTelemetryState
        {
            IsConnected = _isConnected,
            IsTrackPowerOn = _isTrackPowerOn,
            StatusText = _statusText,
            SerialNumber = _serialNumber,
            FirmwareVersion = _firmwareVersion,
            HardwareType = _hardwareType,
            MainCurrent = _mainCurrent,
            ProgCurrent = _progCurrent,
            FilteredMainCurrent = _filteredMainCurrent,
            Temperature = _temperature,
            SupplyVoltage = _supplyVoltage,
            VccVoltage = _vccVoltage,
            IsZ21Connecting = _isZ21Connecting,
            HasSeenSuccessfulConnection = _hasSeenSuccessfulZ21Connection,
            IsManualDisconnectRequested = _isManualDisconnectRequested,
            IsEmergencyStopActive = _isEmergencyStopActive,
            IsShortCircuitActive = _isShortCircuitActive,
            IsProgrammingModeActive = _isProgrammingModeActive,
            LastFailSafeReason = _lastFailSafeReason,
            LastFailSafeAt = _lastFailSafeAt,
            IsOperatorAckRequired = _isOperatorAckRequired
        };

        foreach (var (address, state) in _locomotiveStates)
        {
            telemetry.LocomotiveStates[address] = state;
        }

        return MobaRuntimeSnapshotBuilder.Create(
            telemetry,
            _activeProjectContext,
            usage.ActiveTrainId,
            usage.Usage,
            usage.Diagnostics);
    }

    private void UpdateVehicleUsageRuntimeState()
    {
        _vehicleUsageTracker.UpdateRuntimeState(
            _isConnected,
            _isTrackPowerOn,
            _isEmergencyStopActive,
            _isShortCircuitActive,
            _isProgrammingModeActive,
            _locomotiveStates);
    }

    private void SelectActiveTrainForLocomotive(int address, int speed)
    {
        if (speed <= 0 || _activeProjectContext == null)
        {
            return;
        }

        var locomotive = _activeProjectContext.ActiveProject.Locomotives.FirstOrDefault(candidate =>
            candidate.DigitalAddress == (uint)address);
        if (locomotive == null)
        {
            return;
        }

        var candidates = _activeProjectContext.ActiveProject.Trains
            .Where(train => train.Vehicles.Any(vehicle =>
                vehicle.VehicleKind == Domain.Enum.TrainVehicleKind.Locomotive
                && vehicle.VehicleId == locomotive.Id))
            .Select(train => train.Id)
            .Distinct()
            .ToList();
        var currentTrainId = _vehicleUsageTracker.GetSnapshot().ActiveTrainId;
        if (currentTrainId.HasValue && candidates.Contains(currentTrainId.Value))
        {
            return;
        }

        _vehicleUsageTracker.SetActiveTrain(candidates.Count == 1 ? candidates[0] : null);
    }

    private void StartVehicleUsageCheckpointTimer()
    {
        _vehicleUsageCheckpointTimer?.Dispose();
        _vehicleUsageCheckpointTimer = _timeProvider.CreateTimer(
            _ => CheckpointVehicleUsage(publishSnapshot: false),
            null,
            VehicleUsageCheckpointInterval,
            VehicleUsageCheckpointInterval);
    }

    private bool CheckpointVehicleUsage(bool publishSnapshot)
    {
        var committed = _vehicleUsageTracker.Checkpoint();
        if (!committed)
        {
            return false;
        }

        if (publishSnapshot)
        {
            PublishSnapshot();
        }

        PublishVehicleUsageCheckpointCommitted();

        return true;
    }

    private void PublishVehicleUsageCheckpointCommitted()
    {
        if (_activeProjectContext != null)
        {
            var usage = _vehicleUsageTracker.GetSnapshot().Usage;
            _eventBus?.Publish(new VehicleUsageCheckpointCommittedEvent(
                _activeProjectContext.ActiveProject.Id,
                _timeProvider.GetUtcNow(),
                usage));
        }
    }
}
