// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAweb.Host;

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Moba.Web;

/// <summary>Starts the demo host on a loopback port (FR-002, FR-072, SC-003).</summary>
[TestFixture]
internal sealed class MobaWebHostTests
{
    private const string FixtureDocument = "<!doctype html><title>fixture</title><p>fixture entry document</p>";

    [Test]
    public async Task Root_ServesEntryDocument()
    {
        var reply = await FetchAsync("/").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reply.Status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(reply.Body, Is.EqualTo(FixtureDocument));
        }
    }

    [Test]
    public async Task Root_WithoutWebRootOverride_ServesPlaceholderFromBuildOutput()
    {
        var reply = await FetchAsync("/", useDefaultWebRoot: true).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reply.Status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(reply.Body, Does.Contain("MOBAdemo is being built"));
        }
    }

    [Test]
    public async Task Health_ReturnsOk()
    {
        var reply = await FetchAsync("/health").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reply.Status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(reply.Body, Is.EqualTo("ok"));
        }
    }

    [TestCase("/api/runtime")]
    [TestCase("/api/solution")]
    [TestCase("/hubs/runtime")]
    [TestCase("/hubs/runtime/negotiate")]
    [TestCase("/no-such-path")]
    public async Task OtherPaths_Return404(string path)
    {
        var reply = await FetchAsync(path).ConfigureAwait(false);

        Assert.That(reply.Status, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Responses_CarryNoServerHeader()
    {
        var reply = await FetchAsync("/health").ConfigureAwait(false);

        Assert.That(reply.HasServerHeader, Is.False);
    }

    [Test]
    public async Task UnhandledException_InDevelopment_ReturnsNeutral500()
    {
        const string secret = "secret-boom-message";
        var reply = await FetchAsync(
            "/boom",
            environment: "Development",
            configure: app => app.MapGet("/boom", (Func<string>)(() => throw new InvalidOperationException(secret))))
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reply.Status, Is.EqualTo(HttpStatusCode.InternalServerError));
            Assert.That(reply.Body, Is.EqualTo("Internal Server Error"));
            Assert.That(reply.Body, Does.Not.Contain(secret));
            Assert.That(reply.Body, Does.Not.Contain(" at "));
            Assert.That(reply.Body, Does.Not.Contain(":\\"));
            Assert.That(reply.MediaType, Is.EqualTo("text/plain"));
        }
    }

    private static async Task<Reply> FetchAsync(
        string path,
        bool useDefaultWebRoot = false,
        string environment = "Production",
        Action<WebApplication>? configure = null)
    {
        var host = await RunningHost.StartAsync(useDefaultWebRoot, environment, configure).ConfigureAwait(false);
        try
        {
            using var response = await host.Client.GetAsync(new Uri(path, UriKind.Relative)).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return new Reply(
                response.StatusCode,
                body,
                response.Content.Headers.ContentType?.MediaType,
                response.Headers.Contains("Server"));
        }
        finally
        {
            await host.DisposeAsync().ConfigureAwait(false);
        }
    }

    private sealed record Reply(HttpStatusCode Status, string Body, string? MediaType, bool HasServerHeader);

    /// <summary>A started host with a temporary web root; disposing stops it and deletes the root.</summary>
    private sealed class RunningHost : IAsyncDisposable
    {
        private readonly string? _webRoot;
        private readonly WebApplication _app;

        private RunningHost(string? webRoot, WebApplication app, HttpClient client)
        {
            _webRoot = webRoot;
            _app = app;
            Client = client;
        }

        public HttpClient Client { get; }

        public static async Task<RunningHost> StartAsync(
            bool useDefaultWebRoot = false,
            string environment = "Production",
            Action<WebApplication>? configure = null)
        {
            string? webRoot = null;
            WebApplication? app = null;
            try
            {
                if (!useDefaultWebRoot)
                {
                    webRoot = Path.Combine(Path.GetTempPath(), "mobaweb-host-" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(webRoot);
                    await File.WriteAllTextAsync(Path.Combine(webRoot, "index.html"), FixtureDocument)
                        .ConfigureAwait(false);
                }

                app = MobaWebHost.Build(
                    ["--urls", "http://127.0.0.1:0", "--environment", environment],
                    webRoot,
                    configure);
                await app.StartAsync().ConfigureAwait(false);

                var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!
                    .Addresses.Single();
                return new RunningHost(webRoot, app, new HttpClient { BaseAddress = new Uri(address) });
            }
            catch
            {
                await CleanUpAsync(app, webRoot).ConfigureAwait(false);
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                Client.Dispose();
            }
            finally
            {
                await CleanUpAsync(_app, _webRoot).ConfigureAwait(false);
            }
        }

        private static async Task CleanUpAsync(WebApplication? app, string? webRoot)
        {
            try
            {
                if (app != null)
                {
                    try
                    {
                        await app.StopAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        await app.DisposeAsync().ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                if (webRoot != null && Directory.Exists(webRoot))
                {
                    Directory.Delete(webRoot, recursive: true);
                }
            }
        }
    }
}
