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
    /// Determines whether the given packet is a LAN_X_GET_VERSION response (X-Header 0x63).
    /// Response format: 09-00-40-00 63-21 XBUS_VER CMDST_ID_MSB CMDST_ID_LSB
    /// </summary>
    public static bool IsLanXGetVersionResponse(byte[] data)
        => data.Length >= 9 && IsLanXHeader(data) && data[4] == 0x63;

    /// <summary>
    /// Parses the LAN_X_GET_VERSION response (0x63).
    /// Format: 09-00-40-00 63-21 XBUS_VER CMDST_ID_MSB CMDST_ID_LSB
    /// XBUS_VER is the X-Bus protocol version; CMDST_ID identifies the command station (e.g. 0x12 = Z21 family).
    /// Does not contain serial number or full hardware type; use as fallback when LAN_GET_SERIAL_NUMBER / LAN_GET_HWINFO are not sent by the device.
    /// </summary>
    public static bool TryParseLanXGetVersionResponse(byte[] data, out byte xbusVer, out ushort cmdstId)
    {
        xbusVer = 0;
        cmdstId = 0;
        if (data.Length < 9 || !IsLanXGetVersionResponse(data)) return false;
        xbusVer = data[6];
        cmdstId = (ushort)((data[7] << 8) | data[8]);
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
        byte status;
        switch (xHeader)
        {
            case Z21Protocol.XHeader.X_STATUS:
                // LAN_X_BC_STATUS (0x61): [xHeader, DB0=status, XOR]
                // The status flags are in DB0 (byte index 5), not in the XOR byte.
                status = data[5];
                break;
            case Z21Protocol.XHeader.X_STATUS_CHANGED:
                // LAN_X_STATUS_CHANGED (0x62): [xHeader, DB0, status, XOR]
                status = data[6];
                break;
            default:
                return null;
        }

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
    /// Determines whether the given packet is a turnout info response (LAN_X_TURNOUT_INFO).
    /// </summary>
    public static bool IsTurnoutInfo(byte[] data)
        => data.Length >= 9 && IsLanXHeader(data) && data[4] == Z21Protocol.XHeader.X_TURNOUT_INFO;

    /// <summary>
    /// Determines whether the given packet is a loco info response (LAN_X_LOCO_INFO).
    /// </summary>
    /// <param name="data">Raw Z21 packet bytes.</param>
    /// <returns><c>true</c> if the packet contains loco info, otherwise <c>false</c>.</returns>
    public static bool IsLocoInfo(byte[] data)
        => data.Length >= 7 && IsLanXHeader(data) && data[4] == Z21Protocol.XHeader.X_LOCO_INFO;

    /// <summary>
    /// Parses LAN_X_TURNOUT_INFO (0x43). ZZ=00 not switched, 01=P=0, 10=P=1 per Z21 spec section 5.3.
    /// </summary>
    public static bool TryParseTurnoutInfo(byte[] data, out TurnoutInfo? turnoutInfo)
    {
        turnoutInfo = null;
        if (!IsTurnoutInfo(data))
        {
            return false;
        }

        var functionAddress = (data[5] << 8) | data[6];
        var state = data[7] & 0x03;
        turnoutInfo = new TurnoutInfo
        {
            FunctionAddress = functionAddress,
            IsSwitched = state != 0,
            OutputPosition = state == 2
        };
        return true;
    }

    /// <summary>
    /// Parses the LAN_X_LOCO_INFO response (X-Bus Loco Information).
    /// Format: 40-00-EF + Adr_MSB Adr_LSB + DB2 + DB3 + DB4..DB8 (F0-F31) + XOR
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

            // Parse function status per Z21 LAN spec v1.13 section 4.4 (DB4-DB8).
            // DB4: 0DSLFGHJ (L=F0, J=F1, H=F2, G=F3, F=F4)
            // DB5: F5-F12, DB6: F13-F20, DB7: F21-F28, DB8: F29-F31
            uint functions = 0;

            if (data.Length >= 10)
            {
                byte db4 = data[9];
                if ((db4 & 0x10) != 0) functions |= 1u << 0; // F0 (L)
                if ((db4 & 0x01) != 0) functions |= 1u << 1; // F1 (J)
                if ((db4 & 0x02) != 0) functions |= 1u << 2; // F2 (H)
                if ((db4 & 0x04) != 0) functions |= 1u << 3; // F3 (G)
                if ((db4 & 0x08) != 0) functions |= 1u << 4; // F4 (F)
            }

            if (data.Length >= 11)
            {
                byte db5 = data[10];
                for (int bit = 0; bit < 8; bit++)
                {
                    if ((db5 & (1 << bit)) != 0)
                    {
                        functions |= 1u << (5 + bit);
                    }
                }
            }

            if (data.Length >= 12)
            {
                byte db6 = data[11];
                for (int bit = 0; bit < 8; bit++)
                {
                    if ((db6 & (1 << bit)) != 0)
                    {
                        functions |= 1u << (13 + bit);
                    }
                }
            }

            if (data.Length >= 13)
            {
                byte db7 = data[12];
                for (int bit = 0; bit < 8; bit++)
                {
                    if ((db7 & (1 << bit)) != 0)
                    {
                        functions |= 1u << (21 + bit);
                    }
                }
            }

            if (data.Length >= 14)
            {
                byte db8 = data[13];
                for (int bit = 0; bit < 3; bit++)
                {
                    if ((db8 & (1 << bit)) != 0)
                    {
                        functions |= 1u << (29 + bit);
                    }
                }
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