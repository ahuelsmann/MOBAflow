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
        if (responseTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(responseTimeout));
        }

        if (maximumRetryDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumRetryDelay));
        }

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
public sealed class DisplayTransportAnomalyEventArgs : EventArgs
{
    /// <summary>
    /// Initializes safe correlation diagnostics.
    /// </summary>
    /// <param name="anomaly">Observed anomaly.</param>
    /// <param name="requestId">Related request identifier, or zero when unavailable.</param>
    /// <param name="diagnostic">Safe diagnostic text.</param>
    public DisplayTransportAnomalyEventArgs(
        DisplayTransportAnomaly anomaly,
        uint requestId,
        string diagnostic)
    {
        Anomaly = anomaly;
        RequestId = requestId;
        Diagnostic = diagnostic;
    }

    /// <summary>
    /// Gets the observed anomaly.
    /// </summary>
    public DisplayTransportAnomaly Anomaly { get; }

    /// <summary>
    /// Gets the related request identifier, or zero when unavailable.
    /// </summary>
    public uint RequestId { get; }

    /// <summary>
    /// Gets safe diagnostic text.
    /// </summary>
    public string Diagnostic { get; }
}