// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Events;

/// <summary>
/// Z21 Connection Events - Fired when Z21 device connection status changes.
/// </summary>

public sealed record Z21ConnectionEstablishedEvent : EventBase;

public sealed record Z21ConnectionLostEvent : EventBase;

public sealed record Z21TrackPowerChangedEvent(
    bool IsOn) : EventBase;

/// <summary>
/// Z21 System Events - Fired when Z21 system state updates.
/// These events contain raw data extracted from Backend.Model classes to avoid circular dependencies.
/// </summary>

public sealed record XBusStatusChangedEvent(
    bool EmergencyStop,
    bool TrackOff,
    bool ShortCircuit,
    bool Programming) : EventBase;

public sealed record SystemStateChangedEvent(
    int MainCurrent,
    int ProgCurrent,
    int FilteredMainCurrent,
    int Temperature,
    int SupplyVoltage,
    int VccVoltage,
    int CentralState,
    int CentralStateEx) : EventBase;

public sealed record VersionInfoChangedEvent(
    uint SerialNumber,
    int HardwareTypeCode,
    int FirmwareVersionCode) : EventBase;

public sealed record LocomotiveInfoChangedEvent(
    int Address,
    int Speed,
    bool IsForward,
    bool IsF0On,
    bool IsF1On,
    bool IsF2On,
    bool IsF3On,
    bool IsF4On,
    bool IsF5On,
    bool IsF6On,
    bool IsF7On,
    bool IsF8On,
    bool IsF9On,
    bool IsF10On,
    bool IsF11On,
    bool IsF12On,
    bool IsF13On,
    bool IsF14On,
    bool IsF15On,
    bool IsF16On,
    bool IsF17On,
    bool IsF18On,
    bool IsF19On,
    bool IsF20On) : EventBase;

/// <summary>
/// Locomotive Control Events - Fired when locomotive state changes via Z21.
/// </summary>

public sealed record LocomotiveSpeedChangedEvent(
    int Address,
    int Speed,
    int MaxSpeed = 126) : EventBase;

public sealed record LocomotiveDirectionChangedEvent(
    int Address,
    bool Forward) : EventBase;

public sealed record LocomotiveFunctionToggledEvent(
    int Address,
    int Function,
    bool IsOn) : EventBase;

public sealed record LocomotiveEmergencyStopEvent(
    int? SpecificAddress = null) : EventBase;

/// <summary>
/// Feedback Point Events - Fired when occupancy detection changes.
/// </summary>

public sealed record FeedbackPointTriggeredEvent(
    int FeedbackId,
    bool IsOccupied,
    int? LastLocomotiveAddress = null) : EventBase;

public sealed record FeedbackPointClearedEvent(
    int FeedbackId) : EventBase;

/// <summary>
/// Signal and Switch Events - Fired when Signal Box state changes.
/// </summary>

public sealed record SignalAspectChangedEvent(
    int SignalId,
    string Aspect,
    string? PreviousAspect = null) : EventBase;

public sealed record SwitchPositionChangedEvent(
    int SwitchId,
    bool IsLeft,
    bool? PreviousPosition = null) : EventBase;

/// <summary>
/// System Health Events - Fired for health check failures.
/// </summary>

public sealed record HealthCheckFailedEvent(
    string ServiceName,
    string ErrorMessage,
    DateTime LastSuccessTime) : EventBase;

public sealed record HealthCheckRecoveredEvent(
    string ServiceName,
    TimeSpan DowntimeDuration) : EventBase;
