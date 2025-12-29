// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Microsoft.Extensions.Logging;
using Model;

using System.Collections.Concurrent;

/// <summary>
/// Monitors and logs Z21/UDP traffic for debugging and visualization.
/// Stores the last N packets in a circular buffer and logs to Serilog for persistence.
/// </summary>
public class Z21Monitor
{
    private readonly ConcurrentQueue<Z21TrafficPacket> _packets = new();
    private readonly ILogger<Z21Monitor> _logger;
    private const int MaxPackets = 1000;

    public Z21Monitor(ILogger<Z21Monitor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Event fired when a new packet is logged.
    /// </summary>
    public event EventHandler<Z21TrafficPacket>? PacketLogged;

    /// <summary>
    /// Logs a sent packet.
    /// </summary>
    public void LogSentPacket(byte[] data, string packetType = "Unknown", string details = "")
    {
        var packet = new Z21TrafficPacket
        {
            IsSent = true,
            Data = data,
            PacketType = packetType,
            Details = details,
            IsFeedbackRelated = false  // Sent packets are never feedback-related
        };

        // ✅ Log to Serilog for persistence (Debug level for traffic)
        _logger.LogDebug("Z21 TX: {PacketType} Bytes={Length} Data={DataHex}", 
            packet.PacketType, data.Length, packet.DataHex);

        AddPacket(packet);
    }

    /// <summary>
    /// Logs a received packet.
    /// </summary>
    public void LogReceivedPacket(byte[] data, string packetType = "Unknown", string details = "")
    {
        var packet = new Z21TrafficPacket
        {
            IsSent = false,
            Data = data,
            PacketType = packetType,
            Details = details
        };

        // Check if this is a feedback-related packet and extract InPort
        packet.IsFeedbackRelated = IsFeedbackPacket(data);
        if (packet.IsFeedbackRelated)
        {
            packet.InPort = ExtractInPort(data);
            packet.AllInPorts = ExtractAllInPorts(data);
            
            // If no InPort could be extracted (e.g., all bits zero), treat as non-feedback
            // This prevents highlighting packets with no actual feedback data
            if (packet.InPort == null || packet.InPort == 0)
            {
                packet.IsFeedbackRelated = false;
            }
        }

        // ✅ Log to Serilog for persistence (Debug level for traffic, Info for feedback)
        if (packet.IsFeedbackRelated && packet.InPort.HasValue)
        {
            _logger.LogInformation("Z21 RX: {PacketType} InPort={InPort} AllInPorts={AllInPorts} Data={DataHex}", 
                packet.PacketType, packet.InPort, string.Join(",", packet.AllInPorts), packet.DataHex);
        }
        else
        {
            _logger.LogDebug("Z21 RX: {PacketType} Bytes={Length} Data={DataHex}", 
                packet.PacketType, data.Length, packet.DataHex);
        }

        AddPacket(packet);
    }

    private void AddPacket(Z21TrafficPacket packet)
    {
        _packets.Enqueue(packet);

        // Remove oldest packet if exceeding max size
        while (_packets.Count > MaxPackets)
        {
            _packets.TryDequeue(out _);
        }

        PacketLogged?.Invoke(this, packet);
    }

    /// <summary>
    /// Gets all logged packets (newest first).
    /// </summary>
    public IEnumerable<Z21TrafficPacket> GetPackets()
    {
        return _packets.Reverse();
    }

    /// <summary>
    /// Clears all logged packets.
    /// </summary>
    public void Clear()
    {
        _packets.Clear();
    }

    /// <summary>
    /// Parses packet type from raw Z21 data (basic implementation).
    /// </summary>
    public static string ParsePacketType(byte[] data)
    {
        if (data.Length < 4) return "Unknown (too short)";

        // Z21 packet structure: [DataLen (2 bytes)] [Header (2 bytes)] [Data (n bytes)] [XOR (1 byte)]
        var header = BitConverter.ToUInt16(data, 2);

        return header switch
        {
            0x10 => "LAN_GET_CODE",
            0x1A => "LAN_GET_HWINFO",
            0x21 => ParseLanXCommand(data),       // X-Bus commands
            0x40 => "LAN_GET_SERIAL_NUMBER",
            0x50 => "LAN_SET_BROADCASTFLAGS",
            0x51 => "LAN_GET_BROADCASTFLAGS",
            0x60 => "LAN_GET_LOCOMODE",
            0x61 => "LAN_SET_LOCOMODE",
            0x70 => "LAN_GET_TURNOUTMODE",
            0x71 => "LAN_SET_TURNOUTMODE",
            0x80 => "LAN_RMBUS_DATACHANGED",      // R-Bus feedback data
            0x81 => "LAN_RMBUS_GETDATA",
            0x82 => "LAN_RMBUS_PROGRAMMODULE",
            0x84 => "LAN_SYSTEMSTATE_GETDATA",
            0x85 => "LAN_SYSTEMSTATE_DATACHANGED",
            0x88 => "LAN_RAILCOM_DATACHANGED",
            0x89 => "LAN_RAILCOM_GETDATA",
            0xA0 => "LAN_LOCONET_Z21_RX",
            0xA1 => "LAN_LOCONET_Z21_TX",
            0xA2 => "LAN_LOCONET_FROM_LAN",
            0xA3 => "LAN_LOCONET_DISPATCH_ADDR",
            0xA4 => "LAN_LOCONET_DETECTOR",
            0xC4 => "LAN_CAN_DETECTOR",
            _ => $"Unknown (0x{header:X4})"
        };
    }

    private static string ParseLanXCommand(byte[] data)
    {
        if (data.Length < 5) return "LAN_X (incomplete)";

        var xHeader = data[4];
        return xHeader switch
        {
            0x21 => "LAN_X_SET_TRACK_POWER",
            0x80 => "LAN_X_SET_STOP",
            0xE3 => "LAN_X_GET_LOCO_INFO",
            0xE4 => "LAN_X_SET_LOCO_DRIVE",
            0xE5 => "LAN_X_SET_LOCO_FUNCTION",
            0x53 => "LAN_X_SET_TURNOUT",
            0x43 => "LAN_X_GET_TURNOUT_INFO",
            0x61 => "LAN_X_STATUS_CHANGED",
            0x63 => "LAN_X_GET_VERSION",
            _ => $"LAN_X (0x{xHeader:X2})"
        };
    }

    /// <summary>
    /// Checks if a packet is feedback-related (LAN_RMBUS_DATACHANGED or similar).
    /// </summary>
    private static bool IsFeedbackPacket(byte[] data)
    {
        if (data.Length < 4) return false;

        var header = BitConverter.ToUInt16(data, 2);
        return header switch
        {
            0x80 => true,  // LAN_RMBUS_DATACHANGED
            0x88 => true,  // LAN_RAILCOM_DATACHANGED
            0xA4 => true,  // LAN_LOCONET_DETECTOR
            0xC4 => true,  // LAN_CAN_DETECTOR
            _ => false
        };
    }

    /// <summary>
    /// Extracts the InPort number from a feedback packet using bit-level analysis.
    /// Returns null if not a feedback packet or InPort cannot be determined.
    /// For LAN_RMBUS_DATACHANGED packets, uses Z21FeedbackParser to correctly extract the first active InPort.
    /// </summary>
    private static int? ExtractInPort(byte[] data)
    {
        if (data.Length < 6) return null;

        var header = BitConverter.ToUInt16(data, 2);
        
        // LAN_RMBUS_DATACHANGED: Use Z21FeedbackParser for correct bit-to-InPort conversion
        if (header == 0x80 && data.Length >= 13)
        {
            int inPort = Protocol.Z21FeedbackParser.ExtractFirstInPort(data);
            return inPort > 0 ? inPort : null;  // Return null if no bits set
        }

        // For other feedback types (RailCom, LocoNet, CAN), would need specific parsing logic
        return null;
    }

    /// <summary>
    /// Extracts ALL active InPort numbers from a feedback packet.
    /// Returns empty list if not a feedback packet or no bits are set.
    /// Useful for Traffic Monitor to display all active feedback points.
    /// </summary>
    private static List<int> ExtractAllInPorts(byte[] data)
    {
        if (data.Length < 13) return [];

        var header = BitConverter.ToUInt16(data, 2);
        
        // LAN_RMBUS_DATACHANGED: Use Z21FeedbackParser to extract all active InPorts
        if (header == 0x80)
        {
            return Protocol.Z21FeedbackParser.ExtractAllInPorts(data);
        }

        // For other feedback types, return empty list
        return [];
    }
}
