// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Transport;

using Moba.Display.Protocol;

/// <summary>
/// Transfers complete RGB565 big-endian frames over one negotiated protocol v1.0 session.
/// </summary>
public sealed class DisplayProtocolFrameSession
{
    private readonly DisplayProtocolClient _client;
    private readonly DisplayIdentifierSequence _frameIds;
    private CapabilitiesResponsePayload? _capabilities;

    /// <summary>
    /// Initializes a frame-transfer session over an existing correlated protocol client.
    /// </summary>
    /// <param name="client">Protocol client connected to one display endpoint.</param>
    /// <param name="frameIds">Frame identifier sequence, or a process-wide sequence when omitted.</param>
    public DisplayProtocolFrameSession(
        DisplayProtocolClient client,
        DisplayIdentifierSequence? frameIds = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
        _frameIds = frameIds ?? new DisplayIdentifierSequence();
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
                width,
                height,
                capabilities,
                frameId,
                cancellationToken).ConfigureAwait(false);

            var completion = await SendRequiredResultAsync(
                new CompleteFramePayload(frameCrc32),
                capabilities.SessionId,
                frameId,
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
        ushort width,
        ushort height,
        CapabilitiesResponsePayload capabilities,
        uint frameId,
        CancellationToken cancellationToken)
    {
        var bytesPerRow = checked(width * 2);
        var rowsPerRegion = capabilities.MaximumRegionPayloadLength / bytesPerRow;
        if (rowsPerRegion == 0)
        {
            throw new InvalidOperationException("The negotiated region limit cannot carry one display row.");
        }

        var regionCount = checked((height + rowsPerRegion - 1) / rowsPerRegion);
        if (regionCount > ushort.MaxValue)
        {
            throw new InvalidOperationException("The frame requires too many protocol regions.");
        }

        for (var regionIndex = 0; regionIndex < regionCount; regionIndex++)
        {
            var y = regionIndex * rowsPerRegion;
            var regionHeight = Math.Min(rowsPerRegion, height - y);
            var byteOffset = checked(y * bytesPerRow);
            var byteCount = checked(regionHeight * bytesPerRow);
            var isFinal = regionIndex == regionCount - 1;
            await SendRequiredResultAsync(
                new FrameRegionPayload(
                    0,
                    (ushort)y,
                    width,
                    (ushort)regionHeight,
                    (uint)byteOffset,
                    rgb565Frame.Slice(byteOffset, byteCount)),
                capabilities.SessionId,
                frameId,
                cancellationToken,
                new DisplayPacketSequence(
                    (ushort)regionIndex,
                    (ushort)regionCount,
                    isFinal)).ConfigureAwait(false);
        }
    }

    private async Task<ResultPayload> SendRequiredResultAsync(
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
            DisplayRequestOptions.Default,
            packetSequence,
            cancellationToken).ConfigureAwait(false);
        if (!outcome.IsSuccessful || outcome.Response is not ResultPayload result)
        {
            ThrowIfCancelled(outcome, cancellationToken);
            throw CreateRequestException(request.MessageType.ToString(), outcome);
        }

        if (result.ResultCode != DisplayResultCode.Ok)
        {
            if (result.ResultCode == DisplayResultCode.WrongSession)
            {
                _capabilities = null;
            }

            throw new InvalidOperationException(
                $"Display request {request.MessageType} failed with {result.ResultCode}.");
        }

        return result;
    }

    private async Task TryAbortFrameAsync(uint sessionId, uint frameId)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
        var options = new DisplayRequestOptions(1, TimeSpan.FromMilliseconds(200), TimeSpan.Zero);
        await _client.SendRequestAsync(
            new AbortFramePayload(DisplayAbortReason.HostCancellation),
            sessionId,
            frameId,
            options,
            cancellationToken: timeout.Token).ConfigureAwait(false);
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

    private static InvalidOperationException CreateRequestException(
        string operation,
        DisplayRequestOutcome outcome) =>
        new(
            $"{operation} failed with {outcome.Failure}: "
            + (outcome.Diagnostic ?? "No compatible response was received."));

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
}