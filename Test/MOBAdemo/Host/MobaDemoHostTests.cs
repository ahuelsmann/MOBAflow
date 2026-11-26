// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdemo.Host;

using System.Net;
using Microsoft.AspNetCore.Builder;

/// <summary>Starts the demo host on a loopback port (FR-002, FR-072, SC-003).</summary>
[TestFixture]
internal sealed class MobaDemoHostTests
{
    private const string FixtureDocument = "<!doctype html><title>fixture</title><p>fixture entry document</p>";
    private const string BuildOwnershipMarker = ".mobademo-build-owned";
    private const string WebRootOwnershipMarker = ".mobademo-webroot";

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
    public async Task Root_WithoutWebRootOverride_ServesTheEntryDocumentOfTheBuildOutput()
    {
        // Synchronize the default web root from the active host web root (wwwroot-build when built, otherwise
        // the committed placeholder), so incremental test builds never serve stale files.
        var activeSource = FindActiveSourceWebRoot();
        var defaultWebRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        if (activeSource != null)
        {
            SynchronizeDefaultWebRoot(defaultWebRoot, activeSource);
        }

        var expectedPath = Path.Combine(activeSource ?? defaultWebRoot, "index.html");
        var expected = await File.ReadAllTextAsync(expectedPath).ConfigureAwait(false);

        var reply = await FetchAsync("/", useDefaultWebRoot: true).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reply.Status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(reply.Body, Is.EqualTo(expected));
            Assert.That(reply.Body, Does.Contain("MOBAdemo is being built").Or.Contain("<div id=\"root\">"));
            if (activeSource != null && Path.GetFileName(activeSource).Equals("wwwroot", StringComparison.OrdinalIgnoreCase))
            {
                Assert.That(Directory.Exists(Path.Combine(defaultWebRoot, "assets")), Is.False);
            }
        }
    }

    [Test]
    [NonParallelizable]
    public async Task SynchronizeDefaultWebRoot_RemovesStaleAssetsAndReplacesNewerStaleIndex()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "mobademo-sync-" + Guid.NewGuid().ToString("N"));
        try
        {
            var source = Path.Combine(tempRoot, "source");
            var target = Path.Combine(tempRoot, "bin-wwwroot");
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(Path.Combine(target, "assets"));

            var sourceIndex = Path.Combine(source, "index.html");
            await File.WriteAllTextAsync(sourceIndex, "placeholder-index").ConfigureAwait(false);
            File.SetLastWriteTimeUtc(sourceIndex, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            await File.WriteAllTextAsync(Path.Combine(source, BuildOwnershipMarker), "owned\n").ConfigureAwait(false);

            var staleIndex = Path.Combine(target, "index.html");
            await File.WriteAllTextAsync(staleIndex, "stale-built-index").ConfigureAwait(false);
            File.SetLastWriteTimeUtc(staleIndex, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
            await File.WriteAllTextAsync(Path.Combine(target, "assets", "old-stale.js"), "stale").ConfigureAwait(false);

            SynchronizeDefaultWebRoot(target, source);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await File.ReadAllTextAsync(staleIndex).ConfigureAwait(false), Is.EqualTo("placeholder-index"));
                Assert.That(Directory.Exists(Path.Combine(target, "assets")), Is.False);
                Assert.That(File.Exists(Path.Combine(target, BuildOwnershipMarker)), Is.False);
                Assert.That(File.Exists(Path.Combine(target, WebRootOwnershipMarker)), Is.True);
            }
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
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

    private static string? FindActiveSourceWebRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Moba.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory == null)
        {
            return null;
        }

        var hostRoot = Path.Combine(directory.FullName, "MOBAdemo", "Host");
        var builtRoot = Path.Combine(hostRoot, "wwwroot-build");
        return File.Exists(Path.Combine(builtRoot, "index.html"))
            ? builtRoot
            : Path.Combine(hostRoot, "wwwroot");
    }

    private static void SynchronizeDefaultWebRoot(string targetWebRoot, string sourceWebRoot)
    {
        if (Directory.Exists(targetWebRoot))
        {
            Directory.Delete(targetWebRoot, recursive: true);
        }

        Directory.CreateDirectory(targetWebRoot);
        foreach (var directory in Directory.EnumerateDirectories(sourceWebRoot, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceWebRoot, directory);
            Directory.CreateDirectory(Path.Combine(targetWebRoot, relative));
        }

        foreach (var file in Directory.EnumerateFiles(sourceWebRoot, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceWebRoot, file);
            var fileName = Path.GetFileName(relative);
            if (fileName.Equals(BuildOwnershipMarker, StringComparison.OrdinalIgnoreCase)
                || fileName.Equals(WebRootOwnershipMarker, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            File.Copy(file, Path.Combine(targetWebRoot, relative), overwrite: true);
        }

        File.WriteAllText(Path.Combine(targetWebRoot, WebRootOwnershipMarker), "MOBAdemo generated web root\n");
    }

    private sealed record Reply(HttpStatusCode Status, string Body, string? MediaType, bool HasServerHeader);
}
