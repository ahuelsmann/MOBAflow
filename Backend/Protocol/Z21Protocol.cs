namespace Moba.Backend.Protocol;

public static class Z21Protocol
{
    public const int DefaultPort = 21105;

    // Top-level headers (first two bytes = length, next two bytes = header)
    public static class Header
    {
        public const byte LAN_X_HEADER = 0x40;   // 07-00-40-00 ...
        public const byte LAN_SET_BROADCASTFLAGS = 0x50; // 08-00-50-00 ...
        public const byte LAN_RBUS_DATACHANGED = 0x80;   // 0F-00-80-00 ...
        public const byte LAN_SYSTEMSTATE = 0x84;        // 14-00-84-00 ...
        public const byte LAN_SYSTEMSTATE_GETDATA = 0x85; // 04-00-85-00
    }

    // X-Header DB0 values
    public static class XHeader
    {
        public const byte X_GET_STATUS = 0x24; // LAN_X_GET_STATUS
        public const byte X_SET_STOP = 0x80;
        public const byte X_STATUS = 0x61;     // status reply group
        public const byte X_STATUS_CHANGED = 0x62;
        public const byte X_TRACK_POWER = 0x21; // used with DB0 0x81/0x80
    }

    public static class TrackPowerDb0
    {
        public const byte OFF = 0x80;
        public const byte ON = 0x81;
    }

    public static class BroadcastFlags
    {
        public const uint All = 0xFFFF_FFFF;
    }

    public static string ToHex(byte[] data) => BitConverter.ToString(data);
}
