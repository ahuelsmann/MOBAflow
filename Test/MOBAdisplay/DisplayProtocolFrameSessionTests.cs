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
            Assert.That(endpoint.PresentedFrame.ToArray(), Is.EqualTo(frame));
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

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!condition())
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), timeout.Token).ConfigureAwait(false);
        }
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