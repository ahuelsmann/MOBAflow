// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Common.Configuration;
using Common.Multiplex;
using Domain;
using Interface;
using Manager;
using Microsoft.Extensions.Logging;
using Common.Runtime;
using Model;
using Protocol;
using System.Net;
using System.Threading;

/// <summary>
/// In-process runtime owning Z21 connection state and the active project execution context.
/// </summary>
public sealed class MobaRuntimeService : IMobaRuntime, IDisposable
{
    private readonly IZ21 _z21;
    private readonly IWorkflowService _workflowService;
    private readonly ActionExecutionContext _executionContext;
    private readonly AppSettings _settings;
    private readonly ProjectRuntimeFactory _projectRuntimeFactory;
    private readonly ILogger<MobaRuntimeService> _logger;

    private ActiveProjectContext? _activeProjectContext;
    private Timer? _z21AutoConnectTimer;
    private int _autoConnectAttemptInProgress;

    private readonly Dictionary<int, LocomotiveRuntimeSnapshot> _locomotiveStates = [];

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
        ProjectRuntimeFactory projectRuntimeFactory,
        ILogger<MobaRuntimeService> logger)
    {
        ArgumentNullException.ThrowIfNull(z21);
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(projectRuntimeFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _z21 = z21;
        _workflowService = workflowService;
        _executionContext = executionContext;
        _settings = settings;
        _projectRuntimeFactory = projectRuntimeFactory;
        _logger = logger;

        _z21.OnConnectedChanged += OnZ21ConnectedChanged;
        _z21.OnConnectionLost += OnZ21ConnectionLost;
        _z21.OnSystemStateChanged += OnZ21SystemStateChanged;
        _z21.OnXBusStatusChanged += OnZ21XBusStatusChanged;
        _z21.OnVersionInfoChanged += OnZ21VersionInfoChanged;
        _z21.OnLocoInfoChanged += OnZ21LocomotiveInfoChanged;
        _z21.Received += OnZ21FeedbackReceived;
        _workflowService.ActionExecutionError += OnActionExecutionError;

        if (_z21.TrafficMonitor != null)
        {
            _z21.TrafficMonitor.PacketLogged += OnTrafficPacketLogged;
        }

        PublishSnapshot();
        _ = TryAutoConnectToZ21Async();
    }

    /// <inheritdoc />
    public MobaRuntimeSnapshot Current { get; private set; } = MobaRuntimeSnapshot.Empty;

    /// <inheritdoc />
    public event EventHandler<MobaRuntimeSnapshot>? SnapshotChanged;

    /// <inheritdoc />
    public event EventHandler<Z21TrafficPacket>? TrafficPacketLogged;

    /// <inheritdoc />
    public event EventHandler<FeedbackResult>? FeedbackReceived;

    /// <inheritdoc />
    public Task ActivateProjectAsync(Project editableProject, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(editableProject);

        var activeProject = _projectRuntimeFactory.CreateActiveProject(editableProject);
        var journeyManager = new JourneyManager(_z21, activeProject, _workflowService, _executionContext);
        journeyManager.StationChanged += OnJourneyRuntimeChanged;
        journeyManager.FeedbackReceived += OnJourneyRuntimeChanged;

        var nextContext = new ActiveProjectContext(activeProject, journeyManager);
        ReplaceActiveProjectContext(nextContext);

        _logger.LogInformation(
            "Activated project '{ProjectName}' for runtime with {JourneyCount} journeys",
            activeProject.Name,
            activeProject.Journeys.Count);

        PublishSnapshot();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (!TryGetConfiguredEndpoint(out var address, out var port, out var errorMessage))
        {
            _isZ21Connecting = false;
            _statusText = errorMessage;
            PublishSnapshot();
            return;
        }

        try
        {
            _isZ21Connecting = true;
            _isManualDisconnectRequested = false;
            _statusText = "Connecting...";
            PublishSnapshot();

            _z21.SetSystemStatePollingInterval(_settings.Z21.SystemStatePollingIntervalSeconds);
            await _z21.ConnectAsync(address!, port, cancellationToken).ConfigureAwait(false);

            if (!_isConnected)
            {
                _statusText = $"Waiting for Z21 at {_settings.Z21.CurrentIpAddress}:{port}...";
            }

            PublishSnapshot();
        }
        catch (Exception ex)
        {
            _isZ21Connecting = false;
            _statusText = $"Connection failed: {ex.Message}";
            PublishSnapshot();
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _isManualDisconnectRequested = true;
            _isZ21Connecting = false;
            _isOperatorAckRequired = false;
            _statusText = "Disconnecting...";
            PublishSnapshot();

            await _z21.DisconnectAsync().ConfigureAwait(false);

            _isConnected = false;
            _isTrackPowerOn = false;
            _statusText = "Disconnected";
            PublishSnapshot();
        }
        catch (Exception ex)
        {
            _statusText = $"Error: {ex.Message}";
            PublishSnapshot();
        }
    }

    /// <inheritdoc />
    public async Task SetTrackPowerAsync(bool isOn, CancellationToken cancellationToken = default)
    {
        try
        {
            if (isOn)
            {
                await _z21.SetTrackPowerOnAsync(cancellationToken).ConfigureAwait(false);
                _isTrackPowerOn = true;
            }
            else
            {
                await _z21.SetTrackPowerOffAsync(cancellationToken).ConfigureAwait(false);
                _isTrackPowerOn = false;
            }

            PublishSnapshot();
        }
        catch (Exception ex)
        {
            _statusText = $"Track power error: {ex.Message}";
            PublishSnapshot();
        }
    }

    /// <inheritdoc />
    public void SetSystemStatePollingInterval(int intervalSeconds)
    {
        _settings.Z21.SystemStatePollingIntervalSeconds = Math.Max(intervalSeconds, 0);
        _z21.SetSystemStatePollingInterval(_settings.Z21.SystemStatePollingIntervalSeconds);
    }

    /// <inheritdoc />
    public async Task SetLocomotiveDriveAsync(int address, int speed, bool forward, CancellationToken cancellationToken = default)
    {
        await _z21.SetLocoDriveAsync(address, speed, forward, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetLocomotiveFunctionAsync(int address, int functionIndex, bool isOn, CancellationToken cancellationToken = default)
    {
        await _z21.SetLocoFunctionAsync(address, functionIndex, isOn, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RequestLocomotiveInfoAsync(int address, CancellationToken cancellationToken = default)
    {
        await _z21.GetLocoInfoAsync(address, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task AcknowledgeFailSafeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_isConnected)
        {
            return Task.CompletedTask;
        }

        _isOperatorAckRequired = false;
        _lastFailSafeReason = "Operator released the system for normal operation.";
        PublishSnapshot();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SimulateFeedbackAsync(int inPort, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_activeProjectContext == null)
        {
            _statusText = "Error: No active project. Load or select a project first.";
            PublishSnapshot();
            return Task.CompletedTask;
        }

        try
        {
            _z21.SimulateFeedback(inPort);
            _statusText = $"Simulated feedback for InPort {inPort}";
        }
        catch (Exception ex)
        {
            _statusText = $"Error: {ex.Message}";
        }

        PublishSnapshot();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ResetJourneyAsync(Guid journeyId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_activeProjectContext == null)
        {
            return Task.CompletedTask;
        }

        var journey = _activeProjectContext.ActiveProject.Journeys.FirstOrDefault(j => j.Id == journeyId);
        if (journey == null)
        {
            return Task.CompletedTask;
        }

        _activeProjectContext.JourneyManager.Reset(journey);
        _statusText = $"Journey '{journey.Name}' reset";
        PublishSnapshot();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SetSignalAspectAsync(SbSignal signal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signal);

        if (!signal.IsMultiplexed)
        {
            _logger.LogWarning(
                "Signal '{SignalName}' (ID: {SignalId}) is not marked as multiplexed. Configure IsMultiplexed=true.",
                signal.Name,
                signal.Id.ToString()[..8]);
            return;
        }

        if (string.IsNullOrEmpty(signal.MultiplexerArticleNumber))
        {
            _logger.LogWarning("Signal '{SignalName}': Multiplexer article number not configured.", signal.Name);
            return;
        }

        if (signal.BaseAddress <= 0 || signal.BaseAddress > 2044)
        {
            _logger.LogWarning(
                "Signal '{SignalName}': Invalid base address {Address}. Must be 1-2044.",
                signal.Name,
                signal.BaseAddress);
            return;
        }

        if (signal.BaseAddress % 2 == 0)
        {
            _logger.LogWarning(
                "Signal '{SignalName}': Base address {Address} must be odd (Viessmann DCC multiplexer pairing).",
                signal.Name,
                signal.BaseAddress);
            return;
        }

        if (!MultiplexerHelper.TryGetMaxAddressOffset(
                signal.MultiplexerArticleNumber,
                signal.MainSignalArticleNumber,
                out var maxOffset))
        {
            _logger.LogWarning(
                "Signal '{SignalName}': No multiplexer address mapping for multiplexer {Mux} and signal article {Article}.",
                signal.Name,
                signal.MultiplexerArticleNumber,
                signal.MainSignalArticleNumber ?? "(default)");
            return;
        }

        if (signal.BaseAddress + maxOffset > 2044)
        {
            _logger.LogWarning(
                "Signal '{SignalName}': Base address {Address} with max offset {MaxOffset} exceeds DCC limit 2044.",
                signal.Name,
                signal.BaseAddress,
                maxOffset);
            return;
        }

        if (!_z21.IsConnected)
        {
            _logger.LogWarning("Signal '{SignalName}': Z21 not connected; skipping command send.", signal.Name);
            return;
        }

        try
        {
            if (!MultiplexerHelper.TryGetTurnoutCommand(
                    signal.MultiplexerArticleNumber,
                    signal.MainSignalArticleNumber,
                    signal.SignalAspect,
                    out var turnoutCommand))
            {
                _logger.LogWarning(
                    "Signal '{SignalName}': Aspect {Aspect} not supported by multiplexer mapping.",
                    signal.Name,
                    signal.SignalAspect);
                return;
            }

            var dccAddress = signal.BaseAddress + turnoutCommand.AddressOffset;
            if (dccAddress is < 1 or > 2044)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(signal.BaseAddress),
                    $"Calculated DCC address {dccAddress} is outside the valid range (1-2044).");
            }

            var activate = turnoutCommand.Activate;
            if (ShouldInvertPolarityForOffset(turnoutCommand.AddressOffset))
            {
                activate = !activate;
            }

            await _z21.SetTurnoutAsync(
                    dccAddress,
                    turnoutCommand.Output,
                    activate,
                    false,
                    cancellationToken)
                .ConfigureAwait(false);

            _statusText = $"Signal '{signal.Name}' gestellt: DCC-Adresse {dccAddress}, Ausgang {turnoutCommand.Output}, Activate={activate}";
            PublishSnapshot();
        }
        catch (Exception ex)
        {
            _statusText = $"❌ Signal-Fehler: {ex.Message}";
            PublishSnapshot();
            _logger.LogError(ex, "Failed to set signal aspect for '{SignalName}'", signal.Name);
            throw;
        }
    }
    public async Task SendTurnoutCommandAsync(int decoderAddress, int output, bool activate, bool queue = false, CancellationToken cancellationToken = default)
    {
        if (!_z21.IsConnected)
        {
            _logger.LogWarning("Raw turnout command skipped because Z21 is not connected");
            _statusText = "⚠️ Z21 nicht verbunden";
            PublishSnapshot();
            return;
        }

        await _z21.SetTurnoutAsync(decoderAddress, output, activate, queue, cancellationToken).ConfigureAwait(false);
        _statusText = $"Turnout gestellt: DCC-Adresse {decoderAddress}, Ausgang {output}, Activate={activate}, Queue={queue}";
        PublishSnapshot();
    }

    /// <inheritdoc />
    public IReadOnlyList<Z21TrafficPacket> GetTrafficPackets()
    {
        return [.. (_z21.TrafficMonitor?.GetPackets() ?? Enumerable.Empty<Z21TrafficPacket>())];
    }

    /// <inheritdoc />
    public void ClearTrafficMonitor()
    {
        _z21.TrafficMonitor?.Clear();
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
        _z21.Received -= OnZ21FeedbackReceived;
        _workflowService.ActionExecutionError -= OnActionExecutionError;

        if (_z21.TrafficMonitor != null)
        {
            _z21.TrafficMonitor.PacketLogged -= OnTrafficPacketLogged;
        }

        StopAutoConnectTimer();
        ReplaceActiveProjectContext(null);
    }

    private void ReplaceActiveProjectContext(ActiveProjectContext? nextContext)
    {
        if (_activeProjectContext != null)
        {
            _activeProjectContext.JourneyManager.StationChanged -= OnJourneyRuntimeChanged;
            _activeProjectContext.JourneyManager.FeedbackReceived -= OnJourneyRuntimeChanged;
            _activeProjectContext.Dispose();
        }

        _activeProjectContext = nextContext;
    }

    private async Task TryAutoConnectToZ21Async()
    {
        if (string.IsNullOrEmpty(_settings.Z21.CurrentIpAddress))
        {
            _isZ21Connecting = false;
            _statusText = "No Z21 IP configured";
            PublishSnapshot();
            return;
        }

        _isZ21Connecting = true;
        _statusText = $"Connecting to {_settings.Z21.CurrentIpAddress}...";
        PublishSnapshot();

        await AttemptZ21ConnectionAsync().ConfigureAwait(false);
        StartAutoConnectTimer();
    }

    private void StartAutoConnectTimer()
    {
        StopAutoConnectTimer();

        var retryInterval = TimeSpan.FromSeconds(_settings.Z21.AutoConnectRetryIntervalSeconds);
        _z21AutoConnectTimer = new Timer(
            state =>
            {
                _ = state;
                if (!_isConnected && !_isManualDisconnectRequested)
                {
                    _ = AttemptZ21ConnectionAsync();
                }
            },
            null,
            retryInterval,
            retryInterval);

        _logger.LogInformation(
            "Z21 auto-connect retry timer started ({RetryInterval}s interval)",
            _settings.Z21.AutoConnectRetryIntervalSeconds);
    }

    private void StopAutoConnectTimer()
    {
        _z21AutoConnectTimer?.Dispose();
        _z21AutoConnectTimer = null;
    }

    private async Task AttemptZ21ConnectionAsync()
    {
        if (Interlocked.CompareExchange(ref _autoConnectAttemptInProgress, 1, 0) == 1)
        {
            return;
        }

        try
        {
            if (_isConnected || _isManualDisconnectRequested)
            {
                return;
            }

            if (!TryGetConfiguredEndpoint(out var address, out var port, out var errorMessage))
            {
                _isZ21Connecting = false;
                _statusText = errorMessage;
                PublishSnapshot();
                return;
            }

            try
            {
                _isZ21Connecting = true;
                _statusText = "Connecting to Z21...";
                PublishSnapshot();

                _z21.SetSystemStatePollingInterval(_settings.Z21.SystemStatePollingIntervalSeconds);
                await _z21.ConnectAsync(address!, port).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _isZ21Connecting = false;
                _statusText = $"Z21 unavailable: {ex.Message}";
                PublishSnapshot();
                _logger.LogWarning(ex, "Automatic Z21 connection attempt failed");
            }
        }
        finally
        {
            Interlocked.Exchange(ref _autoConnectAttemptInProgress, 0);
        }
    }

    private bool TryGetConfiguredEndpoint(out IPAddress? address, out int port, out string errorMessage)
    {
        address = null;
        port = 21105;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(_settings.Z21.CurrentIpAddress))
        {
            errorMessage = "No IP address configured in AppSettings";
            return false;
        }

        if (!IPAddress.TryParse(_settings.Z21.CurrentIpAddress, out address))
        {
            errorMessage = $"Invalid Z21 IP address '{_settings.Z21.CurrentIpAddress}'";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(_settings.Z21.DefaultPort)
            && int.TryParse(_settings.Z21.DefaultPort, out var parsedPort))
        {
            port = parsedPort;
        }

        return true;
    }

    private void OnZ21ConnectedChanged(bool connected)
    {
        _isConnected = connected;
        _isZ21Connecting = false;
        _statusText = connected
            ? GetConnectedStatusText()
            : GetDisconnectedStatusText();

        PublishSnapshot();
    }

    private void OnZ21ConnectionLost()
    {
        _isConnected = false;
        _isTrackPowerOn = false;
        _isEmergencyStopActive = false;
        _isShortCircuitActive = false;
        _isProgrammingModeActive = false;
        _statusText = "Connection lost - reconnect required";
        TriggerFailSafe("Unexpected loss of the Z21 connection.");
        PublishSnapshot();
    }

    private void OnZ21SystemStateChanged(SystemState systemState)
    {
        _isConnected = true;
        _isZ21Connecting = false;
        _hasSeenSuccessfulZ21Connection = true;

        _isTrackPowerOn = systemState.IsTrackPowerOn;
        _isEmergencyStopActive = systemState.IsEmergencyStop;
        _isShortCircuitActive = systemState.IsShortCircuit;
        _isProgrammingModeActive = systemState.IsProgrammingMode;

        _mainCurrent = systemState.MainCurrent;
        _progCurrent = systemState.ProgCurrent;
        _filteredMainCurrent = systemState.FilteredMainCurrent;
        _temperature = systemState.Temperature;
        _supplyVoltage = systemState.SupplyVoltage;
        _vccVoltage = systemState.VccVoltage;
        _statusText = BuildSystemStateStatusText(systemState);

        PublishSnapshot();
    }

    private void OnZ21XBusStatusChanged(XBusStatus xBusStatus)
    {
        _isTrackPowerOn = !xBusStatus.TrackOff;
        _isEmergencyStopActive = xBusStatus.EmergencyStop;
        _isShortCircuitActive = xBusStatus.ShortCircuit;
        _isProgrammingModeActive = xBusStatus.Programming;
        PublishSnapshot();
    }

    private void OnZ21VersionInfoChanged(Z21VersionInfo versionInfo)
    {
        _serialNumber = versionInfo.SerialNumber == 0 ? "-" : versionInfo.SerialNumber.ToString();
        _firmwareVersion = versionInfo.FirmwareVersionCode == 0 ? "-" : versionInfo.FirmwareVersion;
        _hardwareType = versionInfo.HardwareTypeCode == 0 ? "-" : versionInfo.HardwareType;
        PublishSnapshot();
    }

    private void OnJourneyRuntimeChanged(object? sender, EventArgs args)
    {
        _ = sender;
        _ = args;
        PublishSnapshot();
    }

    private void OnZ21LocomotiveInfoChanged(LocoInfo locoInfo)
    {
        ArgumentNullException.ThrowIfNull(locoInfo);

        _locomotiveStates[locoInfo.Address] = new LocomotiveRuntimeSnapshot
        {
            Address = locoInfo.Address,
            Speed = locoInfo.Speed,
            IsForward = locoInfo.IsForward,
            Functions = locoInfo.Functions
        };

        PublishSnapshot();
    }

    private void OnZ21FeedbackReceived(FeedbackResult feedback)
    {
        FeedbackReceived?.Invoke(this, feedback);
    }

    private void OnActionExecutionError(object? sender, ActionExecutionErrorEventArgs e)
    {
        _ = sender;
        _statusText = $"❌ Action '{e.Action.Name}' failed: {e.ErrorMessage}";
        PublishSnapshot();
        _logger.LogError(e.Exception, "Action '{ActionName}' execution failed: {ErrorMessage}", e.Action.Name, e.ErrorMessage);
    }

    private void OnTrafficPacketLogged(object? sender, Z21TrafficPacket packet)
    {
        TrafficPacketLogged?.Invoke(this, packet);
    }

    private void TriggerFailSafe(string reason)
    {
        _lastFailSafeReason = string.IsNullOrWhiteSpace(reason)
            ? "Unexpected loss of the Z21 connection."
            : reason.Trim();
        _lastFailSafeAt = DateTimeOffset.Now;
        _isManualDisconnectRequested = false;
        _isZ21Connecting = false;
        _isOperatorAckRequired = true;
    }

    private bool ShouldInvertPolarityForOffset(int addressOffset)
    {
        return _settings.SignalBox.GetInvertPolarityForOffset(addressOffset);
    }

    private string BuildSystemStateStatusText(SystemState systemState)
    {
        List<string> warnings = [];
        if (systemState.IsEmergencyStop)
        {
            warnings.Add("EMERGENCY STOP");
        }

        if (systemState.IsShortCircuit)
        {
            warnings.Add("SHORT CIRCUIT");
        }

        if (systemState.IsProgrammingMode)
        {
            warnings.Add("Programming");
        }

        return warnings.Count > 0
            ? $"Connected | {string.Join(" | ", warnings)}"
            : "Connected";
    }

    private string GetConnectedStatusText()
    {
        _hasSeenSuccessfulZ21Connection = true;
        _isManualDisconnectRequested = false;
        return $"Connected to {_settings.Z21.CurrentIpAddress}";
    }

    private string GetDisconnectedStatusText()
    {
        return _isManualDisconnectRequested
            ? "Disconnected"
            : "Z21 disconnected";
    }

    private void PublishSnapshot()
    {
        var snapshot = CreateSnapshot();
        Current = snapshot;
        SnapshotChanged?.Invoke(this, snapshot);
    }

    private MobaRuntimeSnapshot CreateSnapshot()
    {
        var journeyStates = new Dictionary<Guid, JourneyRuntimeSnapshot>();

        if (_activeProjectContext != null)
        {
            foreach (var journey in _activeProjectContext.ActiveProject.Journeys)
            {
                var state = _activeProjectContext.JourneyManager.GetState(journey.Id);
                if (state == null)
                {
                    continue;
                }

                journeyStates[journey.Id] = new JourneyRuntimeSnapshot
                {
                    JourneyId = journey.Id,
                    Counter = state.Counter,
                    CurrentPos = state.CurrentPos,
                    CurrentStationName = state.CurrentStationName,
                    LastFeedbackTime = state.LastFeedbackTime,
                    IsActive = state.IsActive
                };
            }
        }

        return new MobaRuntimeSnapshot
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
            IsOperatorAckRequired = _isOperatorAckRequired,
            JourneyStates = journeyStates,
            LocomotiveStates = new Dictionary<int, LocomotiveRuntimeSnapshot>(_locomotiveStates),
            CreatedAt = DateTimeOffset.Now
        };
    }
}





