// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.Service;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Moba.Common.Discovery;
using Moba.Common.Security;

#if ANDROID
using Xamarin.Android.Net;
#endif

/// <summary>
/// Calls pairing and token refresh over fingerprint-pinned HTTPS.
/// </summary>
public sealed class PinnedRemoteControlTransport : IRemoteControlTransport
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<RemotePairingSubmissionResult> SubmitPairingAsync(
        MobApiDiscoveryEndpoint endpoint,
        string pairingSecret,
        string clientNonce,
        string displayName,
        RemoteControlRole requestedRole,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(endpoint);
        using var response = await client.PostAsJsonAsync(
            "api/control-plane/pairing/submit",
            new PairingSubmissionRequest(pairingSecret, clientNonce, displayName, requestedRole),
            JsonOptions,
            cancellationToken).ConfigureAwait(false);

        var result = await response.Content
            .ReadFromJsonAsync<RemotePairingSubmissionResult>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        return result ?? throw new InvalidDataException("MOBApi returned an empty pairing response.");
    }

    public async Task<RemotePairingClaimResult> ClaimPairingAsync(
        MobApiDiscoveryEndpoint endpoint,
        string requestId,
        string claimToken,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(endpoint);
        using var response = await client.PostAsJsonAsync(
            "api/control-plane/pairing/claim",
            new PairingClaimRequest(requestId, claimToken),
            JsonOptions,
            cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.Accepted)
        {
            var pending = await response.Content
                .ReadFromJsonAsync<PairingClaimStatusResponse>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            return new RemotePairingClaimResult(pending?.Status ?? RemotePairingClaimStatus.PendingApproval);
        }

        if (response.IsSuccessStatusCode)
        {
            var token = await response.Content
                .ReadFromJsonAsync<RemoteControlTokenResponse>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            return token is null
                ? throw new InvalidDataException("MOBApi returned an empty token response.")
                : new RemotePairingClaimResult(RemotePairingClaimStatus.Succeeded, token);
        }

        var failed = await response.Content
            .ReadFromJsonAsync<PairingClaimStatusResponse>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        return new RemotePairingClaimResult(failed?.Status ?? RemotePairingClaimStatus.Invalid);
    }

    public async Task<RemoteControlTokenResponse> RefreshAsync(
        RemoteControlCredential credential,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(credential);
        var endpoint = new MobApiDiscoveryEndpoint(
            credential.ServerAddress,
            0,
            credential.HttpsPort,
            credential.ServerInstanceId,
            credential.ServerPublicKeyFingerprint,
            DiscoveryResponseParser.CurrentProtocolVersion);

        using var client = CreateClient(endpoint);
        using var response = await client.PostAsJsonAsync(
            "api/control-plane/token/refresh",
            new RefreshTokenRequest(credential.CredentialId, credential.RefreshToken),
            JsonOptions,
            cancellationToken).ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new RemoteCredentialRejectedException();
        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<RemoteControlTokenResponse>(JsonOptions, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidDataException("MOBApi returned an empty token response.");
    }

    private static HttpClient CreateClient(MobApiDiscoveryEndpoint endpoint)
    {
        if (endpoint.HttpsPort is not (> 0 and < 65536) ||
            string.IsNullOrWhiteSpace(endpoint.ServerPublicKeyFingerprint))
        {
            throw new ArgumentException("An HTTPS port and certificate fingerprint are required.", nameof(endpoint));
        }

        return new HttpClient(CreatePinnedHandler(endpoint.ServerPublicKeyFingerprint), disposeHandler: true)
        {
            BaseAddress = new UriBuilder(Uri.UriSchemeHttps, endpoint.IpAddress, endpoint.HttpsPort.Value).Uri,
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    private static HttpMessageHandler CreatePinnedHandler(string fingerprint)
    {
#if ANDROID
        return new AndroidMessageHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            ServerCertificateCustomValidationCallback = (_, certificate, _, _) =>
                ServerCertificatePinning.Matches(certificate, fingerprint),
            UseProxy = false
        };
#else
        return new SocketsHttpHandler
        {
            UseProxy = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, certificate, _, _) =>
                    ServerCertificatePinning.Matches(certificate, fingerprint)
            }
        };
#endif
    }

    private sealed record PairingSubmissionRequest(
        string PairingSecret,
        string ClientNonce,
        string DisplayName,
        RemoteControlRole RequestedRole);

    private sealed record PairingClaimRequest(string RequestId, string ClaimToken);

    private sealed record PairingClaimStatusResponse(RemotePairingClaimStatus Status);

    private sealed record RefreshTokenRequest(string CredentialId, string RefreshToken);
}