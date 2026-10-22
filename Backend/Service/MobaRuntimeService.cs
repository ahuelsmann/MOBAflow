// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Common.Configuration;
using Common.Events;
using Contracts.Runtime;
using Interface;
using Manager;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading;

/// <summary>Groups the collaborators of one <see cref="MobaRuntimeService"/>; the caller owns all of them.</summary>
public sealed class MobaRuntimeServiceDependencies
{
    /// <summary>Gets the Z21 connection of the runtime.</summary>
    public required IZ21 Z21 { get; init; }

    /// <summary>Gets the factory for per-run action execution contexts.</summary>
    public required ActionExecutionContextFactory ExecutionContextFactory { get; init; }

    /// <summary>Gets the application settings.</summary>
    public required AppSettings Settings { get; init; }

    /// <summary>Gets the event bus for runtime and journey events.</summary>
    public required IEventBus EventBus { get; init; }

    /// <summary>Gets the factory for the journey manager of the active project.</summary>
    public required JourneyManagerFactory JourneyManagerFactory { get; init; }

    /// <summary>Gets the interlocking runtime activated with the project.</summary>
    public required IInterlockingRuntime InterlockingRuntime { get; init; }

    /// <summary>Gets the InPort counters; the runtime subscribes to them but does not dispose them.</summary>
    public required InPortCounterService InPortCounterService { get; init; }

    /// <summary>
    /// Gets the Z21 endpoint source, or null to connect to the Z21 of the project the runtime runs
    /// (that source depends on the runtime's own active project, so the runtime creates it). Production hosts
    /// provide a source: MOBAsmart registers one for its selected project and <c>ProjectRuntimeFactory</c> passes
    /// one per project; the default applies only when the host registers none.
    /// </summary>
    public IZ21EndpointSource? EndpointSource { get; init; }
}

/// <summary>
/// In-process runtime owning Z21 connection state and the active project execution context.
/// Implementation is split across partial files for readability (runtime API, Z21 handlers, auto-connect, helpers, snapshot).
/// </summary>
public sealed partial class MobaRuntimeService : IMobaRuntime, IDisposable
{
    private readonly IZ21 _z21;
    private readonly ActionExecutionContextFactory _executionContextFactory;
    private readonly JourneyManagerFactory _journeyManagerFactory;
    private readonly AppSettings _settings;
    private readonly ILogger<MobaRuntimeService> _logger;
    private readonly IEventBus _eventBus;
    private readonly IZ21EndpointSource _endpointSource;
    private readonly IInterlockingRuntime _interlockingRuntime;
    private readonly InPortCounterService _inPortCounters;

    private ActiveProjectContext? _activeProjectContext;
    private Timer? _z21AutoConnectTimer;
    private int _autoConnectAttemptInProgress;
    private readonly SemaphoreSlim _startLock = new(1, 1);
    // Activation, definition refresh and connection handoff must not interleave.
    private readonly SemaphoreSlim _projectActivationLock = new(1, 1);
    private bool _started;

    private readonly ConcurrentDictionary<int, LocomotiveRuntimeSnapshot> _locomotiveStates = new();
    private readonly ConcurrentDictionary<int, DateTimeOffset> _lastLocomotiveFunctionCommandAt = new();
    private readonly ConcurrentDictionary<int, DateTimeOffset> _lastLocomotiveDriveCommandAt = new();
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
    public MobaRuntimeService(MobaRuntimeServiceDependencies dependencies, ILogger<MobaRuntimeService> logger)
    {
        ArgumentNullException.ThrowIfNull(dependencies);
        ArgumentNullException.ThrowIfNull(dependencies.Z21);
        ArgumentNullException.ThrowIfNull(dependencies.ExecutionContextFactory);
        ArgumentNullException.ThrowIfNull(dependencies.Settings);
        ArgumentNullException.ThrowIfNull(dependencies.EventBus);
        ArgumentNullException.ThrowIfNull(dependencies.JourneyManagerFactory);
        ArgumentNullException.ThrowIfNull(dependencies.InterlockingRuntime);
        ArgumentNullException.ThrowIfNull(dependencies.InPortCounterService);
        ArgumentNullException.ThrowIfNull(logger);

        _z21 = dependencies.Z21;
        _executionContextFactory = dependencies.ExecutionContextFactory;
        _inPortCounters = dependencies.InPortCounterService;
        _journeyManagerFactory = dependencies.JourneyManagerFactory;
        _settings = dependencies.Settings;
        _logger = logger;
        _eventBus = dependencies.EventBus;
        // When the host registers no source, the runtime connects to the Z21 of the project it runs.
        _endpointSource = dependencies.EndpointSource ?? new ProjectZ21EndpointSource(() => _activeProjectContext?.ActiveProject);
        _interlockingRuntime = dependencies.InterlockingRuntime;
        _z21.OnConnectedChanged += OnZ21ConnectedChanged;
        _inPortCounters.SnapshotChanged += OnJourneyRuntimeChanged;
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
            // An unreadable counter file is reported in the snapshot and must not block startup.
            await _inPortCounters.TryInitializeAsync(cancellationToken).ConfigureAwait(false);
            if (_z21.IsConnected)
            {
                // A runtime created again for the same Z21 takes over the open connection.
                OnZ21ConnectedChanged(true);
                StartAutoConnectTimer();
            }
            else
            {
                PublishSnapshot();
                BeginAutoConnectToZ21();
            }
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
        ReplaceActiveProjectContext(null);
        _inPortCounters.SnapshotChanged -= OnJourneyRuntimeChanged;
        _startLock.Dispose();
        _projectActivationLock.Dispose();
    }

    private void ReplaceActiveProjectContext(ActiveProjectContext? nextContext)
    {
        if (_activeProjectContext != null)
        {
            _activeProjectContext.JourneyManager.StationChanged -= OnJourneyStationChanged;
            _activeProjectContext.JourneyManager.FeedbackReceived -= OnJourneyRuntimeChanged;
            _activeProjectContext.Dispose();
        }

        _activeProjectContext = nextContext;
    }

    private void PublishSnapshot()
    {
        var snapshot = CreateSnapshot();
        Current = snapshot;
        _eventBus.Publish(new RuntimeSnapshotChangedEvent(snapshot));
    }

    private MobaRuntimeSnapshot CreateSnapshot()
    {
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
            _inPortCounters.GetSnapshot(),
            _inPortCounters.PersistenceError);
    }
}
