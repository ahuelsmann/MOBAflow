// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Network;

/// <summary>
/// Tests for the real <see cref="UdpWrapper"/> lifecycle and guard rails.
/// Network I/O is avoided; these verify connection state, disposal and send preconditions.
/// </summary>
[TestFixture]
internal sealed class UdpWrapperTests
{
    [Test]
    public void IsConnected_InitiallyFalse()
    {
        using var wrapper = new UdpWrapper(NullLogger<UdpWrapper>.Instance);

        Assert.That(wrapper.IsConnected, Is.False);
    }

    [Test]
    public void SendAsync_WhenNotConnected_ThrowsUdpNotConnectedException()
    {
        using var wrapper = new UdpWrapper(NullLogger<UdpWrapper>.Instance);

        Assert.ThrowsAsync<UdpNotConnectedException>(() =>
            wrapper.SendAsync([0x04, 0x00, 0x85, 0x00]));
    }

    [Test]
    public void Dispose_SetsIsConnectedFalse()
    {
        var wrapper = new UdpWrapper(NullLogger<UdpWrapper>.Instance);

        wrapper.Dispose();

        Assert.That(wrapper.IsConnected, Is.False);
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var wrapper = new UdpWrapper(NullLogger<UdpWrapper>.Instance);

        wrapper.Dispose();
        wrapper.Dispose();

        Assert.That(wrapper.IsConnected, Is.False);
    }

    [Test]
    public void ConnectAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var wrapper = new UdpWrapper(NullLogger<UdpWrapper>.Instance);
        wrapper.Dispose();

        Assert.ThrowsAsync<ObjectDisposedException>(() =>
            wrapper.ConnectAsync(System.Net.IPAddress.Loopback));
    }
}