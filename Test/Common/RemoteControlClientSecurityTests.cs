// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Moba.Common.Discovery;
using Moba.Common.Security;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

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
    public async Task GetConnectionSessionAsync_Should_ReturnPinnedEndpointWithoutRefreshingValidSession()
    {
        var transport = new FakeTransport
        {
            ClaimResult = new RemotePairingClaimResult(
                RemotePairingClaimStatus.Succeeded,
                CreateTokenResponse("access-token", "refresh-token"))
        };
        var service = new RemoteControlSessionService(new FakeCredentialStore(), transport);
        await service.ClaimAsync(CreateAttempt());

        var connection = await service.GetConnectionSessionAsync(TimeSpan.FromSeconds(30));

        Assert.Multiple(() =>
        {
            Assert.That(connection, Is.Not.Null);
            Assert.That(connection!.Endpoint, Is.EqualTo(CreateEndpoint()));
            Assert.That(connection.AccessSession.AccessToken, Is.EqualTo("access-token"));
            Assert.That(transport.RefreshCount, Is.Zero);
        });
    }

    [Test]
    public async Task GetConnectionSessionAsync_Should_RotateSessionBeforeExpiry()
    {
        var transport = new FakeTransport
        {
            ClaimResult = new RemotePairingClaimResult(
                RemotePairingClaimStatus.Succeeded,
                CreateTokenResponse(
                    "expiring-access",
                    "first-refresh",
                    DateTimeOffset.UtcNow.AddSeconds(10))),
            RefreshResult = CreateTokenResponse("refreshed-access", "rotated-refresh")
        };
        var service = new RemoteControlSessionService(new FakeCredentialStore(), transport);
        await service.ClaimAsync(CreateAttempt());

        var connection = await service.GetConnectionSessionAsync(TimeSpan.FromSeconds(30));

        Assert.Multiple(() =>
        {
            Assert.That(connection?.AccessSession.AccessToken, Is.EqualTo("refreshed-access"));
            Assert.That(transport.RefreshCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task AuthenticatedHttpClient_Should_SendBearerTokenToPinnedHttpsEndpoint()
    {
        var transport = new FakeTransport
        {
            ClaimResult = new RemotePairingClaimResult(
                RemotePairingClaimStatus.Succeeded,
                CreateTokenResponse("access-token", "refresh-token"))
        };
        var sessionService = new RemoteControlSessionService(new FakeCredentialStore(), transport);
        await sessionService.ClaimAsync(CreateAttempt());
        var handler = new RecordingHttpMessageHandler();
        var factory = new FakeRemoteControlHttpClientFactory(handler);
        var client = new RemoteControlAuthenticatedHttpClient(sessionService, factory);

        using var response = await client.GetAsync("api/runtime-settings");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.RequestMessage, Is.Null);
            Assert.That(factory.LastEndpoint, Is.EqualTo(CreateEndpoint()));
            Assert.That(handler.LastRequestUri, Is.EqualTo(new Uri("https://192.0.2.1:5443/api/runtime-settings")));
            Assert.That(handler.LastAuthorizationScheme, Is.EqualTo("Bearer"));
            Assert.That(handler.LastAuthorizationParameter, Is.EqualTo("access-token"));
        });
    }

    [Test]
    public async Task AuthenticatedHttpClient_Should_RejectCrossAuthorityPathBeforeSendingCredential()
    {
        var transport = new FakeTransport
        {
            ClaimResult = new RemotePairingClaimResult(
                RemotePairingClaimStatus.Succeeded,
                CreateTokenResponse("access-token", "refresh-token"))
        };
        var sessionService = new RemoteControlSessionService(new FakeCredentialStore(), transport);
        await sessionService.ClaimAsync(CreateAttempt());
        var handler = new RecordingHttpMessageHandler();
        var client = new RemoteControlAuthenticatedHttpClient(
            sessionService,
            new FakeRemoteControlHttpClientFactory(handler));

        Assert.That(
            async () => await client.GetAsync("//attacker.example/api/runtime-settings"),
            Throws.TypeOf<ArgumentException>());
        Assert.That(handler.LastAuthorizationParameter, Is.Null);
    }

    [Test]
    public async Task AuthenticatedHttpClient_Should_PostContentWithBearerToken()
    {
        var transport = new FakeTransport
        {
            ClaimResult = new RemotePairingClaimResult(
                RemotePairingClaimStatus.Succeeded,
                CreateTokenResponse("access-token", "refresh-token"))
        };
        var sessionService = new RemoteControlSessionService(new FakeCredentialStore(), transport);
        await sessionService.ClaimAsync(CreateAttempt());
        var handler = new RecordingHttpMessageHandler();
        var client = new RemoteControlAuthenticatedHttpClient(
            sessionService,
            new FakeRemoteControlHttpClientFactory(handler));
        using var content = new StringContent("{\"value\":1}", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("api/runtime/commands/test", content);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(handler.LastMethod, Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.LastContent, Is.EqualTo("{\"value\":1}"));
            Assert.That(handler.LastAuthorizationParameter, Is.EqualTo("access-token"));
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
    public async Task RestoreAsync_Should_RequireNewPairingAndRecover_WhenSecureStorageWasLost()
    {
        var store = new FakeCredentialStore();
        var transport = new FakeTransport
        {
            ClaimResult = new RemotePairingClaimResult(
                RemotePairingClaimStatus.Succeeded,
                CreateTokenResponse("replacement-access", "replacement-refresh"))
        };
        var service = new RemoteControlSessionService(store, transport);

        var restored = await service.RestoreAsync();
        var pairing = await service.BeginPairingAsync(
            CreateEndpoint(),
            new string('P', 43),
            "MOBAsmart replacement pairing",
            RemoteControlRole.ReadOnly);
        var replacement = await service.ClaimAsync(pairing);

        Assert.Multiple(() =>
        {
            Assert.That(restored, Is.Null);
            Assert.That(transport.RefreshCount, Is.Zero);
            Assert.That(replacement?.AccessToken, Is.EqualTo("replacement-access"));
            Assert.That(store.Saved?.RefreshToken, Is.EqualTo("replacement-refresh"));
            Assert.That(service.CurrentConnectionSession, Is.Not.Null);
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

    private static RemoteControlTokenResponse CreateTokenResponse(
        string accessToken,
        string refreshToken,
        DateTimeOffset? expiresAt = null) => new(
        "credential-1",
        accessToken,
        expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(5),
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

        public int RefreshCount { get; private set; }

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
            CancellationToken cancellationToken = default)
        {
            RefreshCount++;
            return RejectRefresh
                ? Task.FromException<RemoteControlTokenResponse>(new RemoteCredentialRejectedException())
                : Task.FromResult(RefreshResult);
        }
    }

    private sealed class FakeRemoteControlHttpClientFactory(RecordingHttpMessageHandler handler)
        : IRemoteControlHttpClientFactory
    {
        public MobApiDiscoveryEndpoint? LastEndpoint { get; private set; }

        public HttpClient CreateClient(MobApiDiscoveryEndpoint endpoint)
        {
            LastEndpoint = endpoint;
            return new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = new UriBuilder(Uri.UriSchemeHttps, endpoint.IpAddress, endpoint.HttpsPort!.Value).Uri
            };
        }

        public HttpMessageHandler CreateHandler(MobApiDiscoveryEndpoint endpoint)
        {
            LastEndpoint = endpoint;
            return handler;
        }

        public bool ValidateServerCertificate(MobApiDiscoveryEndpoint endpoint, X509Certificate? certificate)
        {
            LastEndpoint = endpoint;
            return certificate is not null;
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public string? LastContent { get; private set; }

        public HttpMethod? LastMethod { get; private set; }

        public string? LastAuthorizationParameter { get; private set; }

        public string? LastAuthorizationScheme { get; private set; }

        public Uri? LastRequestUri { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastMethod = request.Method;
            LastRequestUri = request.RequestUri;
            LastAuthorizationScheme = request.Headers.Authorization?.Scheme;
            LastAuthorizationParameter = request.Headers.Authorization?.Parameter;
            LastContent = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };
        }
    }
}