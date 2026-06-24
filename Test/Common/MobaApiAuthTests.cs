// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.Common.Configuration;
using Moba.Common.Security;

namespace Moba.Test.Common;

[TestFixture]
internal sealed class MobaApiAuthTests
{
    [Test]
    public void TryEnsureApiKey_GeneratesKey_WhenMissing()
    {
        var settings = new RestApiSettings();

        var generated = MobaApiAuth.TryEnsureApiKey(settings, out var apiKey);

        Assert.That(generated, Is.True);
        Assert.That(apiKey, Is.Not.Empty);
        Assert.That(settings.ApiKey, Is.EqualTo(apiKey));
    }

    [Test]
    public void TryEnsureApiKey_ReusesExistingKey()
    {
        var settings = new RestApiSettings { ApiKey = "existing-key" };

        var generated = MobaApiAuth.TryEnsureApiKey(settings, out var apiKey);

        Assert.That(generated, Is.False);
        Assert.That(apiKey, Is.EqualTo("existing-key"));
    }

    [Test]
    public void KeysMatch_UsesConstantTimeComparison()
    {
        Assert.That(MobaApiAuth.KeysMatch("abc", "abc"), Is.True);
        Assert.That(MobaApiAuth.KeysMatch("abc", "abd"), Is.False);
        Assert.That(MobaApiAuth.KeysMatch(null, "abc"), Is.False);
    }

    [Test]
    public void IsPublicPath_OnlyAllowsHealthEndpoint()
    {
        Assert.That(MobaApiAuth.IsPublicPath("/api/photos/health"), Is.True);
        Assert.That(MobaApiAuth.IsPublicPath("/api/status"), Is.False);
    }
}
