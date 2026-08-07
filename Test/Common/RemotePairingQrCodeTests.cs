// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Common;

using Moba.Common.Security;

[TestFixture]
internal sealed class RemotePairingQrCodeTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 6, 14, 0, 0, TimeSpan.Zero);

    [Test]
    public void EncodeAndDecode_Should_RoundTripValidPrivateLanInvitation()
    {
        var invitation = CreateInvitation();

        var encoded = RemotePairingQrCode.Encode(invitation);
        var result = RemotePairingQrCode.Decode(encoded, new FixedTimeProvider(Now));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Failure, Is.EqualTo(RemotePairingQrFailure.None));
            Assert.That(result.Invitation, Is.EqualTo(invitation));
        });
    }

    [Test]
    public void Decode_Should_ReturnExpired_WhenInvitationLifetimeElapsed()
    {
        var encoded = RemotePairingQrCode.Encode(CreateInvitation() with { ExpiresAt = Now });

        var result = RemotePairingQrCode.Decode(encoded, new FixedTimeProvider(Now));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Failure, Is.EqualTo(RemotePairingQrFailure.Expired));
            Assert.That(result.Invitation, Is.Null);
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("https://192.168.0.27:5002")]
    [TestCase("MOBAFLOW_PAIRING|1|not-base64")]
    public void Decode_Should_ReturnInvalid_WhenPayloadIsMalformed(string? payload)
    {
        var result = RemotePairingQrCode.Decode(payload, new FixedTimeProvider(Now));

        Assert.That(result.Failure, Is.EqualTo(RemotePairingQrFailure.Invalid));
    }

    [TestCase("8.8.8.8")]
    [TestCase("127.0.0.1")]
    [TestCase("2001:db8::1")]
    public void Encode_Should_RejectNonPrivateEndpoint(string ipAddress)
    {
        var invitation = CreateInvitation() with { IpAddress = ipAddress };

        Assert.That(() => RemotePairingQrCode.Encode(invitation), Throws.ArgumentException);
    }

    [Test]
    public void ToString_Should_RedactFingerprintAndSecret()
    {
        var invitation = CreateInvitation();

        var text = invitation.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Not.Contain(invitation.PairingSecret));
            Assert.That(text, Does.Not.Contain(invitation.ServerPublicKeyFingerprint));
            Assert.That(text, Does.Contain("[REDACTED]"));
        });
    }

    private static RemotePairingQrInvitation CreateInvitation() => new(
        "192.168.0.27",
        5001,
        5002,
        Guid.Parse("11111111-2222-3333-4444-555555555555").ToString("N"),
        new string('A', 64),
        new string('B', 43),
        Now.AddMinutes(2));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}