// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Protocol;

public static class Z21Command
{
    /// <summary>
    /// Builds the LAN_GET_SERIAL_NUMBER command to request the Z21 serial number.
    /// Response: 08-00-10-00 XX-XX-XX-XX (4 bytes serial number, little-endian)
    /// </summary>
    public static byte[] BuildGetSerialNumber()
        => [0x04, 0x00, Z21Protocol.Header.LAN_GET_SERIAL_NUMBER, 0x00];

    /// <summary>
    /// Builds the LAN_GET_HWINFO command to request hardware type and firmware version.
    /// Response: 0C-00-1A-00 TT-TT-TT-TT VV-VV-VV-VV (4 bytes HW type + 4 bytes FW version)
    /// </summary>
    public static byte[] BuildGetHwInfo()
        => [0x04, 0x00, Z21Protocol.Header.LAN_GET_HWINFO, 0x00];

    /// <summary>
    /// Builds the LAN_LOGOFF command to unsubscribe from Z21 broadcasts.
    /// This should be sent before disconnecting to immediately free the client slot.
    /// Without this, the Z21 waits 60 seconds before removing the client.
    /// </summary>
    public static byte[] BuildLogoff()
        => [0x04, 0x00, Z21Protocol.Header.LAN_LOGOFF, 0x00];

    public static byte[] BuildHandshake()
        => [0x04, 0x00, Z21Protocol.Header.LAN_SYSTEMSTATE_GETDATA, 0x00];

    /// <summary>
    /// Builds LAN_SET_BROADCASTFLAGS with ALL flags set (0xFFFFFFFF).
    /// WARNING: This subscribes to EVERYTHING and causes high traffic.
    /// Consider using BuildBroadcastFlagsBasic() instead.
    /// </summary>
    public static byte[] BuildBroadcastFlagsAll()
        => [0x08, 0x00, Z21Protocol.Header.LAN_SET_BROADCASTFLAGS, 0x00, 0xFF, 0xFF, 0xFF, 0xFF];

    /// <summary>
    /// Builds LAN_SET_BROADCASTFLAGS with only essential flags for MOBAflow:
    /// - RBUS (0x02): R-Bus feedback sensors (track occupancy)
    /// - SYSTEMSTATE (0x08): System state (current, temperature, track power)
    /// 
    /// This reduces Z21 traffic by ~90% compared to subscribing to all events.
    /// </summary>
    public static byte[] BuildBroadcastFlagsBasic()
    {
        var flags = Z21Protocol.BroadcastFlags.Basic;
        return
        [
            0x08, 0x00,
            Z21Protocol.Header.LAN_SET_BROADCASTFLAGS, 0x00,
            (byte)(flags & 0xFF),
            (byte)((flags >> 8) & 0xFF),
            (byte)((flags >> 16) & 0xFF),
            (byte)((flags >> 24) & 0xFF)
        ];
    }

    public static byte[] BuildTrackPowerOn()
        => [0x07, 0x00, Z21Protocol.Header.LAN_X_HEADER, 0x00, Z21Protocol.XHeader.X_TRACK_POWER, Z21Protocol.TrackPowerDb0.ON, 0xA0];

    public static byte[] BuildTrackPowerOff()
        => [0x07, 0x00, Z21Protocol.Header.LAN_X_HEADER, 0x00, Z21Protocol.XHeader.X_TRACK_POWER, Z21Protocol.TrackPowerDb0.OFF, 0xA1];

    public static byte[] BuildEmergencyStop()
        => [0x06, 0x00, Z21Protocol.Header.LAN_X_HEADER, 0x00, Z21Protocol.XHeader.X_SET_STOP, 0x80];

    public static byte[] BuildGetStatus()
        => [0x07, 0x00, Z21Protocol.Header.LAN_X_HEADER, 0x00, Z21Protocol.XHeader.X_TRACK_POWER, Z21Protocol.XHeader.X_GET_STATUS, 0x05];
}
