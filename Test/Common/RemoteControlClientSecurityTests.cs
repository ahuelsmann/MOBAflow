// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Moba.Common.Discovery;
using Moba.Common.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Moba.Test.Common;

[TestFixture]
internal sealed class RemoteControlClientSecurityTests
{
    [Test]
    public void ServerCertificatePinning_Should_MatchSubjectPublicKeyInfoFingerprint()
    {
        using var certificate = CreateCertificate();
        var fingerprint = ServerCertificatePinning.GetFingerprint(certificate);

        var matches = ServerCertificatePinning.Matches(certificate, fingerprint.ToLowerInvariant());

        Assert.Multiple(() =>
        {
            Assert.That(matches, Is.True);
            Assert.That(certificate.Subject, Does.Contain("MOBAflow test"));
        });
    }

    [Test]
    public void ServerCertificatePinning_Should_RejectMissingInvalidOrDifferentPins()
    {
        using var certificate = CreateCertificate();

        Assert.Multiple(() =>
        {
            Assert.That(ServerCertificatePinning.Matches(null, new string('A', 64)), Is.False);
            Assert.That(ServerCertificatePinning.Matches(certificate, "not-a-pin"), Is.False);
            Assert.That(ServerCertificatePinning.Matches(certificate, new string('A', 64)), Is.False);
        });
    }

    [Test]
    public async Task BeginPairingAsync_Should_RejectLegacyDiscoveryMetadata()
    {
        var transport = new FakeTransport();
        var service = new RemoteControlSessionService(new FakeCredentialStore(), transport);
        var legacy = new MobApiDiscoveryEndpoint("192.0.2.1", 5001, null, null, null, 1);

        Assert.That(
            async () => await service.BeginPairingAsync(
                legacy,
                new string('S', 43),
                "MOBAsmart",
                RemoteControlRole.ReadOnly),
            Throws.ArgumentException);
        Assert.That(transport.SubmittedNonces, Is.Empty);
    }

    [Test]
    public async Task BeginPairingAsync_Should_CreateUniqueNonceAndPendingAttempt()
    {
        var transport = new FakeTransport
        {
            SubmissionResult = new RemotePairingSubmissionResult(
                RemotePairingSubmissionStatus.Accepted,
                "request-1",
                "claim-secret",
                "123456")
        };
        var service = new RemoteControlSessionService(new FakeCredentialStore(), transport);

        var first = await service.BeginPairingAsync(
            CreateEndpoint(),
            new string('S', 43),
            " MOBAsmart ",
            RemoteControlRole.RemoteControl);
        await service.BeginPairingAsync(
            CreateEndpoint(),
            new string('S', 43),
            "MOBAsmart",
            RemoteControlRole.ReadOnly);

        Assert.Multiple(() =>
        {
            Assert.That(first.ConfirmationCode, Is.EqualTo("123456"));
            Assert.That(transport.SubmittedNonces, Has.Count.EqualTo(2));
            Assert.That(transport.SubmittedNonces[0], Has.Length.EqualTo(43));
            Assert.That(transport.SubmittedNonces[1], Is.Not.EqualTo(transport.SubmittedNonces[0]));
            Assert.That(transport.LastDisplayName, Is.EqualTo("MOBAsmart"));
        });
    }

    [Test]
    public async Task ClaimAsync_Should_NotPersistWhileApprovalIsPending()
    {
        var store = new FakeCredentialStore();
        var transport = new FakeTransport
        {
            ClaimResult = new RemotePairingClaimResult(RemotePairingClaimStatus.PendingApproval)
        };
        var service = new RemoteControlSessionService(store, transport);

        var result = await service.ClaimAsync(CreateAttempt());

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Null);
            Assert.That(store.Saved, Is.Null);
            Assert.That(service.CurrentAccessSession, Is.Null);
        });
    }

    [Test]
    public async Task ClaimAsync_Should_PersistRefreshCredentialAndKeepAccessTokenInMemory()
    {
        var store = new FakeCredentialStore();
        var transport = new FakeTransport
        {
            ClaimResult = new RemotePairingClaimResult(
                RemotePairingClaimStatus.Succeeded,
                CreateTokenResponse("access-token", "refresh-token"))
        };
        var service = new RemoteControlSessionService(store, transport);

        var session = await service.ClaimAsync(CreateAttempt());

        Assert.Multiple(() =>
        {
            Assert.That(session?.AccessToken, Is.EqualTo("access-token"));
            Assert.That(store.Saved?.RefreshToken, Is.EqualTo("refresh-token"));
            Assert.That(store.Saved?.ServerPublicKeyFingerprint, Is.EqualTo(new string('A', 64)));
            Assert.That(store.SerializedValues, Has.None.Contains("access-token"));
        });
    }

    [Test]
    public async Task RefreshAsync_Should_KeepCurrentSessionWhenRotatedCredentialCannotBePersisted()
    {
        var store = new FakeCredentialStore();
        var transport = new FakeTransport
        {
            ClaimResult = new RemotePairingClaimResult(
                RemotePairingClaimStatus.Succeeded,
                CreateTokenResponse("first-access", "first-refresh"))
        };
        var service = new RemoteControlSessionService(store, transport);
        await service.ClaimAsync(CreateAttempt());
        transport.RefreshResult = CreateTokenResponse("second-access", "second-refresh");
        store.FailOnSave = true;

        Assert.That(async () => await service.RefreshAsync(), Throws.TypeOf<IOException>());
        Assert.That(service.CurrentAccessSession?.AccessToken, Is.EqualTo("first-access"));
    }

    [Test]
    public async Task RestoreAsync_Should_ClearRejectedCredential()
    {
        var store = new FakeCredentialStore
        {
            Saved = CreateCredential("stored-refresh")
        };
        var transport = new FakeTransport { RejectRefresh = true };
        var service = new RemoteControlSessionService(store, transport);

        var session = await service.RestoreAsync();

        Assert.Multiple(() =>
        {
            Assert.That(session, Is.Null);
            Assert.That(store.Saved, Is.Null);
            Assert.That(store.ClearCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task RestoreAsync_Should_ClearMalformedStoredCredential()
    {
        var store = new FakeCredentialStore
        {
            Saved = CreateCredential("stored-refresh") with { ServerPublicKeyFingerprint = "invalid" }
        };
        var service = new RemoteControlSessionService(store, new FakeTransport());

        var session = await service.RestoreAsync();

        Assert.Multiple(() =>
        {
            Assert.That(session, Is.Null);
            Assert.That(store.Saved, Is.Null);
            Assert.That(store.ClearCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void RemoteControlRole_Should_MatchServerWireContract()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)RemoteControlRole.RemoteControl, Is.EqualTo(1));
            Assert.That((int)RemoteControlRole.ReadOnly, Is.EqualTo(2));
        });
    }

    [Test]
    public void SecurityModels_Should_RedactTokensFromDiagnosticText()
    {
        var credential = CreateCredential("refresh-secret");
        var session = new RemoteControlAccessSession(
            "access-secret",
            DateTimeOffset.UtcNow.AddMinutes(5),
            RemoteControlRole.ReadOnly,
            1);
        var token = CreateTokenResponse("access-secret", "refresh-secret");
        var attempt = CreateAttempt();

        Assert.Multiple(() =>
        {
            Assert.That(credential.ToString(), Does.Not.Contain("refresh-secret"));
            Assert.That(session.ToString(), Does.Not.Contain("access-secret"));
            Assert.That(token.ToString(), Does.Not.Contain("access-secret"));
            Assert.That(token.ToString(), Does.Not.Contain("refresh-secret"));
            Assert.That(attempt.ToString(), Does.Not.Contain("claim-secret"));
        });
    }

    private static X509Certificate2 CreateCertificate()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=MOBAflow test", key, HashAlgorithmName.SHA256);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(5));
    }

    private static MobApiDiscoveryEndpoint CreateEndpoint() => new(
        "192.0.2.1",
        5001,
        5443,
        "11111111111111111111111111111111",
        new string('A', 64),
        DiscoveryResponseParser.CurrentProtocolVersion);

    private static RemotePairingAttempt CreateAttempt() => new(
        CreateEndpoint(),
        "request-1",
        "claim-secret",
        "123456");

    private static RemoteControlCredential CreateCredential(string refreshToken) => new(
        "11111111111111111111111111111111",
        "192.0.2.1",
        5443,
        new string('A', 64),
        "credential-1",
        refreshToken,
        RemoteControlRole.ReadOnly,
        1);

    private static RemoteControlTokenResponse CreateTokenResponse(string accessToken, string refreshToken) => new(
        "credential-1",
        accessToken,
        DateTimeOffset.UtcNow.AddMinutes(5),
        refreshToken,
        RemoteControlRole.ReadOnly,
        1);

    private sealed class FakeCredentialStore : IRemoteControlCredentialStore
    {
        public RemoteControlCredential? Saved { get; set; }

        public bool FailOnSave { get; set; }

        public int ClearCount { get; private set; }

        public List<string> SerializedValues { get; } = [];

        public Task<RemoteControlCredential?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Saved);

        public Task SaveAsync(RemoteControlCredential credential, CancellationToken cancellationToken = default)
        {
            if (FailOnSave)
                throw new IOException("Protected storage unavailable.");

            Saved = credential;
            SerializedValues.Add(System.Text.Json.JsonSerializer.Serialize(credential));
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            Saved = null;
            ClearCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTransport : IRemoteControlTransport
    {
        public RemotePairingSubmissionResult SubmissionResult { get; set; } =
            new(RemotePairingSubmissionStatus.Accepted, "request-1", "claim-secret", "123456");

        public RemotePairingClaimResult ClaimResult { get; set; } =
            new(RemotePairingClaimStatus.PendingApproval);

        public RemoteControlTokenResponse RefreshResult { get; set; } =
            CreateTokenResponse("refreshed-access", "refreshed-refresh");

        public bool RejectRefresh { get; set; }

        public List<string> SubmittedNonces { get; } = [];

        public string? LastDisplayName { get; private set; }

        public Task<RemotePairingSubmissionResult> SubmitPairingAsync(
            MobApiDiscoveryEndpoint endpoint,
            string pairingSecret,
            string clientNonce,
            string displayName,
            RemoteControlRole requestedRole,
            CancellationToken cancellationToken = default)
        {
            SubmittedNonces.Add(clientNonce);
            LastDisplayName = displayName;
            return Task.FromResult(SubmissionResult);
        }

        public Task<RemotePairingClaimResult> ClaimPairingAsync(
            MobApiDiscoveryEndpoint endpoint,
            string requestId,
            string claimToken,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ClaimResult);

        public Task<RemoteControlTokenResponse> RefreshAsync(
            RemoteControlCredential credential,
            CancellationToken cancellationToken = default) =>
            RejectRefresh
                ? Task.FromException<RemoteControlTokenResponse>(new RemoteCredentialRejectedException())
                : Task.FromResult(RefreshResult);
    }
}