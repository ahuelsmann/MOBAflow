#if !SKIP_ANDROID_TESTS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAsmart;

using Microsoft.Extensions.DependencyInjection;

using Moba.MAUI.Extensions;
using Moba.MAUI.Service;

using System.Net.Http;

#if ANDROID
using Xamarin.Android.Net;
#endif

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
    public void CreateLanSignalRHandler_ShouldReturnHandler()
    {
        using var handler = MobiLanHttpClientFactory.CreateLanSignalRHandler();

        Assert.That(handler, Is.Not.Null);
    }

    [TestCase(MobiHttpClientNames.Platform)]
    [TestCase(MobiHttpClientNames.LanHealth)]
    [TestCase(MobiHttpClientNames.LanDiscovery)]
    public void RegisteredClient_ShouldNotFollowRedirects(string clientName)
    {
        var services = new ServiceCollection();
        services.AddMobiHttpClients();
        using var provider = services.BuildServiceProvider();
        var handlerFactory = provider.GetRequiredService<IHttpMessageHandlerFactory>();

        AssertRedirectsDisabled(handlerFactory.CreateHandler(clientName));
    }

    [Test]
    public void CreateLanSignalRHandler_ShouldNotFollowRedirects()
    {
        using var handler = MobiLanHttpClientFactory.CreateLanSignalRHandler();

        AssertRedirectsDisabled(handler);
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

    private static void AssertRedirectsDisabled(HttpMessageHandler handler)
    {
        while (handler is DelegatingHandler { InnerHandler: { } inner })
        {
            handler = inner;
        }

        switch (handler)
        {
            case SocketsHttpHandler sockets:
                Assert.That(sockets.AllowAutoRedirect, Is.False);
                break;
            case HttpClientHandler http:
                Assert.That(http.AllowAutoRedirect, Is.False);
                break;
#if ANDROID
            case AndroidMessageHandler android:
                Assert.That(android.AllowAutoRedirect, Is.False);
                break;
#endif
            default:
                Assert.Fail($"Unexpected primary HTTP handler: {handler.GetType().Name}.");
                break;
        }
    }
}
#endif
