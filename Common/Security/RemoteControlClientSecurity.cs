// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Moba.Common.Discovery;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

namespace Moba.Common.Security;

/// <summary>
/// Roles that a remote MOBAsmart client may request during pairing.
/// Numeric values intentionally match the MOBApi control-plane contract.
/// </summary>
public enum RemoteControlRole
{
    RemoteControl = 1,
    ReadOnly = 2
}

/// <summary>
/// Stores only the long-lived values required to rotate a remote credential.
/// Access tokens deliberately do not belong to this model.
/// </summary>
public sealed record RemoteControlCredential(
    string ServerInstanceId,
    string ServerAddress,
    int HttpsPort,
    string ServerPublicKeyFingerprint,
    string CredentialId,
    string RefreshToken,
    RemoteControlRole Role,
    long CapabilityVersion)
{
    public override string ToString() =>
        $"RemoteControlCredential {{ ServerInstanceId = {ServerInstanceId}, CredentialId = {CredentialId}, Role = {Role}, CapabilityVersion = {CapabilityVersion}, RefreshToken = [REDACTED] }}";
}

/// <summary>
/// Abstracts the platform-protected credential store used by the mobile client.
/// </summary>
public interface IRemoteControlCredentialStore
{
    Task<RemoteControlCredential?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(RemoteControlCredential credential, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Describes the short-lived bearer token held only by the current process.
/// </summary>
public sealed record RemoteControlAccessSession(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    RemoteControlRole Role,
    long CapabilityVersion)
{
    public override string ToString() =>
        $"RemoteControlAccessSession {{ AccessToken = [REDACTED], ExpiresAt = {ExpiresAt:O}, Role = {Role}, CapabilityVersion = {CapabilityVersion} }}";
}

/// <summary>
/// Binds an in-memory access session to the authenticated, fingerprint-pinned server endpoint.
/// </summary>
public sealed record RemoteControlConnectionSession(
    MobApiDiscoveryEndpoint Endpoint,
    RemoteControlAccessSession AccessSession)
{
    public override string ToString() =>
        $"RemoteControlConnectionSession {{ Endpoint = {Endpoint}, AccessSession = {AccessSession} }}";
}

public sealed record RemoteControlTokenResponse(
    string CredentialId,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    RemoteControlRole Role,
    long CapabilityVersion)
{
    public override string ToString() =>
        $"RemoteControlTokenResponse {{ CredentialId = {CredentialId}, AccessToken = [REDACTED], RefreshToken = [REDACTED], Role = {Role}, CapabilityVersion = {CapabilityVersion} }}";
}

public enum RemotePairingSubmissionStatus
{
    Accepted,
    Invalid,
    Expired,
    Closed,
    RoleNotAllowed,
    ReplayDetected,
    Cooldown
}

public enum RemotePairingClaimStatus
{
    Succeeded,
    PendingApproval,
    Rejected,
    Invalid,
    Expired,
    AlreadyClaimed
}

public sealed record RemotePairingSubmissionResult(
    RemotePairingSubmissionStatus Status,
    string? RequestId = null,
    string? ClaimToken = null,
    string? ConfirmationCode = null);

public sealed record RemotePairingClaimResult(
    RemotePairingClaimStatus Status,
    RemoteControlTokenResponse? Token = null);

/// <summary>
/// Keeps the claim secret in memory while local approval is pending.
/// </summary>
public sealed record RemotePairingAttempt(
    MobApiDiscoveryEndpoint Endpoint,
    string RequestId,
    string ClaimToken,
    string ConfirmationCode)
{
    public override string ToString() =>
        $"RemotePairingAttempt {{ ServerInstanceId = {Endpoint.ServerInstanceId}, RequestId = {RequestId}, ClaimToken = [REDACTED], ConfirmationCode = {ConfirmationCode} }}";
}

/// <summary>
/// Performs the pinned HTTPS calls required for pairing and credential rotation.
/// </summary>
public interface IRemoteControlTransport
{
    Task<RemotePairingSubmissionResult> SubmitPairingAsync(
        MobApiDiscoveryEndpoint endpoint,
        string pairingSecret,
        string clientNonce,
        string displayName,
        RemoteControlRole requestedRole,
        CancellationToken cancellationToken = default);

    Task<RemotePairingClaimResult> ClaimPairingAsync(
        MobApiDiscoveryEndpoint endpoint,
        string requestId,
        string claimToken,
        CancellationToken cancellationToken = default);

    Task<RemoteControlTokenResponse> RefreshAsync(
        RemoteControlCredential credential,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Creates an HTTPS client whose server certificate is pinned to the supplied discovery endpoint.
/// </summary>
public interface IRemoteControlHttpClientFactory
{
    /// <summary>
    /// Creates an HTTPS client pinned to <paramref name="endpoint"/>.
    /// </summary>
    HttpClient CreateClient(MobApiDiscoveryEndpoint endpoint);

    /// <summary>
    /// Creates an HTTP handler pinned to <paramref name="endpoint"/>.
    /// </summary>
    HttpMessageHandler CreateHandler(MobApiDiscoveryEndpoint endpoint);

    /// <summary>
    /// Validates a server certificate against the fingerprint pinned to <paramref name="endpoint"/>.
    /// </summary>
    bool ValidateServerCertificate(MobApiDiscoveryEndpoint endpoint, X509Certificate? certificate);
}

/// <summary>
/// Sends authenticated requests through the current endpoint-bound remote-control session.
/// </summary>
public interface IRemoteControlAuthenticatedHttpClient
{
    /// <summary>
    /// Sends an authenticated GET request to a path on the pinned MOBApi endpoint.
    /// </summary>
    Task<HttpResponseMessage> GetAsync(
        string relativePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an authenticated POST request to a path on the pinned MOBApi endpoint.
    /// </summary>
    Task<HttpResponseMessage> PostAsync(
        string relativePath,
        HttpContent content,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Sends authenticated requests without exposing bearer credentials to UI consumers.
/// </summary>
public sealed class RemoteControlAuthenticatedHttpClient : IRemoteControlAuthenticatedHttpClient
{
    private static readonly TimeSpan MinimumSessionLifetime = TimeSpan.FromSeconds(30);
    private readonly IRemoteControlHttpClientFactory _httpClientFactory;
    private readonly RemoteControlSessionService _sessionService;

    public RemoteControlAuthenticatedHttpClient(
        RemoteControlSessionService sessionService,
        IRemoteControlHttpClientFactory httpClientFactory)
    {
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public async Task<HttpResponseMessage> GetAsync(
        string relativePath,
        CancellationToken cancellationToken = default) =>
        await SendAsync(HttpMethod.Get, relativePath, null, cancellationToken).ConfigureAwait(false);

    public async Task<HttpResponseMessage> PostAsync(
        string relativePath,
        HttpContent content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        return await SendAsync(HttpMethod.Post, relativePath, content, cancellationToken).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string relativePath,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(relativePath) ||
            !Uri.TryCreate(relativePath, UriKind.Relative, out var requestUri))
        {
            throw new ArgumentException("An authenticated MOBApi path must be relative.", nameof(relativePath));
        }

        var connection = await _sessionService
            .GetConnectionSessionAsync(MinimumSessionLifetime, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new RemoteCredentialRejectedException();

        using var client = _httpClientFactory.CreateClient(connection.Endpoint);
        if (client.BaseAddress?.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Authenticated MOBApi requests require HTTPS.");

        var resolvedRequestUri = new Uri(client.BaseAddress, requestUri);
        if (!string.Equals(
                resolvedRequestUri.GetLeftPart(UriPartial.Authority),
                client.BaseAddress.GetLeftPart(UriPartial.Authority),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "An authenticated MOBApi path must remain on the pinned server endpoint.",
                nameof(relativePath));
        }

        using var request = new HttpRequestMessage(method, resolvedRequestUri)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            connection.AccessSession.AccessToken);
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await BufferResponseAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<HttpResponseMessage> BufferResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var buffered = new HttpResponseMessage(response.StatusCode)
        {
            ReasonPhrase = response.ReasonPhrase,
            Version = response.Version
        };

        foreach (var header in response.Headers)
            buffered.Headers.TryAddWithoutValidation(header.Key, header.Value);

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        buffered.Content = new ByteArrayContent(content);
        foreach (var header in response.Content.Headers)
            buffered.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return buffered;
    }
}

/// <summary>
/// Signals that MOBApi rejected or revoked the stored refresh credential.
/// </summary>
public sealed class RemoteCredentialRejectedException : Exception
{
    public RemoteCredentialRejectedException()
        : base("The remote-control credential was rejected and must be paired again.")
    {
    }
}

/// <summary>
/// Coordinates pairing and refresh-token rotation without persisting access tokens.
/// </summary>
public sealed class RemoteControlSessionService
{
    private const int PairingSecretLength = 43;
    private readonly IRemoteControlCredentialStore _credentialStore;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly IRemoteControlTransport _transport;

    public RemoteControlSessionService(
        IRemoteControlCredentialStore credentialStore,
        IRemoteControlTransport transport)
    {
        _credentialStore = credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public RemoteControlAccessSession? CurrentAccessSession { get; private set; }

    public RemoteControlConnectionSession? CurrentConnectionSession { get; private set; }

    public async Task<RemotePairingAttempt> BeginPairingAsync(
        MobApiDiscoveryEndpoint endpoint,
        string pairingSecret,
        string displayName,
        RemoteControlRole requestedRole,
        CancellationToken cancellationToken = default)
    {
        ValidateAuthenticatedEndpoint(endpoint);
        if (pairingSecret?.Length != PairingSecretLength)
            throw new ArgumentException("Pairing secret must contain 43 characters.", nameof(pairingSecret));
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        if (displayName.Length > 100)
            throw new ArgumentException("Display name must not exceed 100 characters.", nameof(displayName));
        if (!Enum.IsDefined(requestedRole))
            throw new ArgumentOutOfRangeException(nameof(requestedRole));

        var nonce = CreateNonce();
        var result = await _transport.SubmitPairingAsync(
            endpoint,
            pairingSecret,
            nonce,
            displayName.Trim(),
            requestedRole,
            cancellationToken).ConfigureAwait(false);

        if (result.Status != RemotePairingSubmissionStatus.Accepted ||
            string.IsNullOrWhiteSpace(result.RequestId) ||
            string.IsNullOrWhiteSpace(result.ClaimToken) ||
            string.IsNullOrWhiteSpace(result.ConfirmationCode))
        {
            throw new InvalidOperationException($"Pairing submission was not accepted ({result.Status}).");
        }

        return new RemotePairingAttempt(
            endpoint,
            result.RequestId,
            result.ClaimToken,
            result.ConfirmationCode);
    }

    public async Task<RemoteControlAccessSession?> ClaimAsync(
        RemotePairingAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        ValidateAuthenticatedEndpoint(attempt.Endpoint);

        return await RunLockedAsync(async () =>
        {
            var result = await _transport.ClaimPairingAsync(
                attempt.Endpoint,
                attempt.RequestId,
                attempt.ClaimToken,
                cancellationToken).ConfigureAwait(false);

            if (result.Status == RemotePairingClaimStatus.PendingApproval)
                return null;
            if (result.Status != RemotePairingClaimStatus.Succeeded || result.Token is null)
                throw new InvalidOperationException($"Pairing claim failed ({result.Status}).");

            return await AcceptTokenAsync(attempt.Endpoint, result.Token, cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RemoteControlAccessSession?> RestoreAsync(CancellationToken cancellationToken = default)
    {
        return await RunLockedAsync(
            () => RefreshStoredCredentialAsync(cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<RemoteControlAccessSession?> RefreshAsync(CancellationToken cancellationToken = default)
    {
        return await RunLockedAsync(
            () => RefreshStoredCredentialAsync(cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<RemoteControlConnectionSession?> GetConnectionSessionAsync(
        TimeSpan minimumRemainingLifetime,
        CancellationToken cancellationToken = default)
    {
        if (minimumRemainingLifetime < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(minimumRemainingLifetime));

        return await RunLockedAsync(async () =>
        {
            if (CurrentConnectionSession is not null &&
                CurrentConnectionSession.AccessSession.ExpiresAt - DateTimeOffset.UtcNow > minimumRemainingLifetime)
            {
                return CurrentConnectionSession;
            }

            var refreshed = await RefreshStoredCredentialAsync(cancellationToken).ConfigureAwait(false);
            return refreshed is null ? null : CurrentConnectionSession;
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await RunLockedAsync(async () =>
        {
            await _credentialStore.ClearAsync(cancellationToken).ConfigureAwait(false);
            CurrentAccessSession = null;
            CurrentConnectionSession = null;
            return true;
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<RemoteControlAccessSession?> RefreshStoredCredentialAsync(CancellationToken cancellationToken)
    {
        var credential = await _credentialStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (credential is null)
        {
            CurrentAccessSession = null;
            CurrentConnectionSession = null;
            return null;
        }

        try
        {
            return await RefreshCoreAsync(credential, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is RemoteCredentialRejectedException or ArgumentException or InvalidDataException)
        {
            await _credentialStore.ClearAsync(cancellationToken).ConfigureAwait(false);
            CurrentAccessSession = null;
            CurrentConnectionSession = null;
            return null;
        }
    }

    private async Task<RemoteControlAccessSession> RefreshCoreAsync(
        RemoteControlCredential credential,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(credential.Role) ||
            string.IsNullOrWhiteSpace(credential.CredentialId) ||
            string.IsNullOrWhiteSpace(credential.RefreshToken))
        {
            throw new InvalidDataException("The stored remote-control credential is invalid.");
        }

        var endpoint = new MobApiDiscoveryEndpoint(
            credential.ServerAddress,
            0,
            credential.HttpsPort,
            credential.ServerInstanceId,
            credential.ServerPublicKeyFingerprint,
            DiscoveryResponseParser.CurrentProtocolVersion);
        ValidateAuthenticatedEndpoint(endpoint);

        var response = await _transport.RefreshAsync(credential, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(response.CredentialId, credential.CredentialId, StringComparison.Ordinal))
            throw new InvalidDataException("MOBApi returned a token for a different remote-control credential.");
        return await AcceptTokenAsync(endpoint, response, cancellationToken).ConfigureAwait(false);
    }

    private async Task<RemoteControlAccessSession> AcceptTokenAsync(
        MobApiDiscoveryEndpoint endpoint,
        RemoteControlTokenResponse response,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(response.CredentialId) ||
            string.IsNullOrWhiteSpace(response.AccessToken) ||
            string.IsNullOrWhiteSpace(response.RefreshToken) ||
            response.AccessTokenExpiresAt <= DateTimeOffset.UtcNow ||
            !Enum.IsDefined(response.Role) ||
            response.CapabilityVersion < 0)
        {
            throw new InvalidDataException("MOBApi returned an invalid remote-control token response.");
        }

        var credential = new RemoteControlCredential(
            endpoint.ServerInstanceId!,
            endpoint.IpAddress,
            endpoint.HttpsPort!.Value,
            endpoint.ServerPublicKeyFingerprint!,
            response.CredentialId,
            response.RefreshToken,
            response.Role,
            response.CapabilityVersion);

        // Persist the rotated refresh token before the predecessor is discarded from memory.
        await _credentialStore.SaveAsync(credential, cancellationToken).ConfigureAwait(false);

        CurrentAccessSession = new RemoteControlAccessSession(
            response.AccessToken,
            response.AccessTokenExpiresAt,
            response.Role,
            response.CapabilityVersion);
        CurrentConnectionSession = new RemoteControlConnectionSession(endpoint, CurrentAccessSession);
        return CurrentAccessSession;
    }

    private static string CreateNonce()
    {
        var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return value.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private async Task<T> RunLockedAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await action().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static void ValidateAuthenticatedEndpoint(MobApiDiscoveryEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        if (endpoint.ProtocolVersion < DiscoveryResponseParser.CurrentProtocolVersion ||
            !IPAddress.TryParse(endpoint.IpAddress, out _) ||
            endpoint.HttpsPort is not (> 0 and < 65536) ||
            !Guid.TryParseExact(endpoint.ServerInstanceId, "N", out _) ||
            endpoint.ServerPublicKeyFingerprint?.Length != 64 ||
            !endpoint.ServerPublicKeyFingerprint.All(Uri.IsHexDigit))
        {
            throw new ArgumentException("Discovery did not provide a complete authenticated MOBApi endpoint.", nameof(endpoint));
        }
    }
}
