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
    private string _appCopy = string.Empty;
    private string _output = string.Empty;
    private string _placeholderHashBefore = string.Empty;

    [OneTimeSetUp]
    public async Task BuildOnceAsync()
    {
        await BuildScript.RequireToolchainAsync().ConfigureAwait(false);
        _root = Path.Combine(Path.GetTempPath(), "mobademo-build-" + Guid.NewGuid().ToString("N"));
        _appCopy = Path.Combine(_root, "app-fixture");
        _output = Path.Combine(_root, "web");
        BuildScript.CopyDirectory(
            Path.Combine(BuildScript.RepositoryRoot, "MOBAdemo", "app"),
            _appCopy,
            relative => relative.Split(Path.DirectorySeparatorChar)[0] is "node_modules" or "dist");
        _placeholderHashBefore = HashPlaceholder();
        await BuildScript.RunAsync(_output, "-AppPath", _appCopy).ConfigureAwait(false);
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
        foreach (var name in new[] { Environment.UserName, Environment.MachineName })
        {
            patterns.AddRange(HostDetail.NamePatterns(name));
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
    public async Task Rebuild_AfterRealSourceChange_ServesNewAssetAndOldAssetReturnsNotFound()
    {
        // The app is copied so the repository sources stay untouched; the change alters the bundle content,
        // so Vite emits a different content hash.
        var app = Path.Combine(_root, "app-copy");
        var web = Path.Combine(_root, "rebuilt");
        BuildScript.CopyDirectory(
            Path.Combine(BuildScript.RepositoryRoot, "MOBAdemo", "app"),
            app,
            relative => relative.Split(Path.DirectorySeparatorChar)[0] is "node_modules" or "dist");
        await BuildScript.RunAsync(web, "-AppPath", app).ConfigureAwait(false);
        var oldAsset = Directory.EnumerateFiles(Path.Combine(web, "assets"), "*.js").Select(Path.GetFileName).Single()!;
        var plantedFile = Path.Combine(web, "old.txt");
        await File.WriteAllTextAsync(plantedFile, "stale").ConfigureAwait(false);

        var main = Path.Combine(app, "src", "main.tsx");
        await File.AppendAllTextAsync(main, "\nconsole.info(\"mobademo-hash-change-marker\");\n").ConfigureAwait(false);
        await BuildScript.RunAsync(web, "-AppPath", app).ConfigureAwait(false);

        var newAsset = Directory.EnumerateFiles(Path.Combine(web, "assets"), "*.js").Select(Path.GetFileName).Single()!;
        var oldReply = await FetchAsync(web, "/assets/" + oldAsset).ConfigureAwait(false);
        var newReply = await FetchAsync(web, "/assets/" + newAsset).ConfigureAwait(false);
        var indexReply = await FetchAsync(web, "/").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(newAsset, Is.Not.EqualTo(oldAsset), "A real change must produce a new content hash.");
            Assert.That(File.Exists(plantedFile), Is.False, "The rebuild must delete files of the earlier output.");
            Assert.That(oldReply.Status, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(newReply.Status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(Encoding.UTF8.GetString(indexReply.Body), Does.Contain(newAsset));
            Assert.That(Encoding.UTF8.GetString(indexReply.Body), Does.Not.Contain(oldAsset));
        }
    }

    [Test]
    public async Task Build_WithDebugConfiguration_BuildsDevelopmentBundleWithCompressedSiblings()
    {
        var debugOutput = Path.Combine(_root, "debug-web");
        Directory.CreateDirectory(debugOutput);
        var result = await BuildScript.TryRunAsync(debugOutput, "-Configuration", "Debug", "-AppPath", _appCopy)
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.Zero, "Debug build failed:" + Environment.NewLine + result.Output);
            Assert.That(result.Output, Does.Contain("npm run build (development mode)"));
            Assert.That(result.Output, Does.Contain("--mode development"));
            Assert.That(File.Exists(Path.Combine(debugOutput, ".mobademo-build-owned")), Is.True);
            Assert.That(File.Exists(Path.Combine(debugOutput, "index.html")), Is.True);
            Assert.That(File.Exists(Path.Combine(debugOutput, "index.html.br")), Is.True);
            Assert.That(File.Exists(Path.Combine(debugOutput, "index.html.gz")), Is.True);
            Assert.That(Directory.EnumerateFiles(Path.Combine(debugOutput, "assets"), "*.js"), Is.Not.Empty);
        }

        var reply = await FetchAsync(debugOutput, "/").ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(reply.Status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(Encoding.UTF8.GetString(reply.Body), Does.Contain("<div id=\"root\"></div>"));
        }
    }

    [Test]
    public async Task Build_WithDebugConfiguration_WhenNpmFails_PreservesErrorOutputAndPreviousOutput()
    {
        var fixtureRoot = Path.Combine(_root, "fake-npm-debug-" + Guid.NewGuid().ToString("N"));
        var fakeBin = Path.Combine(fixtureRoot, "bin");
        var fakeApp = Path.Combine(fixtureRoot, "app");
        var previousOutput = Path.Combine(fixtureRoot, "web");
        var argsLog = Path.Combine(fixtureRoot, "npm-args.log");
        Directory.CreateDirectory(fakeBin);
        Directory.CreateDirectory(fakeApp);
        Directory.CreateDirectory(previousOutput);
        await File.WriteAllTextAsync(Path.Combine(previousOutput, ".mobademo-build-owned"), "MOBAdemo build output\n")
            .ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(previousOutput, "index.html"), "keep-previous-debug")
            .ConfigureAwait(false);

        if (OperatingSystem.IsWindows())
        {
            await File.WriteAllTextAsync(Path.Combine(fakeBin, "node.cmd"), "@echo off\r\nexit /b 0\r\n")
                .ConfigureAwait(false);
            var npmScript = string.Join(
                "\r\n",
                "@echo off",
                $"echo %*>>\"{argsLog}\"",
                "if \"%1\"==\"ci\" exit /b 0",
                "echo fake-npm-stdout-error",
                "echo fake-npm-stderr-error 1>&2",
                "exit /b 42",
                string.Empty);
            await File.WriteAllTextAsync(Path.Combine(fakeBin, "npm.cmd"), npmScript).ConfigureAwait(false);
        }
        else
        {
            var nodePath = Path.Combine(fakeBin, "node");
            var npmPath = Path.Combine(fakeBin, "npm");
            await File.WriteAllTextAsync(nodePath, "#!/usr/bin/env sh\nexit 0\n").ConfigureAwait(false);
            var npmScript = string.Join(
                "\n",
                "#!/usr/bin/env sh",
                $"printf '%s\\n' \"$*\" >> \"{argsLog}\"",
                "if [ \"$1\" = \"ci\" ]; then",
                "  exit 0",
                "fi",
                "echo \"fake-npm-stdout-error\"",
                "echo \"fake-npm-stderr-error\" >&2",
                "exit 42",
                string.Empty);
            await File.WriteAllTextAsync(npmPath, npmScript).ConfigureAwait(false);
            var executableMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;
            File.SetUnixFileMode(nodePath, executableMode);
            File.SetUnixFileMode(npmPath, executableMode);
        }

        var pathValue = fakeBin + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
        var env = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["PATH"] = pathValue,
        };
        var result = await BuildScript.TryRunWithEnvironmentAsync(
            previousOutput,
            env,
            "-Configuration",
            "Debug",
            "-AppPath",
            fakeApp).ConfigureAwait(false);

        var loggedArgs = await File.ReadAllTextAsync(argsLog).ConfigureAwait(false);
        var preservedIndex = await File.ReadAllTextAsync(Path.Combine(previousOutput, "index.html")).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.Not.Zero, result.Output);
            Assert.That(result.Output, Does.Contain("npm run build (development mode) failed with exit code 42."));
            Assert.That(result.Output, Does.Contain("fake-npm-stdout-error"));
            Assert.That(result.Output, Does.Contain("fake-npm-stderr-error"));
            Assert.That(loggedArgs, Does.Contain("run build -- --mode development"));
            Assert.That(preservedIndex, Is.EqualTo("keep-previous-debug"));
        }
    }

    [Test]
    public async Task Build_CopiesTopLevelDotfilesAndDotDirectoriesFromDist()
    {
        var fixtureRoot = Path.Combine(_root, "fake-npm-dotfiles-" + Guid.NewGuid().ToString("N"));
        var fakeBin = Path.Combine(fixtureRoot, "bin");
        var fakeApp = Path.Combine(fixtureRoot, "app");
        var output = Path.Combine(fixtureRoot, "web");
        Directory.CreateDirectory(fakeBin);
        Directory.CreateDirectory(fakeApp);

        if (OperatingSystem.IsWindows())
        {
            await File.WriteAllTextAsync(Path.Combine(fakeBin, "node.cmd"), "@echo off\r\nexit /b 0\r\n")
                .ConfigureAwait(false);
            var npmScript = string.Join(
                "\r\n",
                "@echo off",
                "if \"%1\"==\"ci\" exit /b 0",
                "if \"%1\"==\"run\" if \"%2\"==\"compress\" exit /b 0",
                "mkdir dist\\.well-known",
                "echo ok> dist\\index.html",
                "echo nojekyll> dist\\.nojekyll",
                "echo security> dist\\.well-known\\security.txt",
                "exit /b 0",
                string.Empty);
            await File.WriteAllTextAsync(Path.Combine(fakeBin, "npm.cmd"), npmScript).ConfigureAwait(false);
        }
        else
        {
            var nodePath = Path.Combine(fakeBin, "node");
            var npmPath = Path.Combine(fakeBin, "npm");
            await File.WriteAllTextAsync(nodePath, "#!/usr/bin/env sh\nexit 0\n").ConfigureAwait(false);
            var npmScript = string.Join(
                "\n",
                "#!/usr/bin/env sh",
                "if [ \"$1\" = \"ci\" ]; then exit 0; fi",
                "if [ \"$1\" = \"run\" ] && [ \"$2\" = \"compress\" ]; then exit 0; fi",
                "mkdir -p dist/.well-known",
                "printf 'ok\\n' > dist/index.html",
                "printf 'nojekyll\\n' > dist/.nojekyll",
                "printf 'security\\n' > dist/.well-known/security.txt",
                "exit 0",
                string.Empty);
            await File.WriteAllTextAsync(npmPath, npmScript).ConfigureAwait(false);
            var executableMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;
            File.SetUnixFileMode(nodePath, executableMode);
            File.SetUnixFileMode(npmPath, executableMode);
        }

        var env = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["PATH"] = fakeBin + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH"),
        };
        var result = await BuildScript.TryRunWithEnvironmentAsync(output, env, "-AppPath", fakeApp)
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.Zero, result.Output);
            Assert.That(File.Exists(Path.Combine(output, "index.html")), Is.True);
            Assert.That(File.Exists(Path.Combine(output, ".nojekyll")), Is.True);
            Assert.That(File.Exists(Path.Combine(output, ".well-known", "security.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(output, ".mobademo-build-owned")), Is.True);
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

    private static void CopyDirectory(string source, string target) => BuildScript.CopyDirectory(source, target);

    private sealed record Reply(HttpStatusCode Status, byte[] Body, string ContentEncoding, string? CacheControl);
}
