// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Discovery;

using System.Net;

/// <summary>
/// Tests subnet candidate generation for network discovery.
/// </summary>
[TestFixture]
internal class SubnetCandidateBuilderTests
{
    [Test]
    public void BuildCandidates_ReturnsHostsForEachDistinctSubnet()
    {
        var candidates = SubnetCandidateBuilder.BuildCandidates(
            [
                IPAddress.Parse("192.168.0.25"),
                IPAddress.Parse("10.0.0.8")
            ]);

        Assert.Multiple(() =>
        {
            Assert.That(candidates, Has.Count.EqualTo(506));
            Assert.That(candidates, Does.Contain(IPAddress.Parse("192.168.0.1")));
            Assert.That(candidates, Does.Contain(IPAddress.Parse("10.0.0.254")));
            Assert.That(candidates, Does.Not.Contain(IPAddress.Parse("192.168.0.25")));
            Assert.That(candidates, Does.Not.Contain(IPAddress.Parse("10.0.0.8")));
        });
    }

    [Test]
    public void BuildCandidates_DeduplicatesRepeatedSubnetAddresses()
    {
        var candidates = SubnetCandidateBuilder.BuildCandidates(
            [
                IPAddress.Parse("192.168.0.25"),
                IPAddress.Parse("192.168.0.30")
            ]);

        Assert.Multiple(() =>
        {
            Assert.That(candidates, Has.Count.EqualTo(252));
            Assert.That(candidates, Does.Not.Contain(IPAddress.Parse("192.168.0.25")));
            Assert.That(candidates, Does.Not.Contain(IPAddress.Parse("192.168.0.30")));
        });
    }

    [TestCase("10.0.0.1", true)]
    [TestCase("172.16.0.1", true)]
    [TestCase("172.31.255.254", true)]
    [TestCase("172.32.0.1", false)]
    [TestCase("192.168.1.20", true)]
    [TestCase("8.8.8.8", false)]
    public void IsPrivateIPv4_ReturnsExpectedResult(string address, bool expected)
    {
        var isPrivate = SubnetCandidateBuilder.IsPrivateIPv4(IPAddress.Parse(address));

        Assert.That(isPrivate, Is.EqualTo(expected));
    }
}