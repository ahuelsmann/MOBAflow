// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend;

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
