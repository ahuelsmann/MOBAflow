// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Runtime;

/// <summary>
/// Tests for MOBAsmart foreground-service keep-alive policy.
/// </summary>
[TestFixture]
internal sealed class MobileBackgroundKeepAlivePolicyTests
{
    [TestCase(true, false, false, false, true)]
    [TestCase(false, true, true, true, true)]
    [TestCase(false, true, true, false, false)]
    [TestCase(false, true, false, true, false)]
    [TestCase(false, false, true, true, false)]
    [TestCase(false, false, false, false, false)]
    public void ShouldKeepAlive_ReturnsExpected(
        bool isLocalZ21Connected,
        bool isMobaflowConnectionEnabled,
        bool isRestApiReachable,
        bool isRuntimeHubConnected,
        bool expected)
    {
        var result = MobileBackgroundKeepAlivePolicy.ShouldKeepAlive(
            isLocalZ21Connected,
            isMobaflowConnectionEnabled,
            isRestApiReachable,
            isRuntimeHubConnected);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void GetNotificationMessage_ReturnsCombinedText_WhenBothConnectionsActive()
    {
        var message = MobileBackgroundKeepAlivePolicy.GetNotificationMessage(true, true);

        Assert.That(message, Is.EqualTo("Z21 and MOBAflow session active"));
    }

    [Test]
    public void GetNotificationMessage_ReturnsMobaflowText_WhenOnlyMobaflowSessionActive()
    {
        var message = MobileBackgroundKeepAlivePolicy.GetNotificationMessage(false, true);

        Assert.That(message, Is.EqualTo("MOBAflow session active"));
    }

    [Test]
    public void GetNotificationMessage_ReturnsZ21Text_WhenOnlyLocalZ21Active()
    {
        var message = MobileBackgroundKeepAlivePolicy.GetNotificationMessage(true, false);

        Assert.That(message, Is.EqualTo("Z21 connection maintained"));
    }
}
