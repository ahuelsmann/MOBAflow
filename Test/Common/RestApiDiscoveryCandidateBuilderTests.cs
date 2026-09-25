// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Configuration;
using Moba.Common.Discovery;

using System.Net;

[TestFixture]
internal sealed class RestApiDiscoveryCandidateBuilderTests
{
    [Test]
    public void BuildFullProbeOrder_SkipsLegacyFactoryDefault_AndPrefersRecentBeforeSubnet()
    {
        var settings = new RestApiSettings
        {
            CurrentIpAddress = RestApiDiscoveryCandidateBuilder.LegacyFactoryDefaultIp,
            RecentIpAddresses = ["192.168.0.34"],
        };

        var local = new[] { IPAddress.Parse("192.168.0.50") };
        var subnet = new[]
        {
            IPAddress.Parse("192.168.0.33"),
            IPAddress.Parse("192.168.0.34"),
            IPAddress.Parse("192.168.0.51"),
        };

        var order = RestApiDiscoveryCandidateBuilder.BuildFullProbeOrder(settings, local, subnet);

        Assert.Multiple(() =>
        {
            Assert.That(order[0].ToString(), Is.EqualTo("192.168.0.34"));
            Assert.That(order.Select(ip => ip.ToString()), Does.Not.Contain(RestApiDiscoveryCandidateBuilder.LegacyFactoryDefaultIp));
            Assert.That(RestApiDiscoveryCandidateBuilder.ShouldProbeSavedIp(settings.CurrentIpAddress), Is.False);
        });
    }

    [Test]
    public void BuildQuickWindowCandidates_IncludesNeighborsAroundLocalIp()
    {
        var local = new[] { IPAddress.Parse("192.168.0.50") };

        var quick = RestApiDiscoveryCandidateBuilder.BuildQuickWindowCandidates(local, radius: 2);

        Assert.Multiple(() =>
        {
            Assert.That(quick.Select(ip => ip.ToString()), Does.Contain("192.168.0.49"));
            Assert.That(quick.Select(ip => ip.ToString()), Does.Contain("192.168.0.51"));
            Assert.That(quick.Select(ip => ip.ToString()), Does.Not.Contain("192.168.0.50"));
        });
    }

    [Test]
    public void BuildLocalSubnetProbeOrder_IncludesPcOnPhoneSubnet()
    {
        var settings = new RestApiSettings();
        var phoneAddresses = new[] { IPAddress.Parse("192.168.0.35") };

        var candidates = RestApiDiscoveryCandidateBuilder.BuildLocalSubnetProbeOrder(
            settings,
            phoneAddresses);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(candidates, Does.Contain(IPAddress.Parse("192.168.0.27")));
            Assert.That(candidates, Does.Not.Contain(IPAddress.Parse("192.168.0.35")));
            Assert.That(candidates, Has.Count.EqualTo(253));
        }
    }

    [Test]
    public void BuildLocalSubnetProbeOrder_DoesNotScanPublicSubnets()
    {
        var settings = new RestApiSettings();
        var localAddresses = new[] { IPAddress.Parse("203.0.113.20") };

        var candidates = RestApiDiscoveryCandidateBuilder.BuildLocalSubnetProbeOrder(
            settings,
            localAddresses);

        Assert.That(candidates, Is.Empty);
    }

    [Test]
    public void BuildSubnetFromAnchor_IncludesPcOnSameSubnetAsZ21()
    {
        var anchor = IPAddress.Parse("192.168.0.111");
        var subnet = RestApiDiscoveryCandidateBuilder.BuildSubnetFromAnchor(anchor);

        Assert.That(subnet.Select(ip => ip.ToString()), Does.Contain("192.168.0.34"));
        Assert.That(subnet.Select(ip => ip.ToString()), Does.Not.Contain("192.168.0.111"));
    }

    [Test]
    public void BuildFullProbeOrder_WithPhoneNearPc_PrefersPcOverHostsNearZ21()
    {
        var settings = new RestApiSettings();
        var phone = new[] { IPAddress.Parse("192.168.0.50") };
        var subnet = new[]
        {
            IPAddress.Parse("192.168.0.34"),
            IPAddress.Parse("192.168.0.110"),
            IPAddress.Parse("192.168.0.112"),
        };

        var order = RestApiDiscoveryCandidateBuilder.BuildFullProbeOrder(settings, phone, subnet);

        Assert.That(order[0].ToString(), Is.EqualTo("192.168.0.34"));
    }

    [Test]
    public void MobApiHealthProbe_AcceptsMobApiPayload()
    {
        const string body = """{"service":"MOBAflow MOBApi","status":"healthy","version":"1.0.0"}""";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(MobApiHealthProbe.IsHealthyResponse(body), Is.True);
            Assert.That(MobApiHealthProbe.IsHealthyResponse("""{"status":"ok"}"""), Is.False);
            Assert.That(MobApiHealthProbe.IsHealthyResponse("""{"service":"other","status":"healthy"}"""), Is.False);
            Assert.That(MobApiHealthProbe.IsHealthyResponse("""{"status":1}"""), Is.False);
            Assert.That(MobApiHealthProbe.IsHealthyResponse("""{"status":"healthy","other":true}"""), Is.False);
            Assert.That(MobApiHealthProbe.IsHealthyResponse("not-json"), Is.False);
        }
    }

    [TestCase("{\"status\":\"healthy\"}")]
    [TestCase("{\"service\":\"MOBAflow MOBApi\",\"status\":\"unhealthy\"}")]
    [TestCase("{\"service\":\"Other MOBAflow Service\",\"status\":\"healthy\"}")]
    [TestCase("{\"service\":42,\"status\":\"healthy\"}")]
    [TestCase("[\"MOBAflow MOBApi\",\"healthy\"]")]
    [TestCase("MOBAflow healthy")]
    public void MobApiHealthProbe_RejectsMissingOrInvalidServiceIdentity(string body)
    {
        Assert.That(MobApiHealthProbe.IsHealthyResponse(body), Is.False);
    }
}
