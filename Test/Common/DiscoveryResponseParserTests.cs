// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Discovery;

/// <summary>
/// Tests for discovery response parsing so protocol changes are caught by tests.
/// </summary>
[TestFixture]
internal class DiscoveryResponseParserTests
{
    [Test]
    public void TryParse_Valid_response_returns_true_and_ip_port()
    {
        var success = DiscoveryResponseParser.TryParse("MOBAFLOW_REST_API|192.168.0.100|5001", out var ip, out var port);

        Assert.That(success, Is.True);
        Assert.That(ip, Is.EqualTo("192.168.0.100"));
        Assert.That(port, Is.EqualTo(5001));
    }

    [Test]
    public void TryParse_With_trailing_null_and_spaces_parses()
    {
        var success = DiscoveryResponseParser.TryParse("MOBAFLOW_REST_API|10.0.0.1|8080 \0  ", out var ip, out var port);

        Assert.That(success, Is.True);
        Assert.That(ip, Is.EqualTo("10.0.0.1"));
        Assert.That(port, Is.EqualTo(8080));
    }

    [Test]
    public void TryParse_Empty_returns_false()
    {
        var success = DiscoveryResponseParser.TryParse("", out var ip, out var port);

        Assert.That(success, Is.False);
        Assert.That(ip, Is.Null);
        Assert.That(port, Is.Null);
    }

    [Test]
    public void TryParse_Null_returns_false()
    {
        var success = DiscoveryResponseParser.TryParse(null, out var ip, out var port);

        Assert.That(success, Is.False);
        Assert.That(ip, Is.Null);
        Assert.That(port, Is.Null);
    }

    [Test]
    public void TryParse_Wrong_prefix_returns_false()
    {
        var success = DiscoveryResponseParser.TryParse("OTHER_PREFIX|192.168.0.1|5001", out var ip, out var port);

        Assert.That(success, Is.False);
        Assert.That(ip, Is.Null);
        Assert.That(port, Is.Null);
    }

    [Test]
    public void TryParse_Too_few_parts_returns_false()
    {
        var success = DiscoveryResponseParser.TryParse("MOBAFLOW_REST_API|192.168.0.1", out var ip, out var port);

        Assert.That(success, Is.False);
        Assert.That(ip, Is.Null);
        Assert.That(port, Is.Null);
    }

    [Test]
    public void TryParse_Invalid_port_returns_false()
    {
        var success = DiscoveryResponseParser.TryParse("MOBAFLOW_REST_API|192.168.0.1|abc", out var ip, out var port);

        Assert.That(success, Is.False);
        Assert.That(ip, Is.Null);
        Assert.That(port, Is.Null);
    }

    [Test]
    public void TryParse_Port_zero_returns_false()
    {
        var success = DiscoveryResponseParser.TryParse("MOBAFLOW_REST_API|192.168.0.1|0", out _, out _);

        Assert.That(success, Is.False);
    }

    [Test]
    public void TryParse_Port_65536_returns_false()
    {
        var success = DiscoveryResponseParser.TryParse("MOBAFLOW_REST_API|192.168.0.1|65536", out _, out _);

        Assert.That(success, Is.False);
    }

    [Test]
    public void ResponsePrefix_matches_expected_protocol()
    {
        Assert.That(DiscoveryResponseParser.ResponsePrefix, Is.EqualTo("MOBAFLOW_REST_API"));
    }

    [Test]
    public void Discovery_protocol_constants_match_expected_values()
    {
        Assert.That(DiscoveryResponseParser.RequestMessage, Is.EqualTo("MOBAFLOW_DISCOVER"));
        Assert.That(DiscoveryResponseParser.MulticastPort, Is.EqualTo(21106));
        Assert.That(DiscoveryResponseParser.MulticastAddress, Is.EqualTo("239.255.42.99"));
    }

    [Test]
    public void TryParse_Valid_port_65535_returns_true()
    {
        var success = DiscoveryResponseParser.TryParse("MOBAFLOW_REST_API|127.0.0.1|65535", out var ip, out var port);

        Assert.That(success, Is.True);
        Assert.That(ip, Is.EqualTo("127.0.0.1"));
        Assert.That(port, Is.EqualTo(65535));
    }

    [Test]
    public void TryParse_Valid_port_1_returns_true()
    {
        var success = DiscoveryResponseParser.TryParse("MOBAFLOW_REST_API|0.0.0.0|1", out _, out var port);

        Assert.That(success, Is.True);
        Assert.That(port, Is.EqualTo(1));
    }

    [Test]
    public void TryParse_Current_response_returns_https_identity_metadata()
    {
        const string instanceId = "d3ae2669706c4b7391167df884017420";
        const string fingerprint = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";
        var response = DiscoveryResponseParser.CreateResponse(
            "192.168.1.20",
            5001,
            5002,
            instanceId,
            fingerprint);

        var success = DiscoveryResponseParser.TryParse(response, out MobApiDiscoveryEndpoint? endpoint);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(endpoint?.IpAddress, Is.EqualTo("192.168.1.20"));
            Assert.That(endpoint?.HttpPort, Is.EqualTo(5001));
            Assert.That(endpoint?.HttpsPort, Is.EqualTo(5002));
            Assert.That(endpoint?.ServerInstanceId, Is.EqualTo(instanceId));
            Assert.That(endpoint?.ServerPublicKeyFingerprint, Is.EqualTo(fingerprint));
            Assert.That(endpoint?.ProtocolVersion, Is.EqualTo(DiscoveryResponseParser.CurrentProtocolVersion));
        });
    }

    [Test]
    public void TryParse_Current_response_preserves_legacy_http_result()
    {
        var response = DiscoveryResponseParser.CreateResponse(
            "10.0.0.10",
            5001,
            5002,
            "d3ae2669706c4b7391167df884017420",
            "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF");

        var success = DiscoveryResponseParser.TryParse(response, out var ip, out var port);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(ip, Is.EqualTo("10.0.0.10"));
            Assert.That(port, Is.EqualTo(5001));
        });
    }

    [TestCase("3", "5002", "d3ae2669706c4b7391167df884017420", "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF")]
    [TestCase("2", "0", "d3ae2669706c4b7391167df884017420", "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF")]
    [TestCase("2", "5002", "not-a-guid", "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF")]
    [TestCase("2", "5002", "d3ae2669706c4b7391167df884017420", "not-a-fingerprint")]
    public void TryParse_Invalid_current_metadata_returns_false(
        string version,
        string httpsPort,
        string instanceId,
        string fingerprint)
    {
        var response = $"MOBAFLOW_REST_API|192.168.1.20|5001|{version}|{httpsPort}|{instanceId}|{fingerprint}";

        var success = DiscoveryResponseParser.TryParse(response, out MobApiDiscoveryEndpoint? endpoint);

        Assert.That(success, Is.False);
        Assert.That(endpoint, Is.Null);
    }
}