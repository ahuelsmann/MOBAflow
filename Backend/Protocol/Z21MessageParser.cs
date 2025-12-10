// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Protocol;

public record XBusStatus(bool EmergencyStop, bool TrackOff, bool ShortCircuit, bool Programming);

public static class Z21MessageParser
{
    public static bool IsLanXHeader(byte[] data)
        => data.Length >= 4 && data[2] == Z21Protocol.Header.LAN_X_HEADER && data[3] == 0x00;

    public static bool IsRBusFeedback(byte[] data)
        => data.Length >= 4 && data[2] == Z21Protocol.Header.LAN_RBUS_DATACHANGED && data[3] == 0x00;

    public static bool IsSystemState(byte[] data)
        => data.Length >= 4 && data[2] == Z21Protocol.Header.LAN_SYSTEMSTATE && data[3] == 0x00;

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
