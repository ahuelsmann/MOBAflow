// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Events;

/// <summary>
/// Z21 Connection Events - fired when Z21 device connection status changes.
/// </summary>
public sealed record Z21ConnectionEstablishedEvent : EventBase;

/// <summary>
/// Z21 connection was lost.
/// </summary>
public sealed record Z21ConnectionLostEvent : EventBase;

/// <summary>
/// Track power state has changed.
/// </summary>
/// <param name="IsOn">True if track power is on; false if it is off.</param>
public sealed record Z21TrackPowerChangedEvent(
    bool IsOn) : EventBase;

/// <summary>
/// Z21 system events - fired when Z21 system state updates.
/// These events contain raw data extracted from backend model classes to avoid circular dependencies.
/// </summary>
public sealed record XBusStatusChangedEvent(
    /// <summary>True if emergency stop is active.</summary>
    bool EmergencyStop,
    /// <summary>True if track power is off.</summary>
    bool TrackOff,
    /// <summary>True if a short circuit is detected.</summary>
    bool ShortCircuit,
    /// <summary>True if the system is in programming mode.</summary>
    bool Programming) : EventBase;

/// <summary>
/// Snapshot of Z21 system state values.
/// </summary>
public sealed record SystemStateChangedEvent(
    /// <summary>Main track current in milliamps.</summary>
    int MainCurrent,
    /// <summary>Programming track current in milliamps.</summary>
    int ProgCurrent,
    /// <summary>Filtered main current in milliamps.</summary>
    int FilteredMainCurrent,
    /// <summary>Internal temperature in degrees Celsius.</summary>
    int Temperature,
    /// <summary>Supply voltage in tenths of volts.</summary>
    int SupplyVoltage,
    /// <summary>Logic supply voltage in tenths of volts.</summary>
    int VccVoltage,
    /// <summary>Bitmask representing central state flags.</summary>
    int CentralState,
    /// <summary>Extended central state bitmask.</summary>
    int CentralStateEx) : EventBase;

/// <summary>
/// Z21 firmware and hardware version information has changed.
/// </summary>
public sealed record VersionInfoChangedEvent(
    /// <summary>Unique Z21 serial number.</summary>
    uint SerialNumber,
    /// <summary>Hardware type code reported by Z21.</summary>
    int HardwareTypeCode,
    /// <summary>Firmware version code reported by Z21.</summary>
    int FirmwareVersionCode) : EventBase;

/// <summary>
/// Detailed locomotive state information received from Z21.
/// </summary>
public sealed record LocomotiveInfoChangedEvent(
    /// <summary>Locomotive DCC address.</summary>
    int Address,
    /// <summary>Current speed step.</summary>
    int Speed,
    /// <summary>True if locomotive direction is forward.</summary>
    bool IsForward,
    /// <summary>True if function F0 is on.</summary>
    bool IsF0On,
    /// <summary>True if function F1 is on.</summary>
    bool IsF1On,
    /// <summary>True if function F2 is on.</summary>
    bool IsF2On,
    /// <summary>True if function F3 is on.</summary>
    bool IsF3On,
    /// <summary>True if function F4 is on.</summary>
    bool IsF4On,
    /// <summary>True if function F5 is on.</summary>
    bool IsF5On,
    /// <summary>True if function F6 is on.</summary>
    bool IsF6On,
    /// <summary>True if function F7 is on.</summary>
    bool IsF7On,
    /// <summary>True if function F8 is on.</summary>
    bool IsF8On,
    /// <summary>True if function F9 is on.</summary>
    bool IsF9On,
    /// <summary>True if function F10 is on.</summary>
    bool IsF10On,
    /// <summary>True if function F11 is on.</summary>
    bool IsF11On,
    /// <summary>True if function F12 is on.</summary>
    bool IsF12On,
    /// <summary>True if function F13 is on.</summary>
    bool IsF13On,
    /// <summary>True if function F14 is on.</summary>
    bool IsF14On,
    /// <summary>True if function F15 is on.</summary>
    bool IsF15On,
    /// <summary>True if function F16 is on.</summary>
    bool IsF16On,
    /// <summary>True if function F17 is on.</summary>
    bool IsF17On,
    /// <summary>True if function F18 is on.</summary>
    bool IsF18On,
    /// <summary>True if function F19 is on.</summary>
    bool IsF19On,
    /// <summary>True if function F20 is on.</summary>
    bool IsF20On) : EventBase;

/// <summary>
/// Locomotive control events - fired when locomotive state changes via Z21.
/// </summary>
public sealed record LocomotiveSpeedChangedEvent(
    /// <summary>Locomotive DCC address.</summary>
    int Address,
    /// <summary>Current speed step.</summary>
    int Speed,
    /// <summary>Maximum speed step supported by decoder.</summary>
    int MaxSpeed = 126) : EventBase;

/// <summary>
/// Locomotive direction has changed.
/// </summary>
public sealed record LocomotiveDirectionChangedEvent(
    /// <summary>Locomotive DCC address.</summary>
    int Address,
    /// <summary>True if direction is forward.</summary>
    bool Forward) : EventBase;

/// <summary>
/// Single locomotive function has been toggled.
/// </summary>
public sealed record LocomotiveFunctionToggledEvent(
    /// <summary>Locomotive DCC address.</summary>
    int Address,
    /// <summary>Function index (e.g., 0 for F0).</summary>
    int Function,
    /// <summary>True if the function is now on.</summary>
    bool IsOn) : EventBase;

/// <summary>
/// Emergency stop for one or all locomotives.
/// </summary>
public sealed record LocomotiveEmergencyStopEvent(
    /// <summary>Specific locomotive address or null for global stop.</summary>
    int? SpecificAddress = null) : EventBase;

/// <summary>
/// Z21 R-Bus feedback received (LAN_RMBUS_DATACHANGED). InPort is 1-based.
/// </summary>
public sealed record FeedbackReceivedEvent(
    /// <summary>1-based feedback input port number.</summary>
    int InPort) : EventBase;

/// <summary>
/// Feedback point events - fired when occupancy detection changes.
/// </summary>
public sealed record FeedbackPointTriggeredEvent(
    /// <summary>Identifier of the feedback point.</summary>
    int FeedbackId,
    /// <summary>True if the feedback point is occupied.</summary>
    bool IsOccupied,
    /// <summary>Optional last locomotive address detected at this feedback.</summary>
    int? LastLocomotiveAddress = null) : EventBase;

/// <summary>
/// Feedback point has transitioned to the cleared state.
/// </summary>
public sealed record FeedbackPointClearedEvent(
    /// <summary>Identifier of the feedback point.</summary>
    int FeedbackId) : EventBase;

/// <summary>
/// Signal and switch events - fired when signal box state changes.
/// </summary>
public sealed record SignalAspectChangedEvent(
    /// <summary>Identifier of the signal.</summary>
    int SignalId,
    /// <summary>New signal aspect.</summary>
    string Aspect,
    /// <summary>Previous signal aspect, if available.</summary>
    string? PreviousAspect = null) : EventBase;

/// <summary>
/// Switch position has changed in the signal box.
/// </summary>
public sealed record SwitchPositionChangedEvent(
    /// <summary>Identifier of the switch.</summary>
    int SwitchId,
    /// <summary>True if switch is in left/straight position; false for right/diverging.</summary>
    bool IsLeft,
    /// <summary>Previous switch position, if known.</summary>
    bool? PreviousPosition = null) : EventBase;

/// <summary>
/// System health events - fired for health check failures.
/// </summary>
public sealed record HealthCheckFailedEvent(
    /// <summary>Name of the monitored service.</summary>
    string ServiceName,
    /// <summary>Error message describing the failure.</summary>
    string ErrorMessage,
    /// <summary>Timestamp of the last successful health check.</summary>
    DateTime LastSuccessTime) : EventBase;

/// <summary>
/// Health check for a service has recovered.
/// </summary>
public sealed record HealthCheckRecoveredEvent(
    /// <summary>Name of the monitored service.</summary>
    string ServiceName,
    /// <summary>Duration of the downtime before recovery.</summary>
    TimeSpan DowntimeDuration) : EventBase;

/// <summary>
/// TripLog service: new entry added (for UI update).
/// </summary>
public sealed record TripLogEntryAddedEvent : EventBase;

/// <summary>
/// Post-startup initialization: status text for status bar.
/// </summary>
/// <param name="IsRunning">True if the application is fully initialized.</param>
/// <param name="StatusText">Status text to show in the status bar.</param>
public sealed record PostStartupStatusEvent(bool IsRunning, string StatusText) : EventBase;
