// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Common.Configuration;
using Moba.SharedUI.Mobile;

[TestFixture]
[Category("Unit")]
internal sealed class RestApiRecentEndpointHistoryTests
{
    private static readonly string[] UnchangedAddresses = ["192.0.2.10", "192.0.2.20"];
    private static readonly string[] PromotedAddresses = ["192.0.2.20", "192.0.2.10", "192.0.2.30"];

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("192.0.2.20")]
    public void RecordRecentIpRejectsMissingSettingsBeforeCheckingAddress(string ip)
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            RestApiRecentEndpointHistory.RecordRecentIp(null!, ip));

        Assert.That(exception!.ParamName, Is.EqualTo("rest"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t")]
    [TestCase("\r\n")]
    [TestCase("\u00A0")]
    public void RecordRecentIpIgnoresBlankAddressesWithoutChangingSettings(string? ip)
    {
        var rest = new RestApiSettings
        {
            RecentIpAddresses = ["192.0.2.10", "192.0.2.20"],
            CurrentIpAddress = "192.0.2.99",
            Port = 5015,
            IsConnectionEnabled = true,
        };

        var changed = RestApiRecentEndpointHistory.RecordRecentIp(rest, ip!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(changed, Is.False);
            Assert.That(rest.RecentIpAddresses, Is.EqualTo(UnchangedAddresses));
            Assert.That(rest.CurrentIpAddress, Is.EqualTo("192.0.2.99"));
            Assert.That(rest.Port, Is.EqualTo(5015));
            Assert.That(rest.IsConnectionEnabled, Is.True);
        }
    }

    [TestCase("192.0.2.20")]
    [TestCase("  192.0.2.20  ")]
    [TestCase("\t192.0.2.20\r\n")]
    public void RecordRecentIpRemovesEveryMatchingEntryAndPreservesOtherAddresses(string ip)
    {
        var rest = new RestApiSettings
        {
            RecentIpAddresses = ["192.0.2.10", "192.0.2.20", "192.0.2.30", "192.0.2.20"],
            CurrentIpAddress = "192.0.2.99",
            Port = 5015,
            IsConnectionEnabled = true,
        };

        var changed = RestApiRecentEndpointHistory.RecordRecentIp(rest, ip);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(changed, Is.True);
            Assert.That(rest.RecentIpAddresses, Is.EqualTo(PromotedAddresses));
            Assert.That(rest.CurrentIpAddress, Is.EqualTo("192.0.2.99"));
            Assert.That(rest.Port, Is.EqualTo(5015));
            Assert.That(rest.IsConnectionEnabled, Is.True);
        }
    }

    [TestCase("  192.0.2.10  ")]
    [TestCase("\t192.0.2.10\r\n")]
    public void RecordRecentIpDoesNotRequestPersistenceWhenTrimmedAddressIsAlreadyFirst(string ip)
    {
        var rest = new RestApiSettings { RecentIpAddresses = ["192.0.2.10", "192.0.2.20"] };

        var changed = RestApiRecentEndpointHistory.RecordRecentIp(rest, ip);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(changed, Is.False);
            Assert.That(rest.RecentIpAddresses, Is.EqualTo(UnchangedAddresses));
        }
    }

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