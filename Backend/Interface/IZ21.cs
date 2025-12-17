// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

using Protocol;
using Service;
using System.Net;

public delegate void Feedback(FeedbackResult feedbackContent);
public delegate void SystemStateChanged(SystemState systemState);
public delegate void XBusStatusChanged(XBusStatus status);
public delegate void VersionInfoChanged(Z21VersionInfo versionInfo);

public interface IZ21 : IDisposable
{
    event Feedback? Received;
    event SystemStateChanged? OnSystemStateChanged;
    event XBusStatusChanged? OnXBusStatusChanged;
    event VersionInfoChanged? OnVersionInfoChanged;
    event Action? OnConnectionLost;

    bool IsConnected { get; }
    Z21Monitor? TrafficMonitor { get; }

    /// <summary>
    /// Current version information of the Z21 (serial number, firmware, hardware).
    /// Updated after successful connection via LAN_GET_SERIAL_NUMBER and LAN_GET_HWINFO.
    /// </summary>
    Z21VersionInfo? VersionInfo { get; }

    Task ConnectAsync(IPAddress address, int port = 21105, CancellationToken cancellationToken = default);
    Task DisconnectAsync();

    Task SendCommandAsync(byte[] sendBytes);

    /// <summary>
    /// Turns the track power ON.
    /// LAN_X_SET_TRACK_POWER_ON (X-Header: 0x21, DB0: 0x81)
    /// </summary>
    Task SetTrackPowerOnAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Turns the track power OFF.
    /// LAN_X_SET_TRACK_POWER_OFF (X-Header: 0x21, DB0: 0x80)
    /// </summary>
    Task SetTrackPowerOffAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers an emergency stop (locomotives stop but track power remains on).
    /// LAN_X_SET_STOP (X-Header: 0x80)
    /// </summary>
    Task SetEmergencyStopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests the current status from the Z21.
    /// </summary>
    Task GetStatusAsync(CancellationToken cancellationToken = default);

    void SimulateFeedback(int inPort);
}
