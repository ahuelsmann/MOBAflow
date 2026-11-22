// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdemo.Host;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Moba.Demo;

/// <summary>A started demo host with a temporary web root; disposing stops it and deletes the root.</summary>
internal sealed class RunningHost : IAsyncDisposable
{
    /// <summary>Content of the <c>index.html</c> every temporary web root starts with.</summary>
    internal const string FixtureDocument = "<!doctype html><title>fixture</title><p>fixture entry document</p>";

    private readonly string? _webRoot;
    private readonly WebApplication _app;

    private RunningHost(string? webRoot, WebApplication app, HttpClient client)
    {
        _webRoot = webRoot;
        _app = app;
        Client = client;
    }

    public HttpClient Client { get; }

    /// <summary>Loopback address the host listens on, for tests that talk raw HTTP.</summary>
    public Uri Address => Client.BaseAddress!;

    /// <summary>Starts a host. <paramref name="seedWebRoot"/> may add files to the temporary web root first.</summary>
    public static async Task<RunningHost> StartAsync(
        bool useDefaultWebRoot = false,
        string environment = "Production",
        Action<WebApplication>? configure = null,
        Action<string>? seedWebRoot = null)
    {
        string? webRoot = null;
        WebApplication? app = null;
        try
        {
            if (!useDefaultWebRoot)
            {
                webRoot = Path.Combine(Path.GetTempPath(), "mobademo-host-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(webRoot);
                await File.WriteAllTextAsync(Path.Combine(webRoot, "index.html"), FixtureDocument)
                    .ConfigureAwait(false);
                seedWebRoot?.Invoke(webRoot);
            }

            app = MobaDemoHost.Build(
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
