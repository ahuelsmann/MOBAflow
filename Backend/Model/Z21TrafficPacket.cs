// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Model;

/// <summary>
/// Represents a single Z21/UDP packet for traffic monitoring.
/// </summary>
public class Z21TrafficPacket
{
    /// <summary>
    /// Timestamp when the packet was sent/received.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Direction: true = Sent (→), false = Received (←).
    /// </summary>
    public bool IsSent { get; set; }

    /// <summary>
    /// Raw packet data (hex bytes).
    /// </summary>
    public byte[] Data { get; set; } = [];

    /// <summary>
    /// Human-readable packet type (e.g., "LAN_X_SET_TRACK_POWER_ON", "LAN_X_TURNOUT", "LAN_RAILCOM_DATACHANGED").
    /// </summary>
    public string PacketType { get; set; } = "Unknown";

    /// <summary>
    /// Additional details (e.g., "InPort=5", "Address=3, Speed=80").
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Formats the packet data as hex string for display.
    /// </summary>
    public string DataHex => BitConverter.ToString(Data).Replace("-", " ");

    /// <summary>
    /// Direction icon for UI display.
    /// </summary>
    public string DirectionIcon => IsSent ? "→" : "←";

    /// <summary>
    /// Formatted timestamp for UI display.
    /// </summary>
    public string TimestampFormatted => Timestamp.ToString("HH:mm:ss.fff");
}