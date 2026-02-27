// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Protocol;

using Model;

/// <summary>
/// Represents the decoded X‑Bus status flags reported by the Z21.
/// </summary>
public sealed record XBusStatus
{
    /// <summary>
    /// Gets a value indicating whether an emergency stop is active.
    /// </summary>
    public bool EmergencyStop { get; init; }

    /// <summary>
    /// Gets a value indicating whether track power is switched off.
    /// </summary>
    public bool TrackOff { get; init; }

    /// <summary>
    /// Gets a value indicating whether a short circuit has been detected.
    /// </summary>
    public bool ShortCircuit { get; init; }

    /// <summary>
    /// Gets a value indicating whether the system is in programming mode.
    /// </summary>
    public bool Programming { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="XBusStatus"/> record.
    /// </summary>
    /// <param name="emergencyStop">True if emergency stop is active.</param>
    /// <param name="trackOff">True if track power is off.</param>
    /// <param name="shortCircuit">True if a short circuit is detected.</param>
    /// <param name="programming">True if the system is in programming mode.</param>
    public XBusStatus(bool emergencyStop, bool trackOff, bool shortCircuit, bool programming)
    {
        EmergencyStop = emergencyStop;
        TrackOff = trackOff;
        ShortCircuit = shortCircuit;
        Programming = programming;
    }
}

/// <summary>
/// Parser utilities for decoding Z21 LAN protocol messages.
/// Provides helpers to detect packet types and extract structured information
/// such as serial number, hardware info, system state, X‑Bus status and loco info.
/// </summary>
public static class Z21MessageParser
{
    /// <summary>
    /// Determines whether the given packet uses the LAN_X_HEADER (X‑Bus tunneling).
    /// </summary>
    /// <param name="data">Raw Z21 packet bytes.</param>
    /// <returns><c>true</c> if the header is LAN_X_HEADER, otherwise <c>false</c>.</returns>
    public static bool IsLanXHeader(byte[] data)
        => data is { Length: >= 4 } && data[2] == Z21Protocol.Header.LAN_X_HEADER && data[3] == 0x00;

    /// <summary>
    /// Determines whether the given packet is an R‑Bus feedback packet (LAN_RMBUS_DATACHANGED).
    /// </summary>
    /// <param name="data">Raw Z21 packet bytes.</param>
    /// <returns><c>true</c> if the packet is R‑Bus feedback, otherwise <c>false</c>.</returns>
    public static bool IsRBusFeedback(byte[] data)
        => data is { Length: >= 4 } && data[2] == Z21Protocol.Header.LAN_RMBUS_DATACHANGED && data[3] == 0x00;

    /// <summary>
    /// Determines whether the given packet is a system state update (LAN_SYSTEMSTATE_DATACHANGED).
    /// </summary>
    /// <param name="data">Raw Z21 packet bytes.</param>
    /// <returns><c>true</c> if the packet is a system state packet, otherwise <c>false</c>.</returns>
    public static bool IsSystemState(byte[] data)
        => data is { Length: >= 4 } && data[2] == Z21Protocol.Header.LAN_SYSTEMSTATE && data[3] == 0x00;

    /// <summary>
    /// Determines whether the given packet is a serial number response (LAN_GET_SERIAL_NUMBER).
    /// </summary>
    /// <param name="data">Raw Z21 packet bytes.</param>
    /// <returns><c>true</c> if the packet contains serial number data, otherwise <c>false</c>.</returns>
    public static bool IsSerialNumber(byte[] data)
        => data.Length >= 8 && data[2] == Z21Protocol.Header.LAN_GET_SERIAL_NUMBER && data[3] == 0x00;

    /// <summary>
    /// Determines whether the given packet is a hardware info response (LAN_GET_HWINFO).
    /// </summary>
    /// <param name="data">Raw Z21 packet bytes.</param>
    /// <returns><c>true</c> if the packet contains hardware info, otherwise <c>false</c>.</returns>
    public static bool IsHwInfo(byte[] data)
        => data.Length >= 12 && data[2] == Z21Protocol.Header.LAN_GET_HWINFO && data[3] == 0x00;

    /// <summary>
    /// Parses the LAN_GET_SERIAL_NUMBER response.
    /// Format: 08-00-10-00 XX-XX-XX-XX (4 bytes serial number, little-endian)
    /// </summary>
    public static bool TryParseSerialNumber(byte[] data, out uint serialNumber)
    {
        serialNumber = 0;
        if (data.Length < 8) return false;
        serialNumber = BitConverter.ToUInt32(data, 4);
        return true;
    }

    /// <summary>
    /// Parses the LAN_GET_HWINFO response.
    /// Format: 0C-00-1A-00 TT-TT-TT-TT VV-VV-VV-VV (4 bytes HW type + 4 bytes FW version)
    /// </summary>
    public static bool TryParseHwInfo(byte[] data, out uint hardwareType, out uint firmwareVersion)
    {
        hardwareType = 0;
        firmwareVersion = 0;
        if (data.Length < 12) return false;
        hardwareType = BitConverter.ToUInt32(data, 4);
        firmwareVersion = BitConverter.ToUInt32(data, 8);
        return true;
    }

    /// <summary>
    /// Tries to parse an X‑Bus status broadcast packet.
    /// </summary>
    /// <param name="data">Raw Z21 packet bytes.</param>
    /// <returns>
    /// An <see cref="XBusStatus"/> instance when the packet contains valid status information;
    /// otherwise <c>null</c>.
    /// </returns>
    public static XBusStatus? TryParseXBusStatus(byte[] data)
    {
        if (data.Length < 7) return null;
        byte xHeader = data[4];
        if (xHeader != Z21Protocol.XHeader.X_STATUS && xHeader != Z21Protocol.XHeader.X_STATUS_CHANGED)
            return null;
        byte status = data[6];
        bool emergencyStop = (status & 0x01) != 0;
        bool trackOff = (status & 0x02) != 0;
        bool shortCircuit = (status & 0x04) != 0;
        bool programming = (status & 0x20) != 0;
        return new XBusStatus(emergencyStop, trackOff, shortCircuit, programming);
    }

    /// <summary>
    /// Tries to parse a system state broadcast packet (LAN_SYSTEMSTATE_DATACHANGED).
    /// </summary>
    /// <param name="data">Raw Z21 packet bytes.</param>
    /// <param name="mainCurrent">Main track current in milliamps.</param>
    /// <param name="progCurrent">Programming track current in milliamps.</param>
    /// <param name="filteredMainCurrent">Filtered main track current in milliamps.</param>
    /// <param name="temperature">Internal temperature in degrees Celsius.</param>
    /// <param name="supplyVoltage">Supply voltage in millivolts.</param>
    /// <param name="vccVoltage">Logic supply voltage in millivolts.</param>
    /// <param name="centralState">Central state bitmask.</param>
    /// <param name="centralStateEx">Extended central state bitmask.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
    public static bool TryParseSystemState(byte[] data, out int mainCurrent, out int progCurrent, out int filteredMainCurrent, out int temperature, out int supplyVoltage, out int vccVoltage, out byte centralState, out byte centralStateEx)
    {
        mainCurrent = progCurrent = filteredMainCurrent = temperature = supplyVoltage = vccVoltage = 0;
        centralState = centralStateEx = 0;
        // Needs at least 18 bytes total (4 header + 14 payload used by parser)
        if (data.Length < 18) return false;
        mainCurrent = BitConverter.ToInt16(data, 4);
        progCurrent = BitConverter.ToInt16(data, 6);
        filteredMainCurrent = BitConverter.ToInt16(data, 8);
        temperature = BitConverter.ToInt16(data, 10);
        supplyVoltage = BitConverter.ToUInt16(data, 12);
        vccVoltage = BitConverter.ToUInt16(data, 14);
        centralState = data[16];
        centralStateEx = data[17];
        return true;
    }

    /// <summary>
    /// Determines whether the given packet is a loco info response (LAN_X_LOCO_INFO).
    /// </summary>
    /// <param name="data">Raw Z21 packet bytes.</param>
    /// <returns><c>true</c> if the packet contains loco info, otherwise <c>false</c>.</returns>
    public static bool IsLocoInfo(byte[] data)
        => data.Length >= 7 && IsLanXHeader(data) && data[4] == Z21Protocol.XHeader.X_LOCO_INFO;

    /// <summary>
    /// Parses the LAN_X_LOCO_INFO response (X-Bus Loco Information).
    /// Format: 40-00-EF + Adr_MSB Adr_LSB + Speedsteps(1) + Speed(1) + Functions0-4(1) + Functions5-8(1) + Functions9-12(1) + Functions13-20(1) + Functions21-28(1)
    /// Speed encoding: 0=stop, 1=e-stop, 2-127=speed 1-126 (for 128 speed steps)
    /// </summary>
    public static bool TryParseLocoInfo(byte[] data, out LocoInfo? locoInfo)
    {
        locoInfo = null;
        if (data.Length < 11 || !IsLocoInfo(data))
            return false;

        try
        {
            // Parse address (little-endian, but Z21 uses big-endian for addresses)
            ushort address = (ushort)((data[5] << 8) | data[6]);

            // Mask out the C0 bit for 14-bit addresses
            address = (ushort)(address & 0x3FFF);

            // Parse speed steps and current speed
            byte speedSteps = (byte)(data[7] & 0x0F); // Lower 4 bits: 0=14, 2=28, 3=128
            byte speedByte = data[8];
            
            // Speed: bit 7 = direction, bits 0-6 = speed value
            bool forward = (speedByte & 0x80) != 0;
            int speed = speedByte & 0x7F;
            
            // Decode speed: 0=stop, 1=e-stop, 2-127=speed 1-126
            if (speed > 1) speed--; // Adjust encoding

            // Parse function status (F0-F28 in 5 bytes) as bitmask (uint)
            uint functions = 0;
            
            if (data.Length >= 9)
            {
                byte f04 = data[9];
                if ((f04 & 0x10) != 0) functions |= 0x01; // F0 (light)
                if ((f04 & 0x01) != 0) functions |= 0x02; // F1
                if ((f04 & 0x02) != 0) functions |= 0x04; // F2
                if ((f04 & 0x04) != 0) functions |= 0x08; // F3
                if ((f04 & 0x08) != 0) functions |= 0x10; // F4
            }

            if (data.Length >= 10)
            {
                byte f58 = data[10];
                if ((f58 & 0x01) != 0) functions |= 0x20; // F5
                if ((f58 & 0x02) != 0) functions |= 0x40; // F6
                if ((f58 & 0x04) != 0) functions |= 0x80; // F7
                if ((f58 & 0x08) != 0) functions |= 0x100; // F8
            }

            if (data.Length >= 11)
            {
                byte f912 = data[11];
                if ((f912 & 0x01) != 0) functions |= 0x200; // F9
                if ((f912 & 0x02) != 0) functions |= 0x400; // F10
                if ((f912 & 0x04) != 0) functions |= 0x800; // F11
                if ((f912 & 0x08) != 0) functions |= 0x1000; // F12
            }

            if (data.Length >= 12)
            {
                byte f1320 = data[12];
                if ((f1320 & 0x01) != 0) functions |= 0x2000; // F13
                if ((f1320 & 0x02) != 0) functions |= 0x4000; // F14
                if ((f1320 & 0x04) != 0) functions |= 0x8000; // F15
                if ((f1320 & 0x08) != 0) functions |= 0x10000; // F16
                if ((f1320 & 0x10) != 0) functions |= 0x20000; // F17
                if ((f1320 & 0x20) != 0) functions |= 0x40000; // F18
                if ((f1320 & 0x40) != 0) functions |= 0x80000; // F19
                if ((f1320 & 0x80) != 0) functions |= 0x100000; // F20
            }

            if (data.Length >= 13)
            {
                byte f2128 = data[13];
                if ((f2128 & 0x01) != 0) functions |= 0x200000; // F21
                if ((f2128 & 0x02) != 0) functions |= 0x400000; // F22
                if ((f2128 & 0x04) != 0) functions |= 0x800000; // F23
                if ((f2128 & 0x08) != 0) functions |= 0x1000000; // F24
                if ((f2128 & 0x10) != 0) functions |= 0x2000000; // F25
                if ((f2128 & 0x20) != 0) functions |= 0x4000000; // F26
                if ((f2128 & 0x40) != 0) functions |= 0x8000000; // F27
                if ((f2128 & 0x80) != 0) functions |= 0x10000000; // F28
            }

            locoInfo = new LocoInfo
            {
                Address = address,
                Speed = speed,
                IsForward = forward,
                SpeedSteps = speedSteps switch
                {
                    0 => 14,
                    2 => 28,
                    _ => 128  // Default to 128 for unknown values
                },
                Functions = functions
            };

            return true;
        }
        catch
        {
            return false;
        }
    }
}
