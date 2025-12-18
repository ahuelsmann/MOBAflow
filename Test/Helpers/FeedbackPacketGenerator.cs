// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Helpers;

/// <summary>
/// Helper to generate valid Z21 RBus feedback packets for unit testing.
/// 
/// Z21 RBus Feedback Packet Format:
/// [Length(2 bytes)] [CRC(2 bytes)] [0xF0] [0xA1] [InPort(1 byte)] [Value(1 byte)]
/// Total: 8 bytes
/// 
/// Example: InPort 5 with bit 0 active
/// Bytes: [0x04, 0x00, 0xF0, 0xA1, 0x05, 0x03]
/// 
/// Usage:
/// var feedback = FeedbackPacketGenerator.CreateFeedbackPacket(5);
/// _fakeUdp.RaiseReceived(feedback);
/// </summary>
public static class FeedbackPacketGenerator
{
    /// <summary>
    /// Creates a valid Z21 RBus feedback packet.
    /// </summary>
    /// <param name="inPort">Feedback point number (1-128)</param>
    /// <param name="value">Bit value (0x00 = all off, 0x03 = bit 0 on, 0xFF = all on)</param>
    /// <returns>Valid Z21 RBus feedback packet (6 bytes)</returns>
    public static byte[] CreateFeedbackPacket(byte inPort, byte value = 0x03)
    {
        // Format: [Length] [CRC] [0xF0] [0xA1] [InPort] [Value]
        return new byte[] { 0x04, 0x00, 0xF0, 0xA1, inPort, value };
    }

    /// <summary>
    /// Creates a feedback packet with typical bit 0 active (0x03).
    /// This is the most common feedback pattern.
    /// </summary>
    /// <param name="inPort">Feedback point number (1-128)</param>
    /// <returns>Valid Z21 RBus feedback packet with bit 0 active</returns>
    public static byte[] CreateFeedbackPacket(byte inPort)
    {
        return CreateFeedbackPacket(inPort, 0x03);
    }

    /// <summary>
    /// Creates multiple sequential feedback packets.
    /// Useful for testing multiple feedback points in sequence.
    /// </summary>
    /// <param name="startPort">Starting InPort number</param>
    /// <param name="count">Number of packets to generate</param>
    /// <returns>Enumerable of feedback packets</returns>
    /// <example>
    /// // Generate packets for InPort 1-5
    /// foreach (var packet in FeedbackPacketGenerator.CreateSequence(1, 5))
    /// {
    ///     _fakeUdp.RaiseReceived(packet);
    ///     System.Threading.Thread.Sleep(50);
    /// }
    /// </example>
    public static IEnumerable<byte[]> CreateSequence(int startPort, int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return CreateFeedbackPacket((byte)(startPort + i));
        }
    }

    /// <summary>
    /// Creates a feedback packet with all bits active (0xFF).
    /// Useful for testing maximum value handling.
    /// </summary>
    /// <param name="inPort">Feedback point number (1-128)</param>
    /// <returns>Feedback packet with all bits active</returns>
    public static byte[] CreateFeedbackPacketAllBitsActive(byte inPort)
    {
        return CreateFeedbackPacket(inPort, 0xFF);
    }

    /// <summary>
    /// Creates a feedback packet with all bits inactive (0x00).
    /// Useful for testing "no feedback" scenarios.
    /// </summary>
    /// <param name="inPort">Feedback point number (1-128)</param>
    /// <returns>Feedback packet with no bits active</returns>
    public static byte[] CreateFeedbackPacketAllBitsInactive(byte inPort)
    {
        return CreateFeedbackPacket(inPort, 0x00);
    }

    /// <summary>
    /// Common test feedback packets (predefined for convenience).
    /// </summary>
    public static class Common
    {
        /// <summary>InPort 1, bit 0 active</summary>
        public static byte[] InPort1 => CreateFeedbackPacket(1);

        /// <summary>InPort 5, bit 0 active</summary>
        public static byte[] InPort5 => CreateFeedbackPacket(5);

        /// <summary>InPort 10, bit 0 active</summary>
        public static byte[] InPort10 => CreateFeedbackPacket(10);

        /// <summary>InPort 128 (max), bit 0 active</summary>
        public static byte[] InPort128 => CreateFeedbackPacket(128);

        /// <summary>InPort 5, all bits active</summary>
        public static byte[] InPort5AllBits => CreateFeedbackPacketAllBitsActive(5);
    }
}
