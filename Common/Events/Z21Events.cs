// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
//
namespace Moba.Common.Events;

/// <summary>
/// Z21 connection events - fired when the Z21 device connects.
/// </summary>
public sealed record Z21ConnectionEstablishedEvent : EventBase;

/// <summary>
/// Z21 connection events - fired when the Z21 device disconnects.
/// </summary>
public sealed record Z21ConnectionLostEvent : EventBase;

/// <summary>
/// Track power state has changed.
/// </summary>
public sealed record Z21TrackPowerChangedEvent : EventBase
{
    /// <summary>
    /// Gets a value indicating whether track power is currently on.
    /// </summary>
    public bool IsOn { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Z21TrackPowerChangedEvent"/> record.
    /// </summary>
    /// <param name="isOn">True if track power is on; false if it is off.</param>
    public Z21TrackPowerChangedEvent(bool isOn)
    {
        IsOn = isOn;
    }
}

/// <summary>
/// Z21 system events - fired when Z21 system state updates.
/// These events contain raw data extracted from backend model classes to avoid circular dependencies.
/// </summary>
public sealed record XBusStatusChangedEvent : EventBase
{
    /// <summary>
    /// Gets a value indicating whether emergency stop is active.
    /// </summary>
    public bool EmergencyStop { get; init; }

    /// <summary>
    /// Gets a value indicating whether track power is off.
    /// </summary>
    public bool TrackOff { get; init; }

    /// <summary>
    /// Gets a value indicating whether a short circuit is detected.
    /// </summary>
    public bool ShortCircuit { get; init; }

    /// <summary>
    /// Gets a value indicating whether the system is in programming mode.
    /// </summary>
    public bool Programming { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="XBusStatusChangedEvent"/> record.
    /// </summary>
    /// <param name="emergencyStop">True if emergency stop is active.</param>
    /// <param name="trackOff">True if track power is off.</param>
    /// <param name="shortCircuit">True if a short circuit is detected.</param>
    /// <param name="programming">True if the system is in programming mode.</param>
    public XBusStatusChangedEvent(bool emergencyStop, bool trackOff, bool shortCircuit, bool programming)
    {
        EmergencyStop = emergencyStop;
        TrackOff = trackOff;
        ShortCircuit = shortCircuit;
        Programming = programming;
    }
}

/// <summary>
/// Snapshot of Z21 system state values.
/// </summary>
public sealed record SystemStateChangedEvent : EventBase
{
    /// <summary>
    /// Gets the main track current in milliamps.
    /// </summary>
    public int MainCurrent { get; init; }

    /// <summary>
    /// Gets the programming track current in milliamps.
    /// </summary>
    public int ProgCurrent { get; init; }

    /// <summary>
    /// Gets the filtered main current in milliamps.
    /// </summary>
    public int FilteredMainCurrent { get; init; }

    /// <summary>
    /// Gets the internal temperature in degrees Celsius.
    /// </summary>
    public int Temperature { get; init; }

    /// <summary>
    /// Gets the supply voltage in tenths of volts.
    /// </summary>
    public int SupplyVoltage { get; init; }

    /// <summary>
    /// Gets the logic supply voltage in tenths of volts.
    /// </summary>
    public int VccVoltage { get; init; }

    /// <summary>
    /// Gets the bitmask representing central state flags.
    /// </summary>
    public int CentralState { get; init; }

    /// <summary>
    /// Gets the extended central state bitmask.
    /// </summary>
    public int CentralStateEx { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemStateChangedEvent"/> record.
    /// </summary>
    /// <param name="mainCurrent">Main track current in milliamps.</param>
    /// <param name="progCurrent">Programming track current in milliamps.</param>
    /// <param name="filteredMainCurrent">Filtered main current in milliamps.</param>
    /// <param name="temperature">Internal temperature in degrees Celsius.</param>
    /// <param name="supplyVoltage">Supply voltage in tenths of volts.</param>
    /// <param name="vccVoltage">Logic supply voltage in tenths of volts.</param>
    /// <param name="centralState">Bitmask representing central state flags.</param>
    /// <param name="centralStateEx">Extended central state bitmask.</param>
    public SystemStateChangedEvent(
        int mainCurrent,
        int progCurrent,
        int filteredMainCurrent,
        int temperature,
        int supplyVoltage,
        int vccVoltage,
        int centralState,
        int centralStateEx)
    {
        MainCurrent = mainCurrent;
        ProgCurrent = progCurrent;
        FilteredMainCurrent = filteredMainCurrent;
        Temperature = temperature;
        SupplyVoltage = supplyVoltage;
        VccVoltage = vccVoltage;
        CentralState = centralState;
        CentralStateEx = centralStateEx;
    }
}

/// <summary>
/// Z21 firmware and hardware version information has changed.
/// </summary>
public sealed record VersionInfoChangedEvent : EventBase
{
    /// <summary>
    /// Gets the unique Z21 serial number.
    /// </summary>
    public uint SerialNumber { get; init; }

    /// <summary>
    /// Gets the hardware type code reported by Z21.
    /// </summary>
    public int HardwareTypeCode { get; init; }

    /// <summary>
    /// Gets the firmware version code reported by Z21.
    /// </summary>
    public int FirmwareVersionCode { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionInfoChangedEvent"/> record.
    /// </summary>
    /// <param name="serialNumber">Unique Z21 serial number.</param>
    /// <param name="hardwareTypeCode">Hardware type code reported by Z21.</param>
    /// <param name="firmwareVersionCode">Firmware version code reported by Z21.</param>
    public VersionInfoChangedEvent(uint serialNumber, int hardwareTypeCode, int firmwareVersionCode)
    {
        SerialNumber = serialNumber;
        HardwareTypeCode = hardwareTypeCode;
        FirmwareVersionCode = firmwareVersionCode;
    }
}

/// <summary>
/// Detailed locomotive state information received from Z21.
/// </summary>
public sealed record LocomotiveInfoChangedEvent : EventBase
{
    /// <summary>Gets the locomotive DCC address.</summary>
    public int Address { get; init; }

    /// <summary>Gets the current speed step.</summary>
    public int Speed { get; init; }

    /// <summary>Gets a value indicating whether the locomotive direction is forward.</summary>
    public bool IsForward { get; init; }

    /// <summary>Gets a value indicating whether function F0 is on.</summary>
    public bool IsF0On { get; init; }

    /// <summary>Gets a value indicating whether function F1 is on.</summary>
    public bool IsF1On { get; init; }

    /// <summary>Gets a value indicating whether function F2 is on.</summary>
    public bool IsF2On { get; init; }

    /// <summary>Gets a value indicating whether function F3 is on.</summary>
    public bool IsF3On { get; init; }

    /// <summary>Gets a value indicating whether function F4 is on.</summary>
    public bool IsF4On { get; init; }

    /// <summary>Gets a value indicating whether function F5 is on.</summary>
    public bool IsF5On { get; init; }

    /// <summary>Gets a value indicating whether function F6 is on.</summary>
    public bool IsF6On { get; init; }

    /// <summary>Gets a value indicating whether function F7 is on.</summary>
    public bool IsF7On { get; init; }

    /// <summary>Gets a value indicating whether function F8 is on.</summary>
    public bool IsF8On { get; init; }

    /// <summary>Gets a value indicating whether function F9 is on.</summary>
    public bool IsF9On { get; init; }

    /// <summary>Gets a value indicating whether function F10 is on.</summary>
    public bool IsF10On { get; init; }

    /// <summary>Gets a value indicating whether function F11 is on.</summary>
    public bool IsF11On { get; init; }

    /// <summary>Gets a value indicating whether function F12 is on.</summary>
    public bool IsF12On { get; init; }

    /// <summary>Gets a value indicating whether function F13 is on.</summary>
    public bool IsF13On { get; init; }

    /// <summary>Gets a value indicating whether function F14 is on.</summary>
    public bool IsF14On { get; init; }

    /// <summary>Gets a value indicating whether function F15 is on.</summary>
    public bool IsF15On { get; init; }

    /// <summary>Gets a value indicating whether function F16 is on.</summary>
    public bool IsF16On { get; init; }

    /// <summary>Gets a value indicating whether function F17 is on.</summary>
    public bool IsF17On { get; init; }

    /// <summary>Gets a value indicating whether function F18 is on.</summary>
    public bool IsF18On { get; init; }

    /// <summary>Gets a value indicating whether function F19 is on.</summary>
    public bool IsF19On { get; init; }

    /// <summary>Gets a value indicating whether function F20 is on.</summary>
    public bool IsF20On { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocomotiveInfoChangedEvent"/> record.
    /// </summary>
    /// <param name="address">Locomotive DCC address.</param>
    /// <param name="speed">Current speed step.</param>
    /// <param name="isForward">True if locomotive direction is forward.</param>
    /// <param name="isF0On">True if function F0 is on.</param>
    /// <param name="isF1On">True if function F1 is on.</param>
    /// <param name="isF2On">True if function F2 is on.</param>
    /// <param name="isF3On">True if function F3 is on.</param>
    /// <param name="isF4On">True if function F4 is on.</param>
    /// <param name="isF5On">True if function F5 is on.</param>
    /// <param name="isF6On">True if function F6 is on.</param>
    /// <param name="isF7On">True if function F7 is on.</param>
    /// <param name="isF8On">True if function F8 is on.</param>
    /// <param name="isF9On">True if function F9 is on.</param>
    /// <param name="isF10On">True if function F10 is on.</param>
    /// <param name="isF11On">True if function F11 is on.</param>
    /// <param name="isF12On">True if function F12 is on.</param>
    /// <param name="isF13On">True if function F13 is on.</param>
    /// <param name="isF14On">True if function F14 is on.</param>
    /// <param name="isF15On">True if function F15 is on.</param>
    /// <param name="isF16On">True if function F16 is on.</param>
    /// <param name="isF17On">True if function F17 is on.</param>
    /// <param name="isF18On">True if function F18 is on.</param>
    /// <param name="isF19On">True if function F19 is on.</param>
    /// <param name="isF20On">True if function F20 is on.</param>
    public LocomotiveInfoChangedEvent(
        int address,
        int speed,
        bool isForward,
        bool isF0On,
        bool isF1On,
        bool isF2On,
        bool isF3On,
        bool isF4On,
        bool isF5On,
        bool isF6On,
        bool isF7On,
        bool isF8On,
        bool isF9On,
        bool isF10On,
        bool isF11On,
        bool isF12On,
        bool isF13On,
        bool isF14On,
        bool isF15On,
        bool isF16On,
        bool isF17On,
        bool isF18On,
        bool isF19On,
        bool isF20On)
    {
        Address = address;
        Speed = speed;
        IsForward = isForward;
        IsF0On = isF0On;
        IsF1On = isF1On;
        IsF2On = isF2On;
        IsF3On = isF3On;
        IsF4On = isF4On;
        IsF5On = isF5On;
        IsF6On = isF6On;
        IsF7On = isF7On;
        IsF8On = isF8On;
        IsF9On = isF9On;
        IsF10On = isF10On;
        IsF11On = isF11On;
        IsF12On = isF12On;
        IsF13On = isF13On;
        IsF14On = isF14On;
        IsF15On = isF15On;
        IsF16On = isF16On;
        IsF17On = isF17On;
        IsF18On = isF18On;
        IsF19On = isF19On;
        IsF20On = isF20On;
    }
}

/// <summary>
/// Locomotive control events - fired when locomotive speed changes via Z21.
/// </summary>
public sealed record LocomotiveSpeedChangedEvent : EventBase
{
    /// <summary>Gets the locomotive DCC address.</summary>
    public int Address { get; init; }

    /// <summary>Gets the current speed step.</summary>
    public int Speed { get; init; }

    /// <summary>Gets the maximum speed step supported by the decoder.</summary>
    public int MaxSpeed { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocomotiveSpeedChangedEvent"/> record.
    /// </summary>
    /// <param name="address">Locomotive DCC address.</param>
    /// <param name="speed">Current speed step.</param>
    /// <param name="maxSpeed">Maximum speed step supported by the decoder.</param>
    public LocomotiveSpeedChangedEvent(int address, int speed, int maxSpeed = 126)
    {
        Address = address;
        Speed = speed;
        MaxSpeed = maxSpeed;
    }
}

/// <summary>
/// Locomotive direction has changed.
/// </summary>
public sealed record LocomotiveDirectionChangedEvent : EventBase
{
    /// <summary>Gets the locomotive DCC address.</summary>
    public int Address { get; init; }

    /// <summary>Gets a value indicating whether the direction is forward.</summary>
    public bool Forward { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocomotiveDirectionChangedEvent"/> record.
    /// </summary>
    /// <param name="address">Locomotive DCC address.</param>
    /// <param name="forward">True if direction is forward.</param>
    public LocomotiveDirectionChangedEvent(int address, bool forward)
    {
        Address = address;
        Forward = forward;
    }
}

/// <summary>
/// Single locomotive function has been toggled.
/// </summary>
public sealed record LocomotiveFunctionToggledEvent : EventBase
{
    /// <summary>Gets the locomotive DCC address.</summary>
    public int Address { get; init; }

    /// <summary>Gets the function index (for example 0 for F0).</summary>
    public int Function { get; init; }

    /// <summary>Gets a value indicating whether the function is now on.</summary>
    public bool IsOn { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocomotiveFunctionToggledEvent"/> record.
    /// </summary>
    /// <param name="address">Locomotive DCC address.</param>
    /// <param name="function">Function index.</param>
    /// <param name="isOn">True if the function is now on.</param>
    public LocomotiveFunctionToggledEvent(int address, int function, bool isOn)
    {
        Address = address;
        Function = function;
        IsOn = isOn;
    }
}

/// <summary>
/// Emergency stop for one or all locomotives.
/// </summary>
public sealed record LocomotiveEmergencyStopEvent : EventBase
{
    /// <summary>
    /// Gets the specific locomotive address, or null when the stop applies to all locomotives.
    /// </summary>
    public int? SpecificAddress { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocomotiveEmergencyStopEvent"/> record.
    /// </summary>
    /// <param name="specificAddress">Specific locomotive address or null for global stop.</param>
    public LocomotiveEmergencyStopEvent(int? specificAddress = null)
    {
        SpecificAddress = specificAddress;
    }
}

/// <summary>
/// Z21 R-Bus feedback received (LAN_RMBUS_DATACHANGED). InPort is 1-based.
/// </summary>
public sealed record FeedbackReceivedEvent : EventBase
{
    /// <summary>
    /// Gets the 1-based feedback input port number.
    /// </summary>
    public int InPort { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedbackReceivedEvent"/> record.
    /// </summary>
    /// <param name="inPort">1-based feedback input port number.</param>
    public FeedbackReceivedEvent(int inPort)
    {
        InPort = inPort;
    }
}

/// <summary>
/// Feedback point events - fired when occupancy detection changes.
/// </summary>
public sealed record FeedbackPointTriggeredEvent : EventBase
{
    /// <summary>Gets the identifier of the feedback point.</summary>
    public int FeedbackId { get; init; }

    /// <summary>Gets a value indicating whether the feedback point is occupied.</summary>
    public bool IsOccupied { get; init; }

    /// <summary>Gets the last locomotive address detected at this feedback, if available.</summary>
    public int? LastLocomotiveAddress { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedbackPointTriggeredEvent"/> record.
    /// </summary>
    /// <param name="feedbackId">Identifier of the feedback point.</param>
    /// <param name="isOccupied">True if the feedback point is occupied.</param>
    /// <param name="lastLocomotiveAddress">Optional last locomotive address detected at this feedback.</param>
    public FeedbackPointTriggeredEvent(int feedbackId, bool isOccupied, int? lastLocomotiveAddress = null)
    {
        FeedbackId = feedbackId;
        IsOccupied = isOccupied;
        LastLocomotiveAddress = lastLocomotiveAddress;
    }
}

/// <summary>
/// Feedback point has transitioned to the cleared state.
/// </summary>
public sealed record FeedbackPointClearedEvent : EventBase
{
    /// <summary>Gets the identifier of the feedback point.</summary>
    public int FeedbackId { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedbackPointClearedEvent"/> record.
    /// </summary>
    /// <param name="feedbackId">Identifier of the feedback point.</param>
    public FeedbackPointClearedEvent(int feedbackId)
    {
        FeedbackId = feedbackId;
    }
}

/// <summary>
/// Signal and switch events - fired when signal box state changes.
/// </summary>
public sealed record SignalAspectChangedEvent : EventBase
{
    /// <summary>Gets the identifier of the signal.</summary>
    public int SignalId { get; init; }

    /// <summary>Gets the new signal aspect.</summary>
    public string Aspect { get; init; } = string.Empty;

    /// <summary>Gets the previous signal aspect, if available.</summary>
    public string? PreviousAspect { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalAspectChangedEvent"/> record.
    /// </summary>
    /// <param name="signalId">Identifier of the signal.</param>
    /// <param name="aspect">New signal aspect.</param>
    /// <param name="previousAspect">Previous signal aspect, if available.</param>
    public SignalAspectChangedEvent(int signalId, string aspect, string? previousAspect = null)
    {
        SignalId = signalId;
        Aspect = aspect;
        PreviousAspect = previousAspect;
    }
}

/// <summary>
/// Switch position has changed in the signal box.
/// </summary>
public sealed record SwitchPositionChangedEvent : EventBase
{
    /// <summary>Gets the identifier of the switch.</summary>
    public int SwitchId { get; init; }

    /// <summary>Gets a value indicating whether the switch is in left or straight position.</summary>
    public bool IsLeft { get; init; }

    /// <summary>Gets the previous switch position, if known.</summary>
    public bool? PreviousPosition { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SwitchPositionChangedEvent"/> record.
    /// </summary>
    /// <param name="switchId">Identifier of the switch.</param>
    /// <param name="isLeft">True if switch is in left or straight position; false for right or diverging.</param>
    /// <param name="previousPosition">Previous switch position, if known.</param>
    public SwitchPositionChangedEvent(int switchId, bool isLeft, bool? previousPosition = null)
    {
        SwitchId = switchId;
        IsLeft = isLeft;
        PreviousPosition = previousPosition;
    }
}

/// <summary>
/// System health events - fired for health check failures.
/// </summary>
public sealed record HealthCheckFailedEvent : EventBase
{
    /// <summary>Gets the name of the monitored service.</summary>
    public string ServiceName { get; init; } = string.Empty;

    /// <summary>Gets the error message describing the failure.</summary>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>Gets the timestamp of the last successful health check.</summary>
    public DateTime LastSuccessTime { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthCheckFailedEvent"/> record.
    /// </summary>
    /// <param name="serviceName">Name of the monitored service.</param>
    /// <param name="errorMessage">Error message describing the failure.</param>
    /// <param name="lastSuccessTime">Timestamp of the last successful health check.</param>
    public HealthCheckFailedEvent(string serviceName, string errorMessage, DateTime lastSuccessTime)
    {
        ServiceName = serviceName;
        ErrorMessage = errorMessage;
        LastSuccessTime = lastSuccessTime;
    }
}

/// <summary>
/// Health check for a service has recovered.
/// </summary>
public sealed record HealthCheckRecoveredEvent : EventBase
{
    /// <summary>Gets the name of the monitored service.</summary>
    public string ServiceName { get; init; } = string.Empty;

    /// <summary>Gets the duration of the downtime before recovery.</summary>
    public TimeSpan DowntimeDuration { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthCheckRecoveredEvent"/> record.
    /// </summary>
    /// <param name="serviceName">Name of the monitored service.</param>
    /// <param name="downtimeDuration">Duration of the downtime before recovery.</param>
    public HealthCheckRecoveredEvent(string serviceName, TimeSpan downtimeDuration)
    {
        ServiceName = serviceName;
        DowntimeDuration = downtimeDuration;
    }
}

/// <summary>
/// TripLog service: new entry added (for UI update).
/// </summary>
public sealed record TripLogEntryAddedEvent : EventBase;

/// <summary>
/// Post-startup initialization: status text for status bar.
/// </summary>
public sealed record PostStartupStatusEvent : EventBase
{
    /// <summary>Gets a value indicating whether the application is fully initialized.</summary>
    public bool IsRunning { get; init; }

    /// <summary>Gets the status text to show in the status bar.</summary>
    public string StatusText { get; init; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostStartupStatusEvent"/> record.
    /// </summary>
    /// <param name="isRunning">True if the application is fully initialized.</param>
    /// <param name="statusText">Status text to show in the status bar.</param>
    public PostStartupStatusEvent(bool isRunning, string statusText)
    {
        IsRunning = isRunning;
        StatusText = statusText;
    }
}

