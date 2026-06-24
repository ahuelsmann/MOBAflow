// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.Common.Security;

namespace Moba.Test.Common;

[TestFixture]
internal sealed class MobaPairingPayloadTests
{
    [Test]
    public void RoundTrip_PreservesHostPortAndKey()
    {
        var payload = MobaPairingPayload.Create("192.168.0.42", 5001, "pair-key-123");
        var json = payload.ToJson();

        Assert.That(MobaPairingPayload.TryParse(json, out var parsed), Is.True);
        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Host, Is.EqualTo("192.168.0.42"));
        Assert.That(parsed.Port, Is.EqualTo(5001));
        Assert.That(parsed.ApiKey, Is.EqualTo("pair-key-123"));
    }

    [Test]
    public void TryParse_RejectsInvalidPayload()
    {
        Assert.That(MobaPairingPayload.TryParse("""{"v":1,"h":"","p":5001,"k":"x"}""", out _), Is.False);
        Assert.That(MobaPairingPayload.TryParse("not-json", out _), Is.False);
    }
}
