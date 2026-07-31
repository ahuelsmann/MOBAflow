// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Transport;

using Moba.Display.Protocol;

using System.Collections.Concurrent;

/// <summary>
/// Sends versioned display requests and correlates validated responses across bounded retries.
/// </summary>
public sealed class DisplayProtocolClient : IDisposable
{
    private readonly IDisplayDatagramTransport _transport;
    private readonly DisplayIdentifierSequence _requestIds;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<uint, PendingResponse> _pendingResponses = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a protocol client over an already configured datagram transport.
    /// </summary>
    /// <param name="transport">Connected datagram transport.</param>
    /// <param name="requestIds">Request identifier sequence, or a new sequence when omitted.</param>
    /// <param name="timeProvider">Time source used for waits and retry delays.</param>
    public DisplayProtocolClient(
        IDisplayDatagramTransport transport,
        DisplayIdentifierSequence? requestIds = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(transport);
        _transport = transport;
        _requestIds = requestIds ?? new DisplayIdentifierSequence();
        _timeProvider = timeProvider ?? TimeProvider.System;
        _transport.DatagramReceived += OnDatagramReceived;
    }

    /// <summary>
    /// Raised when a received datagram cannot be matched to the expected response contract.
    /// </summary>
    public event EventHandler<DisplayTransportAnomalyEventArgs>? TransportAnomaly;

    /// <summary>
    /// Sends a request with one stable request identifier and bounded retry behavior.
    /// </summary>
    /// <param name="request">Typed request payload.</param>
    /// <param name="sessionId">Negotiated session identifier, or zero for hello.</param>
    /// <param name="frameId">Frame identifier for frame-scoped messages, otherwise zero.</param>
    /// <param name="options">Bounded wait and retry policy.</param>
    /// <param name="packetSequence">Optional logical packet position for a multi-region frame.</param>
    /// <param name="cancellationToken">Stops sending, waiting, and further retries.</param>
    public async Task<DisplayRequestOutcome> SendRequestAsync(
        IDisplayProtocolPayload request,
        uint sessionId = 0,
        uint frameId = 0,
        DisplayRequestOptions? options = null,
        DisplayPacketSequence? packetSequence = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequestEnvelope(request.MessageType, sessionId, frameId);
        var requestOptions = options ?? DisplayRequestOptions.Default;
        var encodedPayload = DisplayPayloadCodec.Encode(request);
        var expectedResponse = GetExpectedResponse(request.MessageType);
        var requestId = _requestIds.Next();

        if (_disposed)
        {
            return Failure(requestId, 0, DisplayRequestFailure.ClientDisposed, "The display protocol client is disposed.");
        }

        for (var attempt = 1; attempt <= requestOptions.MaximumAttempts; attempt++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Failure(requestId, attempt - 1, DisplayRequestFailure.Cancelled, "The display request was cancelled.");
            }

            if (_disposed)
            {
                return Failure(requestId, attempt - 1, DisplayRequestFailure.ClientDisposed, "The display protocol client is disposed.");
            }

            var pending = new PendingResponse(expectedResponse, frameId, sessionId);
            if (!_pendingResponses.TryAdd(requestId, pending))
            {
                return Failure(requestId, attempt - 1, DisplayRequestFailure.TransportFailure, "The request identifier is already active.");
            }

            try
            {
                var datagram = CreateDatagram(
                    request.MessageType,
                    encodedPayload,
                    requestId,
                    frameId,
                    sessionId,
                    attempt > 1,
                    packetSequence);
                await _transport.SendAsync(datagram, cancellationToken).ConfigureAwait(false);
                var resolution = await pending.Completion.Task
                    .WaitAsync(requestOptions.ResponseTimeout, _timeProvider, cancellationToken)
                    .ConfigureAwait(false);

                if (resolution.Failure != DisplayRequestFailure.None)
                {
                    return Failure(requestId, attempt, resolution.Failure, resolution.Diagnostic);
                }

                if (resolution.Payload is ResultPayload result
                    && ShouldRetry(result)
                    && attempt < requestOptions.MaximumAttempts)
                {
                    var retryDelay = GetRetryDelay(result, requestOptions.MaximumRetryDelay);
                    if (retryDelay > TimeSpan.Zero)
                    {
                        await Task.Delay(retryDelay, _timeProvider, cancellationToken).ConfigureAwait(false);
                    }

                    continue;
                }

                return new DisplayRequestOutcome(requestId, attempt, resolution.Payload, DisplayRequestFailure.None, null);
            }
            catch (TimeoutException)
            {
                if (attempt == requestOptions.MaximumAttempts)
                {
                    return Failure(
                        requestId,
                        attempt,
                        DisplayRequestFailure.TimedOut,
                        $"No matching response arrived after {attempt} attempt(s).");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return Failure(requestId, attempt, DisplayRequestFailure.Cancelled, "The display request was cancelled.");
            }
            catch (Exception ex)
            {
                if (attempt == requestOptions.MaximumAttempts)
                {
                    return Failure(
                        requestId,
                        attempt,
                        DisplayRequestFailure.TransportFailure,
                        $"Datagram transport failed with {ex.GetType().Name}: {ex.Message}");
                }
            }
            finally
            {
                _pendingResponses.TryRemove(requestId, out _);
            }
        }

        return Failure(requestId, requestOptions.MaximumAttempts, DisplayRequestFailure.TimedOut, "The request policy was exhausted.");
    }

    /// <summary>
    /// Stops response correlation and completes active requests as disposed.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _transport.DatagramReceived -= OnDatagramReceived;
        foreach (var pending in _pendingResponses.Values)
        {
            pending.Completion.TrySetResult(
                PendingResolution.Failed(DisplayRequestFailure.ClientDisposed, "The display protocol client is disposed."));
        }

        _pendingResponses.Clear();
    }

    private static byte[] CreateDatagram(
        DisplayMessageType messageType,
        ReadOnlyMemory<byte> payload,
        uint requestId,
        uint frameId,
        uint sessionId,
        bool isRetry,
        DisplayPacketSequence? packetSequence)
    {
        var flags = DisplayProtocolFlags.AcknowledgementRequired;
        if (isRetry)
        {
            flags |= DisplayProtocolFlags.Retry;
        }

        var sequence = packetSequence ?? new DisplayPacketSequence(0, 1, false);
        if (sequence.IsFinalPacket)
        {
            flags |= DisplayProtocolFlags.FinalPacket;
        }

        var header = new DisplayPacketHeader(
            DisplayProtocol.CurrentVersion,
            messageType,
            flags,
            requestId,
            frameId,
            sessionId,
            sequence.PacketIndex,
            sequence.PacketCount);
        return DisplayPacketCodec.Encode(new DisplayProtocolPacket(header, payload));
    }

    private static bool ShouldRetry(ResultPayload result) =>
        result.ResultCode == DisplayResultCode.Busy
        || result.Flags.HasFlag(DisplayResultFlags.Retryable);

    private static TimeSpan GetRetryDelay(ResultPayload result, TimeSpan maximumRetryDelay)
    {
        var requested = TimeSpan.FromMilliseconds(result.RetryAfterMilliseconds);
        return requested <= maximumRetryDelay ? requested : maximumRetryDelay;
    }

    private static DisplayMessageType GetExpectedResponse(DisplayMessageType requestType) =>
        requestType switch
        {
            DisplayMessageType.HelloRequest => DisplayMessageType.CapabilitiesResponse,
            DisplayMessageType.HealthRequest => DisplayMessageType.HealthResponse,
            DisplayMessageType.BeginFrame
                or DisplayMessageType.FrameRegion
                or DisplayMessageType.CompleteFrame
                or DisplayMessageType.AbortFrame
                or DisplayMessageType.Clear
                or DisplayMessageType.SetBrightness
                or DisplayMessageType.RenderTestPattern => DisplayMessageType.Result,
            _ => throw new ArgumentException("The payload is a response and cannot be sent as a request.", nameof(requestType))
        };

    private static void ValidateRequestEnvelope(DisplayMessageType messageType, uint sessionId, uint frameId)
    {
        var valid = messageType switch
        {
            DisplayMessageType.HelloRequest => sessionId == 0 && frameId == 0,
            DisplayMessageType.HealthRequest => sessionId != 0 && frameId == 0,
            DisplayMessageType.BeginFrame
                or DisplayMessageType.FrameRegion
                or DisplayMessageType.CompleteFrame
                or DisplayMessageType.AbortFrame => sessionId != 0 && frameId != 0,
            DisplayMessageType.Clear
                or DisplayMessageType.SetBrightness
                or DisplayMessageType.RenderTestPattern => sessionId != 0 && frameId == 0,
            _ => false
        };

        if (!valid)
        {
            throw new ArgumentException("Session or frame identifier does not match the request message type.", nameof(messageType));
        }
    }

    private void OnDatagramReceived(object? sender, DisplayDatagramReceivedEventArgs eventArgs)
    {
        if (!DisplayPacketCodec.TryDecode(eventArgs.Datagram.Span, out var packet, out var packetError) || packet is null)
        {
            RaiseAnomaly(DisplayTransportAnomaly.InvalidDatagram, 0, $"Envelope decode failed: {packetError}.");
            return;
        }

        var requestId = packet.Header.RequestId;
        if (!_pendingResponses.TryGetValue(requestId, out var pending))
        {
            RaiseAnomaly(DisplayTransportAnomaly.UnmatchedResponse, requestId, "No active request matches the response.");
            return;
        }

        if (!packet.Header.Flags.HasFlag(DisplayProtocolFlags.Response))
        {
            CompleteFailure(
                pending,
                DisplayTransportAnomaly.MissingResponseFlag,
                requestId,
                DisplayRequestFailure.MissingResponseFlag,
                "The matching datagram is not marked as a response.");
            return;
        }

        if (packet.Header.MessageType != pending.ExpectedMessageType
            && packet.Header.MessageType != DisplayMessageType.Result)
        {
            CompleteFailure(
                pending,
                DisplayTransportAnomaly.UnexpectedMessageType,
                requestId,
                DisplayRequestFailure.UnexpectedMessageType,
                $"Expected {pending.ExpectedMessageType}, received {packet.Header.MessageType}.");
            return;
        }

        if (packet.Header.FrameId != pending.FrameId)
        {
            CompleteFailure(
                pending,
                DisplayTransportAnomaly.WrongFrameId,
                requestId,
                DisplayRequestFailure.WrongFrameId,
                "The response frame identifier does not match the request.");
            return;
        }

        if (packet.Header.SessionId != pending.SessionId)
        {
            CompleteFailure(
                pending,
                DisplayTransportAnomaly.WrongSessionId,
                requestId,
                DisplayRequestFailure.WrongSessionId,
                "The response session identifier does not match the request.");
            return;
        }

        if (!DisplayPayloadCodec.TryDecode(
                packet.Header.MessageType,
                packet.Payload.Span,
                out var payload,
                out var payloadError)
            || payload is null)
        {
            CompleteFailure(
                pending,
                DisplayTransportAnomaly.InvalidPayload,
                requestId,
                DisplayRequestFailure.InvalidPayload,
                $"Payload decode failed: {payloadError}.");
            return;
        }

        if (!pending.Completion.TrySetResult(PendingResolution.Succeeded(payload)))
        {
            RaiseAnomaly(DisplayTransportAnomaly.DuplicateResponse, requestId, "The response arrived more than once.");
        }
    }

    private void CompleteFailure(
        PendingResponse pending,
        DisplayTransportAnomaly anomaly,
        uint requestId,
        DisplayRequestFailure failure,
        string diagnostic)
    {
        RaiseAnomaly(anomaly, requestId, diagnostic);
        if (!pending.Completion.TrySetResult(PendingResolution.Failed(failure, diagnostic)))
        {
            RaiseAnomaly(DisplayTransportAnomaly.DuplicateResponse, requestId, "The invalid response arrived more than once.");
        }
    }

    private void RaiseAnomaly(DisplayTransportAnomaly anomaly, uint requestId, string diagnostic)
    {
        var handlers = TransportAnomaly?.GetInvocationList();
        if (handlers is null)
        {
            return;
        }

        var eventArgs = new DisplayTransportAnomalyEventArgs(anomaly, requestId, diagnostic);
        foreach (var handler in handlers.Cast<EventHandler<DisplayTransportAnomalyEventArgs>>())
        {
            try
            {
                handler(this, eventArgs);
            }
            catch
            {
                // Diagnostic listeners must not break protocol response processing.
            }
        }
    }

    private static DisplayRequestOutcome Failure(
        uint requestId,
        int attemptCount,
        DisplayRequestFailure failure,
        string? diagnostic) =>
        new(requestId, attemptCount, null, failure, diagnostic);

    private sealed record PendingResponse(
        DisplayMessageType ExpectedMessageType,
        uint FrameId,
        uint SessionId)
    {
        public TaskCompletionSource<PendingResolution> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed record PendingResolution(
        IDisplayProtocolPayload? Payload,
        DisplayRequestFailure Failure,
        string? Diagnostic)
    {
        public static PendingResolution Succeeded(IDisplayProtocolPayload payload) =>
            new(payload, DisplayRequestFailure.None, null);

        public static PendingResolution Failed(DisplayRequestFailure failure, string diagnostic) =>
            new(null, failure, diagnostic);
    }
}