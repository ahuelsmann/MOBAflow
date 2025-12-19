// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Domain.Message;

using CommunityToolkit.Mvvm.Messaging.Messages;

/// <summary>
/// Message: A track feedback point was triggered.
/// 
/// Published by: Z21.cs when UDP feedback is received and parsed
/// Subscribed by: JourneyManager, WorkflowManager, StationManager, ViewModels, Logging, etc.
/// 
/// This message represents the lowest-level feedback from the track.
/// It's the primary event that drives all feedback processing in MOBAflow.
/// 
/// Architecture:
/// - Inherits from ValueChangedMessage
/// where Value = InPort
/// - Includes RawData (byte[]) for protocol analysis if needed
/// - ReceivedAt timestamp for timing analysis
/// 
/// Thread Safety:
/// - Message is published on background thread (UDP receiver thread)
/// - All subscribers must handle thread-safe operations or dispatch to UI thread
/// </summary>
public sealed class FeedbackReceivedMessage : ValueChangedMessage<uint>
{
    /// <summary>
    /// Raw UDP packet data for protocol-level analysis.
    /// </summary>
    public byte[] RawData { get; }

    /// <summary>
    /// Timestamp when feedback was received (UTC).
    /// </summary>
    public DateTime ReceivedAt { get; }

    /// <summary>
    /// Creates a new feedback received message with current UTC timestamp.
    /// </summary>
    /// <param name="inPort">Track feedback point number (1-128)</param>
    /// <param name="rawData">Raw UDP packet bytes from Z21</param>
    public FeedbackReceivedMessage(uint inPort, byte[] rawData) : this(inPort, rawData, DateTime.UtcNow) { }

    /// <summary>
    /// Creates a new feedback received message with explicit timestamp.
    /// </summary>
    /// <param name="inPort">Track feedback point number (1-128)</param>
    /// <param name="rawData">Raw UDP packet bytes from Z21</param>
    /// <param name="receivedAt">When feedback was received (UTC)</param>
    public FeedbackReceivedMessage(uint inPort, byte[] rawData, DateTime receivedAt) : base(inPort)
    {
        RawData = rawData;
        ReceivedAt = receivedAt;
    }
}