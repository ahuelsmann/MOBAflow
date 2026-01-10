// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Protocol;

/// <summary>
/// Utility class for parsing Z21 LAN_RMBUS_DATACHANGED feedback packets.
/// 
/// Z21 Protocol Structure:
/// Byte 0-1: DataLen (0x0F 00 = 15 bytes)
/// Byte 2-3: Header (0x80 00 = LAN_RMBUS_DATACHANGED)
/// Byte 4:   Group Number (0-based module ID: 0=Module 1, 1=Module 2, etc.)
/// Byte 5-12: Data Bytes (8 bytes = 64 bits representing InPorts 1-64 of this module)
/// Byte 13:  XOR Checksum
/// 
/// Each module handles 64 InPorts (8 bytes × 8 bits).
/// InPort calculation: (GroupNumber × 64) + (ByteIndex × 8) + BitPosition + 1
/// 
/// Example:
/// 0F 00 80 00 00 02 00 00 00 00 00 00 00 00 00
/// └──┘ └──┘ └┘ └─────────────────────┘ └┘
/// Len  Hdr  Grp  Data (8 bytes)       XOR
///           0    Bit 1 set = InPort 2
/// </summary>
public static class Z21FeedbackParser
{
    /// <summary>
    /// Extracts the first active InPort from a LAN_RMBUS_DATACHANGED packet.
    /// Returns the first set bit as InPort number (1-based).
    /// </summary>
    /// <param name="data">Raw Z21 UDP packet data</param>
    /// <returns>First active InPort number, or 0 if no feedback active</returns>
    public static int ExtractFirstInPort(byte[] data)
    {
        if (data.Length < 6) return 0;
        
        int groupNumber = data[4];  // Module ID (0-based)
        
        // Scan 8 data bytes (up to 64 feedback points per module)
        for (int byteIndex = 0; byteIndex < 8; byteIndex++)
        {
            if (5 + byteIndex >= data.Length) break;
            
            byte feedbackByte = data[5 + byteIndex];
            if (feedbackByte == 0) continue;  // No bits set in this byte
            
            // Find first set bit (LSB to MSB)
            for (int bit = 0; bit < 8; bit++)
            {
                if ((feedbackByte & (1 << bit)) != 0)
                {
                    // InPort = (Module × 64) + (Byte × 8) + Bit + 1 (1-based!)
                    return (groupNumber * 64) + (byteIndex * 8) + bit + 1;
                }
            }
        }
        
        return 0;  // No feedback active
    }

    /// <summary>
    /// Extracts ALL active InPorts from a LAN_RMBUS_DATACHANGED packet.
    /// Returns all set bits as InPort numbers (1-based).
    /// Useful for displaying all active feedback points in Traffic Monitor.
    /// </summary>
    /// <param name="data">Raw Z21 UDP packet data</param>
    /// <returns>List of all active InPort numbers (1-based), or empty list if none active</returns>
    public static List<int> ExtractAllInPorts(byte[] data)
    {
        var inPorts = new List<int>();
        if (data.Length < 6) return inPorts;
        
        int groupNumber = data[4];  // Module ID (0-based)
        
        // Scan 8 data bytes (up to 64 feedback points per module)
        for (int byteIndex = 0; byteIndex < 8; byteIndex++)
        {
            if (5 + byteIndex >= data.Length) break;
            
            byte feedbackByte = data[5 + byteIndex];
            if (feedbackByte == 0) continue;  // No bits set in this byte
            
            // Check all 8 bits
            for (int bit = 0; bit < 8; bit++)
            {
                if ((feedbackByte & (1 << bit)) != 0)
                {
                    // InPort = (Module × 64) + (Byte × 8) + Bit + 1 (1-based!)
                    int inPort = (groupNumber * 64) + (byteIndex * 8) + bit + 1;
                    inPorts.Add(inPort);
                }
            }
        }
        
        return inPorts;
    }

    /// <summary>
    /// Gets the module/group number from a LAN_RMBUS_DATACHANGED packet.
    /// </summary>
    /// <param name="data">Raw Z21 UDP packet data</param>
    /// <returns>Group number (0-based), or -1 if invalid packet</returns>
    public static int GetGroupNumber(byte[] data)
    {
        return data.Length >= 5 ? data[4] : -1;
    }

    /// <summary>
    /// Gets the raw feedback state bytes (8 bytes representing 64 bits).
    /// </summary>
    /// <param name="data">Raw Z21 UDP packet data</param>
    /// <returns>8-byte array of feedback state, or empty array if invalid</returns>
    public static byte[] GetFeedbackState(byte[] data)
    {
        if (data.Length < 13) return [];
        
        var state = new byte[8];
        Array.Copy(data, 5, state, 0, 8);
        return state;
    }
}
