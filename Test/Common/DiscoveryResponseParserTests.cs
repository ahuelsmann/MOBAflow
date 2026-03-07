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
        var success = DiscoveryResponseParser.TryParse("MOBAFLOW_REST_API|192.168.0.1|0", out var ip, out var port);

        Assert.That(success, Is.False);
    }

    [Test]
    public void TryParse_Port_65536_returns_false()
    {
        var success = DiscoveryResponseParser.TryParse("MOBAFLOW_REST_API|192.168.0.1|65536", out var ip, out var port);

        Assert.That(success, Is.False);
    }

    [Test]
    public void ResponsePrefix_matches_expected_protocol()
    {
        Assert.That(DiscoveryResponseParser.ResponsePrefix, Is.EqualTo("MOBAFLOW_REST_API"));
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
        var success = DiscoveryResponseParser.TryParse("MOBAFLOW_REST_API|0.0.0.0|1", out var ip, out var port);

        Assert.That(success, Is.True);
        Assert.That(port, Is.EqualTo(1));
    }
}
