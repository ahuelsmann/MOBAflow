// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Model;

/// <summary>
/// Represents the version information of the Z21 digital command station.
/// Retrieved via LAN_GET_SERIAL_NUMBER (0x10) and LAN_GET_HWINFO (0x1A).
/// </summary>
public class Z21VersionInfo
{
    /// <summary>
    /// Z21 serial number (e.g., 101953).
    /// Retrieved via LAN_GET_SERIAL_NUMBER.
    /// </summary>
    public uint SerialNumber { get; set; }

    /// <summary>
    /// Raw hardware type code from Z21.
    /// Common values: 0x00000201 = z21start, 0x00000202 = Z21/z21, 0x00000203 = smartRail, etc.
    /// </summary>
    public uint HardwareTypeCode { get; set; }

    /// <summary>
    /// Human-readable hardware type name (e.g., "Z21", "z21start", "Z21a").
    /// </summary>
    public string HardwareType => HardwareTypeCode switch
    {
        0x00000200 => "Z21 (old)",
        0x00000201 => "z21start",
        0x00000202 => "Z21",
        0x00000203 => "smartRail",
        0x00000204 => "z21small",
        0x00000205 => "z21select",
        0x00000206 => "Z21a",      // The most common current model
        0x00000211 => "z21 single booster",
        0x00000212 => "z21 dual booster",
        _ => $"Unknown (0x{HardwareTypeCode:X8})"
    };

    /// <summary>
    /// Raw firmware version code from Z21.
    /// Format: Major.Minor encoded as BCD (e.g., 0x0143 = V1.43).
    /// </summary>
    public uint FirmwareVersionCode { get; set; }

    /// <summary>
    /// Human-readable firmware version string (e.g., "V1.43").
    /// </summary>
    public string FirmwareVersion
    {
        get
        {
            // Firmware version is encoded in BCD format
            // Example: 0x0143 = V1.43
            var major = (FirmwareVersionCode >> 8) & 0xFF;
            var minor = FirmwareVersionCode & 0xFF;
            return $"V{major}.{minor:X2}";
        }
    }

    /// <summary>
    /// Returns a summary string with all version information.
    /// </summary>
    public override string ToString()
        => $"S/N: {SerialNumber}, HW: {HardwareType}, FW: {FirmwareVersion}";
}

/// <summary>
/// Represents a single Z21/UDP packet for traffic monitoring.
/// </summary>
public class Z21TrafficPacket
{
    /// <summary>
    /// Timestamp when the packet was sent/received.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Direction: true = Sent (↑), false = Received (↓).
    /// </summary>
    public bool IsSent { get; set; }

    /// <summary>
    /// Raw packet data (hex bytes).
    /// </summary>
    public byte[] Data { get; set; } = [];

    /// <summary>
    /// Human-readable packet type (e.g., "LAN_X_SET_TRACK_POWER_ON", "LAN_X_TURNOUT", "LAN_RAILCOM_DATACHANGED").
    /// </summary>
    public string PacketType { get; set; } = "Unknown";

    /// <summary>
    /// Additional details (e.g., "InPort=5", "Address=3, Speed=80").
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this packet is feedback-related (LAN_RMBUS_DATACHANGED or similar).
    /// </summary>
    public bool IsFeedbackRelated { get; set; }

    /// <summary>
    /// Primary InPort number if this is a feedback packet (first active bit), otherwise null.
    /// For packets with multiple active feedback points, this is the lowest numbered InPort.
    /// </summary>
    public int? InPort { get; set; }

    /// <summary>
    /// All active InPort numbers if this is a feedback packet with multiple bits set.
    /// Empty list for non-feedback packets or if no bits are set.
    /// Useful for displaying all active feedback points in Traffic Monitor UI.
    /// </summary>
    public List<int> AllInPorts { get; set; } = [];

    /// <summary>
    /// Formats the packet data as hex string for display (spaced bytes).
    /// Example: "0F 00 80 00 00 01 00 00 00 00 00 00 00 00 00"
    /// </summary>
    public string DataHex => BitConverter.ToString(Data).Replace("-", " ");

    /// <summary>
    /// Direction icon for UI display: ↓ = Received, ↑ = Sent.
    /// </summary>
    public string DirectionIcon => IsSent ? "↑" : "↓";

    /// <summary>
    /// Formatted timestamp for UI display.
    /// </summary>
    public string TimestampFormatted => Timestamp.ToString("HH:mm:ss.fff");

        /// <summary>
        /// Formatted InPort for UI display (empty if null).
        /// Shows all active InPorts if multiple bits are set (e.g., "1,2,5").
        /// </summary>
        public string InPortFormatted => AllInPorts.Count > 1 
            ? string.Join(",", AllInPorts) 
            : InPort?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Represents locomotive information received from Z21.
    /// LAN_X_LOCO_INFO (0xEF) response structure.
    /// </summary>
    public class LocoInfo
    {
        /// <summary>
        /// DCC locomotive address (1-9999).
        /// </summary>
        public int Address { get; set; }

        /// <summary>
        /// Current speed (0-126 for 128 speed steps, 0=stop).
        /// </summary>
        public int Speed { get; set; }

        /// <summary>
        /// Direction: true = forward, false = backward.
        /// </summary>
        public bool IsForward { get; set; }

        /// <summary>
        /// Speed steps mode: 14, 28, or 128.
        /// </summary>
        public int SpeedSteps { get; set; } = 128;

        /// <summary>
        /// Function states F0-F31 as bitmask.
        /// Bit 0 = F0 (light), Bit 1 = F1 (sound), etc.
        /// </summary>
        public uint Functions { get; set; }

        /// <summary>
        /// Returns true if F0 (light) is on.
        /// </summary>
        public bool IsF0On => (Functions & 0x01) != 0;

        /// <summary>
        /// Returns true if F1 (typically sound) is on.
        /// </summary>
        public bool IsF1On => (Functions & 0x02) != 0;

        /// <summary>
        /// Gets the state of a specific function.
        /// </summary>
        public bool GetFunction(int index) => (Functions & (1u << index)) != 0;
    }
