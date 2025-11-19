namespace Moba.Backend.Protocol;

public static class Z21Command
{
    public static byte[] BuildHandshake()
        => [0x04, 0x00, Z21Protocol.Header.LAN_SYSTEMSTATE_GETDATA, 0x00];

    public static byte[] BuildBroadcastFlagsAll()
        => [0x08, 0x00, Z21Protocol.Header.LAN_SET_BROADCASTFLAGS, 0x00, 0xFF, 0xFF, 0xFF, 0xFF];

    public static byte[] BuildTrackPowerOn()
        => [0x07, 0x00, Z21Protocol.Header.LAN_X_HEADER, 0x00, Z21Protocol.XHeader.X_TRACK_POWER, Z21Protocol.TrackPowerDb0.ON, 0xA0];

    public static byte[] BuildTrackPowerOff()
        => [0x07, 0x00, Z21Protocol.Header.LAN_X_HEADER, 0x00, Z21Protocol.XHeader.X_TRACK_POWER, Z21Protocol.TrackPowerDb0.OFF, 0xA1];

    public static byte[] BuildEmergencyStop()
        => [0x06, 0x00, Z21Protocol.Header.LAN_X_HEADER, 0x00, Z21Protocol.XHeader.X_SET_STOP, 0x80];

    public static byte[] BuildGetStatus()
        => [0x07, 0x00, Z21Protocol.Header.LAN_X_HEADER, 0x00, Z21Protocol.XHeader.X_TRACK_POWER, Z21Protocol.XHeader.X_GET_STATUS, 0x05];
}