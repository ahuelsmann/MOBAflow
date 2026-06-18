// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Backend;
using Common.Events;
using Common.Runtime;
using Events;
using Microsoft.Extensions.Logging;
using Model;
using Protocol;

/// <summary>
/// Z21 callbacks, journey projection refresh, and fail-safe handling for <see cref="MobaRuntimeService"/>.
/// </summary>
public sealed partial class MobaRuntimeService
{
    private void OnZ21ConnectedChanged(bool connected)
    {
        _isConnected = connected;
        _isZ21Connecting = false;
        if (connected)
        {
            _hasSeenSuccessfulZ21Connection = true;
            _isManualDisconnectRequested = false;
        }

        _statusText = connected
            ? MobaRuntimeStatusFormatter.GetConnectedStatusText(_settings.Z21.CurrentIpAddress)
            : MobaRuntimeStatusFormatter.GetDisconnectedStatusText(_isManualDisconnectRequested);

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
        _statusText = MobaRuntimeStatusFormatter.BuildSystemStateStatusText(systemState);

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
        _eventBus?.Publish(new FeedbackReceivedEvent(feedback.InPort));
    }

    private void OnActionExecutionError(object? sender, ActionExecutionErrorEventArgs e)
    {
        _ = sender;
        _statusText = $"Action '{e.Action.Name}' failed: {e.ErrorMessage}";
        PublishSnapshot();
        _logger.LogError(e.Exception, "Action '{ActionName}' execution failed: {ErrorMessage}", e.Action.Name, e.ErrorMessage);
    }

    private void OnTrafficPacketLogged(object? sender, Z21TrafficPacket packet)
    {
        _ = sender;
        _eventBus?.Publish(new Z21TrafficPacketLoggedEvent(packet));
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
}