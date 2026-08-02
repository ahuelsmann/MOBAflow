// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdisplay;

using Moba.Display.Protocol;
using Moba.Display.Transport;
using System.Net;

[TestFixture]
[Category("Integration")]
internal sealed class UdpDisplayDeviceClientTests
{
    [Test]
    public async Task ConnectAsync_Should_EnableHealthAndStandardPattern_WhenNegotiationSucceeds()
    {
        var endpointTransport = new FakeDisplayEndpoint(width: 5, height: 4);
        using var client = new UdpDisplayDeviceClient(_ => endpointTransport);
        var endpoint = new DisplayEndpoint(IPAddress.Loopback, 4210);

        var negotiation = await client.ConnectAsync(endpoint).ConfigureAwait(false);
        var health = await client.QueryHealthAsync().ConfigureAwait(false);
        var pattern = await client.SendStandardTestPatternAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(negotiation.IsSuccessful, Is.True);
            Assert.That(negotiation.Capabilities!.Width, Is.EqualTo(5));
            Assert.That(negotiation.Capabilities.Height, Is.EqualTo(4));
            Assert.That(health.IsSuccessful, Is.True);
            Assert.That(health.Health!.Value.HealthState, Is.EqualTo(DisplayHealthState.Ready));
            Assert.That(pattern.IsSuccessful, Is.True);
            Assert.That(endpointTransport.PresentationCount, Is.EqualTo(1));
            Assert.That(
                endpointTransport.PresentedFrame.ToArray(),
                Is.EqualTo(DisplayConformancePattern.CreateRgb565(5, 4)));
        }
    }

    [Test]
    public async Task QueryHealthAsync_Should_InvalidateSession_WhenDeviceReboots()
    {
        var endpointTransport = new FakeDisplayEndpoint();
        using var client = new UdpDisplayDeviceClient(_ => endpointTransport);
        await client.ConnectAsync(new DisplayEndpoint(IPAddress.Loopback, 4210)).ConfigureAwait(false);

        endpointTransport.Reboot();
        var health = await client.QueryHealthAsync().ConfigureAwait(false);
        var pattern = await client.SendStandardTestPatternAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(health.ResultCode, Is.EqualTo(DisplayResultCode.WrongSession));
            Assert.That(pattern.IsSuccessful, Is.False);
            Assert.That(pattern.Diagnostic, Does.Contain("No negotiated display session"));
            Assert.That(endpointTransport.PresentationCount, Is.Zero);
        }
    }

    [Test]
    public async Task SendStandardTestPatternAsync_Should_DropCapabilities_WhenDeviceReboots()
    {
        var endpointTransport = new FakeDisplayEndpoint();
        using var client = new UdpDisplayDeviceClient(_ => endpointTransport);
        await client.ConnectAsync(new DisplayEndpoint(IPAddress.Loopback, 4210)).ConfigureAwait(false);
        endpointTransport.Reboot();

        var firstPattern = await client.SendStandardTestPatternAsync().ConfigureAwait(false);
        var secondPattern = await client.SendStandardTestPatternAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstPattern.ResultCode, Is.EqualTo(DisplayResultCode.WrongSession));
            Assert.That(secondPattern.IsSuccessful, Is.False);
            Assert.That(secondPattern.Diagnostic, Does.Contain("No negotiated display session"));
            Assert.That(endpointTransport.PresentationCount, Is.Zero);
        }
    }

    [Test]
    public async Task SetBrightnessAsync_Should_UseNegotiatedSession_WhenCapabilityIsAdvertised()
    {
        var endpointTransport = new FakeDisplayEndpoint();
        using var client = new UdpDisplayDeviceClient(_ => endpointTransport);
        await client.ConnectAsync(new DisplayEndpoint(IPAddress.Loopback, 4210)).ConfigureAwait(false);

        var builtInPattern = await client.RenderBuiltInTestPatternAsync().ConfigureAwait(false);
        var brightness = await client.SetBrightnessAsync(60).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(builtInPattern.IsSuccessful, Is.True);
            Assert.That(brightness.IsSuccessful, Is.True);
            Assert.That(
                endpointTransport.ReceivedPackets.Select(packet => packet.Header.MessageType),
                Does.Contain(DisplayMessageType.RenderTestPattern));
            Assert.That(
                endpointTransport.ReceivedPackets.Select(packet => packet.Header.MessageType),
                Does.Contain(DisplayMessageType.SetBrightness));
        }
    }

    [Test]
    public async Task ConnectAsync_Should_RejectCapabilities_WhenVersionIsOutsideOfferedRange()
    {
        var endpointTransport = new FakeDisplayEndpoint(
            selectedVersion: new DisplayProtocolVersion(1, 1));
        using var client = new UdpDisplayDeviceClient(_ => endpointTransport);

        var negotiation = await client.ConnectAsync(
            new DisplayEndpoint(IPAddress.Loopback, 4210)).ConfigureAwait(false);
        var health = await client.QueryHealthAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(negotiation.IsSuccessful, Is.False);
            Assert.That(negotiation.RequestFailure, Is.EqualTo(DisplayRequestFailure.InvalidPayload));
            Assert.That(negotiation.Diagnostic, Does.Contain("selected protocol version"));
            Assert.That(health.IsSuccessful, Is.False);
        }
    }

    [Test]
    public async Task ConnectAsync_Should_RejectResponse_WhenEnvelopeVersionDoesNotMatchRequest()
    {
        var endpointTransport = new FakeDisplayEndpoint();
        endpointTransport.UseProtocolVersionForNextResponse(new DisplayProtocolVersion(9, 0));
        using var client = new UdpDisplayDeviceClient(_ => endpointTransport);

        var negotiation = await client.ConnectAsync(
            new DisplayEndpoint(IPAddress.Loopback, 4210)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(negotiation.IsSuccessful, Is.False);
            Assert.That(negotiation.RequestFailure, Is.EqualTo(DisplayRequestFailure.InvalidDatagram));
            Assert.That(negotiation.Diagnostic, Does.Contain("response protocol version"));
        }
    }

    [Test]
    public async Task SendStandardTestPatternAsync_Should_BlockPattern_WhenFrameExceedsSafetyLimit()
    {
        var endpointTransport = new FakeDisplayEndpoint(
            width: ushort.MaxValue,
            height: ushort.MaxValue);
        using var client = new UdpDisplayDeviceClient(_ => endpointTransport);

        var negotiation = await client.ConnectAsync(
            new DisplayEndpoint(IPAddress.Loopback, 4210)).ConfigureAwait(false);
        var health = await client.QueryHealthAsync().ConfigureAwait(false);
        var pattern = await client.SendStandardTestPatternAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(negotiation.IsSuccessful, Is.True);
            Assert.That(health.IsSuccessful, Is.True);
            Assert.That(pattern.IsSuccessful, Is.False);
            Assert.That(pattern.Diagnostic, Does.Contain("host safety limit"));
            Assert.That(endpointTransport.PresentationCount, Is.Zero);
        }
    }
}
