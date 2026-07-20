// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

using Model;

using Protocol;

using Service;

using System.Net;

/// <summary>
/// Delegate for Z21 feedback events carrying a parsed <see cref="FeedbackResult"/>.
/// </summary>
/// <param name="feedbackContent">Parsed feedback data including InPort and raw packet.</param>
public delegate void Feedback(FeedbackResult feedbackContent);

/// <summary>
/// Delegate for Z21 system state change events.
/// </summary>
/// <param name="systemState">The current system state snapshot.</param>
public delegate void SystemStateChanged(SystemState systemState);

/// <summary>
/// Delegate for X‑Bus status change events (emergency stop, track off, short circuit, programming mode).
/// </summary>
/// <param name="status">The current X‑Bus status flags.</param>
public delegate void XBusStatusChanged(XBusStatus status);

/// <summary>
/// Delegate for Z21 version info change events (serial number, firmware, hardware).
/// </summary>
/// <param name="versionInfo">The current version information.</param>
public delegate void VersionInfoChanged(Z21VersionInfo versionInfo);

/// <summary>
/// Connection, power and feedback role of the Z21 command station.
/// </summary>
public interface IZ21Connection : IDisposable
{
    event Feedback? Received;

    event SystemStateChanged? OnSystemStateChanged;

    event XBusStatusChanged? OnXBusStatusChanged;

    event Action? OnConnectionLost;

    event Action<bool>? OnConnectedChanged;

    bool IsConnected { get; }

    Task ConnectAsync(IPAddress address, int port = 21105, CancellationToken cancellationToken = default);

    Task DisconnectAsync();

    Task SetTrackPowerOnAsync(CancellationToken cancellationToken = default);

    Task SetTrackPowerOffAsync(CancellationToken cancellationToken = default);

    Task SetEmergencyStopAsync(CancellationToken cancellationToken = default);

    Task GetStatusAsync(CancellationToken cancellationToken = default);

    Task RecoverConnectionAsync(IPAddress address, int port = 21105, CancellationToken cancellationToken = default);
}

/// <summary>
/// Locomotive control role of the Z21 command station.
/// </summary>
public interface ILocoControl
{
    event Action<LocoInfo>? OnLocoInfoChanged;

    Task SetLocoDriveAsync(int address, int speed, bool forward, CancellationToken cancellationToken = default);

    Task SetLocoFunctionAsync(int address, int functionIndex, bool on, CancellationToken cancellationToken = default);

    Task SetAllLocoFunctionsOffAsync(int address, CancellationToken cancellationToken = default);

    Task GetLocoInfoAsync(int address, CancellationToken cancellationToken = default);
}

/// <summary>
/// Accessory and turnout control role of the Z21 command station.
/// </summary>
public interface IAccessoryControl
{
    Task SetTurnoutAsync(int decoderAddress, int output, bool activate, bool queue = false, CancellationToken cancellationToken = default);

    Task SetExtAccessoryAsync(int extAccessoryAddress, int commandValue, CancellationToken cancellationToken = default);

    Task GetTurnoutInfoAsync(int decoderAddress, CancellationToken cancellationToken = default);
}

/// <summary>
/// Diagnostic, raw command and monitoring role of the Z21 command station.
/// </summary>
public interface IZ21Diagnostics
{
    event VersionInfoChanged? OnVersionInfoChanged;

    Z21Monitor? TrafficMonitor { get; }

    Z21VersionInfo? VersionInfo { get; }

    /// <summary>
    /// Gets current diagnostics for the ordered application-event pipeline.
    /// </summary>
    Z21EventPipelineSnapshot EventPipelineSnapshot { get; }

    Task SendCommandAsync(byte[] sendBytes);

    Task GetRailComDataAsync(int address, CancellationToken cancellationToken = default);

    void SimulateFeedback(int inPort);

    void SetSystemStatePollingInterval(int intervalSeconds);
}

/// <summary>
/// Backward-compatible aggregate facade for existing Z21 consumers.
/// Prefer the narrower role interfaces for new code.
/// </summary>
public interface IZ21 : IZ21Connection, ILocoControl, IAccessoryControl, IZ21Diagnostics
{
}
