// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdemo.Host;

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Moba.Demo;

/// <summary>Per-client request limits, forwarded headers and HSTS behind the App Service front end (FR-056, FR-078, plan T8 and T9).</summary>
[TestFixture]
[NonParallelizable]
internal sealed class MobaDemoHostRateLimitTests
{
    private const int NonStaticLimit = 3;
    private const int StaticLimit = 4;
    private const string ScriptPath = "/assets/app.js";
    private const string Script = "console.log('mobademo');\n";
    private const string PlatformSwitch = "--ForwardedHeaders_Enabled=true";
    private const string NoPlatformSwitch = "--ForwardedHeaders_Enabled=false";

    private static readonly HostLimits SmallLimits = new(NonStaticLimit, StaticLimit);
    private static readonly string[] SecurityHeaders =
    [
        "Content-Security-Policy",
        "X-Content-Type-Options",
        "Referrer-Policy",
        "Permissions-Policy",
        "Cross-Origin-Resource-Policy",
    ];

    private RunningHost? _host;

    [TearDown]
    public async Task StopHostAsync()
    {
        if (_host != null)
        {
            await _host.DisposeAsync().ConfigureAwait(false);
            _host = null;
        }
    }

    [Test]
    public async Task NonStaticRequests_AreLimitedPerClientAddress()
    {
        await StartAsync(SmallLimits).ConfigureAwait(false);

        for (var i = 0; i < NonStaticLimit; i++)
        {
            using var accepted = await GetAsync("/health", "10.0.0.1").ConfigureAwait(false);
            Assert.That(accepted.StatusCode, Is.EqualTo(HttpStatusCode.OK), "request " + (i + 1));
        }

        using var rejected = await GetAsync("/health", "10.0.0.1").ConfigureAwait(false);
        await AssertNeutralTooManyRequestsAsync(rejected).ConfigureAwait(false);
    }

    [Test]
    public async Task StaticRequests_AreLimitedPerClientAddress()
    {
        await StartAsync(SmallLimits).ConfigureAwait(false);

        for (var i = 0; i < StaticLimit; i++)
        {
            using var accepted = await GetAsync(ScriptPath, "10.0.0.1").ConfigureAwait(false);
            Assert.That(accepted.StatusCode, Is.EqualTo(HttpStatusCode.OK), "request " + (i + 1));
        }

        using var rejected = await GetAsync(ScriptPath, "10.0.0.1").ConfigureAwait(false);
        using var otherClient = await GetAsync(ScriptPath, "10.0.0.2").ConfigureAwait(false);
        await AssertNeutralTooManyRequestsAsync(rejected).ConfigureAwait(false);
        Assert.That(otherClient.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task FileWithUnknownContentType_IsNotFoundAndCountsAsNonStatic()
    {
        await StartAsync(SmallLimits).ConfigureAwait(false);
        for (var i = 0; i < NonStaticLimit; i++)
        {
            using var notFound = await GetAsync("/assets/data.unknownext", "10.0.0.1").ConfigureAwait(false);
            Assert.That(notFound.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "request " + (i + 1));
        }

        using var rejected = await GetAsync("/assets/data.unknownext", "10.0.0.1").ConfigureAwait(false);
        using var script = await GetAsync(ScriptPath, "10.0.0.1").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rejected.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
            Assert.That(script.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
    }

    [Test]
    public async Task EntryDocumentAtRoot_CountsAsStaticAndNotAsNonStatic()
    {
        await StartAsync(SmallLimits).ConfigureAwait(false);

        for (var i = 0; i < StaticLimit; i++)
        {
            using var accepted = await GetAsync("/", "10.0.0.1").ConfigureAwait(false);
            Assert.That(accepted.StatusCode, Is.EqualTo(HttpStatusCode.OK), "request " + (i + 1));
        }

        using var rejected = await GetAsync("/", "10.0.0.1").ConfigureAwait(false);
        using var health = await GetAsync("/health", "10.0.0.1").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rejected.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
            Assert.That(health.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
    }

    [TestCase("/assets/app.js.br")]
    [TestCase("/assets/app.js.gz")]
    public async Task CompressedVariantRequestedByItsOwnName_IsNotFoundAndCountsAsNonStatic(string path)
    {
        await StartAsync(SmallLimits).ConfigureAwait(false);
        for (var i = 0; i < NonStaticLimit; i++)
        {
            using var notFound = await GetAsync(path, "10.0.0.1").ConfigureAwait(false);
            Assert.That(notFound.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "request " + (i + 1));
        }

        using var rejected = await GetAsync(path, "10.0.0.1").ConfigureAwait(false);
        using var script = await GetAsync(ScriptPath, "10.0.0.1").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rejected.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
            Assert.That(script.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
    }

    [Test]
    public async Task StaticAndNonStaticBudgets_AreIndependent()
    {
        await StartAsync(SmallLimits).ConfigureAwait(false);
        for (var i = 0; i <= NonStaticLimit; i++)
        {
            (await GetAsync("/health", "10.0.0.1").ConfigureAwait(false)).Dispose();
        }

        using var script = await GetAsync(ScriptPath, "10.0.0.1").ConfigureAwait(false);

        Assert.That(script.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [TestCase("GET", "/no-such-path")]
    [TestCase("POST", "/health")]
    [TestCase("POST", "/")]
    public async Task NotFoundAndRejectedMethods_CountAsNonStatic(string method, string path)
    {
        await StartAsync(SmallLimits).ConfigureAwait(false);
        HttpStatusCode first;
        using (var response = await SendAsync(new HttpMethod(method), path, "10.0.0.1").ConfigureAwait(false))
        {
            first = response.StatusCode;
        }

        for (var i = 1; i < NonStaticLimit; i++)
        {
            (await SendAsync(new HttpMethod(method), path, "10.0.0.1").ConfigureAwait(false)).Dispose();
        }

        using var rejected = await SendAsync(new HttpMethod(method), path, "10.0.0.1").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.EqualTo(method == "GET" ? HttpStatusCode.NotFound : HttpStatusCode.MethodNotAllowed));
            Assert.That(rejected.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
        }
    }

    [Test]
    public async Task AnotherClientAddress_IsNotLimited()
    {
        await StartAsync(SmallLimits).ConfigureAwait(false);
        for (var i = 0; i <= NonStaticLimit; i++)
        {
            (await GetAsync("/health", "10.0.0.1").ConfigureAwait(false)).Dispose();
        }

        using var other = await GetAsync("/health", "10.0.0.2").ConfigureAwait(false);

        Assert.That(other.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task HeadAndConditional304Requests_CountAsRequests()
    {
        await StartAsync(SmallLimits).ConfigureAwait(false);
        using var first = await GetAsync(ScriptPath, "10.0.0.1").ConfigureAwait(false);
        var tag = first.Headers.ETag!.ToString();
        using var head = await SendAsync(HttpMethod.Head, ScriptPath, "10.0.0.1").ConfigureAwait(false);
        using var notModified = await SendAsync(HttpMethod.Get, ScriptPath, "10.0.0.1", ("If-None-Match", tag))
            .ConfigureAwait(false);
        using var notModifiedAgain = await SendAsync(HttpMethod.Get, ScriptPath, "10.0.0.1", ("If-None-Match", tag))
            .ConfigureAwait(false);
        using var rejected = await SendAsync(HttpMethod.Get, ScriptPath, "10.0.0.1", ("If-None-Match", tag))
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(head.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(notModified.StatusCode, Is.EqualTo(HttpStatusCode.NotModified));
            Assert.That(notModifiedAgain.StatusCode, Is.EqualTo(HttpStatusCode.NotModified));
            Assert.That(rejected.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
        }
    }

    [Test]
    public async Task EquivalentAddressForms_ShareOnePartition()
    {
        await StartAsync(SmallLimits).ConfigureAwait(false);
        (await GetAsync("/health", "::ffff:10.0.0.1").ConfigureAwait(false)).Dispose();
        (await GetAsync("/health", "10.0.0.1:4711").ConfigureAwait(false)).Dispose();
        (await GetAsync("/health", "10.0.0.1").ConfigureAwait(false)).Dispose();

        using var rejected = await GetAsync("/health", "[::ffff:10.0.0.1]:4712").ConfigureAwait(false);

        Assert.That(rejected.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
    }

    [Test]
    public async Task ProductionDefaults_Allow60NonStaticAnd200StaticRequests()
    {
        await StartAsync(null).ConfigureAwait(false);

        for (var i = 0; i < 60; i++)
        {
            (await GetAsync("/health", "10.0.0.1").ConfigureAwait(false)).Dispose();
        }

        using var nonStatic = await GetAsync("/health", "10.0.0.1").ConfigureAwait(false);
        for (var i = 0; i < 200; i++)
        {
            (await GetAsync("/", "10.0.0.1").ConfigureAwait(false)).Dispose();
        }

        using var staticFile = await GetAsync("/", "10.0.0.1").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(nonStatic.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
            Assert.That(staticFile.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ForwardedAddress_IsTheRightMostEntry_ForEveryStateOfThePlatformSwitch(bool platformSwitch)
    {
        await StartAsync(null, [platformSwitch ? PlatformSwitch : NoPlatformSwitch]).ConfigureAwait(false);

        // The front end appends the visitor's address; an entry the caller put in front of it is ignored once,
        // not twice (one forwarding pass).
        using var response = await GetAsync("/probe", "9.9.9.9, 10.0.0.2:5555").ConfigureAwait(false);

        Assert.That(await response.Content.ReadAsStringAsync().ConfigureAwait(false), Is.EqualTo("http|10.0.0.2"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ForwardedProtoHttps_SetsTheSchemeAndHsts_ForANonLoopbackHost(bool platformSwitch)
    {
        await StartAsync(null, [platformSwitch ? PlatformSwitch : NoPlatformSwitch]).ConfigureAwait(false);

        using var response = await SendAsync(
            HttpMethod.Get,
            "/probe",
            "10.0.0.2",
            ("X-Forwarded-Proto", "https"),
            ("Host", "mobaflow-demo.azurewebsites.net")).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await response.Content.ReadAsStringAsync().ConfigureAwait(false), Is.EqualTo("https|10.0.0.2"));
            Assert.That(response.Headers.GetValues("Strict-Transport-Security").Single(), Is.EqualTo("max-age=31536000"));
        }
    }

    [Test]
    public async Task HstsIsAbsent_ForPlainHttpAndForALoopbackHostEvenWhenForwardedAsHttps()
    {
        await StartAsync(null).ConfigureAwait(false);

        using var plain = await SendAsync(HttpMethod.Get, "/probe", "10.0.0.2", ("Host", "mobaflow-demo.azurewebsites.net"))
            .ConfigureAwait(false);
        using var loopback = await SendAsync(HttpMethod.Get, "/probe", "10.0.0.2", ("X-Forwarded-Proto", "https"))
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await plain.Content.ReadAsStringAsync().ConfigureAwait(false), Is.EqualTo("http|10.0.0.2"));
            Assert.That(plain.Headers.Contains("Strict-Transport-Security"), Is.False);
            Assert.That(await loopback.Content.ReadAsStringAsync().ConfigureAwait(false), Is.EqualTo("https|10.0.0.2"));
            Assert.That(loopback.Headers.Contains("Strict-Transport-Security"), Is.False);
        }
    }

    [Test]
    public async Task ForwardedHost_IsNotHonoured()
    {
        await StartAsync(null).ConfigureAwait(false);

        using var response = await SendAsync(
            HttpMethod.Get,
            "/probe",
            "10.0.0.2",
            ("X-Forwarded-Proto", "https"),
            ("X-Forwarded-Host", "mobaflow-demo.azurewebsites.net")).ConfigureAwait(false);

        Assert.That(response.Headers.Contains("Strict-Transport-Security"), Is.False);
    }

    private static async Task AssertNeutralTooManyRequestsAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
            Assert.That(body, Is.EqualTo("Too Many Requests"));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/plain"));
            Assert.That(response.Headers.CacheControl?.ToString(), Is.EqualTo("no-cache"));
            Assert.That(response.Headers.RetryAfter?.Delta?.TotalSeconds, Is.GreaterThanOrEqualTo(1));
            Assert.That(response.Headers.RetryAfter?.Delta?.TotalSeconds % 1, Is.Zero);
            foreach (var name in SecurityHeaders)
            {
                Assert.That(response.Headers.Contains(name), Is.True, name);
            }
        }
    }

    private async Task StartAsync(HostLimits? limits, string[]? extraArgs = null) =>
        _host = await RunningHost.StartAsync(
            configure: app => app.MapGet(
                "/probe",
                (HttpContext context) => context.Request.Scheme + "|" + context.Connection.RemoteIpAddress),
            seedWebRoot: SeedWebRoot,
            limits: limits,
            extraArgs: extraArgs).ConfigureAwait(false);

    private static void SeedWebRoot(string webRoot)
    {
        var assets = Directory.CreateDirectory(Path.Combine(webRoot, "assets")).FullName;
        File.WriteAllText(Path.Combine(assets, "app.js"), Script);
        File.WriteAllText(Path.Combine(assets, "data.unknownext"), Script);
        File.WriteAllBytes(Path.Combine(assets, "app.js.br"), [1, 2, 3]);
        File.WriteAllBytes(Path.Combine(assets, "app.js.gz"), [1, 2, 3]);
    }

    private Task<HttpResponseMessage> GetAsync(string path, string forwardedFor) =>
        SendAsync(HttpMethod.Get, path, forwardedFor);

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        string forwardedFor,
        params (string Name, string Value)[] headers)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));
        request.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedFor);
        foreach (var (name, value) in headers)
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }

        return await _host!.Client.SendAsync(request).ConfigureAwait(false);
    }
}
