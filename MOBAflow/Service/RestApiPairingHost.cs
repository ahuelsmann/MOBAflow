// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Common.Discovery;
using Common.Security;

using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

/// <summary>
/// Sends authenticated host requests to MOBApi without exposing host credentials to callers.
/// </summary>
internal interface IHostControlPlaneClient
{
    bool IsEnrolled { get; }

    Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Supplies the authenticated LAN endpoint advertised by the running MOBApi instance.
/// </summary>
internal interface IRestApiPairingEndpointProvider
{
    MobApiDiscoveryEndpoint? GetAuthenticatedPairingEndpoint();
}

/// <summary>
/// Describes a pending MOBAsmart administrator pairing request shown to the local owner.
/// </summary>
internal sealed record RestApiPairingRequest(
    string RequestId,
    string DisplayName,
    string ConfirmationCode,
    DateTimeOffset CreatedAt);

/// <summary>
/// Contains the QR invitation shown by MOBAflow. Its encoded payload contains a short-lived secret.
/// </summary>
internal sealed record RestApiPairingInvitation(
    RemotePairingQrInvitation Invitation,
    string EncodedQrPayload)
{
    /// <inheritdoc />
    public override string ToString() =>
        $"RestApiPairingInvitation {{ Invitation = {Invitation}, EncodedQrPayload = [REDACTED] }}";
}

/// <summary>
/// Owns the complete local-host pairing interface used by the REST API settings view.
/// </summary>
internal interface IRestApiPairingHost
{
    bool IsAvailable { get; }

    Task<RestApiPairingInvitation> OpenAdminPairingAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RestApiPairingRequest>> GetPendingRequestsAsync(
        CancellationToken cancellationToken = default);

    Task ApproveAsync(string requestId, CancellationToken cancellationToken = default);

    Task RejectAsync(string requestId, CancellationToken cancellationToken = default);

    Task CancelAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Adapts MOBApi host-only pairing endpoints to the small interface used by the Settings page.
/// </summary>
internal sealed class RestApiPairingHost(
    IHostControlPlaneClient hostClient,
    IRestApiPairingEndpointProvider endpointProvider) : IRestApiPairingHost
{
    private const int AdminRoleValue = 1;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IHostControlPlaneClient _hostClient =
        hostClient ?? throw new ArgumentNullException(nameof(hostClient));
    private readonly IRestApiPairingEndpointProvider _endpointProvider =
        endpointProvider ?? throw new ArgumentNullException(nameof(endpointProvider));

    public bool IsAvailable =>
        _hostClient.IsEnrolled && _endpointProvider.GetAuthenticatedPairingEndpoint() is not null;

    public async Task<RestApiPairingInvitation> OpenAdminPairingAsync(
        CancellationToken cancellationToken = default)
    {
        var endpoint = _endpointProvider.GetAuthenticatedPairingEndpoint()
            ?? throw new InvalidOperationException("Start the REST API before creating a pairing QR code.");
        if (endpoint.HttpsPort is not int httpsPort ||
            string.IsNullOrWhiteSpace(endpoint.ServerInstanceId))
        {
            throw new InvalidOperationException("The authenticated REST API endpoint is incomplete.");
        }

        if (!_hostClient.IsEnrolled)
        {
            throw new InvalidOperationException("MOBAflow is not authenticated with the running REST API.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/control-plane/security/pairing/open")
        {
            Content = JsonContent.Create(new OpenPairingRequest(AdminRoleValue), options: JsonOptions)
        };
        using var response = await _hostClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content
            .ReadFromJsonAsync<OpenPairingResponse>(JsonOptions, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidDataException("MOBApi returned an empty pairing response.");

        EnsureMatchingFingerprint(endpoint.ServerPublicKeyFingerprint, result.ServerPublicKeyFingerprint);
        var invitation = new RemotePairingQrInvitation(
            endpoint.IpAddress,
            endpoint.HttpPort,
            httpsPort,
            endpoint.ServerInstanceId,
            result.ServerPublicKeyFingerprint,
            result.PairingSecret,
            result.ExpiresAt);
        return new RestApiPairingInvitation(invitation, RemotePairingQrCode.Encode(invitation));
    }

    public async Task<IReadOnlyList<RestApiPairingRequest>> GetPendingRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/control-plane/security/pairing/requests");
        using var response = await _hostClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var requests = await response.Content
            .ReadFromJsonAsync<PendingPairingResponse[]>(JsonOptions, cancellationToken)
            .ConfigureAwait(false) ?? [];

        return requests
            .Where(request => request.RequestedRole == AdminRoleValue)
            .Select(request => new RestApiPairingRequest(
                request.RequestId,
                request.DisplayName,
                request.ConfirmationCode,
                request.CreatedAt))
            .ToArray();
    }

    public Task ApproveAsync(string requestId, CancellationToken cancellationToken = default) =>
        SendDecisionAsync(requestId, "approve", cancellationToken);

    public Task RejectAsync(string requestId, CancellationToken cancellationToken = default) =>
        SendDecisionAsync(requestId, "reject", cancellationToken);

    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/control-plane/security/pairing/cancel");
        using var response = await _hostClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private async Task SendDecisionAsync(
        string requestId,
        string decision,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(requestId, "N", out _))
        {
            throw new ArgumentException("The pairing request ID is invalid.", nameof(requestId));
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/control-plane/security/pairing/requests/{requestId}/{decision}");
        using var response = await _hostClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private static void EnsureMatchingFingerprint(string? advertised, string returned)
    {
        if (string.IsNullOrWhiteSpace(advertised))
        {
            throw new InvalidDataException("The authenticated discovery fingerprint is missing.");
        }

        byte[] advertisedBytes;
        byte[] returnedBytes;
        try
        {
            advertisedBytes = Convert.FromHexString(advertised);
            returnedBytes = Convert.FromHexString(returned);
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException("MOBApi returned an invalid server fingerprint.", exception);
        }

        if (advertisedBytes.Length != returnedBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(advertisedBytes, returnedBytes))
        {
            throw new InvalidDataException("The pairing fingerprint does not match the running REST API.");
        }
    }

    private sealed record OpenPairingRequest(int AllowedRole);

    private sealed record OpenPairingResponse(
        string PairingSecret,
        string ServerPublicKeyFingerprint,
        DateTimeOffset ExpiresAt);

    private sealed record PendingPairingResponse(
        string RequestId,
        string DisplayName,
        int RequestedRole,
        string ConfirmationCode,
        DateTimeOffset CreatedAt,
        string Status);
}
