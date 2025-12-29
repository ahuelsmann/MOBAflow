// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Helper;

/// <summary>
/// Decodes Z21 DCC locomotive drive commands (LAN_X_SET_LOCO_DRIVE, 0xE4)
/// into address, speed, and direction information.
/// </summary>
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

            if (bytes.Length >= 7)
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
    /// Decodes LAN_X_SET_LOCO_DRIVE (0xE4) command using Z21 byte mapping.
    /// </summary>
    /// <remarks>
    /// Expected byte layout:
    /// [0-1] Length | [2-3] Header (LAN_X = 0x40 0x00) | [4] X-Header (0xE4)
    /// [5] DB1 (speed step format/flags) | [6] DB2 (address high) | [7] DB3 (address low)
    /// [8] DB4 (speed+direction) | [9] XOR checksum (bytes 4-8).
    /// Speed+direction byte: bit7=1 → Backward, bit7=0 → Forward; bits0-6 = speed (0-127).
    /// </remarks>
    private static DccCommand DecodeLocoDriveCommand(byte[] bytes)
    {
        var result = new DccCommand();

        try
        {
            if (bytes.Length < 9)
            {
                result.ErrorMessage = "Insufficient bytes for LAN_X_SET_LOCO_DRIVE";
                return result;
            }

            // Extract address from bytes[6-7]
            byte addrH = bytes[6];
            byte addrL = bytes[7];

            int address = (addrH << 8) | addrL;
            result.Address = address;

            // Extract speed and direction from bytes[8]
            byte speedDir = bytes[8];

            result.Direction = ((speedDir & 0x80) != 0) ? "Backward" : "Forward";
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
        /// </summary>
        /// <param name="address">DCC address (0-10239).</param>
        /// <param name="speed">Speed (0-127, 128-step mode).</param>
        /// <param name="direction">Direction ("Forward" or "Backward").</param>
        /// <returns>Z21 command bytes ready to send to Z21.</returns>
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
