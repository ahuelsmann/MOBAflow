// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Transport;

using Moba.Display.Protocol;

/// <summary>
/// Configures bounded waits and retries for one display protocol request.
/// </summary>
public sealed record DisplayRequestOptions
{
    /// <summary>
    /// Gets the default request policy.
    /// </summary>
    public static DisplayRequestOptions Default { get; } = new(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));

    /// <summary>
    /// Initializes a validated request policy.
    /// </summary>
    /// <param name="maximumAttempts">Maximum number of sends including the first attempt.</param>
    /// <param name="responseTimeout">Maximum wait for one attempt.</param>
    /// <param name="maximumRetryDelay">Maximum accepted device-requested retry delay.</param>
    public DisplayRequestOptions(
        int maximumAttempts,
        TimeSpan responseTimeout,
        TimeSpan maximumRetryDelay)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumAttempts, 1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(responseTimeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumRetryDelay, TimeSpan.Zero);

        MaximumAttempts = maximumAttempts;
        ResponseTimeout = responseTimeout;
        MaximumRetryDelay = maximumRetryDelay;
    }

    /// <summary>
    /// Gets the maximum number of sends including the first attempt.
    /// </summary>
    public int MaximumAttempts { get; }

    /// <summary>
    /// Gets the maximum wait for one attempt.
    /// </summary>
    public TimeSpan ResponseTimeout { get; }

    /// <summary>
    /// Gets the maximum accepted device-requested retry delay.
    /// </summary>
    public TimeSpan MaximumRetryDelay { get; }
}

/// <summary>
/// Identifies one datagram within a logical packet sequence.
/// </summary>
public readonly record struct DisplayPacketSequence
{
    /// <summary>
    /// Initializes a validated packet sequence position.
    /// </summary>
    /// <param name="packetIndex">Zero-based packet position.</param>
    /// <param name="packetCount">Total number of packets in the logical sequence.</param>
    /// <param name="isFinalPacket">Whether this is the last packet in the sequence.</param>
    public DisplayPacketSequence(ushort packetIndex, ushort packetCount, bool isFinalPacket)
    {
        if (packetCount == 0 || packetIndex >= packetCount)
        {
            throw new ArgumentOutOfRangeException(nameof(packetCount));
        }

        if (isFinalPacket && packetIndex != packetCount - 1)
        {
            throw new ArgumentException("Only the last packet may carry the final-packet flag.", nameof(isFinalPacket));
        }

        PacketIndex = packetIndex;
        PacketCount = packetCount;
        IsFinalPacket = isFinalPacket;
    }

    /// <summary>Gets the zero-based packet position.</summary>
    public ushort PacketIndex { get; }

    /// <summary>Gets the total number of packets in the logical sequence.</summary>
    public ushort PacketCount { get; }

    /// <summary>Gets whether this is the last packet in the logical sequence.</summary>
    public bool IsFinalPacket { get; }
}

/// <summary>
/// Identifies a host-side failure before a valid device response was obtained.
/// </summary>
public enum DisplayRequestFailure
{
    None,
    Cancelled,
    TimedOut,
    TransportFailure,
    InvalidDatagram,
    MissingResponseFlag,
    UnexpectedMessageType,
    WrongFrameId,
    WrongSessionId,
    InvalidPayload,
    ClientDisposed
}

/// <summary>
/// Reports the structured result of one bounded display request.
/// </summary>
/// <param name="RequestId">Identifier used for every attempt.</param>
/// <param name="AttemptCount">Number of datagrams sent.</param>
/// <param name="Response">Validated device payload, when available.</param>
/// <param name="Failure">Host-side failure classification.</param>
/// <param name="Diagnostic">Actionable diagnostic text without packet payload data.</param>
public sealed record DisplayRequestOutcome(
    uint RequestId,
    int AttemptCount,
    IDisplayProtocolPayload? Response,
    DisplayRequestFailure Failure,
    string? Diagnostic)
{
    /// <summary>
    /// Gets whether a validated device response was obtained.
    /// </summary>
    public bool IsSuccessful => Failure == DisplayRequestFailure.None;
}

/// <summary>
/// Preserves a structured device or host failure across the frame-session boundary.
/// </summary>
/// <param name="message">Safe failure explanation.</param>
/// <param name="requestFailure">Host request failure.</param>
/// <param name="resultCode">Structured device result, if available.</param>
/// <param name="innerException">Underlying exception, if available.</param>
public sealed class DisplayProtocolOperationException(
    string message,
    DisplayRequestFailure requestFailure,
    DisplayResultCode? resultCode = null,
    Exception? innerException = null) : InvalidOperationException(message, innerException)
{
    /// <summary>Initializes an empty protocol operation exception.</summary>
    public DisplayProtocolOperationException()
        : this("A display protocol operation failed.", DisplayRequestFailure.None, null, null)
    {
    }

    /// <summary>Initializes a protocol operation exception with a safe message.</summary>
    /// <param name="message">Safe failure explanation.</param>
    public DisplayProtocolOperationException(string message)
        : this(message, DisplayRequestFailure.None, null, null)
    {
    }

    /// <summary>Initializes a protocol operation exception with an inner exception.</summary>
    /// <param name="message">Safe failure explanation.</param>
    /// <param name="innerException">Underlying exception.</param>
    public DisplayProtocolOperationException(string message, Exception innerException)
        : this(message, DisplayRequestFailure.None, null, innerException)
    {
    }

    /// <summary>Gets the host-side request failure.</summary>
    public DisplayRequestFailure RequestFailure { get; } = requestFailure;

    /// <summary>Gets the structured device result, when one was returned.</summary>
    public DisplayResultCode? ResultCode { get; } = resultCode;
}

/// <summary>
/// Identifies an unexpected response observed at the transport boundary.
/// </summary>
public enum DisplayTransportAnomaly
{
    InvalidDatagram,
    UnmatchedResponse,
    MissingResponseFlag,
    UnexpectedMessageType,
    WrongFrameId,
    WrongSessionId,
    InvalidPayload,
    DuplicateResponse
}

/// <summary>
/// Reports safe correlation diagnostics without exposing datagram payloads.
/// </summary>
/// <param name="anomaly">Observed anomaly.</param>
/// <param name="requestId">Related request identifier, or zero when unavailable.</param>
/// <param name="diagnostic">Safe diagnostic text.</param>
public sealed class DisplayTransportAnomalyEventArgs(
    DisplayTransportAnomaly anomaly,
    uint requestId,
    string diagnostic) : EventArgs
{
    /// <summary>
    /// Gets the observed anomaly.
    /// </summary>
    public DisplayTransportAnomaly Anomaly { get; } = anomaly;

    /// <summary>
    /// Gets the related request identifier, or zero when unavailable.
    /// </summary>
    public uint RequestId { get; } = requestId;

    /// <summary>
    /// Gets safe diagnostic text.
    /// </summary>
    public string Diagnostic { get; } = diagnostic;
}
