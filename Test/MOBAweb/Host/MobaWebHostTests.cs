// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAweb.Host;

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Moba.Web;

/// <summary>Starts the demo host on a loopback port with a temporary web root (FR-002, FR-072, SC-003).</summary>
[TestFixture]
internal sealed class MobaWebHostTests
{
    private const string FixtureDocument = "<!doctype html><title>fixture</title><p>fixture entry document</p>";

    private string _webRoot = string.Empty;
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    [SetUp]
    public async Task SetUp()
    {
        _webRoot = Path.Combine(Path.GetTempPath(), "mobaweb-host-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_webRoot);
        await File.WriteAllTextAsync(Path.Combine(_webRoot, "index.html"), FixtureDocument).ConfigureAwait(false);

        _app = MobaWebHost.Build(["--urls", "http://127.0.0.1:0"], _webRoot);
        await _app.StartAsync().ConfigureAwait(false);

        var address = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();
        _client = new HttpClient { BaseAddress = new Uri(address) };
    }

    [TearDown]
    public async Task TearDown()
    {
        _client.Dispose();
        await _app.StopAsync().ConfigureAwait(false);
        await _app.DisposeAsync().ConfigureAwait(false);
        Directory.Delete(_webRoot, recursive: true);
    }

    [Test]
    public async Task Root_ServesEntryDocument()
    {
        using var response = await _client.GetAsync("/").ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadAsStringAsync().ConfigureAwait(false), Is.EqualTo(FixtureDocument));
    }

    [Test]
    public async Task Health_ReturnsOk()
    {
        using var response = await _client.GetAsync("/health").ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadAsStringAsync().ConfigureAwait(false), Is.EqualTo("ok"));
    }

    [TestCase("/api/runtime")]
    [TestCase("/api/solution")]
    [TestCase("/hubs/runtime")]
    [TestCase("/hubs/runtime/negotiate")]
    [TestCase("/no-such-path")]
    public async Task OtherPaths_Return404(string path)
    {
        using var response = await _client.GetAsync(path).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Responses_CarryNoServerHeader()
    {
        using var response = await _client.GetAsync("/health").ConfigureAwait(false);

        Assert.That(response.Headers.Contains("Server"), Is.False);
    }
}
