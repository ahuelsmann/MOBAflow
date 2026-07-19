// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend;

using Common.Events;

using Microsoft.Extensions.Logging;

using Model;

using Network;

using Protocol;

using Service;

public partial class Z21
{
    #region Message Receiving & Parsing
    /// <summary>
    /// Publishes an event asynchronously without blocking the UDP receiver.
    /// Events are queued to a background task so the UDP callback returns immediately.
    /// </summary>
    /// <typeparam name="TEvent">The event type</typeparam>
    /// <param name="event">The event instance to publish</param>
    private void PublishEventAsync<TEvent>(TEvent @event) where TEvent : class, IEvent
    {
        // Fire-and-forget: publish on thread pool without awaiting
        // This allows OnUdpReceived to return immediately
        _ = Task.Run(() =>
        {
            try
            {
                _eventBus.Publish(@event);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error publishing event {EventType}", typeof(TEvent).Name);
            }
        });
    }

    private void UpdateAndPublishVersionInfo(Action<Z21VersionInfo> updateAction)
    {
        VersionInfo ??= new Z21VersionInfo();
        updateAction(VersionInfo);
        OnVersionInfoChanged?.Invoke(VersionInfo);
        PublishEventAsync(new VersionInfoChangedEvent(
            VersionInfo.SerialNumber,
            (int)VersionInfo.HardwareTypeCode,
            (int)VersionInfo.FirmwareVersionCode));
    }

    private void OnUdpReceived(object? sender, UdpReceivedEventArgs e)
    {
        var content = e.Buffer;
        if (content.Length < 4)
        {
            _logger?.LogWarning("Short UDP packet received {Length} bytes: {Payload}", content.Length, Z21Protocol.ToHex(content));
            return;
        }

        // Log received packet to traffic monitor
        // Only call ParsePacketType when actually logging (deferred execution)
        _trafficMonitor?.LogReceivedPacket(
                content,
                Z21Monitor.ParsePacketType(content),
                $"Length: {content.Length} bytes"
            );

        // Log all received packets for debugging
        _logger?.LogDebug("UDP received {Length} bytes: {Payload}", content.Length, Z21Protocol.ToHex(content));

        if (Z21MessageParser.IsLanXHeader(content))
        {
            var xStatus = Z21MessageParser.TryParseXBusStatus(content);
            if (xStatus != null)
            {
                // Z21 is responding - mark as connected
                SetConnectedIfNotAlready();

                OnXBusStatusChanged?.Invoke(xStatus);
                PublishEventAsync(new XBusStatusChangedEvent(
                    xStatus.EmergencyStop,
                    xStatus.TrackOff,
                    xStatus.ShortCircuit,
                    xStatus.Programming));
                _logger?.LogDebug("XBus Status: EmergencyStop={EmergencyStop}, TrackOff={TrackOff}, ShortCircuit={ShortCircuit}, Programming={Programming}", xStatus.EmergencyStop, xStatus.TrackOff, xStatus.ShortCircuit, xStatus.Programming);
            }

            // Parse LocoInfo response
            if (Z21MessageParser.TryParseLocoInfo(content, out var locoInfo) && locoInfo != null)
            {
                // Z21 is responding - mark as connected
                SetConnectedIfNotAlready();

                OnLocoInfoChanged?.Invoke(locoInfo);
                PublishEventAsync(new LocomotiveInfoChangedEvent(
                    locoInfo.Address,
                    locoInfo.Speed,
                    locoInfo.IsForward,
                    locoInfo.IsF0On,
                    locoInfo.GetFunction(1),
                    locoInfo.GetFunction(2),
                    locoInfo.GetFunction(3),
                    locoInfo.GetFunction(4),
                    locoInfo.GetFunction(5),
                    locoInfo.GetFunction(6),
                    locoInfo.GetFunction(7),
                    locoInfo.GetFunction(8),
                    locoInfo.GetFunction(9),
                    locoInfo.GetFunction(10),
                    locoInfo.GetFunction(11),
                    locoInfo.GetFunction(12),
                    locoInfo.GetFunction(13),
                    locoInfo.GetFunction(14),
                    locoInfo.GetFunction(15),
                    locoInfo.GetFunction(16),
                    locoInfo.GetFunction(17),
                    locoInfo.GetFunction(18),
                    locoInfo.GetFunction(19),
                    locoInfo.GetFunction(20)));
                _logger?.LogInformation("Loco Info: {LocoInfo}", locoInfo);
            }

            // Parse LAN_X_GET_VERSION response (0x63) - some Z21 firmware only send this instead of LAN_GET_SERIAL_NUMBER / LAN_GET_HWINFO
            if (Z21MessageParser.TryParseLanXGetVersionResponse(content, out var xbusVer, out var cmdstId))
            {
                SetConnectedIfNotAlready();
                UpdateAndPublishVersionInfo(versionInfo =>
                {
                    // Preserve existing SerialNumber/HardwareTypeCode if already set; otherwise encode X-Bus version in FirmwareVersionCode for display.
                    if (versionInfo.SerialNumber == 0 && versionInfo.HardwareTypeCode == 0)
                    {
                        versionInfo.FirmwareVersionCode = xbusVer; // Display as V0.xx (e.g. V0.40 for xbusVer=0x40)
                        _logger?.LogInformation("Z21 LAN_X_GET_VERSION: X-Bus 0x{XBusVer:X2}, CMDST_ID 0x{CmdstId:X4}", xbusVer, cmdstId);
                    }
                });
            }

            return;
        }

        // Parse SystemState (0x84) - separate from RBusFeedback!
        if (Z21MessageParser.IsSystemState(content))
        {
            if (Z21MessageParser.TryParseSystemState(content, out var mainCurrent, out var progCurrent, out var filteredMainCurrent, out var temperature, out var supplyVoltage, out var vccVoltage, out var centralState, out var centralStateEx))
            {
                // Z21 is responding - mark as connected
                SetConnectedIfNotAlready();

                CurrentSystemState = new SystemState
                {
                    MainCurrent = mainCurrent,
                    ProgCurrent = progCurrent,
                    FilteredMainCurrent = filteredMainCurrent,
                    Temperature = temperature,
                    SupplyVoltage = supplyVoltage,
                    VccVoltage = vccVoltage,
                    CentralState = centralState,
                    CentralStateEx = centralStateEx
                };

                _logger?.LogInformation("SystemState received: MainCurrent={MainCurrent}mA, Temp={Temp}C, Voltage={Voltage}mV",
                    mainCurrent, temperature, supplyVoltage);
                OnSystemStateChanged?.Invoke(CurrentSystemState);
                PublishEventAsync(new SystemStateChangedEvent(
                    CurrentSystemState.MainCurrent,
                    CurrentSystemState.ProgCurrent,
                    CurrentSystemState.FilteredMainCurrent,
                    CurrentSystemState.Temperature,
                    CurrentSystemState.SupplyVoltage,
                    CurrentSystemState.VccVoltage,
                    CurrentSystemState.CentralState,
                    CurrentSystemState.CentralStateEx));
            }
            else
            {
                _logger?.LogWarning("Failed to parse SystemState packet");
            }
            return;
        }

        // Parse RBusFeedback (0x80) - occupancy detection
        if (Z21MessageParser.IsRBusFeedback(content))
        {
            var groupNumber = Z21FeedbackParser.GetGroupNumber(content);
            var activeInPorts = Z21FeedbackParser.ExtractAllInPorts(content).ToHashSet();
            _feedbackStatesByGroup.TryGetValue(groupNumber, out var previousState);
            previousState ??= [];

            foreach (var inPort in activeInPorts.Except(previousState).Order())
            {
                var feedback = new FeedbackResult(content, inPort);
                Received?.Invoke(feedback);
                PublishEventAsync(new FeedbackReceivedEvent(inPort));
                _logger?.LogDebug("R-Bus Feedback activated: InPort={InPort}", inPort);
            }

            _feedbackStatesByGroup[groupNumber] = activeInPorts;
            return;
        }

        // Parse serial number response
        if (Z21MessageParser.IsSerialNumber(content))
        {
            if (Z21MessageParser.TryParseSerialNumber(content, out var serialNumber))
            {
                // Z21 is responding - mark as connected
                SetConnectedIfNotAlready();

                UpdateAndPublishVersionInfo(versionInfo => versionInfo.SerialNumber = serialNumber);
                _logger?.LogInformation("Z21 Serial Number: {SerialNumber}", serialNumber);
            }
            return;
        }

        // Parse hardware info response
        if (Z21MessageParser.IsHwInfo(content))
        {
            if (Z21MessageParser.TryParseHwInfo(content, out var hwType, out var fwVersion))
            {
                UpdateAndPublishVersionInfo(versionInfo =>
                {
                    versionInfo.HardwareTypeCode = hwType;
                    versionInfo.FirmwareVersionCode = fwVersion;
                });
                var versionInfo = VersionInfo;
                if (versionInfo != null)
                {
                    _logger?.LogInformation("Z21 Hardware: {HwType}, Firmware: {FwVersion}", versionInfo.HardwareType, versionInfo.FirmwareVersion);
                }
            }
            return;
        }

        _logger?.LogWarning("Unknown message: {Payload}", Z21Protocol.ToHex(content));
    }
    #endregion
}
