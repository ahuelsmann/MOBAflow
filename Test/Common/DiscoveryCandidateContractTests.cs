// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Configuration;
using Moba.Common.Discovery;

using System.Net;

[TestFixture]
internal sealed class DiscoveryCandidateContractTests
{
    [Test]
    public void QuickWindow_NearLowestHostDoesNotWrapToHighestHosts()
    {
        var candidates = RestApiDiscoveryCandidateBuilder.BuildQuickWindowCandidates(
            [IPAddress.Parse("192.168.10.1")], radius: 3);

        Assert.That(candidates.Select(address => address.ToString()),
            Is.EqualTo(new[] { "192.168.10.2", "192.168.10.3", "192.168.10.4" }));
    }

    [Test]
    public void QuickWindow_NearHighestHostDoesNotWrapToLowestHosts()
    {
        var candidates = RestApiDiscoveryCandidateBuilder.BuildQuickWindowCandidates(
            [IPAddress.Parse("192.168.10.254")], radius: 3);

        Assert.That(candidates.Select(address => address.ToString()),
            Is.EqualTo(new[] { "192.168.10.253", "192.168.10.252", "192.168.10.251" }));
    }

    [TestCase(int.MinValue)]
    [TestCase(-1)]
    [TestCase(0)]
    public void QuickWindow_RadiusBelowMinimumUsesOneNeighborOnEachSide(int radius)
    {
        var candidates = RestApiDiscoveryCandidateBuilder.BuildQuickWindowCandidates(
            [IPAddress.Parse("10.20.30.128")], radius);

        Assert.That(candidates.Select(address => address.ToString()),
            Is.EqualTo(new[] { "10.20.30.129", "10.20.30.127" }));
    }

    [TestCase(127)]
    [TestCase(128)]
    [TestCase(int.MaxValue)]
    public void QuickWindow_LargeRadiusStopsAtMaximumAndExcludesNetworkBroadcastAndSelf(int radius)
    {
        var candidates = RestApiDiscoveryCandidateBuilder.BuildQuickWindowCandidates(
            [IPAddress.Parse("10.20.30.128")], radius);

        Assert.That(candidates, Has.Count.EqualTo(253));
        Assert.That(candidates.Distinct().Count(), Is.EqualTo(253));
        Assert.That(candidates[0], Is.EqualTo(IPAddress.Parse("10.20.30.129")));
        Assert.That(candidates[^1], Is.EqualTo(IPAddress.Parse("10.20.30.1")));
        Assert.That(candidates, Does.Not.Contain(IPAddress.Parse("10.20.30.0")));
        Assert.That(candidates, Does.Not.Contain(IPAddress.Parse("10.20.30.255")));
        Assert.That(candidates, Does.Not.Contain(IPAddress.Parse("10.20.30.128")));
    }

    [Test]
    public void QuickWindow_DeduplicatesHostsWithoutCollapsingDifferentSubnets()
    {
        var candidates = RestApiDiscoveryCandidateBuilder.BuildQuickWindowCandidates(
            [IPAddress.Parse("10.168.30.50"), IPAddress.Parse("192.168.30.50"),
                IPAddress.Parse("10.169.30.50"), IPAddress.Parse("10.168.31.50"),
                IPAddress.Parse("10.168.30.50")], radius: 1);

        Assert.That(candidates.Select(address => address.ToString()), Is.EqualTo(new[]
        {
            "10.168.30.51", "10.168.30.49", "192.168.30.51", "192.168.30.49",
            "10.169.30.51", "10.169.30.49", "10.168.31.51", "10.168.31.49"
        }));
    }

    [TestCase("::1")]
    [TestCase("::ffff:10.20.30.50")]
    [TestCase("203.0.113.50")]
    public void QuickWindow_DoesNotGenerateHostsForNonPrivateIpv4(string local)
    {
        Assert.That(RestApiDiscoveryCandidateBuilder.BuildQuickWindowCandidates([IPAddress.Parse(local)]), Is.Empty);
    }

    [Test]
    public void QuickWindow_RejectsNullInput()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            RestApiDiscoveryCandidateBuilder.BuildQuickWindowCandidates(null!));
        Assert.That(exception!.ParamName, Is.EqualTo("localAddresses"));
    }

    [TestCase("10.1.2.1", "10.1.2.2", "10.1.2.254")]
    [TestCase("10.1.2.254", "10.1.2.1", "10.1.2.253")]
    public void AnchorSubnet_ListsAllOtherHostsInAscendingOrder(string anchor, string first, string last)
    {
        var candidates = RestApiDiscoveryCandidateBuilder.BuildSubnetFromAnchor(IPAddress.Parse(anchor));

        Assert.That(candidates, Has.Count.EqualTo(253));
        Assert.That(candidates.Distinct().Count(), Is.EqualTo(253));
        Assert.That(candidates[0], Is.EqualTo(IPAddress.Parse(first)));
        Assert.That(candidates[^1], Is.EqualTo(IPAddress.Parse(last)));
        Assert.That(candidates, Does.Not.Contain(IPAddress.Parse(anchor)));
        Assert.That(candidates, Does.Not.Contain(IPAddress.Parse("10.1.2.0")));
        Assert.That(candidates, Does.Not.Contain(IPAddress.Parse("10.1.2.255")));
    }

    [TestCase("::1")]
    [TestCase("203.0.113.50")]
    public void AnchorSubnet_RejectsNonPrivateIpv4(string anchor)
    {
        Assert.That(RestApiDiscoveryCandidateBuilder.BuildSubnetFromAnchor(IPAddress.Parse(anchor)), Is.Empty);
    }

    [Test]
    public void AnchorSubnet_RejectsNullInput()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            RestApiDiscoveryCandidateBuilder.BuildSubnetFromAnchor(null!));
        Assert.That(exception!.ParamName, Is.EqualTo("anchor"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t")]
    [TestCase("not-an-address")]
    [TestCase("192.168.0.79")]
    [TestCase(" 192.168.0.79 ")]
    public void SavedIp_MissingInvalidOrFactoryDefaultIsNotPrioritized(string? saved)
    {
        Assert.That(RestApiDiscoveryCandidateBuilder.ShouldProbeSavedIp(saved), Is.False);
    }

    [TestCase("10.20.30.40")]
    [TestCase(" 192.168.10.40 ")]
    public void SavedIp_ValidNonDefaultAddressIsAccepted(string saved)
    {
        Assert.That(RestApiDiscoveryCandidateBuilder.ShouldProbeSavedIp(saved), Is.True);
    }

    [Test]
    public void FullProbeOrder_RecentThenNearbyThenSavedWithFilteringAndDeduplication()
    {
        var settings = new RestApiSettings
        {
            RecentIpAddresses = [null!, "", " \t", "invalid", "::1", "203.0.113.1",
                " 10.20.30.90 ", "10.20.30.90"],
            CurrentIpAddress = " 10.20.30.200 "
        };
        var candidates = RestApiDiscoveryCandidateBuilder.BuildFullProbeOrder(settings,
            [IPAddress.IPv6Loopback, IPAddress.Parse("10.20.30.50")],
            [IPAddress.Parse("10.20.30.60"), IPAddress.Parse("10.20.30.40"),
                IPAddress.Parse("10.20.30.51"), IPAddress.Parse("10.20.30.90"),
                IPAddress.Parse("10.20.30.40"), IPAddress.IPv6Loopback, IPAddress.Parse("203.0.113.5")]);

        Assert.That(candidates.Select(address => address.ToString()), Is.EqualTo(new[]
        {
            "10.20.30.90", "10.20.30.51", "10.20.30.40", "10.20.30.60", "10.20.30.200"
        }));
    }

    [Test]
    public void FullProbeOrder_NullRecentHistoryStillIncludesSavedAddress()
    {
        var settings = new RestApiSettings { RecentIpAddresses = null!, CurrentIpAddress = "10.20.30.40" };

        var candidates = RestApiDiscoveryCandidateBuilder.BuildFullProbeOrder(settings, [], []);

        Assert.That(candidates, Is.EqualTo(new[] { IPAddress.Parse("10.20.30.40") }));
    }

    [TestCase("10.168.30.50", "192.168.30.50", "10.168.30.51")]
    [TestCase("10.20.30.50", "10.21.30.50", "10.20.30.51")]
    [TestCase("10.20.30.50", "10.20.31.50", "10.20.30.51")]
    public void FullProbeOrder_EachSubnetOctetMustMatchForProximity(string local, string otherSubnet, string near)
    {
        var candidates = RestApiDiscoveryCandidateBuilder.BuildFullProbeOrder(new RestApiSettings(),
            [IPAddress.Parse(local)], [IPAddress.Parse(otherSubnet), IPAddress.Parse(near)]);

        Assert.That(candidates.Select(address => address.ToString()), Is.EqualTo(new[] { near, otherSubnet }));
    }

    [Test]
    public void FullProbeOrder_UsesClosestOfMultipleLocalAddresses()
    {
        var candidates = RestApiDiscoveryCandidateBuilder.BuildFullProbeOrder(new RestApiSettings(),
            [IPAddress.Parse("10.20.30.50"), IPAddress.Parse("10.20.30.200")],
            [IPAddress.Parse("10.20.30.60"), IPAddress.Parse("10.20.30.198"), IPAddress.Parse("10.20.30.51")]);

        Assert.That(candidates.Select(address => address.ToString()),
            Is.EqualTo(new[] { "10.20.30.51", "10.20.30.198", "10.20.30.60" }));
    }

    [Test]
    public void FullProbeOrder_NoLocalIpv4UsesAscendingHostNumber()
    {
        var candidates = RestApiDiscoveryCandidateBuilder.BuildFullProbeOrder(new RestApiSettings(),
            [IPAddress.IPv6Loopback], [IPAddress.Parse("10.20.30.60"), IPAddress.Parse("10.20.30.40")]);

        Assert.That(candidates.Select(address => address.ToString()),
            Is.EqualTo(new[] { "10.20.30.40", "10.20.30.60" }));
    }

    [Test]
    public void FullProbeOrder_RejectsNullArguments([Values(0, 1, 2)] int argument)
    {
        var exception = Assert.Throws<ArgumentNullException>(() => RestApiDiscoveryCandidateBuilder.BuildFullProbeOrder(
            argument == 0 ? null! : new RestApiSettings(), argument == 1 ? null! : [], argument == 2 ? null! : []));
        Assert.That(exception!.ParamName, Is.EqualTo(new[] { "settings", "localAddresses", "subnetCandidates" }[argument]));
    }

    [Test]
    public void LocalSubnetProbeOrder_FiltersPublicAndIpv6InputsButPreservesRecentAndSaved()
    {
        var settings = new RestApiSettings { RecentIpAddresses = ["10.20.30.9"], CurrentIpAddress = "10.20.30.8" };

        var candidates = RestApiDiscoveryCandidateBuilder.BuildLocalSubnetProbeOrder(settings,
            [IPAddress.IPv6Loopback, IPAddress.Parse("203.0.113.50")]);

        Assert.That(candidates.Select(address => address.ToString()),
            Is.EqualTo(new[] { "10.20.30.9", "10.20.30.8" }));
    }

    [Test]
    public void LocalSubnetProbeOrder_RejectsNullArguments([Values(true, false)] bool nullSettings)
    {
        var exception = Assert.Throws<ArgumentNullException>(() => RestApiDiscoveryCandidateBuilder.BuildLocalSubnetProbeOrder(
            nullSettings ? null! : new RestApiSettings(), nullSettings ? [] : null!));
        Assert.That(exception!.ParamName, Is.EqualTo(nullSettings ? "settings" : "localAddresses"));
    }

    [TestCase("10.168.30.40", "192.168.30.41")]
    [TestCase("10.20.30.40", "10.21.30.41")]
    [TestCase("10.20.30.40", "10.20.31.41")]
    public void Subnets_DifferentOctetsDoNotCollideWhenExcludingLocalHosts(string first, string second)
    {
        var candidates = SubnetCandidateBuilder.BuildCandidates(
            [IPAddress.Parse(first), IPAddress.Parse(second), IPAddress.Parse(first)]);

        Assert.That(candidates, Has.Count.EqualTo(506));
        Assert.That(candidates.Distinct().Count(), Is.EqualTo(506));
        Assert.That(candidates, Does.Not.Contain(IPAddress.Parse(first)));
        Assert.That(candidates, Does.Not.Contain(IPAddress.Parse(second)));
    }

    [Test]
    public void Subnets_EmptyAndIpv6OnlyInputsProduceNoCandidates([Values(true, false)] bool ipv6Only)
    {
        Assert.That(SubnetCandidateBuilder.BuildCandidates(ipv6Only ? [IPAddress.IPv6Loopback] : []), Is.Empty);
    }

    [Test]
    public void Subnets_RejectNullInput()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => SubnetCandidateBuilder.BuildCandidates(null!));
        Assert.That(exception!.ParamName, Is.EqualTo("localAddresses"));
    }

    [TestCase("10.255.255.254", true)]
    [TestCase("172.15.255.254", false)]
    [TestCase("172.16.0.0", true)]
    [TestCase("172.31.255.255", true)]
    [TestCase("172.33.0.1", false)]
    [TestCase("171.16.0.1", false)]
    [TestCase("173.16.0.1", false)]
    [TestCase("192.167.1.1", false)]
    [TestCase("192.169.1.1", false)]
    [TestCase("193.168.1.1", false)]
    [TestCase("11.168.1.1", false)]
    [TestCase("127.0.0.1", false)]
    [TestCase("::ffff:10.20.30.50", false)]
    public void PrivateIpv4_ChecksRangeBoundariesAndAddressFamily(string address, bool expected)
    {
        Assert.That(SubnetCandidateBuilder.IsPrivateIPv4(IPAddress.Parse(address)), Is.EqualTo(expected));
    }

    [Test]
    public void PrivateIpv4_RejectsNullAddress()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => SubnetCandidateBuilder.IsPrivateIPv4(null!));
        Assert.That(exception!.ParamName, Is.EqualTo("address"));
    }
}
