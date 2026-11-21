// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdemo;

using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Moba.Demo;

/// <summary>Security headers, caching, precompressed files and routing rules of the demo host (FR-007, FR-051, FR-058, FR-059, FR-070 to FR-073).</summary>
[TestFixture]
internal sealed class MobaDemoHostHardeningTests
{
    private const string ScriptPath = "/assets/app.abc123.js";
    private const string TwinPath = "/assets/twin.js";
    private const string Script = "console.log('mobademo');\nconsole.log('mobademo');\nconsole.log('mobademo');\nconsole.log('mobademo');\n";

    private static readonly (string Name, string Value)[] ExpectedSecurityHeaders =
    [
        ("Content-Security-Policy", "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'; object-src 'none'"),
        ("X-Content-Type-Options", "nosniff"),
        ("Referrer-Policy", "no-referrer"),
        ("Permissions-Policy", "camera=(), microphone=(), geolocation=(), payment=(), usb=()"),
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

    [TestCase(ScriptPath, "br", "bytes=999-", HttpStatusCode.RequestedRangeNotSatisfiable, "Range Not Satisfiable", null)]
    [TestCase(ScriptPath, "", "bytes=999-", HttpStatusCode.RequestedRangeNotSatisfiable, "Range Not Satisfiable", null)]
    [TestCase(ScriptPath, "br", null, HttpStatusCode.PreconditionFailed, "Precondition Failed", "\"nope\"")]
    [TestCase("/", "br", null, HttpStatusCode.PreconditionFailed, "Precondition Failed", "\"nope\"")]
    [TestCase("/", "", "bytes=999-", HttpStatusCode.RequestedRangeNotSatisfiable, "Range Not Satisfiable", null)]
    public async Task ErrorResponse_IsNeutralAndCarriesNoVariantOrImmutableHeaders(
        string path,
        string acceptEncoding,
        string? range,
        HttpStatusCode status,
        string text,
        string? ifMatch)
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            path,
            ("Accept-Encoding", acceptEncoding),
            ("Range", range),
            ("If-Match", ifMatch)).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(status));
            Assert.That(body, Is.EqualTo(text));
            Assert.That(response.Content.Headers.ContentEncoding, Is.Empty);
            Assert.That(response.Headers.ETag, Is.Null);
            Assert.That(response.Headers.CacheControl?.ToString(), Is.EqualTo("no-cache"));
        }

        AssertSecurityHeaders(response);
    }

    [Test]
    public async Task SatisfiableRange_OnVariant_IsConsistentWithTheVariant()
    {
        using var full = await SendAsync(HttpMethod.Get, ScriptPath, ("Accept-Encoding", "br")).ConfigureAwait(false);
        var variant = await full.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        using var part = await SendAsync(
            HttpMethod.Get,
            ScriptPath,
            ("Accept-Encoding", "br"),
            ("Range", "bytes=0-3")).ConfigureAwait(false);
        var bytes = await part.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(part.StatusCode, Is.EqualTo(HttpStatusCode.PartialContent));
            Assert.That(part.Content.Headers.ContentEncoding, Is.EqualTo(Br));
            Assert.That(part.Content.Headers.ContentRange?.ToString(), Is.EqualTo($"bytes 0-3/{variant.Length}"));
            Assert.That(part.Headers.ETag, Is.EqualTo(full.Headers.ETag));
            Assert.That(part.Headers.Vary, Does.Contain("Accept-Encoding"));
            Assert.That(bytes, Is.EqualTo(variant[..4]));
        }
    }

    [TestCase("identity;q=1, br;q=0.1", "")]
    [TestCase("identity, br;q=0.5, gzip;q=0.5", "")]
    [TestCase("br;q=0.2, identity;q=0.9", "")]
    [TestCase("identity;q=0, gzip", "gzip")]
    [TestCase("identity;q=0, br;q=0.5, gzip;q=0.4", "br")]
    [TestCase("identity;q=0", "")]
    [TestCase("*;q=0", "")]
    [TestCase("*;q=0, identity", "")]
    [TestCase("*;q=0, br", "br")]
    [TestCase("br;q=0, *;q=0.5", "gzip")]
    [TestCase("br;q=0.9, gzip;q=0.8", "br")]
    [TestCase("gzip;q=0.1", "gzip")]
    [TestCase("br;q=0.001", "br")]
    public async Task AcceptEncoding_HonoursIdentityAndWildcardQuality(string acceptEncoding, string expectedEncoding)
    {
        // RFC 9110 12.5.3: when nothing is acceptable ("identity;q=0" or "*;q=0" alone) the server may answer 406
        // or serve the file unencoded; the host serves it unencoded.
        using var response = await SendAsync(HttpMethod.Get, ScriptPath, ("Accept-Encoding", acceptEncoding))
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(string.Join(",", response.Content.Headers.ContentEncoding), Is.EqualTo(expectedEncoding));
        }
    }

    [Test]
    public async Task VariantsOfEqualSizeAndTime_HaveDistinctETags()
    {
        using var br = await SendAsync(HttpMethod.Get, TwinPath, ("Accept-Encoding", "br")).ConfigureAwait(false);
        using var gz = await SendAsync(HttpMethod.Get, TwinPath, ("Accept-Encoding", "gzip")).ConfigureAwait(false);
        using var plain = await SendAsync(HttpMethod.Get, TwinPath).ConfigureAwait(false);

        Assert.That(
            new[] { br.Headers.ETag!.ToString(), gz.Headers.ETag!.ToString(), plain.Headers.ETag!.ToString() }.Distinct().Count(),
            Is.EqualTo(3));
    }

    [Test]
    public async Task Revalidation_WithTheTagOfAnotherVariant_DoesNotReturn304()
    {
        using var br = await SendAsync(HttpMethod.Get, TwinPath, ("Accept-Encoding", "br")).ConfigureAwait(false);
        using var sameVariant = await SendAsync(
            HttpMethod.Get,
            TwinPath,
            ("Accept-Encoding", "br"),
            ("If-None-Match", br.Headers.ETag!.ToString())).ConfigureAwait(false);
        using var otherVariant = await SendAsync(
            HttpMethod.Get,
            TwinPath,
            ("Accept-Encoding", "gzip"),
            ("If-None-Match", br.Headers.ETag!.ToString())).ConfigureAwait(false);
        using var uncompressed = await SendAsync(
            HttpMethod.Get,
            TwinPath,
            ("If-None-Match", br.Headers.ETag!.ToString())).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sameVariant.StatusCode, Is.EqualTo(HttpStatusCode.NotModified));
            Assert.That(otherVariant.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(uncompressed.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
    }

    [Test]
    public async Task IfRange_WithTheOwnVariantTag_ReturnsPartialContent()
    {
        using var full = await SendAsync(HttpMethod.Get, ScriptPath, ("Accept-Encoding", "br")).ConfigureAwait(false);
        var variant = await full.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        using var part = await SendAsync(
            HttpMethod.Get,
            ScriptPath,
            ("Accept-Encoding", "br"),
            ("Range", "bytes=0-3"),
            ("If-Range", full.Headers.ETag!.ToString())).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(part.StatusCode, Is.EqualTo(HttpStatusCode.PartialContent));
            Assert.That(await part.Content.ReadAsByteArrayAsync().ConfigureAwait(false), Is.EqualTo(variant[..4]));
        }
    }

    [Test]
    public async Task IfRange_WithAnotherVariantTag_ReturnsTheFullVariant()
    {
        using var gzip = await SendAsync(HttpMethod.Get, ScriptPath, ("Accept-Encoding", "gzip")).ConfigureAwait(false);
        using var full = await SendAsync(
            HttpMethod.Get,
            ScriptPath,
            ("Accept-Encoding", "br"),
            ("Range", "bytes=0-3"),
            ("If-Range", gzip.Headers.ETag!.ToString())).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(full.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(full.Content.Headers.ContentEncoding, Is.EqualTo(Br));
        }
    }

    [Test]
    public async Task IfMatch_WithTheOwnVariantTag_IsAccepted()
    {
        using var first = await SendAsync(HttpMethod.Get, ScriptPath, ("Accept-Encoding", "br")).ConfigureAwait(false);
        using var second = await SendAsync(
            HttpMethod.Get,
            ScriptPath,
            ("Accept-Encoding", "br"),
            ("If-Match", first.Headers.ETag!.ToString())).ConfigureAwait(false);

        Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task IfMatch_WithAnotherVariantTag_FailsThePrecondition()
    {
        using var gzip = await SendAsync(HttpMethod.Get, ScriptPath, ("Accept-Encoding", "gzip")).ConfigureAwait(false);
        using var response = await SendAsync(
            HttpMethod.Get,
            ScriptPath,
            ("Accept-Encoding", "br"),
            ("If-Match", gzip.Headers.ETag!.ToString())).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.PreconditionFailed));
    }

    [Test]
    public async Task IfMatch_WithAWeakOwnVariantTag_FailsThePrecondition()
    {
        // RFC 9110 13.1.1: If-Match uses strong comparison, a weak tag never matches.
        using var first = await SendAsync(HttpMethod.Get, ScriptPath, ("Accept-Encoding", "br")).ConfigureAwait(false);
        using var response = await SendAsync(
            HttpMethod.Get,
            ScriptPath,
            ("Accept-Encoding", "br"),
            ("If-Match", "W/" + first.Headers.ETag!)).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.PreconditionFailed));
    }

    [Test]
    public async Task IfMatch_WithAWeakAndAStrongOwnVariantTag_IsAccepted()
    {
        using var first = await SendAsync(HttpMethod.Get, ScriptPath, ("Accept-Encoding", "br")).ConfigureAwait(false);
        var tag = first.Headers.ETag!.ToString();
        using var response = await SendAsync(
            HttpMethod.Get,
            ScriptPath,
            ("Accept-Encoding", "br"),
            ("If-Match", $"W/{tag}, {tag}")).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task IfNoneMatch_WithAWeakOwnVariantTag_StillReturns304()
    {
        using var first = await SendAsync(HttpMethod.Get, ScriptPath, ("Accept-Encoding", "br")).ConfigureAwait(false);
        using var response = await SendAsync(
            HttpMethod.Get,
            ScriptPath,
            ("Accept-Encoding", "br"),
            ("If-None-Match", "W/" + first.Headers.ETag!)).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotModified));
    }

    [Test]
    public async Task IfNoneMatch_WithAnotherVariantTag_IgnoresIfModifiedSince()
    {
        // RFC 9110 13.2.2: with If-None-Match present, If-Modified-Since is ignored.
        using var gzip = await SendAsync(HttpMethod.Get, ScriptPath, ("Accept-Encoding", "gzip")).ConfigureAwait(false);
        var modified = gzip.Content.Headers.LastModified!.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        using var response = await SendAsync(
            HttpMethod.Get,
            ScriptPath,
            ("Accept-Encoding", "br"),
            ("If-None-Match", gzip.Headers.ETag!.ToString()),
            ("If-Modified-Since", modified)).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task HstsIsAbsent_OnPlainHttpLoopback()
    {
        using var response = await SendAsync(HttpMethod.Get, "/").ConfigureAwait(false);

        Assert.That(response.Headers.Contains("Strict-Transport-Security"), Is.False);
    }

    [TestCase(true, "demo.azurewebsites.net", true)]
    [TestCase(true, "DEMO.example.org:8443", true)]
    [TestCase(false, "demo.azurewebsites.net", false)]
    [TestCase(true, "localhost", false)]
    [TestCase(true, "localhost:5001", false)]
    [TestCase(true, "app.localhost", false)]
    [TestCase(true, "127.0.0.1", false)]
    [TestCase(true, "127.0.0.1:5001", false)]
    [TestCase(true, "[::1]", false)]
    [TestCase(true, "[::1]:5001", false)]
    public void Hsts_IsSentOnlyOverHttpsToANonLoopbackHost(bool isHttps, string host, bool expected)
    {
        Assert.That(HostHardening.ShouldSendHsts(isHttps, new HostString(host)), Is.EqualTo(expected));
    }

    [Test]
    public async Task NotModified_ForAnAsset_KeepsTheImmutableCacheControl()
    {
        using var first = await SendAsync(HttpMethod.Get, ScriptPath).ConfigureAwait(false);
        using var second = await SendAsync(
            HttpMethod.Get,
            ScriptPath,
            ("If-None-Match", first.Headers.ETag!.ToString())).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.NotModified));
            Assert.That(second.Headers.CacheControl?.ToString(), Is.EqualTo("public, max-age=31536000, immutable"));
        }
    }

    [TestCase(ScriptPath + ".br.")]
    [TestCase(ScriptPath + ".gz.")]
    [TestCase(ScriptPath + ".br ")]
    [TestCase(ScriptPath + ".BR")]
    [TestCase(ScriptPath + ".Gz")]
    public async Task CompressedSiblingVariants_OfThePathAreNotServed(string path)
    {
        using var response = await SendAsync(HttpMethod.Get, path, ("Accept-Encoding", "br, gzip")).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
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

        // Variants with identical length and identical timestamp: their file-derived ETags would collide.
        var twin = Path.Combine(assets, "twin.js");
        File.WriteAllText(twin, "twin original content, longer than the variants");
        File.WriteAllBytes(twin + ".br", new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        File.WriteAllBytes(twin + ".gz", new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 });
        var stamp = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        foreach (var file in new[] { twin, twin + ".br", twin + ".gz" })
        {
            File.SetLastWriteTimeUtc(file, stamp);
        }
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
