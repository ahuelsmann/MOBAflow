// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Test.Mocks;
using System.Net;

/// <summary>
/// Tests for IUdpClientWrapper interface implementations.
/// Uses FakeUdpClientWrapper to verify contract compliance.
/// </summary>
[TestFixture]
public class UdpClientWrapperTests
{
    private FakeUdpClientWrapper _wrapper = null!;

    [SetUp]
    public void SetUp()
    {
        _wrapper = new FakeUdpClientWrapper();
    }

    [TearDown]
    public void TearDown()
    {
        _wrapper.Dispose();
    }

    [Test]
    public void IsConnected_InitiallyFalse()
    {
        Assert.That(_wrapper.IsConnected, Is.False);
    }

    [Test]
    public async Task ConnectAsync_SetsIsConnectedTrue()
    {
        await _wrapper.ConnectAsync(IPAddress.Loopback);

        Assert.That(_wrapper.IsConnected, Is.True);
    }

    [Test]
    public async Task ConnectAsync_WithCustomPort_Connects()
    {
        await _wrapper.ConnectAsync(IPAddress.Parse("192.168.0.111"));

        Assert.That(_wrapper.IsConnected, Is.True);
    }

    [Test]
    public async Task StopAsync_SetsIsConnectedFalse()
    {
        await _wrapper.ConnectAsync(IPAddress.Loopback);
        Assert.That(_wrapper.IsConnected, Is.True);

        await _wrapper.StopAsync();

        Assert.That(_wrapper.IsConnected, Is.False);
    }

    [Test]
    public async Task SendAsync_StoresPayload()
    {
        var testData = new byte[] { 0x04, 0x00, 0x85, 0x00 };

        await _wrapper.SendAsync(testData);

        Assert.That(_wrapper.SentPayloads, Has.Count.EqualTo(1));
        Assert.That(_wrapper.SentPayloads[0], Is.EqualTo(testData));
    }

    [Test]
    public async Task SendAsync_MultiplePayloads_AllStored()
    {
        var payload1 = new byte[] { 0x04, 0x00, 0x85, 0x00 };
        var payload2 = new byte[] { 0x08, 0x00, 0x50, 0x00 };

        await _wrapper.SendAsync(payload1);
        await _wrapper.SendAsync(payload2);

        Assert.That(_wrapper.SentPayloads, Has.Count.EqualTo(2));
        Assert.That(_wrapper.SentPayloads[0], Is.EqualTo(payload1));
        Assert.That(_wrapper.SentPayloads[1], Is.EqualTo(payload2));
    }

    [Test]
    public void Received_EventRaised_WhenRaiseReceivedCalled()
    {
        var receivedData = Array.Empty<byte>();
        IPEndPoint? receivedEndpoint = null;

        _wrapper.Received += (_, args) =>
        {
            receivedData = args.Buffer;
            receivedEndpoint = args.RemoteEndPoint;
        };

        var testData = new byte[] { 0x0A, 0x00, 0x80, 0x00, 0x05, 0x00, 0x00, 0x00, 0x01, 0x00 };
        var endpoint = new IPEndPoint(IPAddress.Parse("192.168.0.111"), 21105);

        _wrapper.RaiseReceived(testData, endpoint);

        Assert.That(receivedData, Is.EqualTo(testData));
        Assert.That(receivedEndpoint, Is.EqualTo(endpoint));
    }

    [Test]
    public void Received_WithDefaultEndpoint_UsesLoopback()
    {
        IPEndPoint? receivedEndpoint = null;

        _wrapper.Received += (_, args) => receivedEndpoint = args.RemoteEndPoint;

        _wrapper.RaiseReceived([0x04, 0x00, 0x85, 0x00]);

        Assert.That(receivedEndpoint, Is.Not.Null);
        Assert.That(receivedEndpoint!.Address, Is.EqualTo(IPAddress.Loopback));
        Assert.That(receivedEndpoint.Port, Is.EqualTo(21105));
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        _wrapper.Dispose();
        _wrapper.Dispose();

        // Should not throw
        Assert.Pass("Dispose called multiple times without exception");
    }

    [Test]
    public async Task ConnectAsync_CanReconnectAfterStop()
    {
        await _wrapper.ConnectAsync(IPAddress.Loopback);
        await _wrapper.StopAsync();
        Assert.That(_wrapper.IsConnected, Is.False);

        await _wrapper.ConnectAsync(IPAddress.Loopback);

        Assert.That(_wrapper.IsConnected, Is.True);
    }
}
