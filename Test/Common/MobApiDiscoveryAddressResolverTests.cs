// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Discovery;

using System.Net;

/// <summary>
/// Unit tests for MOBApi UDP discovery address selection.
/// </summary>
[TestFixture]
internal sealed class MobApiDiscoveryAddressResolverTests
{
    [Test]
    public void GetLocalIpAddressForRemote_ReturnsFallback_WhenRemoteIsNull()
    {
        var result = MobApiDiscoveryAddressResolver.GetLocalIpAddressForRemote(null);
        Assert.That(result, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void GetLocalIpAddressInSubnet_ReturnsNull_ForNonIpv4Remote()
    {
        var remote = IPAddress.IPv6Loopback;
        Assert.That(MobApiDiscoveryAddressResolver.GetLocalIpAddressInSubnet(remote), Is.Null);
    }

    [Test]
    public void GetLocalIpAddress_PrefersPrivateRange_WhenAvailable()
    {
        var result = MobApiDiscoveryAddressResolver.GetLocalIpAddress();
        Assert.That(IPAddress.TryParse(result, out var parsed), Is.True);
        Assert.That(parsed!.AddressFamily, Is.EqualTo(System.Net.Sockets.AddressFamily.InterNetwork));
    }
}
