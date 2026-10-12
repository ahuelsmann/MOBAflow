// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Discovery;
using Moq;

using System.Net;
using System.Net.NetworkInformation;

/// <summary>
/// Unit tests for MOBApi UDP discovery address selection.
/// </summary>
[TestFixture]
[Category("Unit")]
internal sealed class MobApiDiscoveryAddressResolverTests
{
    private static readonly string[] ExpectedPrivateAddresses = ["10.20.30.40", "172.16.0.1", "192.168.10.2"];

    [TestCase(int.MinValue)]
    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(65536)]
    [TestCase(int.MaxValue)]
    public void UdpResponder_InvalidPortIsRejectedWithoutOpeningSockets(int port)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new MobApiUdpDiscoveryResponder(port));

        Assert.That(exception!.ParamName, Is.EqualTo("restApiPort"));
    }

    [TestCase(1)]
    [TestCase(5001)]
    [TestCase(65535)]
    public void UdpResponder_ValidPortIsAcceptedWithoutStartingListener(int port)
    {
        using var responder = new MobApiUdpDiscoveryResponder(port);

        Assert.DoesNotThrow(responder.Stop);
    }

    [Test]
    public void PrivateAddressSelection_FiltersAndDeduplicatesWithoutChangingInput()
    {
        var addresses = ParseAddresses(
            "::1|127.0.0.1|::ffff:10.20.30.40|198.51.100.1|10.20.30.40|172.15.255.255|172.16.0.1|172.32.0.1|192.168.10.2|10.20.30.40").ToList();
        var original = addresses.ToArray();

        var result = LanIpv4AddressHelper.SelectPrivateIpv4Addresses(addresses);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Select(address => address.ToString()), Is.EqualTo(ExpectedPrivateAddresses));
            Assert.That(addresses, Is.EqualTo(original));
        }
    }

    [Test]
    public void PrivateAddressSelection_EmptyInputProducesNoCandidates()
    {
        Assert.That(LanIpv4AddressHelper.SelectPrivateIpv4Addresses([]), Is.Empty);
    }

    [Test]
    public void PrivateAddressSelection_RejectsNullInput()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => LanIpv4AddressHelper.SelectPrivateIpv4Addresses(null!));

        Assert.That(exception!.ParamName, Is.EqualTo("addresses"));
    }

    [Test]
    public void InterfaceSelection_AcceptsActivePhysicalAdapters(
        [Values(NetworkInterfaceType.Ethernet, NetworkInterfaceType.Wireless80211)] NetworkInterfaceType type,
        [Values] bool isAndroid)
    {
        var network = CreateInterface(OperationalStatus.Up, type, "Physical adapter");

        Assert.That(LanIpv4AddressHelper.IsDiscoveryCapableInterface(network, isAndroid), Is.True);
    }

    [Test]
    public void InterfaceSelection_RejectsInactiveAdapters(
        [Values(OperationalStatus.Down, OperationalStatus.Dormant, OperationalStatus.Unknown)] OperationalStatus status,
        [Values] bool isAndroid)
    {
        var network = CreateInterface(status, NetworkInterfaceType.Ethernet, "Physical adapter");

        Assert.That(LanIpv4AddressHelper.IsDiscoveryCapableInterface(network, isAndroid), Is.False);
    }

    [Test]
    public void InterfaceSelection_RejectsLoopbackOnEveryPlatform([Values] bool isAndroid)
    {
        var network = CreateInterface(OperationalStatus.Up, NetworkInterfaceType.Loopback, "Local adapter");

        Assert.That(LanIpv4AddressHelper.IsDiscoveryCapableInterface(network, isAndroid), Is.False);
    }

    [Test]
    public void InterfaceSelection_RejectsVpnNamesCaseInsensitively(
        [Values("Corporate VPN", "TUN0", "ppp-link", "vpN adapter")] string name, [Values] bool isAndroid)
    {
        var network = CreateInterface(OperationalStatus.Up, NetworkInterfaceType.Ethernet, name);

        Assert.That(LanIpv4AddressHelper.IsDiscoveryCapableInterface(network, isAndroid), Is.False);
    }

    [Test]
    public void InterfaceSelection_AcceptsPhysicalAdapterWithoutName([Values(null, "")] string? name, [Values] bool isAndroid)
    {
        var network = CreateInterface(OperationalStatus.Up, NetworkInterfaceType.Ethernet, name);

        Assert.That(LanIpv4AddressHelper.IsDiscoveryCapableInterface(network, isAndroid), Is.True);
    }

    [Test]
    public void InterfaceSelection_UsesAndroidFallbackForOtherActiveTypes(
        [Values(NetworkInterfaceType.Unknown, NetworkInterfaceType.Ppp, NetworkInterfaceType.Tunnel)] NetworkInterfaceType type,
        [Values] bool isAndroid)
    {
        var network = CreateInterface(OperationalStatus.Up, type, "Mobile adapter");

        Assert.That(LanIpv4AddressHelper.IsDiscoveryCapableInterface(network, isAndroid), Is.EqualTo(isAndroid));
    }

    [Test]
    public void InterfaceSelection_RejectsNullInterface()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => LanIpv4AddressHelper.IsDiscoveryCapableInterface(null!, false));

        Assert.That(exception!.ParamName, Is.EqualTo("ni"));
    }

    [TestCase("2001:db8::1|10.20.30.40|172.16.0.1|192.168.10.2", "192.168.10.2")]
    [TestCase("172.16.0.1|10.20.30.40", "10.20.30.40")]
    [TestCase("192.168.10.3|192.168.10.2", "192.168.10.3")]
    [TestCase("172.16.0.2|172.16.0.1", "172.16.0.2")]
    [TestCase("198.51.100.1|2001:db8::1|::ffff:192.168.10.2", "127.0.0.1")]
    [TestCase("", "127.0.0.1")]
    public void AddressSelection_PreservesRangePriorityAndFirstMatch(string addresses, string expected)
    {
        var result = MobApiDiscoveryAddressResolver.SelectLocalIpAddress(ParseAddresses(addresses));

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void AddressSelection_RejectsNullInput()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => MobApiDiscoveryAddressResolver.SelectLocalIpAddress(null!));

        Assert.That(exception!.ParamName, Is.EqualTo("localAddresses"));
    }

    [TestCase("10.20.30.50", "2001:db8::1|10.20.30.60", "10.20.30.60")]
    [TestCase("10.20.30.50", "10.20.40.60", "10.20.40.60")]
    [TestCase("10.20.30.50", "11.20.30.60|10.21.30.60", null)]
    [TestCase("192.168.20.1", "192.168.99.1|192.168.20.2", "192.168.99.1")]
    [TestCase("10.20.30.50", "2001:db8::1|::ffff:10.20.30.60", null)]
    [TestCase("2001:db8::1", "10.20.30.60", null)]
    [TestCase("10.20.30.50", "", null)]
    public void SubnetAddressSelection_UsesFirstIpv4MatchWithinExistingSubnetRules(string remote, string addresses, string? expected)
    {
        var result = MobApiDiscoveryAddressResolver.SelectLocalIpAddressInSubnet(IPAddress.Parse(remote), ParseAddresses(addresses));

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void SubnetAddressSelection_RejectsNullArguments([Values] bool missingRemote)
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            MobApiDiscoveryAddressResolver.SelectLocalIpAddressInSubnet(missingRemote ? null! : IPAddress.Loopback,
                missingRemote ? [] : null!));

        Assert.That(exception!.ParamName, Is.EqualTo(missingRemote ? "remoteIp" : "localAddresses"));
    }

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

    private static IEnumerable<IPAddress> ParseAddresses(string input) =>
        input.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(IPAddress.Parse);

    private static NetworkInterface CreateInterface(OperationalStatus status, NetworkInterfaceType type, string? name)
    {
        var network = new Mock<NetworkInterface>();
        network.SetupGet(adapter => adapter.OperationalStatus).Returns(status);
        network.SetupGet(adapter => adapter.NetworkInterfaceType).Returns(type);
        network.SetupGet(adapter => adapter.Name).Returns(name!);
        return network.Object;
    }
}
