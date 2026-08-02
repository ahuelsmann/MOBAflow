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
    public async Task ConnectedClientQueriesHealthAndPresentsStandardPattern()
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
    public async Task RebootInvalidatesSessionAndBlocksFurtherCommands()
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
    public async Task RebootDuringFrameSendReturnsWrongSessionAndDropsCapabilities()
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
    public async Task OptionalCommandsUseNegotiatedSession()
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
}
