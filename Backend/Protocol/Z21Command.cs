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

        // ==================== Locomotive Drive Commands (Section 4) ====================

        /// <summary>
        /// Encodes a DCC locomotive address for Z21 protocol.
        /// For addresses >= 128, the MSB is OR'd with 0xC0.
        /// </summary>
        private static (byte msb, byte lsb) EncodeLocoAddress(int address)
        {
            if (address < 128)
            {
                return (0x00, (byte)address);
            }
            // Long address: MSB = 0xC0 | high byte
            return ((byte)(0xC0 | ((address >> 8) & 0x3F)), (byte)(address & 0xFF));
        }

        /// <summary>
        /// Builds LAN_X_SET_LOCO_DRIVE command for 128 speed steps.
        /// Format: 0xE4 0x13 Adr_MSB Adr_LSB RVVVVVVV XOR
        /// </summary>
        /// <param name="address">DCC locomotive address (1-9999)</param>
        /// <param name="speed">Speed value (0=stop, 1=emergency stop, 2-127=speed 1-126)</param>
        /// <param name="forward">True = forward direction</param>
        public static byte[] BuildSetLocoDrive(int address, int speed, bool forward)
        {
            var (adrMsb, adrLsb) = EncodeLocoAddress(address);

            // Speed byte: bit 7 = direction (1=forward), bits 0-6 = speed
            // Speed encoding for 128 steps: 0=stop, 1=emergency stop, 2-127=speed 1-126
            byte speedByte = (byte)((forward ? 0x80 : 0x00) | (speed & 0x7F));

            // DB0 = 0x13 for 128 speed steps
            byte db0 = 0x13;

            // Calculate XOR checksum over X-BUS portion
            byte xor = (byte)(Z21Protocol.XHeader.X_SET_LOCO_DRIVE ^ db0 ^ adrMsb ^ adrLsb ^ speedByte);

            return
            [
                0x0A, 0x00,  // DataLen = 10 bytes
                Z21Protocol.Header.LAN_X_HEADER, 0x00,  // Header
                Z21Protocol.XHeader.X_SET_LOCO_DRIVE,   // 0xE4
                db0,                                     // 0x13 (128 speed steps)
                adrMsb, adrLsb,                         // Address
                speedByte,                               // Direction + Speed
                xor                                      // XOR checksum
            ];
        }

        /// <summary>
        /// Builds LAN_X_SET_LOCO_FUNCTION command.
        /// Format: 0xE4 0xF8 Adr_MSB Adr_LSB TTNNNNNN XOR
        /// </summary>
        /// <param name="address">DCC locomotive address (1-9999)</param>
        /// <param name="functionIndex">Function index (0=F0/light, 1=F1, etc.)</param>
        /// <param name="on">True = on, False = off</param>
        public static byte[] BuildSetLocoFunction(int address, int functionIndex, bool on)
        {
            var (adrMsb, adrLsb) = EncodeLocoAddress(address);

            // Function byte: TT (bits 6-7) = 00=off, 01=on; NNNNNN (bits 0-5) = function index
            // TT: 00 = off, 01 = on, 10 = toggle
            byte funcByte = (byte)((on ? 0x40 : 0x00) | (functionIndex & 0x3F));

            // DB0 = 0xF8 for function command
            byte db0 = 0xF8;

            // Calculate XOR checksum
            byte xor = (byte)(Z21Protocol.XHeader.X_SET_LOCO_FUNCTION ^ db0 ^ adrMsb ^ adrLsb ^ funcByte);

            return
            [
                0x0A, 0x00,  // DataLen = 10 bytes
                Z21Protocol.Header.LAN_X_HEADER, 0x00,
                Z21Protocol.XHeader.X_SET_LOCO_FUNCTION,  // 0xE4
                db0,                                       // 0xF8
                adrMsb, adrLsb,
                funcByte,
                xor
            ];
        }

        /// <summary>
        /// Builds LAN_X_GET_LOCO_INFO command.
        /// Format: 0xE3 0xF0 Adr_MSB Adr_LSB XOR
        /// Subscribes to loco updates (max 16 addresses per client, FIFO).
        /// </summary>
        /// <param name="address">DCC locomotive address (1-9999)</param>
        public static byte[] BuildGetLocoInfo(int address)
        {
            var (adrMsb, adrLsb) = EncodeLocoAddress(address);

            // DB0 = 0xF0
            byte db0 = 0xF0;

            // Calculate XOR checksum
            byte xor = (byte)(Z21Protocol.XHeader.X_GET_LOCO_INFO ^ db0 ^ adrMsb ^ adrLsb);

            return
            [
                0x09, 0x00,  // DataLen = 9 bytes
                Z21Protocol.Header.LAN_X_HEADER, 0x00,
                Z21Protocol.XHeader.X_GET_LOCO_INFO,  // 0xE3
                db0,                                   // 0xF0
                adrMsb, adrLsb,
                xor
            ];
        }
    }
