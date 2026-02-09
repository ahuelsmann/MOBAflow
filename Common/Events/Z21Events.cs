// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Events;

/// <summary>
/// Z21 Connection Events - Fired when Z21 device connection status changes.
/// </summary>

public sealed record Z21ConnectionEstablishedEvent(
    string IpAddress,
    int ProtocolVersion) : EventBase;

public sealed record Z21ConnectionLostEvent(
    string Reason,
    TimeSpan LastAliveTime) : EventBase;

public sealed record Z21TrackPowerChangedEvent(
    bool IsOn) : EventBase;

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
