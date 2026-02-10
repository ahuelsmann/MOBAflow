// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

using Model;
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
    
    /// <summary>
    /// Raised when the connection state changes (true = connected and responding, false = disconnected).
    /// </summary>
    event Action<bool>? OnConnectedChanged;

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
    /// LAN_X_GET_STATUS (X-Header: 0x21, DB0: 0x24)
    /// </summary>
    Task GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to recover a non-responsive Z21 by sending a recovery byte sequence.
    /// Sends: Emergency Stop, LOGOFF, GET_SERIAL_NUMBER, Handshake, Broadcast Flags, Get Status.
    /// If not connected, establishes connection to specified address first.
    /// </summary>
    /// <param name="address">IP address of Z21</param>
    /// <param name="port">UDP port (default: 21105)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecoverConnectionAsync(IPAddress address, int port = 21105, CancellationToken cancellationToken = default);

            /// <summary>
            /// Simulates a feedback event for testing purposes (WinUI only).
            /// </summary>
            void SimulateFeedback(int inPort);

            /// <summary>
            /// Sets the system state polling interval. Use 0 to disable polling.
            /// Changes take effect immediately if connected.
            /// </summary>
            /// <param name="intervalSeconds">Polling interval in seconds (0 = disabled, 1-30 recommended).</param>
            void SetSystemStatePollingInterval(int intervalSeconds);

            // ==================== Locomotive Drive Commands (Section 4) ====================

            /// <summary>
            /// Sets locomotive speed and direction.
            /// LAN_X_SET_LOCO_DRIVE: 0xE4 0x1S Adr_MSB Adr_LSB RVVVVVVV XOR
            /// Uses 128 speed steps by default for maximum precision.
            /// </summary>
            /// <param name="address">DCC locomotive address (1-9999)</param>
            /// <param name="speed">Speed value (0-126, where 0=stop, 1=emergency stop)</param>
            /// <param name="forward">True = forward direction, False = backward</param>
            /// <param name="cancellationToken">Cancellation token</param>
            Task SetLocoDriveAsync(int address, int speed, bool forward, CancellationToken cancellationToken = default);

            /// <summary>
            /// Sets a locomotive function on/off.
            /// LAN_X_SET_LOCO_FUNCTION: 0xE4 0xF8 Adr_MSB Adr_LSB TTNNNNNN XOR
            /// </summary>
            /// <param name="address">DCC locomotive address (1-9999)</param>
            /// <param name="functionIndex">Function index (0=F0/light, 1=F1/sound, etc.)</param>
            /// <param name="on">True = function on, False = function off</param>
            /// <param name="cancellationToken">Cancellation token</param>
            Task SetLocoFunctionAsync(int address, int functionIndex, bool on, CancellationToken cancellationToken = default);

            /// <summary>
            /// Requests locomotive information and subscribes to updates for this address.
            /// LAN_X_GET_LOCO_INFO: 0xE3 0xF0 Adr_MSB Adr_LSB XOR
            /// Max 16 loco addresses can be subscribed per client (FIFO).
            /// </summary>
            /// <param name="address">DCC locomotive address (1-9999)</param>
            /// <param name="cancellationToken">Cancellation token</param>
            Task GetLocoInfoAsync(int address, CancellationToken cancellationToken = default);

            /// <summary>
            /// Raised when locomotive info is received from Z21.
            /// Contains: address, speed, direction, function states.
            /// </summary>
            event Action<LocoInfo>? OnLocoInfoChanged;

            // ==================== RailCom (Section 8) - Future Enhancement ====================

            /// <summary>
            /// Requests RailCom data for a specific locomotive.
            /// LAN_RAILCOM_GETDATA: 07-00-89-00 + Type (0x01) + LocoAddr (16-bit little-endian)
            /// 
            /// RailCom allows bidirectional DCC communication:
            /// - Decoder-reported current consumption
            /// - Decoder temperature monitoring
            /// - Position detection
            /// - CV readback on main track
            /// 
            /// Requirements:
            /// - Z21 firmware 1.29 or higher
            /// - RailCom-capable decoder in locomotive
            /// - RailCom enabled in Z21 settings
            /// 
            /// Note: Currently prepared but not yet fully implemented.
            /// </summary>
            /// <param name="address">DCC locomotive address (1-9999)</param>
            /// <param name="cancellationToken">Cancellation token</param>
            /// <returns>Task that completes when request is sent</returns>
            Task GetRailComDataAsync(int address, CancellationToken cancellationToken = default);

        // ==================== Switching Commands (Accessory Decoders / Signals) ====================

        /// <summary>
        /// Sets a classic DCC turnout or 2-output signal decoder position.
        /// LAN_X_SET_TURNOUT: 0x53 FAdr_MSB FAdr_LSB 10Q0A00P XOR
        /// 
        /// Each decoder address controls 2 outputs (P=0 or P=1).
        /// Common usage: turnouts, 2-output signal decoders.
        /// </summary>
        /// <param name="decoderAddress">Accessory decoder address (1-2044)</param>
        /// <param name="output">Output index: 0 = output 1, 1 = output 2</param>
        /// <param name="activate">True = activate/set, False = deactivate/reset</param>
        /// <param name="queue">True = queue (FW 1.24+), False = immediate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SetTurnoutAsync(int decoderAddress, int output, bool activate, bool queue = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets an extended accessory decoder (e.g., multiplex signal decoder 5229).
        /// LAN_X_SET_EXT_ACCESSORY: 0x54 Adr_MSB Adr_LSB DDDDDDDD 0x00 XOR
        /// 
        /// Supports 256 command values (0-255) per address for maximum flexibility.
        /// Used for multiplex signal decoders with multiple signal aspect combinations.
        /// Example: KS signal with H0 multiplex technology can encode different lamp combinations.
        /// </summary>
        /// <param name="extAccessoryAddress">Extended accessory decoder address (0-255)</param>
        /// <param name="commandValue">Command value (0-255) selecting a specific signal aspect/configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SetExtAccessoryAsync(int extAccessoryAddress, int commandValue, CancellationToken cancellationToken = default);

        /// <summary>
        /// Requests the current status/position of a turnout or signal decoder.
        /// LAN_X_GET_TURNOUT_INFO: 0x43 FAdr_MSB FAdr_LSB XOR
        /// </summary>
        /// <param name="decoderAddress">Accessory decoder address (1-2044)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task GetTurnoutInfoAsync(int decoderAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets a signal aspect for a multiplex signal decoder.
        /// This is a high-level convenience method combining signal addressing with multiplex control.
        /// </summary>
        /// <param name="signal">The signal element containing address, aspect, and multiplex settings</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SetSignalAspectAsync(Domain.SbSignal signal, CancellationToken cancellationToken = default);
        }
