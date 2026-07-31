// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdisplay;

using Moba.Display.Protocol;
using Moba.Display.Transport;

[TestFixture]
[Category("Integration")]
internal sealed class DisplayProtocolFrameSessionTests
{
    private static readonly ushort[] ExpectedPacketIndexes = [0, 1, 2];
    private static readonly bool[] ExpectedFinalPacketFlags = [false, false, true];

    [Test]
    public async Task SendFrameAsync_Should_PreserveConformanceBytesThroughAdapterBoundary()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        using var client = new DisplayProtocolClient(endpoint);
        var session = new DisplayProtocolFrameSession(client);
        var frame = DisplayConformancePattern.CreateRgb565(4, 3);

        // Act
        await session.SendFrameAsync(frame, 4, 3).ConfigureAwait(false);

        // Assert
        var region = endpoint.ReceivedPackets.Single(
            packet => packet.Header.MessageType == DisplayMessageType.FrameRegion);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(endpoint.PresentationCount, Is.EqualTo(1));
            Assert.That(endpoint.PresentedFrame.ToArray(), Is.EqualTo(frame));
            Assert.That(region.Header.PacketIndex, Is.Zero);
            Assert.That(region.Header.PacketCount, Is.EqualTo(1));
            Assert.That(
                region.Header.Flags.HasFlag(DisplayProtocolFlags.FinalPacket),
                Is.True);
        }
    }

    [Test]
    public async Task SendFrameAsync_Should_SequenceRegionsWithinNegotiatedLimit()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint(maximumRegionPayloadLength: 8);
        using var client = new DisplayProtocolClient(endpoint);
        var session = new DisplayProtocolFrameSession(client);
        var frame = DisplayConformancePattern.CreateRgb565(4, 3);

        // Act
        await session.SendFrameAsync(frame, 4, 3).ConfigureAwait(false);

        // Assert
        var regions = endpoint.ReceivedPackets
            .Where(packet => packet.Header.MessageType == DisplayMessageType.FrameRegion)
            .ToArray();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(regions, Has.Length.EqualTo(3));
            Assert.That(
                regions.Select(packet => packet.Header.PacketIndex),
                Is.EqualTo(ExpectedPacketIndexes));
            Assert.That(
                regions.Select(packet => packet.Header.PacketCount),
                Is.All.EqualTo((ushort)3));
            Assert.That(
                regions.Select(packet => packet.Header.Flags.HasFlag(DisplayProtocolFlags.FinalPacket)),
                Is.EqualTo(ExpectedFinalPacketFlags));
            Assert.That(
                regions.Select(packet => packet.Header.Flags.HasFlag(DisplayProtocolFlags.AcknowledgementRequired)),
                Is.All.False);
            Assert.That(endpoint.PresentedFrame.ToArray(), Is.EqualTo(frame));
        }
    }

    [TestCase((ushort)172, (ushort)256)]
    [TestCase((ushort)240, (ushort)480)]
    [TestCase((ushort)800, (ushort)1184)]
    public async Task SendFrameAsync_Should_SplitRowsWithinNegotiatedLimit(
        ushort width,
        ushort maximumRegionPayloadLength)
    {
        // Arrange
        const ushort height = 2;
        var endpoint = new FakeDisplayEndpoint(
            width: width,
            height: height,
            maximumRegionPayloadLength: maximumRegionPayloadLength);
        using var client = new DisplayProtocolClient(endpoint);
        var session = new DisplayProtocolFrameSession(client);
        var frame = DisplayConformancePattern.CreateRgb565(width, height);

        // Act
        await session.SendFrameAsync(frame, width, height).ConfigureAwait(false);

        // Assert
        var regionPackets = endpoint.ReceivedPackets
            .Where(packet => packet.Header.MessageType == DisplayMessageType.FrameRegion)
            .ToArray();
        var regions = regionPackets.Select(DecodeRegion).ToArray();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(regions, Is.Not.Empty);
            Assert.That(
                regions.Select(region => region.PixelBytes.Length),
                Is.All.LessThanOrEqualTo(maximumRegionPayloadLength));
            Assert.That(
                regions.Any(region => region.X > 0),
                Is.EqualTo(width * 2 > maximumRegionPayloadLength));
            Assert.That(
                regionPackets.Select(packet => packet.Header.PacketIndex),
                Is.EqualTo(Enumerable.Range(0, regionPackets.Length).Select(index => (ushort)index)));
            Assert.That(
                regionPackets.Select(packet => packet.Header.PacketCount),
                Is.All.EqualTo((ushort)regionPackets.Length));
            Assert.That(endpoint.PresentedFrame.ToArray(), Is.EqualTo(frame));
        }
    }

    [Test]
    public async Task SendFrameAsync_Should_RepairMissingRegionsBeforePresentation()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint(maximumRegionPayloadLength: 8);
        endpoint.DropFrameRegion(0);
        endpoint.DropFrameRegion(2);
        using var client = new DisplayProtocolClient(endpoint);
        var session = new DisplayProtocolFrameSession(client);
        var frame = DisplayConformancePattern.CreateRgb565(4, 3);

        // Act
        await session.SendFrameAsync(frame, 4, 3).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(endpoint.PresentationCount, Is.EqualTo(1));
            Assert.That(endpoint.PresentedFrame.ToArray(), Is.EqualTo(frame));
            Assert.That(
                endpoint.ReceivedPackets.Count(
                    packet => packet.Header.MessageType == DisplayMessageType.CompleteFrame),
                Is.EqualTo(3));
        }
    }

    [Test]
    public async Task SendFrameAsync_Should_AbortWithoutPresentation_When_RegionRemainsMissing()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint(maximumRegionPayloadLength: 8);
        endpoint.DropFrameRegion(1, int.MaxValue);
        using var client = new DisplayProtocolClient(endpoint);
        var session = new DisplayProtocolFrameSession(client);
        var frame = DisplayConformancePattern.CreateRgb565(4, 3);

        // Act
        var exception = await CaptureExceptionAsync<InvalidOperationException>(
            () => session.SendFrameAsync(frame, 4, 3)).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(endpoint.PresentationCount, Is.Zero);
            Assert.That(
                endpoint.ReceivedPackets.Last().Header.MessageType,
                Is.EqualTo(DisplayMessageType.AbortFrame));
        }
    }

    [Test]
    public async Task SendFrameAsync_Should_PresentOnce_When_CompletionResponseIsLost()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        endpoint.DropNextResponse(DisplayMessageType.CompleteFrame);
        using var client = new DisplayProtocolClient(endpoint);
        var session = new DisplayProtocolFrameSession(client);
        var frame = DisplayConformancePattern.CreateRgb565(4, 3);

        // Act
        await session.SendFrameAsync(frame, 4, 3).ConfigureAwait(false);

        // Assert
        Assert.That(endpoint.PresentationCount, Is.EqualTo(1));
    }

    [Test]
    public async Task SendFrameAsync_Should_RetryBeginBeforeFirmwareStagingTimeout_When_ResponseIsLost()
    {
        // Arrange
        var timeProvider = new ManualTimeProvider();
        var endpoint = new FakeDisplayEndpoint(timeProvider);
        endpoint.DropNextResponse(DisplayMessageType.BeginFrame);
        using var client = new DisplayProtocolClient(endpoint, timeProvider: timeProvider);
        var session = new DisplayProtocolFrameSession(client);
        var frame = DisplayConformancePattern.CreateRgb565(4, 3);
        using var cancellation = new CancellationTokenSource();
        var sendTask = session.SendFrameAsync(frame, 4, 3, cancellation.Token);

        try
        {
            await WaitUntilAsync(
                () => endpoint.ReceivedPackets.Count(
                    packet => packet.Header.MessageType == DisplayMessageType.BeginFrame) == 1)
                .ConfigureAwait(false);
            await WaitForTimerAsync(timeProvider).ConfigureAwait(false);

            // Act
            timeProvider.Advance(TimeSpan.FromMilliseconds(250));
            await WaitUntilAsync(
                () => endpoint.ReceivedPackets.Count(
                    packet => packet.Header.MessageType == DisplayMessageType.BeginFrame) == 2)
                .ConfigureAwait(false);
            await sendTask.ConfigureAwait(false);

            // Assert
            var beginPackets = endpoint.ReceivedPackets
                .Where(packet => packet.Header.MessageType == DisplayMessageType.BeginFrame)
                .ToArray();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(beginPackets, Has.Length.EqualTo(2));
                Assert.That(
                    beginPackets.Select(packet => packet.Header.RequestId).Distinct(),
                    Has.Exactly(1).Items);
                Assert.That(
                    beginPackets[1].Header.Flags.HasFlag(DisplayProtocolFlags.Retry),
                    Is.True);
                Assert.That(endpoint.PresentationCount, Is.EqualTo(1));
            }
        }
        finally
        {
            await cancellation.CancelAsync().ConfigureAwait(false);
        }
    }

    [Test]
    public async Task SendFrameAsync_Should_RejectIncompatibleDimensionsBeforeFrameData()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint(width: 5);
        using var client = new DisplayProtocolClient(endpoint);
        var session = new DisplayProtocolFrameSession(client);
        var frame = DisplayConformancePattern.CreateRgb565(4, 3);

        // Act
        var exception = await CaptureExceptionAsync<InvalidOperationException>(
            () => session.SendFrameAsync(frame, 4, 3)).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(endpoint.ReceivedPackets, Has.Count.EqualTo(1));
            Assert.That(
                endpoint.ReceivedPackets[0].Header.MessageType,
                Is.EqualTo(DisplayMessageType.HelloRequest));
        }
    }

    [Test]
    public async Task SendFrameAsync_Should_RenegotiateAfterWrongSession()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        using var client = new DisplayProtocolClient(endpoint);
        var session = new DisplayProtocolFrameSession(client);
        var frame = DisplayConformancePattern.CreateRgb565(4, 3);
        await session.SendFrameAsync(frame, 4, 3).ConfigureAwait(false);
        endpoint.Reboot();

        // Act
        var exception = await CaptureExceptionAsync<InvalidOperationException>(
            () => session.SendFrameAsync(frame, 4, 3)).ConfigureAwait(false);
        await session.SendFrameAsync(frame, 4, 3).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(endpoint.PresentationCount, Is.EqualTo(2));
            Assert.That(
                endpoint.ReceivedPackets.Count(
                    packet => packet.Header.MessageType == DisplayMessageType.HelloRequest),
                Is.EqualTo(2));
        }
    }

    [Test]
    public async Task SendFrameAsync_Should_PropagateCancellationAndAbortStartedFrame()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        using var client = new DisplayProtocolClient(endpoint);
        var session = new DisplayProtocolFrameSession(client);
        var frame = DisplayConformancePattern.CreateRgb565(4, 3);
        await session.SendFrameAsync(frame, 4, 3).ConfigureAwait(false);
        endpoint.HoldNextResponse();
        using var cancellation = new CancellationTokenSource();

        // Act
        var expectedPacketCount = endpoint.ReceivedPacketCount + 1;
        var sendTask = session.SendFrameAsync(frame, 4, 3, cancellation.Token);
        await WaitUntilAsync(
            () => endpoint.ReceivedPacketCount >= expectedPacketCount).ConfigureAwait(false);
        await cancellation.CancelAsync().ConfigureAwait(false);
        var exception = await CaptureExceptionAsync<OperationCanceledException>(
            () => sendTask).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(
                endpoint.ReceivedPackets.Last().Header.MessageType,
                Is.EqualTo(DisplayMessageType.AbortFrame));
        }
    }

    [Test]
    public async Task SendFrameAsync_Should_RenegotiateAfterAbortReportsWrongSession()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        using var client = new DisplayProtocolClient(endpoint);
        var session = new DisplayProtocolFrameSession(client);
        var frame = DisplayConformancePattern.CreateRgb565(4, 3);
        await session.SendFrameAsync(frame, 4, 3).ConfigureAwait(false);
        endpoint.HoldNextResponse();
        using var cancellation = new CancellationTokenSource();
        var expectedPacketCount = endpoint.ReceivedPacketCount + 1;
        var interruptedSend = session.SendFrameAsync(frame, 4, 3, cancellation.Token);
        await WaitUntilAsync(
            () => endpoint.ReceivedPacketCount >= expectedPacketCount).ConfigureAwait(false);
        endpoint.Reboot();
        await cancellation.CancelAsync().ConfigureAwait(false);
        _ = await CaptureExceptionAsync<OperationCanceledException>(
            () => interruptedSend).ConfigureAwait(false);

        // Act
        await session.SendFrameAsync(frame, 4, 3).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(endpoint.PresentationCount, Is.EqualTo(2));
            Assert.That(
                endpoint.ReceivedPackets.Count(
                    packet => packet.Header.MessageType == DisplayMessageType.HelloRequest),
                Is.EqualTo(2));
        }
    }

    private static FrameRegionPayload DecodeRegion(DisplayProtocolPacket packet)
    {
        var decoded = DisplayPayloadCodec.TryDecode(
            DisplayMessageType.FrameRegion,
            packet.Payload.Span,
            out var payload,
            out var error);
        Assert.That(decoded, Is.True, $"Region payload decode failed with {error}.");
        return payload as FrameRegionPayload
            ?? throw new AssertionException("Decoded payload is not a frame region.");
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!condition())
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), timeout.Token).ConfigureAwait(false);
        }
    }

    private static async Task WaitForTimerAsync(ManualTimeProvider timeProvider)
    {
        for (var attempt = 0; attempt < 100 && timeProvider.ScheduledTimerCount == 0; attempt++)
        {
            await Task.Yield();
        }

        Assert.That(timeProvider.ScheduledTimerCount, Is.GreaterThan(0));
    }

    private static async Task<TException> CaptureExceptionAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (TException exception)
        {
            return exception;
        }

        throw new AssertionException($"Expected {typeof(TException).Name}.");
    }
}