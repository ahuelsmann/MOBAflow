// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Configuration;

[TestFixture]
internal sealed class RestApiRecentEndpointHistoryTests
{
    [Test]
    public void RecordRecentIp_MovesToFront_AndTrims()
    {
        var rest = new RestApiSettings
        {
            RecentIpAddresses = ["10.0.0.2", "10.0.0.1"],
        };

        Assert.That(RestApiRecentEndpointHistory.RecordRecentIp(rest, "  10.0.0.1  "), Is.True);
        Assert.That(rest.RecentIpAddresses[0], Is.EqualTo("10.0.0.1"));
        Assert.That(rest.RecentIpAddresses[1], Is.EqualTo("10.0.0.2"));
    }

    [Test]
    public void RecordRecentIp_WhenAlreadyFirst_ReturnsFalse()
    {
        var rest = new RestApiSettings
        {
            RecentIpAddresses = ["192.168.1.10"],
        };

        Assert.That(RestApiRecentEndpointHistory.RecordRecentIp(rest, "192.168.1.10"), Is.False);
        Assert.That(rest.RecentIpAddresses.Count, Is.EqualTo(1));
    }

    [Test]
    public void RecordRecentIp_CaseInsensitiveDedupe()
    {
        var rest = new RestApiSettings
        {
            RecentIpAddresses = ["192.168.0.5"],
        };

        Assert.That(RestApiRecentEndpointHistory.RecordRecentIp(rest, "192.168.0.5"), Is.False);
        Assert.That(RestApiRecentEndpointHistory.RecordRecentIp(rest, "192.168.0.7"), Is.True);
        Assert.That(rest.RecentIpAddresses[0], Is.EqualTo("192.168.0.7"));
        Assert.That(rest.RecentIpAddresses[1], Is.EqualTo("192.168.0.5"));
        Assert.That(rest.RecentIpAddresses.Count, Is.EqualTo(2));
    }

    [Test]
    public void RecordRecentIp_EnforcesMax()
    {
        var rest = new RestApiSettings();
        for (var i = 0; i < RestApiRecentEndpointHistory.MaxRecentAddresses + 4; i++)
        {
            RestApiRecentEndpointHistory.RecordRecentIp(rest, $"10.0.0.{i}");
        }

        Assert.That(rest.RecentIpAddresses.Count, Is.EqualTo(RestApiRecentEndpointHistory.MaxRecentAddresses));
    }
}
