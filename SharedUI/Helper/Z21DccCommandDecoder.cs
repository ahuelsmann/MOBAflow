// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Helper;

#pragma warning disable CS1570 // XML comment contains badly formatted XML
/// <summary>
/// Decodes Z21 DCC command bytes into human-readable format.
/// Supports parsing LAN_X_SET_LOCO_DRIVE commands.
/// 
/// Z21 Protocol: LAN_X_SET_LOCO_DRIVE (0xE4)
/// Byte structure: [Len(2)] [Header(2)] [0xE4] [AddrH] [AddrL] [Speed/Dir] [XOR]
///
/// Example: "0A 00 80 00 E4 03 E5 80 12"
/// - 0A 00: Length = 10 bytes
/// - 80 00: Header = LAN_X
/// - E4: DB0 = SET_LOCO_DRIVE
/// - 03: DB1 = Address high byte (steps)
/// - E5: DB2 = Address low byte & direction/speed
/// - 80: DB3 = Direction flag & speed
/// - 12: XOR checksum
///
/// DCC Address calculation:
/// - Short address (1-127): directly from bytes
/// - Long address (128-10239): combined from two bytes using Motorola format
/// </summary>
#pragma warning restore CS1570
public static class Z21DccCommandDecoder
{
    /// <summary>
    /// Represents decoded DCC command information.
    /// </summary>
    public class DccCommand
    {
        public int Address { get; set; }
        public int Speed { get; set; }
        public string Direction { get; set; } = "Forward";
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Decodes Z21 DCC command bytes into address, speed, and direction.
    /// Returns a DccCommand object with IsValid=false if bytes cannot be decoded.
    /// </summary>
    public static DccCommand DecodeLocoCommand(byte[]? bytes)
    {
        var result = new DccCommand { IsValid = false };

        if (bytes == null || bytes.Length < 7)
        {
            result.ErrorMessage = "Invalid byte array length";
            return result;
        }

        try
        {
            // Z21 frame structure:
            // [0-1]: Length (little-endian)
            // [2-3]: Header (0x80 0x00 for LAN_X)
            // [4]: X-Header (0xE4 for SET_LOCO_DRIVE)
            // [5]: DB1 - Address high byte or steps (for short address, this is speed steps)
            // [6]: DB2 - Address low byte or direction flag (for short address, this is direction)
            // [7]: DB3 - Speed and direction combined (bit 7 = direction, bits 0-6 = speed)
            // [8]: XOR checksum

            // For compatibility, we check if this looks like a locomotive command
            if (bytes.Length >= 9 && bytes[4] == 0xE4)
            {
                // LAN_X_SET_LOCO_DRIVE format
                return DecodeLocoDriveCommand(bytes);
            }
            else if (bytes.Length >= 7)
            {
                // Fallback: try to parse as generic Z21 locomotive command
                return DecodeGenericLocoCommand(bytes);
            }

            result.ErrorMessage = "Unknown command format";
            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Error decoding: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Decodes LAN_X_SET_LOCO_DRIVE (0xE4) command.
    /// 
    /// Z21 Protocol format: [Length(2)] [Header(2)] [X-Header] [DB1] [DB2] [DB3] [XOR]
    /// Example bytes: 0A 00 40 00 E4 13 00 65 80 12
    ///                      └──┘ LAN_X_HEADER (0x40 0x00)
    ///                          └──┘ SET_LOCO_DRIVE (0xE4)
    ///                             └──┘ DB1 (Address high byte with step info)
    ///                                └──┘ DB2 (Address low byte)
    ///                                   └──┘ DB3 (Speed + Direction byte)
    ///                                      └──┘ XOR checksum
    /// 
    /// DB1 format: [X X A A A A A A]
    ///   - Bits 0-5: Address bits 8-13 (for long addresses)
    ///   - Bits 6-7: Speed step format (00=14, 01=28, 10=128 steps)
    /// 
    /// DB2: Address bits 0-7
    /// 
    /// DB3: [D S S S S S S S]
    ///   - Bit 7 (D): Direction (0=Forward, 1=Backward)
    ///   - Bits 0-6: Speed (0-127 in 128-step mode)
    /// 
    /// For Address 101:
    ///   - Binary: 00000000 01100101
    ///   - DB1: 0x13 (speed steps 128 = 10, address bits 8-13 = 000011)
    ///   - DB2: 0x00 (address bits 0-7 = 00000000) 
    ///   Wait, that's wrong. Let me recalculate:
    ///   
    ///   Address 101 = 0b01100101
    ///   For Z21 14-bit address:
    ///     Bits 8-13: 0b000000
    ///     Bits 0-7:  0b01100101 (0x65)
    ///   
    ///   But we have DB1=0x13, DB2=0x00
    ///   0x13 = 0b00010011
    ///   This suggests: speed_steps=00 (14-step), addr_high=0b010011 = 19
    ///   
    ///   Hmm, that gives address = (19 << 8) | 0x00 = 19  ← This matches the wrong output!
    ///   
    ///   So the actual structure must be DIFFERENT.
    ///   
    ///   Let me try: Maybe it's NOT index [5][6] but rather [5][6] = Address, [7] = Speed
    ///   
    ///   But with bytes: 0A 00 40 00 E4 [03] [E5] [80] 12
    ///                                    └──┘ └──┘ └──┘
    ///   
    ///   If we use the NMRA DCC standard:
    ///   Address high byte: 0x03 & 0x3F = 3
    ///   Address low byte: 0xE5 = 229
    ///   Address = (3 << 8) | 229 = 768 + 229 = 997  ← Still wrong!
    ///   
    ///   NEW INSIGHT: Maybe the header is at index [2-3] NOT [4]!
    ///   Let me re-index:
    ///   
    ///   Bytes: [0A 00] [40 00] [E4] [13] [00] [65] [80] [12]
    ///          Length  Header  DB0  DB1  DB2  DB3  ???  ???
    ///   
    ///   Wait, that's only 8 bytes but we have 9 bytes total!
    ///   
    ///   Let me check if maybe there's padding or the structure is:
    ///   0A 00 = Length
    ///   40 00 = Header  ← But we have 80 00 in actual bytes!
    ///   
    ///   ACTUAL BYTES AGAIN: 0A 00 80 00 E4 03 E5 80 12
    ///   
    ///   Header is 80 00 not 40 00!
    ///   So maybe this is a DIFFERENT command type?
    ///   
    ///   Header 0x80 = LAN_RMBUS_DATACHANGED (R-Bus feedback)
    ///   Header 0x40 = LAN_X_HEADER
    ///   
    ///   But byte[2-3] = 80 00... That's:
    ///   Little-endian: 0x0080 = 128 decimal
    ///   OR byte-swapped header?
    ///   
    ///   Let me try: Header is LITTLE-ENDIAN!
    ///   bytes[2] = 0x80 (low byte)
    ///   bytes[3] = 0x00 (high byte)
    ///   → Header = 0x0080 → But that's still wrong
    ///   
    ///   OR: bytes[2-3] = [0x80 0x00] as two separate header bytes
    ///       Maybe it's [LAN_X=0x40] shifted or modified?
    ///   
    ///   Actually, let me check if index [2] is 0x40 after byte-swap:
    ///   No wait, the monitor shows "LAN_X" for header 0x40...
    ///   
    ///   CRITICAL REALIZATION: Maybe the Base64 decode is byte-swapped!
    ///   
    ///   Let me manually decode the Base64:
    ///   "CgBAAOQTAGWAEg==" 
    ///   
    ///   C g B A A O Q T A G W A E g
    ///   
    ///   Base64 decode:
    ///   0x0A 0x00 0x40 0x00 0xE4 0x13 0x00 0x65 0x80 0x12
    ///   
    ///   AH HA! The bytes are: 0A 00 40 00 E4 13 00 65 80 12
    ///   NOT: 0A 00 80 00 E4 03 E5 80 12
    ///   
    ///   I had the bytes WRONG in my analysis!
    ///   
    ///   Correct bytes: [0x0A 0x00] [0x40 0x00] [0xE4] [0x13] [0x00] [0x65] [0x80] [0x12]
    ///                  Length      LAN_X      DB0    DB1   DB2   DB3   ???   XOR
    ///   
    ///   Now let's decode properly:
    ///   DB1 = 0x13 = 0b00010011
    ///     Bits 6-7: 00 = 14-step mode
    ///     Bits 0-5: 010011 = 19
    ///   
    ///   Hmm, that's STILL giving address 19!
    ///   
    ///   BUT WAIT: DB2 = 0x00, DB3 = 0x65
    ///   
    ///   What if the address is in DB2-DB3, not DB1-DB2?
    ///   
    ///   DB2 = 0x00 (high byte)
    ///   DB3 = 0x65 (low byte) = 101 decimal!
    ///   
    ///   And DB1 = 0x13 contains the speed step info + something else
    ///   
    ///   Then where is Speed and Direction?
    ///   bytes[7] = 0x80 = 0b10000000
    ///     Bit 7 = 1 = Backward ✓
    ///     Bits 0-6 = 0000000 = Speed 0 ✓
    ///   
    ///   BINGO! The correct mapping is:
    ///   bytes[4] = 0xE4 (X-Header SET_LOCO_DRIVE)
    ///   bytes[5] = 0x13 (DB1 - Format/Flags)
    ///   bytes[6] = 0x00 (DB2 - Address high byte)
    ///   bytes[7] = 0x65 (DB3 - Address low byte) = 101
    ///   bytes[8] = 0x80 (DB4 - Speed + Direction)
    ///   bytes[9] = 0x12 (XOR checksum)
    ///   
    ///   So for Address 101:
    ///   Address = (bytes[6] << 8) | bytes[7] = (0x00 << 8) | 0x65 = 101 ✓
    ///   
    ///   For Speed/Direction:
    ///   speedDir = bytes[8] = 0x80
    ///   Direction = (0x80 & 0x80) ? Backward : Forward = Backward ✓
    ///   Speed = 0x80 & 0x7F = 0 ✓
    ///   
    ///   PERFECT! Now I know the correct byte indices!
    /// </summary>
    private static DccCommand DecodeLocoDriveCommand(byte[] bytes)
    {
        var result = new DccCommand();

        try
        {
            // Correct Z21 LAN_X_SET_LOCO_DRIVE byte mapping:
            // bytes[0-1]: Length
            // bytes[2-3]: Header (LAN_X = 0x40 0x00)
            // bytes[4]:   X-Header (0xE4 = SET_LOCO_DRIVE)
            // bytes[5]:   DB1 (Speed step format + flags)
            // bytes[6]:   DB2 (Address high byte)
            // bytes[7]:   DB3 (Address low byte)
            // bytes[8]:   DB4 (Speed + Direction)
            // bytes[9]:   XOR checksum

            if (bytes.Length < 9)
            {
                result.ErrorMessage = "Insufficient bytes for LAN_X_SET_LOCO_DRIVE";
                return result;
            }

            // Extract address from bytes[6-7]
            byte addrH = bytes[6];
            byte addrL = bytes[7];

            // Address is simply: (high << 8) | low
            int address = (addrH << 8) | addrL;
            result.Address = address;

            // Extract speed and direction from bytes[8]
            byte speedDir = bytes[8];

            // Bit 7: Direction (1 = Backward, 0 = Forward)
            result.Direction = ((speedDir & 0x80) != 0) ? "Backward" : "Forward";

            // Bits 0-6: Speed (0-127)
            result.Speed = speedDir & 0x7F;

            result.IsValid = true;
            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Error decoding loco drive: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Fallback decoder for generic locomotive commands.
    /// Attempts to extract address and speed from generic Z21 format.
    /// </summary>
    private static DccCommand DecodeGenericLocoCommand(byte[] bytes)
    {
        var result = new DccCommand();

        try
        {
            // Simple heuristic for generic locomotive commands
            // Assumes: bytes[3-4] = address, bytes[5] = speed
            
            if (bytes.Length >= 5)
            {
                // Try to extract address from bytes 3-4
                byte addrH = bytes[3];
                byte addrL = bytes[4];

                // Simple address extraction
                if (addrH <= 127)
                {
                    result.Address = addrH;
                }
                else
                {
                    result.Address = ((addrH & 0x3F) << 8) | addrL;
                }

                // Extract speed from byte 5 if available
                if (bytes.Length >= 6)
                {
                    result.Speed = bytes[5] & 0x7F;
                    result.Direction = ((bytes[5] & 0x80) == 0) ? "Forward" : "Backward";
                }

                result.IsValid = true;
                return result;
            }

            result.ErrorMessage = "Cannot parse generic locomotive command";
            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Error in generic decoder: {ex.Message}";
            return result;
        }
    }

        /// <summary>
        /// Formats byte array as hexadecimal string (e.g., "0A 00 80 00 E4 03 E5 80 12").
        /// </summary>
        public static string FormatBytesAsHex(byte[]? bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return "(empty)";

            return BitConverter.ToString(bytes).Replace("-", " ");
        }

        /// <summary>
        /// Formats decoded DCC command as human-readable string.
        /// Example: "Addr: 101, Speed: 127, Direction: Forward"
        /// </summary>
        public static string FormatDccCommand(DccCommand? command)
        {
            if (command == null || !command.IsValid)
                return command?.ErrorMessage ?? "(Unknown format)";

            return $"Addr: {command.Address}, Speed: {command.Speed}, Direction: {command.Direction}";
        }

        /// <summary>
        /// Encodes DCC command parameters (Address, Speed, Direction) into Z21 LAN_X_SET_LOCO_DRIVE bytes.
        /// 
        /// Generated byte structure:
        /// [0-1]  Length (10 bytes, little-endian)
        /// [2-3]  Header (LAN_X_HEADER = 0x40 0x00)
        /// [4]    X-Header (SET_LOCO_DRIVE = 0xE4)
        /// [5]    DB1 (Speed step format = 0x13 for 128-step mode)
        /// [6]    DB2 (Address high byte)
        /// [7]    DB3 (Address low byte)
        /// [8]    DB4 (Speed + Direction combined)
        /// [9]    XOR checksum
        /// 
        /// Example output for Address=101, Speed=0, Direction=Backward:
        /// 0A 00 40 00 E4 13 00 65 80 12
        /// </summary>
        /// <param name="address">DCC address (0-10239)</param>
        /// <param name="speed">Speed (0-127, 128-step mode)</param>
        /// <param name="direction">Direction ("Forward" or "Backward")</param>
        /// <returns>Z21 command bytes ready to send to Z21</returns>
        public static byte[] EncodeLocoCommand(int address, int speed, string direction)
        {
            // Validate inputs
            if (address < 0 || address > 10239)
                throw new ArgumentOutOfRangeException(nameof(address), "Address must be between 0 and 10239");

            if (speed < 0 || speed > 127)
                throw new ArgumentOutOfRangeException(nameof(speed), "Speed must be between 0 and 127");

            // Create command bytes
            byte[] bytes = new byte[10];

            // [0-1] Length (10 bytes, little-endian)
            bytes[0] = 0x0A;
            bytes[1] = 0x00;

            // [2-3] Header (LAN_X_HEADER)
            bytes[2] = 0x40;
            bytes[3] = 0x00;

            // [4] X-Header (SET_LOCO_DRIVE)
            bytes[4] = 0xE4;

            // [5] DB1 (Speed step format: 0x13 = 128-step mode)
            bytes[5] = 0x13;

            // [6-7] Address (16-bit, big-endian)
            bytes[6] = (byte)((address >> 8) & 0xFF);  // High byte
            bytes[7] = (byte)(address & 0xFF);         // Low byte

            // [8] Speed + Direction
            byte speedDir = (byte)(speed & 0x7F);  // Bits 0-6: Speed

            // Bit 7: Direction (1 = Backward, 0 = Forward)
            if (direction.Equals("Backward", StringComparison.OrdinalIgnoreCase))
            {
                speedDir |= 0x80;  // Set bit 7
            }

            bytes[8] = speedDir;

            // [9] XOR checksum (XOR of bytes 4-8)
            bytes[9] = (byte)(bytes[4] ^ bytes[5] ^ bytes[6] ^ bytes[7] ^ bytes[8]);

            return bytes;
        }

        /// <summary>
        /// Encodes DCC command from DccCommand object.
        /// </summary>
        public static byte[] EncodeLocoCommand(DccCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            return EncodeLocoCommand(command.Address, command.Speed, command.Direction);
        }
    }
