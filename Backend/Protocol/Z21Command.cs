// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Protocol;

/// <summary>
/// Helper methods for building raw Z21 LAN command packets.
/// Encapsulates the byte-level encoding for common commands such as
/// handshakes, track power control, locomotive drive and accessory switching.
/// </summary>
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

    /// <summary>
    /// Builds the LAN_SYSTEMSTATE_GETDATA command used as an initial handshake.
    /// This requests a full system state update from the Z21.
    /// </summary>
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

    /// <summary>
    /// Builds the LAN_X_SET_TRACK_POWER_ON command to turn track power on.
    /// </summary>
    public static byte[] BuildTrackPowerOn()
        => [0x07, 0x00, Z21Protocol.Header.LAN_X_HEADER, 0x00, Z21Protocol.XHeader.X_TRACK_POWER, Z21Protocol.TrackPowerDb0.ON, 0xA0];

    /// <summary>
    /// Builds the LAN_X_SET_TRACK_POWER_OFF command to turn track power off.
    /// </summary>
    public static byte[] BuildTrackPowerOff()
        => [0x07, 0x00, Z21Protocol.Header.LAN_X_HEADER, 0x00, Z21Protocol.XHeader.X_TRACK_POWER, Z21Protocol.TrackPowerDb0.OFF, 0xA1];

    /// <summary>
    /// Builds the LAN_X_SET_STOP command to trigger a global emergency stop.
    /// </summary>
    public static byte[] BuildEmergencyStop()
        => [0x06, 0x00, Z21Protocol.Header.LAN_X_HEADER, 0x00, Z21Protocol.XHeader.X_SET_STOP, 0x80];

    /// <summary>
    /// Builds the LAN_X_GET_STATUS command to request the current central status.
    /// </summary>
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

    // ==================== Switching Commands (Accessory Decoders) ====================

    /// <summary>
    /// Builds LAN_X_SET_TURNOUT command for classic DCC turnout/signal decoders.
    /// Format: 0x53 FAdr_MSB FAdr_LSB 10Q0A00P XOR
    /// 
    /// Supports all accessory decoders with 2 outputs per address.
    /// Each decoder address controls 2 outputs via P flag (0 or 1).
    /// 
    /// FAdr Encoding (Z21-Protokoll Spezifikation):
    /// FAdr is calculated as: FAdr = DccAddress - 1
    /// Z21 converts back internally: Dcc_Addr = FAdr + 1
    /// Example: Decoder Address 201 → FAdr = 200 (0x00C8).
    /// 
    /// For multiplex signal decoders (e.g., 5229):
    /// - BaseAddress + AddressOffset (e.g., 50 + 0 = 50) is treated as a regular decoder address
    /// - FAdr = 50 - 1 = 49
    /// - Z21 decodes: Dcc_Addr = 49 + 1 = 50, Port = 0 (no special multiplex handling on Z21 side)
    /// 
    /// See Z21-Protokoll.md Section 5.2 for details.
    /// </summary>
    /// <param name="decoderAddress">Accessory decoder address (1-2044)</param>
    /// <param name="output">Output index: 0 = output 1 (P=0), 1 = output 2 (P=1)</param>
    /// <param name="activate">True = activate, False = deactivate</param>
    /// <param name="queue">True = queue command (FW 1.24+), False = immediate execution</param>
    public static byte[] BuildSetTurnout(int decoderAddress, int output, bool activate, bool queue = false)
    {
        // FAdr encoding per Z21 protocol: FAdr = dcc address - 1
        var fAdr = decoderAddress - 1;
        byte adrLsb = (byte)(fAdr & 0xFF);
        byte adrMsb = (byte)((fAdr >> 8) & 0xFF);

        // Build command byte: 10Q0A00P
        // Bit 7-6: 10 (always)
        // Bit 5:   Q = queue flag (1=queue, 0=immediate)
        // Bit 4:   0 (reserved)
        // Bit 3:   A = activate (1=activate, 0=deactivate)
        // Bit 2-1: 00 (reserved)
        // Bit 0:   P = output (0=output1, 1=output2)
        byte cmdByte = (byte)(
            0x80 |                              // 10XXXXXX
            (queue ? 0x20 : 0x00) |            // Q flag
            (activate ? 0x08 : 0x00) |         // A flag
            (output & 0x01)                    // P flag
        );

        // Calculate XOR checksum
        byte xor = (byte)(Z21Protocol.XHeader.X_SET_TURNOUT ^ adrMsb ^ adrLsb ^ cmdByte);

        return
        [
            0x09, 0x00,  // DataLen = 9 bytes (per Z21-Protokoll Section 5.2)
            Z21Protocol.Header.LAN_X_HEADER, 0x00,
            Z21Protocol.XHeader.X_SET_TURNOUT,  // 0x53
            adrMsb, adrLsb,
            cmdByte,
            xor
        ];
    }

    /// <summary>
    /// Builds LAN_X_SET_EXT_ACCESSORY command for extended accessory decoders (e.g., multiplex decoders).
    /// Format: 0x54 Adr_MSB Adr_LSB DDDDDDDD 0x00 XOR
    /// 
    /// Supports 256 different values per address (0-255).
    /// Extended accessory decoders (RawAddress = 4) can hold multiple configuration profiles.
    /// Example: Multiplex signal decoder 5229 with profiles for signal combinations.
    /// </summary>
    /// <param name="extAccessoryAddress">Extended accessory address (0-255)</param>
    /// <param name="commandValue">Command value (0-255) for specific decoder state/aspect</param>
    public static byte[] BuildSetExtAccessory(int extAccessoryAddress, int commandValue)
    {
        // Extended accessory addressing: use raw address (0-255)
        var fAdr = extAccessoryAddress;
        byte adrMsb = (byte)((fAdr >> 8) & 0xFF);
        byte adrLsb = (byte)(fAdr & 0xFF);
        byte cmdByte = (byte)(commandValue & 0xFF);

        // Calculate XOR checksum
        byte xor = (byte)(Z21Protocol.XHeader.X_SET_EXT_ACCESSORY ^ adrMsb ^ adrLsb ^ cmdByte ^ 0x00);

        return
        [
            0x0A, 0x00,  // DataLen = 10 bytes
            Z21Protocol.Header.LAN_X_HEADER, 0x00,
            Z21Protocol.XHeader.X_SET_EXT_ACCESSORY,  // 0x54
            adrMsb, adrLsb,
            cmdByte,
            0x00,  // Reserved byte
            xor
        ];
    }

    /// <summary>
    /// Builds LAN_X_GET_TURNOUT_INFO command to request turnout/signal status.
    /// Format: 0x43 FAdr_MSB FAdr_LSB XOR
    /// 
    /// FAdr Encoding: FAdr = DccAddress - 1 (Z21-Protokoll Spec).
    /// </summary>
    /// <param name="decoderAddress">Accessory decoder address (1-2044)</param>
    public static byte[] BuildGetTurnoutInfo(int decoderAddress)
    {
        var fAdr = decoderAddress - 1;
        byte adrLsb = (byte)(fAdr & 0xFF);
        byte adrMsb = (byte)((fAdr >> 8) & 0xFF);

        byte xor = (byte)(Z21Protocol.XHeader.X_GET_TURNOUT_INFO ^ adrMsb ^ adrLsb);

        return
        [
            0x09, 0x00,  // DataLen = 9 bytes
                Z21Protocol.Header.LAN_X_HEADER, 0x00,
                Z21Protocol.XHeader.X_GET_TURNOUT_INFO,  // 0x43
                adrMsb, adrLsb,
                xor
        ];
    }
}
