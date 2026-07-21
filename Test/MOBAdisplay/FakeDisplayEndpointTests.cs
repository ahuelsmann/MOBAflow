// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdisplay;

using Moba.Display.Protocol;
using Moba.Display.Transport;

[TestFixture]
[Category("Integration")]
internal sealed class FakeDisplayEndpointTests
{
    private static readonly DisplayRequestOptions SingleAttempt =
        new(1, TimeSpan.FromSeconds(10), TimeSpan.Zero);

    [Test]
    public async Task CompleteFrame_Should_PresentExactlyOnce_When_AllRegionsAreValid()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        using var client = new DisplayProtocolClient(endpoint);
        var frame = Enumerable.Range(0, 24).Select(value => (byte)value).ToArray();
        var frameCrc32 = DisplayPacketCodec.ComputeCrc32(frame);
        const uint frameId = 17;
        var begin = new BeginFramePayload(
            4,
            3,
            DisplayPixelFormat.Rgb565BigEndian,
            DisplayRotation.Degrees0,
            (uint)frame.Length,
            frameCrc32);

        // Act
        var beginOutcome = await client.SendRequestAsync(
            begin,
            endpoint.SessionId,
            frameId,
            SingleAttempt);
        var firstRegionOutcome = await client.SendRequestAsync(
            new FrameRegionPayload(0, 0, 4, 1, 0, frame.AsMemory(0, 8)),
            endpoint.SessionId,
            frameId,
            SingleAttempt);
        var incompleteOutcome = await client.SendRequestAsync(
            new CompleteFramePayload(frameCrc32),
            endpoint.SessionId,
            frameId,
            SingleAttempt);
        var secondRegionOutcome = await client.SendRequestAsync(
            new FrameRegionPayload(0, 1, 4, 2, 8, frame.AsMemory(8)),
            endpoint.SessionId,
            frameId,
            SingleAttempt);
        var completeOutcome = await client.SendRequestAsync(
            new CompleteFramePayload(frameCrc32),
            endpoint.SessionId,
            frameId,
            SingleAttempt);
        var duplicateOutcome = await client.SendRequestAsync(
            new CompleteFramePayload(frameCrc32),
            endpoint.SessionId,
            frameId,
            SingleAttempt);

        // Assert
        var incomplete = (ResultPayload)incompleteOutcome.Response!;
        var complete = (ResultPayload)completeOutcome.Response!;
        var duplicate = (ResultPayload)duplicateOutcome.Response!;
        Assert.Multiple(() =>
        {
            Assert.That(beginOutcome.IsSuccessful, Is.True);
            Assert.That(firstRegionOutcome.IsSuccessful, Is.True);
            Assert.That(secondRegionOutcome.IsSuccessful, Is.True);
            Assert.That(incomplete.ResultCode, Is.EqualTo(DisplayResultCode.Incomplete));
            Assert.That(incomplete.FirstMissingByteOffset, Is.EqualTo(8));
            Assert.That(incomplete.MissingByteCount, Is.EqualTo(16));
            Assert.That(complete.ResultCode, Is.EqualTo(DisplayResultCode.Ok));
            Assert.That(complete.Flags.HasFlag(DisplayResultFlags.Presented), Is.True);
            Assert.That(duplicate.Flags.HasFlag(DisplayResultFlags.Duplicate), Is.True);
            Assert.That(endpoint.PresentationCount, Is.EqualTo(1));
            Assert.That(endpoint.PresentedFrame.ToArray(), Is.EqualTo(frame));
        });
    }

    [Test]
    public async Task FrameRegion_Should_BeRejectedWithoutPresentation_When_MetadataIsInconsistent()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        using var client = new DisplayProtocolClient(endpoint);
        var frame = new byte[8];
        var frameCrc32 = DisplayPacketCodec.ComputeCrc32(frame);
        const uint frameId = 23;
        await client.SendRequestAsync(
            new BeginFramePayload(
                2,
                2,
                DisplayPixelFormat.Rgb565BigEndian,
                DisplayRotation.Degrees0,
                8,
                frameCrc32),
            endpoint.SessionId,
            frameId,
            SingleAttempt);

        // Act
        var outcome = await client.SendRequestAsync(
            new FrameRegionPayload(1, 0, 1, 1, 0, frame.AsMemory(0, 2)),
            endpoint.SessionId,
            frameId,
            SingleAttempt);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(((ResultPayload)outcome.Response!).ResultCode, Is.EqualTo(DisplayResultCode.Invalid));
            Assert.That(endpoint.PresentationCount, Is.Zero);
        });
    }

    [Test]
    public async Task Reboot_Should_InvalidatePriorSession()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        using var client = new DisplayProtocolClient(endpoint);
        var priorSession = endpoint.SessionId;

        // Act
        endpoint.Reboot();
        var outcome = await client.SendRequestAsync(
            new HealthRequestPayload(),
            priorSession,
            options: SingleAttempt);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(endpoint.SessionId, Is.Not.EqualTo(priorSession));
            Assert.That(outcome.IsSuccessful, Is.True);
            Assert.That(((ResultPayload)outcome.Response!).ResultCode, Is.EqualTo(DisplayResultCode.WrongSession));
        });
    }

    [Test]
    public async Task ReusedRequestId_Should_ReturnConflict_When_PayloadChanges()
    {
        // Arrange
        var endpoint = new FakeDisplayEndpoint();
        using (var firstClient = new DisplayProtocolClient(endpoint, new DisplayIdentifierSequence()))
        {
            await firstClient.SendRequestAsync(
                new HealthRequestPayload(),
                endpoint.SessionId,
                options: SingleAttempt);
        }

        using var secondClient = new DisplayProtocolClient(endpoint, new DisplayIdentifierSequence());

        // Act
        var outcome = await secondClient.SendRequestAsync(
            new ClearPayload(0),
            endpoint.SessionId,
            options: SingleAttempt);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(outcome.RequestId, Is.EqualTo(1));
            Assert.That(((ResultPayload)outcome.Response!).ResultCode, Is.EqualTo(DisplayResultCode.Conflict));
        });
    }
}