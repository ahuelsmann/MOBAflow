#if !SKIP_ANDROID_TESTS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAsmart;

using Microsoft.Extensions.DependencyInjection;

using Moba.MAUI.Extensions;
using Moba.MAUI.Service;

/// <summary>
/// Tests for <see cref="MobiLanHttpClientFactory"/> LAN handler configuration.
/// </summary>
[TestFixture]
internal sealed class MobiLanHttpClientFactoryTests
{
    [Test]
    public void CreateLanHealthHandler_ShouldReturnHandler()
    {
        using var handler = MobiLanHttpClientFactory.CreateLanHealthHandler();

        Assert.That(handler, Is.Not.Null);
    }

    [Test]
    public void CreateLanHealthClient_ShouldUseSixSecondRequestTimeout()
    {
        using var client = MobiLanHttpClientFactory.CreateLanHealthClient();

        Assert.That(client.Timeout, Is.EqualTo(TimeSpan.FromSeconds(6)));
    }

    [Test]
    public void CreateLanDiscoveryProbeClient_ShouldUseTwoSecondRequestTimeout()
    {
        using var client = MobiLanHttpClientFactory.CreateLanDiscoveryProbeClient();

        Assert.That(client.Timeout, Is.EqualTo(TimeSpan.FromSeconds(2)));
    }

    [Test]
    public void MobiHttpClientNames_ShouldExposeExpectedClientNames()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MobiHttpClientNames.Platform, Is.EqualTo("MobiPlatform"));
            Assert.That(MobiHttpClientNames.LanHealth, Is.EqualTo("MobiLanHealth"));
            Assert.That(MobiHttpClientNames.LanDiscovery, Is.EqualTo("MobiLanDiscovery"));
        });
    }
}
#endif
