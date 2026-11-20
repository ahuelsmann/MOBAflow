// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAweb.Host;

using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Builder;

/// <summary>Security headers, caching, precompressed files and routing rules of the demo host (FR-007, FR-051, FR-058, FR-059, FR-070 to FR-073).</summary>
[TestFixture]
internal sealed class MobaWebHostHardeningTests
{
    private const string ScriptPath = "/assets/app.abc123.js";
    private const string Script = "console.log('mobaweb');\nconsole.log('mobaweb');\nconsole.log('mobaweb');\nconsole.log('mobaweb');\n";

    private static readonly (string Name, string Value)[] ExpectedSecurityHeaders =
    [
        ("Content-Security-Policy", "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'; object-src 'none'"),
        ("X-Content-Type-Options", "nosniff"),
        ("Referrer-Policy", "no-referrer"),
        ("Permissions-Policy", "camera=(), microphone=(), geolocation=(), payment=(), usb=()"),
        ("Strict-Transport-Security", "max-age=31536000"),
        ("Cross-Origin-Resource-Policy", "same-origin"),
    ];

    private static readonly string[] SameOrigin = ["same-origin"];
    private static readonly string[] Br = ["br"];
    private static readonly string[] ReadMethods = ["GET", "HEAD"];
    private static readonly string[] Paths = ["/", "/health", ScriptPath, "/no-such-path"];
    private static readonly string[] CookiePaths = ["/", "/health", ScriptPath, "/no-such-path", "/robots.txt"];
    private static readonly string[] RobotsLines = ["User-agent: *", "Allow: /$", "Disallow: /"];
    private static readonly string[] Encodings = ["br", "gzip", string.Empty];

    private RunningHost? _host;

    [SetUp]
    public async Task StartHostAsync() => _host = await StartAsync().ConfigureAwait(false);

    [TearDown]
    public async Task StopHostAsync()
    {
        if (_host != null)
        {
            await _host.DisposeAsync().ConfigureAwait(false);
        }
    }

    [TestCase("GET", "/", HttpStatusCode.OK)]
    [TestCase("GET", "/health", HttpStatusCode.OK)]
    [TestCase("GET", "/robots.txt", HttpStatusCode.OK)]
    [TestCase("GET", "/no-such-path", HttpStatusCode.NotFound)]
    [TestCase("GET", "/boom", HttpStatusCode.InternalServerError)]
    [TestCase("POST", "/", HttpStatusCode.MethodNotAllowed)]
    public async Task Response_CarriesSecurityHeaders(string method, string path, HttpStatusCode status)
    {
        using var response = await SendAsync(new HttpMethod(method), path).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(status));
        AssertSecurityHeaders(response);
    }

    [Test]
    public async Task NotModifiedResponse_CarriesSecurityHeaders()
    {
        using var first = await SendAsync(HttpMethod.Get, "/").ConfigureAwait(false);
        using var second = await SendAsync(HttpMethod.Get, "/", ("If-None-Match", first.Headers.ETag!.ToString()))
            .ConfigureAwait(false);

        Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.NotModified));
        AssertSecurityHeaders(second);
    }

    [Test]
    public async Task EntryDocument_SendsCrossOriginOpenerPolicy()
    {
        using var response = await SendAsync(HttpMethod.Get, "/").ConfigureAwait(false);

        Assert.That(response.Headers.GetValues("Cross-Origin-Opener-Policy"), Is.EqualTo(SameOrigin));
    }

    [Test]
    public async Task HashedAsset_IsCachedForAYearAsImmutable()
    {
        using var response = await SendAsync(HttpMethod.Get, ScriptPath).ConfigureAwait(false);

        Assert.That(response.Headers.CacheControl?.ToString(), Is.EqualTo("public, max-age=31536000, immutable"));
    }

    [TestCase("/")]
    [TestCase("/health")]
    [TestCase("/robots.txt")]
    [TestCase("/no-such-path")]
    public async Task EverythingElse_IsNoCache(string path)
    {
        using var response = await SendAsync(HttpMethod.Get, path).ConfigureAwait(false);

        Assert.That(response.Headers.CacheControl?.ToString(), Is.EqualTo("no-cache"));
    }

    [Test]
    public async Task EntryDocument_RevalidatesWithETag()
    {
        using var first = await SendAsync(HttpMethod.Get, "/").ConfigureAwait(false);
        using var second = await SendAsync(HttpMethod.Get, "/", ("If-None-Match", first.Headers.ETag!.ToString()))
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.Content.Headers.LastModified, Is.Not.Null);
            Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.NotModified));
            Assert.That(second.Headers.CacheControl?.ToString(), Is.EqualTo("no-cache"));
        }
    }

    [TestCase("br, gzip", "br")]
    [TestCase("gzip, br", "br")]
    [TestCase("gzip", "gzip")]
    [TestCase("br;q=0, gzip", "gzip")]
    [TestCase("gzip;q=1.0, br;q=0.5", "gzip")]
    [TestCase("*", "br")]
    [TestCase("*;q=0, gzip", "gzip")]
    [TestCase("identity", "")]
    [TestCase("br;q=0, gzip;q=0", "")]
    [TestCase(null, "")]
    public async Task PrecompressedFile_IsServedByAcceptEncoding(string? acceptEncoding, string expectedEncoding)
    {
        using var response = await SendAsync(HttpMethod.Get, ScriptPath, ("Accept-Encoding", acceptEncoding))
            .ConfigureAwait(false);
        var body = await DecodeAsync(response).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(string.Join(",", response.Content.Headers.ContentEncoding), Is.EqualTo(expectedEncoding));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/javascript"));
            Assert.That(response.Headers.Vary, Does.Contain("Accept-Encoding"));
            Assert.That(body, Is.EqualTo(Script));
        }
    }

    [Test]
    public async Task EntryDocument_ServesPrecompressedVariantWithHtmlType()
    {
        using var response = await SendAsync(HttpMethod.Get, "/", ("Accept-Encoding", "br")).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Content.Headers.ContentEncoding, Is.EqualTo(Br));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
            Assert.That(response.Headers.GetValues("Cross-Origin-Opener-Policy"), Is.EqualTo(SameOrigin));
            Assert.That(await DecodeAsync(response).ConfigureAwait(false), Is.EqualTo(RunningHost.FixtureDocument));
        }
    }

    [TestCase("br")]
    [TestCase("gzip")]
    [TestCase("")]
    public async Task EachVariant_RevalidatesWithItsOwnETagAndVary(string acceptEncoding)
    {
        using var first = await SendAsync(HttpMethod.Get, ScriptPath, ("Accept-Encoding", acceptEncoding))
            .ConfigureAwait(false);
        using var second = await SendAsync(
            HttpMethod.Get,
            ScriptPath,
            ("Accept-Encoding", acceptEncoding),
            ("If-None-Match", first.Headers.ETag!.ToString())).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.NotModified));
            Assert.That(second.Headers.ETag, Is.EqualTo(first.Headers.ETag));
            Assert.That(second.Headers.Vary, Does.Contain("Accept-Encoding"));
        }
    }

    [Test]
    public async Task Variants_HaveDistinctETags()
    {
        var tags = new List<string>();
        foreach (var encoding in Encodings)
        {
            using var response = await SendAsync(HttpMethod.Get, ScriptPath, ("Accept-Encoding", encoding))
                .ConfigureAwait(false);
            tags.Add(response.Headers.ETag!.ToString());
        }

        Assert.That(tags.Distinct().Count(), Is.EqualTo(3));
    }

    [Test]
    public async Task HeadRequest_ForPrecompressedFile_SendsHeadersWithoutBody()
    {
        using var response = await SendAsync(HttpMethod.Head, ScriptPath, ("Accept-Encoding", "br"))
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentEncoding, Is.EqualTo(Br));
            Assert.That(response.Headers.Vary, Does.Contain("Accept-Encoding"));
            Assert.That(await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false), Is.Empty);
        }
    }

    [TestCase(ScriptPath + ".br")]
    [TestCase(ScriptPath + ".gz")]
    public async Task CompressedSibling_IsNotServedAsItsOwnResource(string path)
    {
        using var response = await SendAsync(HttpMethod.Get, path, ("Accept-Encoding", "br, gzip"))
            .ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task NotFound_IsNeutralPlainText()
    {
        using var response = await SendAsync(HttpMethod.Get, "/no-such-path").ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/plain"));
            Assert.That(body, Is.EqualTo("Not Found"));
        }
    }

    [Test]
    public async Task UnhandledException_Neutral500_KeepsSecurityHeaders()
    {
        using var response = await SendAsync(HttpMethod.Get, "/boom").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            Assert.That(await response.Content.ReadAsStringAsync().ConfigureAwait(false), Is.EqualTo("Internal Server Error"));
        }

        AssertSecurityHeaders(response);
    }

    [Test]
    public async Task RobotsTxt_AllowsTheStartPageOnly()
    {
        using var response = await SendAsync(HttpMethod.Get, "/robots.txt").ConfigureAwait(false);
        var lines = (await response.Content.ReadAsStringAsync().ConfigureAwait(false))
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/plain"));
            Assert.That(lines, Is.EqualTo(RobotsLines));
        }
    }

    [TestCase("/health")]
    [TestCase("/robots.txt")]
    [TestCase("/")]
    public async Task HeadRequest_IsAnswered(string path)
    {
        using var response = await SendAsync(HttpMethod.Head, path).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [TestCase("POST")]
    [TestCase("PUT")]
    [TestCase("DELETE")]
    [TestCase("PATCH")]
    [TestCase("OPTIONS")]
    public async Task NonReadMethods_AreRejectedWith405(string method)
    {
        foreach (var path in Paths)
        {
            using var response = await SendAsync(new HttpMethod(method), path).ConfigureAwait(false);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.MethodNotAllowed), path);
                Assert.That(response.Content.Headers.Allow, Is.EquivalentTo(ReadMethods), path);
            }
        }
    }

    [Test]
    public async Task Responses_SetNoCookie()
    {
        foreach (var path in CookiePaths)
        {
            using var response = await SendAsync(HttpMethod.Get, path).ConfigureAwait(false);
            Assert.That(response.Headers.Contains("Set-Cookie"), Is.False, path);
        }
    }

    [Test]
    public async Task WebSocketUpgradeRequest_IsNotSwitched()
    {
        using var tcp = new TcpClient();
        await tcp.ConnectAsync(_host!.Address.Host, _host!.Address.Port).ConfigureAwait(false);
        var stream = tcp.GetStream();
        var request = Encoding.ASCII.GetBytes(
            "GET /health HTTP/1.1\r\nHost: localhost\r\nConnection: Upgrade\r\nUpgrade: websocket\r\n"
            + "Sec-WebSocket-Version: 13\r\nSec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(request).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.ASCII);
        var answer = await reader.ReadToEndAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(answer, Does.Not.Contain(" 101 "));
            Assert.That(answer, Does.Not.Contain("Sec-WebSocket-Accept"));
        }
    }

    private static void AssertSecurityHeaders(HttpResponseMessage response)
    {
        foreach (var (name, value) in ExpectedSecurityHeaders)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Headers.TryGetValues(name, out var values), Is.True, name);
                Assert.That(values, Is.EqualTo(new[] { value }), name);
            }
        }
    }

    private static Task<RunningHost> StartAsync() =>
        RunningHost.StartAsync(
            configure: app => app.MapGet("/boom", (Func<string>)(() => throw new InvalidOperationException("boom"))),
            seedWebRoot: SeedWebRoot);

    private static void SeedWebRoot(string webRoot)
    {
        var assets = Directory.CreateDirectory(Path.Combine(webRoot, "assets")).FullName;
        WriteVariants(Path.Combine(assets, "app.abc123.js"), Script);
        WriteVariants(Path.Combine(webRoot, "index.html"), RunningHost.FixtureDocument);
    }

    private static void WriteVariants(string path, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        File.WriteAllBytes(path, bytes);
        File.WriteAllBytes(path + ".br", Compress(bytes, stream => new BrotliStream(stream, CompressionLevel.SmallestSize)));
        File.WriteAllBytes(path + ".gz", Compress(bytes, stream => new GZipStream(stream, CompressionLevel.SmallestSize)));
    }

    private static byte[] Compress(byte[] bytes, Func<Stream, Stream> wrap)
    {
        using var target = new MemoryStream();
        using (var compressor = wrap(target))
        {
            compressor.Write(bytes);
        }

        return target.ToArray();
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        params (string Name, string? Value)[] headers)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));
        foreach (var (name, value) in headers)
        {
            if (!string.IsNullOrEmpty(value))
            {
                request.Headers.TryAddWithoutValidation(name, value);
            }
        }

        return await _host!.Client.SendAsync(request).ConfigureAwait(false);
    }

    private static async Task<string> DecodeAsync(HttpResponseMessage response)
    {
        var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        using var source = new MemoryStream(bytes);
        var encoding = response.Content.Headers.ContentEncoding.SingleOrDefault();
        using Stream decoded = encoding switch
        {
            "br" => new BrotliStream(source, CompressionMode.Decompress),
            "gzip" => new GZipStream(source, CompressionMode.Decompress),
            _ => source,
        };
        using var reader = new StreamReader(decoded, Encoding.UTF8);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }
}
