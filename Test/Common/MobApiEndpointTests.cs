// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Discovery;

/// <summary>
/// Protects the single place that builds MOBApi request URIs for MOBAflow and MOBAsmart.
/// </summary>
[TestFixture]
internal sealed class MobApiEndpointTests
{
    [Test]
    public void BaseUri_UsesTrimmedAddressAndPort()
    {
        var endpoint = new MobApiEndpoint(" 192.168.0.20 ", 5001);

        Assert.That(endpoint.BaseUri, Is.EqualTo(new Uri("http://192.168.0.20:5001/")));
    }

    [TestCase("api/solution", "http://192.168.0.20:5001/api/solution")]
    [TestCase("/api/photos/health", "http://192.168.0.20:5001/api/photos/health")]
    [TestCase("runtime-hub", "http://192.168.0.20:5001/runtime-hub")]
    public void Resolve_CombinesBaseUriAndPath(string relativePath, string expected)
    {
        var endpoint = new MobApiEndpoint("192.168.0.20", 5001);

        Assert.That(endpoint.Resolve(relativePath), Is.EqualTo(new Uri(expected)));
    }

    [Test]
    public void Local_TargetsLoopback()
    {
        Assert.That(MobApiEndpoint.Local(5010).Resolve("api/status"), Is.EqualTo(new Uri("http://127.0.0.1:5010/api/status")));
    }
}
