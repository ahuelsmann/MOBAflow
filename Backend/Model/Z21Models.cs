// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
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
    /// Hardware version/revision number.
    /// Typically 0 for most units.
    /// </summary>
    public uint HardwareVersion { get; set; }

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
    /// Direction: true = Sent (→), false = Received (←).
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
    /// Formats the packet data as hex string for display.
    /// </summary>
    public string DataHex => BitConverter.ToString(Data).Replace("-", " ");

    /// <summary>
    /// Direction icon for UI display.
    /// </summary>
    public string DirectionIcon => IsSent ? "→" : "←";

    /// <summary>
    /// Formatted timestamp for UI display.
    /// </summary>
    public string TimestampFormatted => Timestamp.ToString("HH:mm:ss.fff");
}
