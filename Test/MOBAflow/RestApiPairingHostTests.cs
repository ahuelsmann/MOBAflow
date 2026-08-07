#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.MOBAflow;

using Moba.Common.Discovery;
using Moba.Common.Security;
using Moba.WinUI.Service;

using System.Net;
using System.Net.Http.Json;

[TestFixture]
internal sealed class RestApiPairingHostTests
{
    private const string Fingerprint = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Test]
    public async Task OpenAdminPairingAsync_Should_RequestOnlyAdminAndCreateValidatedQrPayload()
    {
        var client = new FakeHostClient(CreateOpenResponse(Fingerprint));
        var host = new RestApiPairingHost(client, new FakeEndpointProvider(CreateEndpoint(Fingerprint)));

        var invitation = await host.OpenAdminPairingAsync();
        var decoded = RemotePairingQrCode.Decode(invitation.EncodedQrPayload);

        Assert.Multiple(() =>
        {
            Assert.That(client.LastRequestUri, Is.EqualTo("api/control-plane/security/pairing/open"));
            Assert.That(client.LastRequestBody, Does.Contain("\"allowedRole\":1"));
            Assert.That(decoded.IsSuccess, Is.True);
            Assert.That(decoded.Invitation?.IpAddress, Is.EqualTo("192.168.0.27"));
            Assert.That(decoded.Invitation?.HttpPort, Is.EqualTo(5001));
            Assert.That(decoded.Invitation?.HttpsPort, Is.EqualTo(5002));
            Assert.That(invitation.ToString(), Does.Not.Contain(new string('B', 43)));
        });
    }

    [Test]
    public void OpenAdminPairingAsync_Should_RejectFingerprintMismatch()
    {
        var client = new FakeHostClient(CreateOpenResponse(new string('C', 64)));
        var host = new RestApiPairingHost(client, new FakeEndpointProvider(CreateEndpoint(Fingerprint)));

        Assert.That(async () => await host.OpenAdminPairingAsync(), Throws.TypeOf<InvalidDataException>());
    }

    [Test]
    public async Task ApproveAsync_Should_SendHostOnlyDecisionRoute()
    {
        var client = new FakeHostClient(new HttpResponseMessage(HttpStatusCode.NoContent));
        var host = new RestApiPairingHost(client, new FakeEndpointProvider(CreateEndpoint(Fingerprint)));
        var requestId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee").ToString("N");

        await host.ApproveAsync(requestId);

        Assert.That(
            client.LastRequestUri,
            Is.EqualTo($"api/control-plane/security/pairing/requests/{requestId}/approve"));
    }

    private static HttpResponseMessage CreateOpenResponse(string fingerprint) => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(new
        {
            pairingSecret = new string('B', 43),
            serverPublicKeyFingerprint = fingerprint,
            expiresAt = DateTimeOffset.UtcNow.AddMinutes(2)
        })
    };

    private static MobApiDiscoveryEndpoint CreateEndpoint(string fingerprint) => new(
        "192.168.0.27",
        5001,
        5002,
        Guid.Parse("11111111-2222-3333-4444-555555555555").ToString("N"),
        fingerprint,
        DiscoveryResponseParser.CurrentProtocolVersion);

    private sealed class FakeEndpointProvider(MobApiDiscoveryEndpoint endpoint) : IRestApiPairingEndpointProvider
    {
        public MobApiDiscoveryEndpoint? GetAuthenticatedPairingEndpoint() => endpoint;
    }

    private sealed class FakeHostClient(HttpResponseMessage response) : IHostControlPlaneClient
    {
        public bool IsEnrolled => true;

        public string? LastRequestBody { get; private set; }

        public string? LastRequestUri { get; private set; }

        public async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken = default)
        {
            LastRequestUri = request.RequestUri?.ToString();
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return response;
        }
    }
}
#endif