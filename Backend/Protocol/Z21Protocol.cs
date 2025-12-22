// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Protocol;

public static class Z21Protocol
{
    /// <summary>
    /// Default UDP port for Z21 communication.
    /// This value should be read from Settings.DefaultPort in the application.
    /// </summary>
    public const int DefaultPort = 21105;

    // Top-level headers (first two bytes = length, next two bytes = header)
    public static class Header
    {
        public const byte LAN_GET_SERIAL_NUMBER = 0x10;  // 04-00-10-00 - Request serial number
        public const byte LAN_GET_HWINFO = 0x1A;         // 04-00-1A-00 - Request HW type and FW version
        public const byte LAN_LOGOFF = 0x30;             // 04-00-30-00 - Unsubscribe from Z21 broadcasts
        public const byte LAN_X_HEADER = 0x40;           // 07-00-40-00 ...
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
        /// <summary>All flags set - subscribes to EVERYTHING (not recommended, causes high traffic)</summary>
        public const uint All = 0xFFFF_FFFF;
        
        /// <summary>
        /// Broadcast flags for basic model railway control.
        /// - RBUS: R-Bus feedback (track occupancy sensors) - essential for feedback points
        /// - SYSTEMSTATE: System state updates (current, temperature, track power status)
        /// </summary>
        public const uint Basic = Rbus | SystemState;
        
        /// <summary>R-Bus feedback messages (Bit 1) - track occupancy/feedback sensors</summary>
        public const uint Rbus = 0x0000_0002;
        
        /// <summary>System state changes (Bit 3) - current, temperature, track power, emergency stop</summary>
        public const uint SystemState = 0x0000_0008;
        
        /// <summary>Driving commands from other controllers (Bit 0) - not needed for feedback-only apps</summary>
        public const uint Driving = 0x0000_0001;
        
        /// <summary>RailCom data (Bit 2) - advanced loco detection, usually not needed</summary>
        public const uint RailCom = 0x0000_0004;
        
        /// <summary>LocoNet messages (Bit 8) - only if using LocoNet devices</summary>
        public const uint LocoNet = 0x0000_0100;
    }

    public static string ToHex(byte[] data) => BitConverter.ToString(data);
}
