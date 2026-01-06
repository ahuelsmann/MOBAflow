// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Protocol;

public record XBusStatus(bool EmergencyStop, bool TrackOff, bool ShortCircuit, bool Programming);

public static class Z21MessageParser
{
    public static bool IsLanXHeader(byte[] data)
        => data is { Length: >= 4 } && data[2] == Z21Protocol.Header.LAN_X_HEADER && data[3] == 0x00;

    public static bool IsRBusFeedback(byte[] data)
        => data is { Length: >= 4 } && data[2] == Z21Protocol.Header.LAN_RBUS_DATACHANGED && data[3] == 0x00;

    public static bool IsSystemState(byte[] data)
        => data is { Length: >= 4 } && data[2] == Z21Protocol.Header.LAN_SYSTEMSTATE && data[3] == 0x00;

    public static bool IsSerialNumber(byte[] data)
        => data.Length >= 8 && data[2] == Z21Protocol.Header.LAN_GET_SERIAL_NUMBER && data[3] == 0x00;

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
}
