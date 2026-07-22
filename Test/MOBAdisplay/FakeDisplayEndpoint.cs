// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdisplay;

using Moba.Display.Protocol;
using Moba.Display.Transport;

using System.IO;

internal sealed class FakeDisplayEndpoint : IDisplayDatagramTransport
{
    private readonly object _syncRoot = new();
    private readonly Queue<FakeDisplayBehavior> _behaviors = new();
    private readonly Dictionary<uint, RequestFingerprint> _requestFingerprints = new();
    private readonly Dictionary<uint, CachedResponse> _responses = new();
    private readonly Dictionary<uint, ResultPayload> _completedFrames = new();
    private readonly List<byte[]> _heldResponses = [];
    private readonly TimeProvider _timeProvider;
    private ActiveFrame? _activeFrame;
    private uint _sessionId = 0x0A0B0C0D;
    private uint _acceptedFrameCount;
    private uint _rejectedFrameCount;

    public FakeDisplayEndpoint(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public event EventHandler<DisplayDatagramReceivedEventArgs>? DatagramReceived;

    public List<DisplayProtocolPacket> ReceivedPackets { get; } = [];

    public int ReceivedPacketCount
    {
        get
        {
            lock (_syncRoot)
            {
                return ReceivedPackets.Count;
            }
        }
    }

    public int PresentationCount { get; private set; }

    public ReadOnlyMemory<byte> PresentedFrame { get; private set; }

    public uint SessionId
    {
        get
        {
            lock (_syncRoot)
            {
                return _sessionId;
            }
        }
    }

    public void DropNextResponse() => Enqueue(new FakeDisplayBehavior(DropResponse: true));

    public void DuplicateNextResponse() => Enqueue(new FakeDisplayBehavior(DeliveryCount: 2));

    public void HoldNextResponse() => Enqueue(new FakeDisplayBehavior(HoldResponse: true));

    public void CorruptNextResponse() => Enqueue(new FakeDisplayBehavior(CorruptResponse: true));

    public void UseWrongFrameIdForNextResponse() => Enqueue(new FakeDisplayBehavior(WrongFrameId: true));

    public void UseWrongSessionIdForNextResponse() => Enqueue(new FakeDisplayBehavior(WrongSessionId: true));

    public void OmitResponseFlagForNextResponse() => Enqueue(new FakeDisplayBehavior(MissingResponseFlag: true));

    public void UseUnexpectedMessageTypeForNextResponse() => Enqueue(new FakeDisplayBehavior(UnexpectedMessageType: true));

    public void InvalidateNextResponsePayload() => Enqueue(new FakeDisplayBehavior(InvalidPayload: true));

    public void FailNextSend() => Enqueue(new FakeDisplayBehavior(TransportFailure: true));

    public void DelayNextResponse(TimeSpan delay) => Enqueue(new FakeDisplayBehavior(Delay: delay));

    public void RejectNextRequest(
        DisplayResultCode resultCode,
        DisplayResultFlags flags = DisplayResultFlags.None,
        uint retryAfterMilliseconds = 0) =>
        Enqueue(new FakeDisplayBehavior(
            ForcedResult: new ResultPayload(resultCode, flags, 0, retryAfterMilliseconds, 0, 0)));

    public void ReleaseHeldResponses()
    {
        byte[][] responses;
        lock (_syncRoot)
        {
            responses = _heldResponses.ToArray();
            _heldResponses.Clear();
        }

        foreach (var response in responses)
        {
            Deliver(response);
        }
    }

    public void Reboot()
    {
        lock (_syncRoot)
        {
            _sessionId = NextNonZero(_sessionId);
            _activeFrame = null;
            _requestFingerprints.Clear();
            _responses.Clear();
            _completedFrames.Clear();
        }
    }

    public ValueTask SendAsync(
        ReadOnlyMemory<byte> datagram,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!DisplayPacketCodec.TryDecode(datagram.Span, out var request, out var packetError) || request is null)
        {
            throw new InvalidDataException($"Host datagram is invalid: {packetError}.");
        }

        if (!DisplayPayloadCodec.TryDecode(
                request.Header.MessageType,
                request.Payload.Span,
                out var requestPayload,
                out var payloadError)
            || requestPayload is null)
        {
            throw new InvalidDataException($"Host payload is invalid: {payloadError}.");
        }

        FakeDisplayBehavior behavior;
        DisplayProtocolPacket response;
        lock (_syncRoot)
        {
            ReceivedPackets.Add(request);
            behavior = _behaviors.Count == 0 ? new FakeDisplayBehavior() : _behaviors.Dequeue();
            if (behavior.TransportFailure)
            {
                throw new IOException("Simulated datagram transport failure.");
            }

            response = behavior.ForcedResult is { } forcedResult
                ? CreateResponse(request, forcedResult)
                : ProcessIdempotently(request, requestPayload);
        }

        if (behavior.DropResponse)
        {
            return ValueTask.CompletedTask;
        }

        var encodedResponse = MutateResponse(response, behavior);
        if (behavior.HoldResponse)
        {
            lock (_syncRoot)
            {
                _heldResponses.Add(encodedResponse);
            }

            return ValueTask.CompletedTask;
        }

        if (behavior.Delay > TimeSpan.Zero)
        {
            _ = DeliverAfterDelayAsync(
                encodedResponse,
                behavior.DeliveryCount,
                behavior.Delay,
                cancellationToken);
            return ValueTask.CompletedTask;
        }

        for (var delivery = 0; delivery < behavior.DeliveryCount; delivery++)
        {
            Deliver(encodedResponse);
        }

        return ValueTask.CompletedTask;
    }

    private async Task DeliverAfterDelayAsync(
        byte[] datagram,
        int deliveryCount,
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, _timeProvider, cancellationToken).ConfigureAwait(false);
            for (var delivery = 0; delivery < deliveryCount; delivery++)
            {
                Deliver(datagram);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The simulated network delivery is discarded when its request is cancelled.
        }
    }

    private DisplayProtocolPacket ProcessIdempotently(
        DisplayProtocolPacket request,
        IDisplayProtocolPayload requestPayload)
    {
        var fingerprint = new RequestFingerprint(
            request.Header.MessageType,
            request.Header.FrameId,
            request.Header.SessionId,
            request.Payload.ToArray());
        if (_requestFingerprints.TryGetValue(request.Header.RequestId, out var priorFingerprint))
        {
            if (!priorFingerprint.Equals(fingerprint))
            {
                return CreateResponse(
                    request,
                    new ResultPayload(DisplayResultCode.Conflict, DisplayResultFlags.None, 0, 0, 0, 0));
            }

            if (_responses.TryGetValue(request.Header.RequestId, out var cached))
            {
                return MarkDuplicate(cached.Response);
            }
        }
        else
        {
            _requestFingerprints.Add(request.Header.RequestId, fingerprint);
        }

        var response = ProcessRequest(request, requestPayload);
        if (IsCacheable(response))
        {
            _responses[request.Header.RequestId] = new CachedResponse(response);
        }

        return response;
    }

    private static bool IsCacheable(DisplayProtocolPacket response)
    {
        if (response.Header.MessageType != DisplayMessageType.Result
            || !DisplayPayloadCodec.TryDecode(
                DisplayMessageType.Result,
                response.Payload.Span,
                out var payload,
                out _)
            || payload is not ResultPayload result)
        {
            return true;
        }

        return result.ResultCode is not DisplayResultCode.Busy and not DisplayResultCode.Incomplete;
    }

    private DisplayProtocolPacket ProcessRequest(
        DisplayProtocolPacket request,
        IDisplayProtocolPayload requestPayload) =>
        requestPayload switch
        {
            HelloRequestPayload hello => ProcessHello(request, hello),
            HealthRequestPayload => ProcessHealth(request),
            BeginFramePayload beginFrame => ProcessBeginFrame(request, beginFrame),
            FrameRegionPayload region => ProcessFrameRegion(request, region),
            CompleteFramePayload completeFrame => ProcessCompleteFrame(request, completeFrame),
            AbortFramePayload => ProcessAbortFrame(request),
            ClearPayload => ProcessControl(request),
            SetBrightnessPayload => ProcessControl(request),
            RenderTestPatternPayload => ProcessControl(request),
            _ => CreateResponse(
                request,
                new ResultPayload(DisplayResultCode.Unsupported, DisplayResultFlags.None, 0, 0, 0, 0))
        };

    private DisplayProtocolPacket ProcessHello(DisplayProtocolPacket request, HelloRequestPayload hello)
    {
        if (request.Header.SessionId != 0
            || !hello.MinimumVersion.HasCompatibleMajorVersion(DisplayProtocol.CurrentVersion)
            || hello.MinimumVersion.CompareTo(DisplayProtocol.CurrentVersion) > 0
            || hello.MaximumVersion.CompareTo(DisplayProtocol.CurrentVersion) < 0)
        {
            return CreateResponse(
                request,
                new ResultPayload(DisplayResultCode.UnsupportedVersion, DisplayResultFlags.None, 0, 0, 0, 0));
        }

        var capabilities = new CapabilitiesResponsePayload(
            DisplayProtocol.CurrentVersion,
            4,
            3,
            DisplayProtocol.DEFAULT_MAX_DATAGRAM_LENGTH,
            512,
            DisplayPixelFormatFlags.Rgb565BigEndian,
            DisplayRotationFlags.Degrees0,
            DisplayOptionalCommandFlags.Clear
                | DisplayOptionalCommandFlags.SetBrightness
                | DisplayOptionalCommandFlags.RenderTestPattern,
            DisplayFrameCapabilityFlags.FullFrameStaging
                | DisplayFrameCapabilityFlags.RegionTransfer
                | DisplayFrameCapabilityFlags.AtomicPresentation,
            DisplayAcknowledgementMode.ControlAndCompletion,
            _sessionId,
            "fake-esp32",
            "1.0.0-test",
            "memory-display");
        return CreateResponse(request, capabilities);
    }

    private DisplayProtocolPacket ProcessHealth(DisplayProtocolPacket request)
    {
        if (!HasCurrentSession(request))
        {
            return WrongSession(request);
        }

        return CreateResponse(
            request,
            new HealthResponsePayload(
                _activeFrame is null ? DisplayHealthState.Ready : DisplayHealthState.Busy,
                DisplayResultCode.Ok,
                60,
                128_000,
                _acceptedFrameCount,
                _rejectedFrameCount,
                _completedFrames.Keys.DefaultIfEmpty().Max()));
    }

    private DisplayProtocolPacket ProcessBeginFrame(DisplayProtocolPacket request, BeginFramePayload beginFrame)
    {
        if (!HasCurrentSession(request))
        {
            return WrongSession(request);
        }

        if (_activeFrame is not null && _activeFrame.FrameId != request.Header.FrameId)
        {
            return CreateResponse(
                request,
                new ResultPayload(DisplayResultCode.Busy, DisplayResultFlags.Retryable, 0, 10, 0, 0));
        }

        _activeFrame ??= new ActiveFrame(request.Header.FrameId, beginFrame);
        return Ok(request);
    }

    private DisplayProtocolPacket ProcessFrameRegion(DisplayProtocolPacket request, FrameRegionPayload region)
    {
        if (!HasCurrentSession(request))
        {
            return WrongSession(request);
        }

        if (_activeFrame is null || _activeFrame.FrameId != request.Header.FrameId)
        {
            return CreateResponse(
                request,
                new ResultPayload(DisplayResultCode.Invalid, DisplayResultFlags.None, 0, 0, 0, 0));
        }

        if (!_activeFrame.TryWrite(region))
        {
            _rejectedFrameCount++;
            return CreateResponse(
                request,
                new ResultPayload(DisplayResultCode.Invalid, DisplayResultFlags.None, 0, 0, 0, 0));
        }

        return Ok(request);
    }

    private DisplayProtocolPacket ProcessCompleteFrame(
        DisplayProtocolPacket request,
        CompleteFramePayload completeFrame)
    {
        if (!HasCurrentSession(request))
        {
            return WrongSession(request);
        }

        if (_completedFrames.TryGetValue(request.Header.FrameId, out var completed))
        {
            return CreateResponse(request, completed with { Flags = completed.Flags | DisplayResultFlags.Duplicate });
        }

        if (_activeFrame is null || _activeFrame.FrameId != request.Header.FrameId)
        {
            return CreateResponse(
                request,
                new ResultPayload(DisplayResultCode.Invalid, DisplayResultFlags.None, 0, 0, 0, 0));
        }

        if (_activeFrame.TryGetMissingRange(out var missingOffset, out var missingCount))
        {
            _rejectedFrameCount++;
            return CreateResponse(
                request,
                new ResultPayload(
                    DisplayResultCode.Incomplete,
                    DisplayResultFlags.None,
                    0,
                    0,
                    missingOffset,
                    missingCount));
        }

        if (completeFrame.FrameCrc32 != _activeFrame.Metadata.FrameCrc32
            || DisplayPacketCodec.ComputeCrc32(_activeFrame.Bytes) != completeFrame.FrameCrc32)
        {
            _rejectedFrameCount++;
            return CreateResponse(
                request,
                new ResultPayload(DisplayResultCode.ChecksumMismatch, DisplayResultFlags.None, 0, 0, 0, 0));
        }

        PresentedFrame = _activeFrame.Bytes.ToArray();
        PresentationCount++;
        _acceptedFrameCount++;
        var result = new ResultPayload(
            DisplayResultCode.Ok,
            DisplayResultFlags.Presented,
            0,
            0,
            0,
            0);
        _completedFrames.Add(request.Header.FrameId, result);
        _activeFrame = null;
        return CreateResponse(request, result);
    }

    private DisplayProtocolPacket ProcessAbortFrame(DisplayProtocolPacket request)
    {
        if (!HasCurrentSession(request))
        {
            return WrongSession(request);
        }

        if (_activeFrame?.FrameId == request.Header.FrameId)
        {
            _activeFrame = null;
        }

        return Ok(request);
    }

    private DisplayProtocolPacket ProcessControl(DisplayProtocolPacket request) =>
        HasCurrentSession(request) ? Ok(request) : WrongSession(request);

    private bool HasCurrentSession(DisplayProtocolPacket request) =>
        request.Header.SessionId == _sessionId;

    private static DisplayProtocolPacket Ok(DisplayProtocolPacket request) =>
        CreateResponse(
            request,
            new ResultPayload(DisplayResultCode.Ok, DisplayResultFlags.None, 0, 0, 0, 0));

    private static DisplayProtocolPacket WrongSession(DisplayProtocolPacket request) =>
        CreateResponse(
            request,
            new ResultPayload(DisplayResultCode.WrongSession, DisplayResultFlags.None, 0, 0, 0, 0));

    private static DisplayProtocolPacket CreateResponse(
        DisplayProtocolPacket request,
        IDisplayProtocolPayload payload)
    {
        var header = new DisplayPacketHeader(
            request.Header.Version,
            payload.MessageType,
            DisplayProtocolFlags.Response,
            request.Header.RequestId,
            request.Header.FrameId,
            request.Header.SessionId);
        return new DisplayProtocolPacket(header, DisplayPayloadCodec.Encode(payload));
    }

    private static DisplayProtocolPacket MarkDuplicate(DisplayProtocolPacket response)
    {
        if (response.Header.MessageType != DisplayMessageType.Result
            || !DisplayPayloadCodec.TryDecode(
                DisplayMessageType.Result,
                response.Payload.Span,
                out var payload,
                out _)
            || payload is not ResultPayload result)
        {
            return response;
        }

        return new DisplayProtocolPacket(
            response.Header,
            DisplayPayloadCodec.Encode(result with { Flags = result.Flags | DisplayResultFlags.Duplicate }));
    }

    private static byte[] MutateResponse(
        DisplayProtocolPacket response,
        FakeDisplayBehavior behavior)
    {
        if (behavior.WrongFrameId
            || behavior.WrongSessionId
            || behavior.MissingResponseFlag
            || behavior.UnexpectedMessageType)
        {
            response = new DisplayProtocolPacket(
                response.Header with
                {
                    FrameId = behavior.WrongFrameId ? NextNonZero(response.Header.FrameId) : response.Header.FrameId,
                    SessionId = behavior.WrongSessionId ? NextNonZero(response.Header.SessionId) : response.Header.SessionId,
                    Flags = behavior.MissingResponseFlag
                        ? response.Header.Flags & ~DisplayProtocolFlags.Response
                        : response.Header.Flags,
                    MessageType = behavior.UnexpectedMessageType
                        ? GetDifferentMessageType(response.Header.MessageType)
                        : response.Header.MessageType
                },
                behavior.InvalidPayload ? ReadOnlyMemory<byte>.Empty : response.Payload);
        }
        else if (behavior.InvalidPayload)
        {
            response = new DisplayProtocolPacket(response.Header, ReadOnlyMemory<byte>.Empty);
        }

        var datagram = DisplayPacketCodec.Encode(response);
        if (behavior.CorruptResponse)
        {
            datagram[^1] ^= 0xFF;
        }

        return datagram;
    }

    private static DisplayMessageType GetDifferentMessageType(DisplayMessageType messageType) =>
        messageType == DisplayMessageType.HealthResponse
            ? DisplayMessageType.CapabilitiesResponse
            : DisplayMessageType.HealthResponse;

    private void Deliver(byte[] datagram) =>
        DatagramReceived?.Invoke(this, new DisplayDatagramReceivedEventArgs(datagram));

    private void Enqueue(FakeDisplayBehavior behavior)
    {
        lock (_syncRoot)
        {
            _behaviors.Enqueue(behavior);
        }
    }

    private static uint NextNonZero(uint value)
    {
        var next = unchecked(value + 1);
        return next == 0 ? 1 : next;
    }

    private sealed class ActiveFrame
    {
        private readonly bool[] _receivedBytes;

        public ActiveFrame(uint frameId, BeginFramePayload metadata)
        {
            FrameId = frameId;
            Metadata = metadata;
            Bytes = new byte[metadata.ExpectedPixelByteCount];
            _receivedBytes = new bool[metadata.ExpectedPixelByteCount];
        }

        public uint FrameId { get; }

        public BeginFramePayload Metadata { get; }

        public byte[] Bytes { get; }

        public bool TryWrite(FrameRegionPayload region)
        {
            if ((uint)region.X + region.Width > Metadata.Width
                || (uint)region.Y + region.Height > Metadata.Height)
            {
                return false;
            }

            var expectedOffset = ((uint)region.Y * Metadata.Width + region.X) * 2;
            if (region.FrameByteOffset != expectedOffset)
            {
                return false;
            }

            var source = region.PixelBytes.Span;
            var bytesPerRegionRow = region.Width * 2;
            for (var row = 0; row < region.Height; row++)
            {
                var destinationOffset = ((region.Y + row) * Metadata.Width + region.X) * 2;
                source.Slice(row * bytesPerRegionRow, bytesPerRegionRow)
                    .CopyTo(Bytes.AsSpan(destinationOffset, bytesPerRegionRow));
                _receivedBytes.AsSpan(destinationOffset, bytesPerRegionRow).Fill(true);
            }

            return true;
        }

        public bool TryGetMissingRange(out uint offset, out uint count)
        {
            var firstMissing = Array.IndexOf(_receivedBytes, false);
            if (firstMissing < 0)
            {
                offset = 0;
                count = 0;
                return false;
            }

            var end = firstMissing;
            while (end < _receivedBytes.Length && !_receivedBytes[end])
            {
                end++;
            }

            offset = (uint)firstMissing;
            count = (uint)(end - firstMissing);
            return true;
        }
    }

    private sealed record CachedResponse(DisplayProtocolPacket Response);

    private sealed class RequestFingerprint : IEquatable<RequestFingerprint>
    {
        private readonly byte[] _payload;

        public RequestFingerprint(
            DisplayMessageType messageType,
            uint frameId,
            uint sessionId,
            byte[] payload)
        {
            MessageType = messageType;
            FrameId = frameId;
            SessionId = sessionId;
            _payload = payload;
        }

        public DisplayMessageType MessageType { get; }

        public uint FrameId { get; }

        public uint SessionId { get; }

        public bool Equals(RequestFingerprint? other) =>
            other is not null
            && MessageType == other.MessageType
            && FrameId == other.FrameId
            && SessionId == other.SessionId
            && _payload.AsSpan().SequenceEqual(other._payload);

        public override bool Equals(object? obj) => Equals(obj as RequestFingerprint);

        public override int GetHashCode() => HashCode.Combine(MessageType, FrameId, SessionId);
    }

    private sealed record FakeDisplayBehavior(
        bool DropResponse = false,
        int DeliveryCount = 1,
        bool HoldResponse = false,
        bool CorruptResponse = false,
        bool WrongFrameId = false,
        bool WrongSessionId = false,
        bool MissingResponseFlag = false,
        bool UnexpectedMessageType = false,
        bool InvalidPayload = false,
        bool TransportFailure = false,
        TimeSpan Delay = default,
        ResultPayload? ForcedResult = null);
}