// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdemo.Build;

using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Moba.Test.MOBAdemo.Host;

/// <summary>
/// End-to-end checks of <c>MOBAdemo/build.ps1</c> (issue #430 slice 3): the built app is served by the demo host,
/// every text asset has Brotli and gzip siblings, the output holds no host detail and a rebuild leaves nothing stale.
/// The script runs once per fixture, because <c>npm ci</c> is slow.
/// </summary>
[TestFixture]
internal sealed class BuildScriptTests
{
    private static readonly string[] TextExtensions = [".js", ".css", ".html", ".svg"];

    private string _root = string.Empty;
    private string _output = string.Empty;
    private string _placeholderHashBefore = string.Empty;

    [OneTimeSetUp]
    public async Task BuildOnceAsync()
    {
        await BuildScript.RequireToolchainAsync().ConfigureAwait(false);
        _root = Path.Combine(Path.GetTempPath(), "mobademo-build-" + Guid.NewGuid().ToString("N"));
        _output = Path.Combine(_root, "web");
        _placeholderHashBefore = HashPlaceholder();
        await BuildScript.RunAsync(_output).ConfigureAwait(false);
    }

    [OneTimeTearDown]
    public void CleanUp()
    {
        if (_root.Length > 0 && Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Test]
    public async Task Host_ServesBuiltEntryDocument()
    {
        var reply = await FetchAsync(_output, "/").ConfigureAwait(false);
        var body = Encoding.UTF8.GetString(reply.Body);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reply.Status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body, Does.Contain("<div id=\"root\"></div>"));
            Assert.That(body, Does.Not.Contain("fixture entry document"));
            Assert.That(body, Does.Not.Contain("is being built"));
        }
    }

    [Test]
    public async Task Host_ServesHashedAssetAsBrotli()
    {
        var asset = Directory.EnumerateFiles(Path.Combine(_output, "assets"), "*.js").First();
        var name = Path.GetFileName(asset);
        var reply = await FetchAsync(_output, "/assets/" + name, "br").ConfigureAwait(false);
        var expected = await File.ReadAllBytesAsync(asset + ".br").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(name, Does.Match(@"-[A-Za-z0-9_-]{6,}\.js$"), "Vite must give the asset a content hash.");
            Assert.That(reply.Status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(reply.ContentEncoding, Is.EqualTo("br"));
            Assert.That(reply.CacheControl, Does.Contain("immutable"));
            Assert.That(reply.Body, Is.EqualTo(expected));
        }
    }

    [Test]
    public async Task EveryTextAsset_HasValidBrotliAndGzipSiblings()
    {
        var textFiles = Directory.EnumerateFiles(_output, "*", SearchOption.AllDirectories)
            .Where(IsTextAsset)
            .ToList();
        var extensions = textFiles.Select(Path.GetExtension).Distinct().ToList();

        Assert.That(extensions, Does.Contain(".html").And.Contain(".js").And.Contain(".css"));
        foreach (var file in textFiles)
        {
            var original = await File.ReadAllBytesAsync(file).ConfigureAwait(false);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(File.Exists(file + ".br"), Is.True, "Missing Brotli sibling of " + file);
                Assert.That(File.Exists(file + ".gz"), Is.True, "Missing gzip sibling of " + file);
                Assert.That(Decompress(file + ".br", brotli: true), Is.EqualTo(original), file + ".br");
                Assert.That(Decompress(file + ".gz", brotli: false), Is.EqualTo(original), file + ".gz");
            }
        }
    }

    [Test]
    public async Task Output_HoldsNoHostDetail()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var tempPath = Path.GetTempPath().TrimEnd('\\', '/');
        var needles = new[]
        {
            BuildScript.RepositoryRoot,
            BuildScript.RepositoryRoot.Replace('\\', '/'),
            tempPath,
            tempPath.Replace('\\', '/'),
            userProfile,
            userProfile.Replace('\\', '/'),
        };
        var patterns = new List<Regex>
        {
            // Absolute local paths: drive paths and the usual home folders.
            Compile(@"[A-Za-z]:[\\/](?:Users|Repo|Windows|Program Files)|(?:^|[^\w./])/(?:home|Users|root)/[\w.-]+"),
            // Private, link-local and loopback style addresses.
            Compile(@"\b(?:10\.\d{1,3}|192\.168|172\.(?:1[6-9]|2\d|3[01])|169\.254)\.\d{1,3}\.\d{1,3}\b"),
            // A host name or loopback address with a port, and the ports the repository configures.
            Compile(
                @"(?:localhost|127\.0\.0\.1|0\.0\.0\.0|\[::1\]|\.local|\.internal|\.lan):\d{2,5}|:(?:5000|5001|5173|8080)\b",
                RegexOptions.IgnoreCase),
        };
        foreach (var word in new[] { Environment.UserName, Environment.MachineName }.Where(word => word.Length >= 3))
        {
            patterns.Add(Compile(@"(?<![A-Za-z0-9])" + Regex.Escape(word) + @"(?![A-Za-z0-9])", RegexOptions.IgnoreCase));
        }

        var findings = new List<string>();
        foreach (var file in Directory.EnumerateFiles(_output, "*", SearchOption.AllDirectories)
                     .Where(path => !path.EndsWith(".br", StringComparison.Ordinal)
                         && !path.EndsWith(".gz", StringComparison.Ordinal)))
        {
            var text = Encoding.Latin1.GetString(await File.ReadAllBytesAsync(file).ConfigureAwait(false));
            findings.AddRange(needles.Where(needle => text.Contains(needle, StringComparison.OrdinalIgnoreCase))
                .Select(needle => Path.GetFileName(file) + " contains " + needle));
            findings.AddRange(patterns.Select(pattern => pattern.Match(text))
                .Where(match => match.Success)
                .Select(match => Path.GetFileName(file) + " matches " + match.Value));
        }

        Assert.That(findings, Is.Empty, "The delivered files must not expose host details (SC-003, FR-030).");
    }

    [Test]
    public void Script_NeverTouchesCommittedPlaceholder()
    {
        Assert.That(HashPlaceholder(), Is.EqualTo(_placeholderHashBefore));
    }

    [Test]
    public async Task Rebuild_ReplacesOutputAndOldAssetReturnsNotFound()
    {
        var second = Path.Combine(_root, "second");
        CopyDirectory(_output, second);
        var staleAsset = Path.Combine(second, "assets", "index-stale0001.js");
        var staleFiles = new[] { staleAsset, staleAsset + ".br", staleAsset + ".gz", Path.Combine(second, "old.txt") };
        foreach (var path in staleFiles)
        {
            await File.WriteAllTextAsync(path, "stale").ConfigureAwait(false);
        }

        await BuildScript.RunAsync(second).ConfigureAwait(false);
        var staleReply = await FetchAsync(second, "/assets/index-stale0001.js").ConfigureAwait(false);
        var indexReply = await FetchAsync(second, "/").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(staleFiles.Where(File.Exists), Is.Empty, "The rebuild must delete files of the earlier output.");
            Assert.That(staleReply.Status, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(indexReply.Status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(
                Directory.EnumerateFiles(Path.Combine(second, "assets"), "*.js").Select(Path.GetFileName),
                Is.EquivalentTo(Directory.EnumerateFiles(Path.Combine(_output, "assets"), "*.js").Select(Path.GetFileName)));
        }
    }

    private static string HashPlaceholder() => Convert.ToHexString(SHA256.HashData(
        File.ReadAllBytes(Path.Combine(BuildScript.RepositoryRoot, "MOBAdemo", "Host", "wwwroot", "index.html"))));

    private static bool IsTextAsset(string path) =>
        TextExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    private static Regex Compile(string pattern, RegexOptions options = RegexOptions.None) =>
        new(pattern, options | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(10));

    private static byte[] Decompress(string path, bool brotli)
    {
        using var file = File.OpenRead(path);
        using Stream decoder = brotli
            ? new BrotliStream(file, CompressionMode.Decompress)
            : new GZipStream(file, CompressionMode.Decompress);
        using var buffer = new MemoryStream();
        decoder.CopyTo(buffer);
        return buffer.ToArray();
    }

    /// <summary>Starts the demo host on a copy of <paramref name="builtRoot"/> and fetches one path.</summary>
    private static async Task<Reply> FetchAsync(string builtRoot, string path, string? acceptEncoding = null)
    {
        var host = await RunningHost.StartAsync(seedWebRoot: webRoot => CopyDirectory(builtRoot, webRoot))
            .ConfigureAwait(false);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
            if (acceptEncoding != null)
            {
                request.Headers.AcceptEncoding.ParseAdd(acceptEncoding);
            }

            using var response = await host.Client.SendAsync(request).ConfigureAwait(false);
            return new Reply(
                response.StatusCode,
                await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false),
                string.Join(",", response.Content.Headers.ContentEncoding),
                response.Headers.CacheControl?.ToString());
        }
        finally
        {
            await host.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(target, Path.GetRelativePath(source, file)), overwrite: true);
        }
    }

    private sealed record Reply(HttpStatusCode Status, byte[] Body, string ContentEncoding, string? CacheControl);
}
