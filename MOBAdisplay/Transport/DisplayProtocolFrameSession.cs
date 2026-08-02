// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Transport;

using Moba.Display.Protocol;

/// <summary>
/// Transfers complete RGB565 big-endian frames over one negotiated protocol v1.0 session.
/// </summary>
public sealed class DisplayProtocolFrameSession
{
    private const int MaximumIncompleteRepairs = 3;
    private static readonly DisplayRequestOptions FrameRequestOptions =
        new(3, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(250));
    private readonly DisplayProtocolClient _client;
    private readonly DisplayIdentifierSequence _frameIds;
    private CapabilitiesResponsePayload? _capabilities;

    /// <summary>
    /// Initializes a frame-transfer session over an existing correlated protocol client.
    /// </summary>
    /// <param name="client">Protocol client connected to one display endpoint.</param>
    /// <param name="frameIds">Frame identifier sequence, or a process-wide sequence when omitted.</param>
    /// <param name="negotiatedCapabilities">Capabilities already negotiated on the same client session.</param>
    public DisplayProtocolFrameSession(
        DisplayProtocolClient client,
        DisplayIdentifierSequence? frameIds = null,
        CapabilitiesResponsePayload? negotiatedCapabilities = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
        _frameIds = frameIds ?? new DisplayIdentifierSequence();
        _capabilities = negotiatedCapabilities;
    }

    /// <summary>
    /// Negotiates capabilities when needed and transfers one complete frame atomically.
    /// </summary>
    /// <param name="rgb565Frame">Complete row-major RGB565 big-endian frame.</param>
    /// <param name="width">Frame width in pixels.</param>
    /// <param name="height">Frame height in pixels.</param>
    /// <param name="cancellationToken">Stops negotiation, transfer, and further retries.</param>
    public async Task SendFrameAsync(
        ReadOnlyMemory<byte> rgb565Frame,
        ushort width,
        ushort height,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var expectedByteCount = checked(width * height * 2);
        if (rgb565Frame.Length != expectedByteCount)
        {
            throw new ArgumentException("RGB565 frame size is invalid.", nameof(rgb565Frame));
        }

        var capabilities = await EnsureNegotiatedAsync(cancellationToken).ConfigureAwait(false);
        ValidateCapabilities(capabilities, width, height);
        var regions = CreateRegions(width, height, capabilities.MaximumRegionPayloadLength);

        var frameId = _frameIds.Next();
        var frameCrc32 = DisplayPacketCodec.ComputeCrc32(rgb565Frame.Span);
        try
        {
            await SendRequiredResultAsync(
                new BeginFramePayload(
                    width,
                    height,
                    DisplayPixelFormat.Rgb565BigEndian,
                    DisplayRotation.Degrees0,
                    (uint)rgb565Frame.Length,
                    frameCrc32),
                capabilities.SessionId,
                frameId,
                cancellationToken).ConfigureAwait(false);
            await SendRegionsAsync(
                rgb565Frame,
                regions,
                regions.Length,
                capabilities.SessionId,
                frameId,
                cancellationToken).ConfigureAwait(false);

            var completion = await CompleteFrameAsync(
                rgb565Frame,
                regions,
                capabilities.SessionId,
                frameId,
                frameCrc32,
                cancellationToken).ConfigureAwait(false);
            if (!completion.Flags.HasFlag(DisplayResultFlags.Presented))
            {
                throw new InvalidOperationException("The display accepted the frame without confirming presentation.");
            }
        }
        catch
        {
            await TryAbortFrameAsync(capabilities.SessionId, frameId).ConfigureAwait(false);

            throw;
        }
    }

    private async Task<CapabilitiesResponsePayload> EnsureNegotiatedAsync(
        CancellationToken cancellationToken)
    {
        if (_capabilities is not null)
        {
            return _capabilities;
        }

        var outcome = await _client.SendRequestAsync(
            new HelloRequestPayload(
                DisplayProtocol.CurrentVersion,
                DisplayProtocol.CurrentVersion,
                DisplayProtocol.DEFAULT_MAX_DATAGRAM_LENGTH),
            options: DisplayRequestOptions.Default,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!outcome.IsSuccessful || outcome.Response is not CapabilitiesResponsePayload capabilities)
        {
            ThrowIfCancelled(outcome, cancellationToken);
            throw CreateRequestException("Protocol negotiation", outcome);
        }

        _capabilities = capabilities;
        return capabilities;
    }

    private async Task SendRegionsAsync(
        ReadOnlyMemory<byte> rgb565Frame,
        FrameRegionDescriptor[] regions,
        int totalRegionCount,
        uint sessionId,
        uint frameId,
        CancellationToken cancellationToken)
    {
        foreach (var region in regions)
        {
            var outcome = await _client.SendUnacknowledgedAsync(
                new FrameRegionPayload(
                    region.X,
                    region.Y,
                    region.Width,
                    region.Height,
                    (uint)region.ByteOffset,
                    rgb565Frame.Slice(region.ByteOffset, region.ByteCount)),
                sessionId,
                frameId,
                new DisplayPacketSequence(
                    region.PacketIndex,
                    checked((ushort)totalRegionCount),
                    region.PacketIndex == totalRegionCount - 1),
                cancellationToken).ConfigureAwait(false);
            if (!outcome.IsSuccessful)
            {
                ThrowIfCancelled(outcome, cancellationToken);
                throw CreateRequestException(DisplayMessageType.FrameRegion.ToString(), outcome);
            }
        }
    }

    private async Task<ResultPayload> CompleteFrameAsync(
        ReadOnlyMemory<byte> rgb565Frame,
        FrameRegionDescriptor[] regions,
        uint sessionId,
        uint frameId,
        uint frameCrc32,
        CancellationToken cancellationToken)
    {
        for (var repairAttempt = 0; repairAttempt <= MaximumIncompleteRepairs; repairAttempt++)
        {
            var result = await SendResultAsync(
                new CompleteFramePayload(frameCrc32),
                sessionId,
                frameId,
                cancellationToken).ConfigureAwait(false);
            if (result.ResultCode == DisplayResultCode.Ok)
            {
                return result;
            }

            if (result.ResultCode != DisplayResultCode.Incomplete
                || repairAttempt == MaximumIncompleteRepairs)
            {
                throw CreateResultException(DisplayMessageType.CompleteFrame, result.ResultCode);
            }

            var missingStart = result.FirstMissingByteOffset;
            var missingEnd = (ulong)missingStart + result.MissingByteCount;
            var missingRegions = regions
                .Where(region => (ulong)region.ByteOffset < missingEnd
                    && (ulong)region.ByteOffset + (uint)region.ByteCount > missingStart)
                .ToArray();
            if (missingRegions.Length == 0)
            {
                throw new InvalidOperationException("The display reported a missing range outside the frame regions.");
            }

            await SendRegionsAsync(
                rgb565Frame,
                missingRegions,
                regions.Length,
                sessionId,
                frameId,
                cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException("The display frame repair policy was exhausted.");
    }

    private async Task<ResultPayload> SendRequiredResultAsync(
        BeginFramePayload request,
        uint sessionId,
        uint frameId,
        CancellationToken cancellationToken,
        DisplayPacketSequence? packetSequence = null)
    {
        var result = await SendResultAsync(
            request,
            sessionId,
            frameId,
            cancellationToken,
            packetSequence).ConfigureAwait(false);
        if (result.ResultCode != DisplayResultCode.Ok)
        {
            throw CreateResultException(request.MessageType, result.ResultCode);
        }

        return result;
    }

    private async Task<ResultPayload> SendResultAsync(
        IDisplayProtocolPayload request,
        uint sessionId,
        uint frameId,
        CancellationToken cancellationToken,
        DisplayPacketSequence? packetSequence = null)
    {
        var outcome = await _client.SendRequestAsync(
            request,
            sessionId,
            frameId,
            FrameRequestOptions,
            packetSequence,
            cancellationToken).ConfigureAwait(false);
        if (!outcome.IsSuccessful || outcome.Response is not ResultPayload result)
        {
            ThrowIfCancelled(outcome, cancellationToken);
            throw CreateRequestException(request.MessageType.ToString(), outcome);
        }

        if (result.ResultCode == DisplayResultCode.WrongSession)
        {
            _capabilities = null;
        }

        return result;
    }

    private async Task TryAbortFrameAsync(uint sessionId, uint frameId)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
        var options = new DisplayRequestOptions(1, TimeSpan.FromMilliseconds(200), TimeSpan.Zero);
        var outcome = await _client.SendRequestAsync(
            new AbortFramePayload(DisplayAbortReason.HostCancellation),
            sessionId,
            frameId,
            options,
            cancellationToken: timeout.Token).ConfigureAwait(false);
        if (outcome.Response is ResultPayload { ResultCode: DisplayResultCode.WrongSession })
        {
            _capabilities = null;
        }
    }

    private static FrameRegionDescriptor[] CreateRegions(
        ushort width,
        ushort height,
        ushort maximumRegionPayloadLength)
    {
        var maximumPixelsPerRegion = maximumRegionPayloadLength / 2;
        if (maximumPixelsPerRegion == 0)
        {
            throw new InvalidOperationException("The negotiated region limit cannot carry one RGB565 pixel.");
        }

        var regions = new List<FrameRegionDescriptor>();
        if (maximumPixelsPerRegion >= width)
        {
            var rowsPerRegion = maximumPixelsPerRegion / width;
            for (var y = 0; y < height; y += rowsPerRegion)
            {
                var regionHeight = Math.Min(rowsPerRegion, height - y);
                var byteOffset = checked(y * width * 2);
                regions.Add(new FrameRegionDescriptor(
                    0,
                    (ushort)y,
                    width,
                    (ushort)regionHeight,
                    byteOffset,
                    checked(regionHeight * width * 2)));
            }
        }
        else
        {
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x += maximumPixelsPerRegion)
                {
                    var regionWidth = Math.Min(maximumPixelsPerRegion, width - x);
                    var byteOffset = checked((y * width + x) * 2);
                    regions.Add(new FrameRegionDescriptor(
                        (ushort)x,
                        (ushort)y,
                        (ushort)regionWidth,
                        1,
                        byteOffset,
                        checked(regionWidth * 2)));
                }
            }
        }

        if (regions.Count > ushort.MaxValue)
        {
            throw new InvalidOperationException("The frame requires too many protocol regions.");
        }

        return regions
            .Select((region, index) => region with { PacketIndex = (ushort)index })
            .ToArray();
    }

    private static void ValidateCapabilities(
        CapabilitiesResponsePayload capabilities,
        ushort width,
        ushort height)
    {
        var requiredFrameCapabilities =
            DisplayFrameCapabilityFlags.FullFrameStaging
            | DisplayFrameCapabilityFlags.RegionTransfer
            | DisplayFrameCapabilityFlags.AtomicPresentation;
        if (capabilities.SelectedVersion != DisplayProtocol.CurrentVersion
            || capabilities.Width != width
            || capabilities.Height != height
            || !capabilities.PixelFormats.HasFlag(DisplayPixelFormatFlags.Rgb565BigEndian)
            || !capabilities.Rotations.HasFlag(DisplayRotationFlags.Degrees0)
            || (capabilities.FrameCapabilities & requiredFrameCapabilities) != requiredFrameCapabilities)
        {
            throw new InvalidOperationException("The negotiated display capabilities do not support this frame.");
        }
    }

    private static DisplayProtocolOperationException CreateRequestException(
        string operation,
        DisplayRequestOutcome outcome) =>
        new(
            $"{operation} failed with {outcome.Failure}: "
            + (outcome.Diagnostic ?? "No compatible response was received."),
            outcome.Failure);

    private static DisplayProtocolOperationException CreateResultException(
        DisplayMessageType messageType,
        DisplayResultCode resultCode) =>
        new(
            $"Display request {messageType} failed with {resultCode}.",
            DisplayRequestFailure.None,
            resultCode);

    private static void ThrowIfCancelled(
        DisplayRequestOutcome outcome,
        CancellationToken cancellationToken)
    {
        if (outcome.Failure == DisplayRequestFailure.Cancelled)
        {
            throw new OperationCanceledException(
                "The display frame transfer was cancelled.",
                cancellationToken);
        }
    }

    private readonly record struct FrameRegionDescriptor(
        ushort X,
        ushort Y,
        ushort Width,
        ushort Height,
        int ByteOffset,
        int ByteCount)
    {
        public ushort PacketIndex { get; init; }
    }
}
