// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Common.Security;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

/// <summary>
/// Keeps the process-bound MOBApi host credential and pinned HTTPS client in memory.
/// </summary>
public sealed class HostControlPlaneSession : IHostControlPlaneClient, IDisposable
{
    private static readonly TimeSpan RefreshLeadTime = TimeSpan.FromSeconds(45);
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly object _stateGate = new();
    private HttpClient? _httpClient;
    private Uri? _baseUri;
    private byte[]? _expectedFingerprint;
    private string? _credentialId;
    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt;
    private string? _renewalToken;
    private bool _disposed;

    public bool IsEnrolled
    {
        get
        {
            lock (_stateGate)
            {
                return _httpClient is not null && !string.IsNullOrEmpty(_accessToken);
            }
        }
    }

    public Uri BaseUri
    {
        get
        {
            lock (_stateGate)
            {
                return _baseUri ?? throw new InvalidOperationException("The MOBApi host session is not configured.");
            }
        }
    }

    public async Task EnrollAsync(
        int hostHttpsPort,
        string publicKeyFingerprint,
        string bootstrapSecret,
        CancellationToken cancellationToken)
    {
        Configure(hostHttpsPort, publicKeyFingerprint);
        HttpClient client;
        lock (_stateGate)
        {
            client = _httpClient!;
        }

        using var response = await client
            .PostAsJsonAsync("api/control-plane/host/bootstrap", new HostBootstrapRequest(bootstrapSecret), cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var token = await response.Content
            .ReadFromJsonAsync<HostTokenResponse>(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidDataException("MOBApi returned an empty host enrollment response.");
        SetToken(token);
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        lock (_stateGate)
        {
            if (!string.IsNullOrEmpty(_accessToken) &&
                _accessTokenExpiresAt > DateTimeOffset.UtcNow.Add(RefreshLeadTime))
                return _accessToken;
        }

        await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            HostRenewalRequest renewal;
            HttpClient client;
            lock (_stateGate)
            {
                if (!string.IsNullOrEmpty(_accessToken) &&
                    _accessTokenExpiresAt > DateTimeOffset.UtcNow.Add(RefreshLeadTime))
                    return _accessToken;
                if (_credentialId is null || _renewalToken is null || _httpClient is null)
                    throw new InvalidOperationException("The MOBApi host session is not enrolled.");

                renewal = new HostRenewalRequest(_credentialId, _renewalToken);
                client = _httpClient;
            }

            using var response = await client
                .PostAsJsonAsync("api/control-plane/host/renew", renewal, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var token = await response.Content
                .ReadFromJsonAsync<HostTokenResponse>(cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidDataException("MOBApi returned an empty host renewal response.");
            SetToken(token);
            return token.AccessToken;
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        HttpClient client;
        lock (_stateGate)
        {
            client = _httpClient ?? throw new InvalidOperationException("The MOBApi host session is not enrolled.");
        }

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false));
        return await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public HttpMessageHandler CreatePinnedHttpMessageHandler() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (_, certificate, _, _) => ValidateServerCertificate(certificate)
    };

    public bool ValidateServerCertificate(X509Certificate? certificate)
    {
        byte[]? expected;
        lock (_stateGate)
        {
            expected = _expectedFingerprint;
        }

        if (certificate is null || expected is null)
            return false;

        using var certificate2 = new X509Certificate2(certificate);
        using var publicKey = certificate2.GetECDsaPublicKey();
        if (publicKey is null)
            return false;

        var actual = SHA256.HashData(publicKey.ExportSubjectPublicKeyInfo());
        return actual.Length == expected.Length && CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    public void Reset()
    {
        lock (_stateGate)
        {
            _httpClient?.Dispose();
            _httpClient = null;
            _baseUri = null;
            _expectedFingerprint = null;
            _credentialId = null;
            _accessToken = null;
            _renewalToken = null;
            _accessTokenExpiresAt = default;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Reset();
        _refreshGate.Dispose();
    }

    private void Configure(int hostHttpsPort, string publicKeyFingerprint)
    {
        if (hostHttpsPort is <= 0 or >= 65536)
            throw new ArgumentOutOfRangeException(nameof(hostHttpsPort));

        var fingerprint = Convert.FromHexString(publicKeyFingerprint);
        if (fingerprint.Length != SHA256.HashSizeInBytes)
            throw new InvalidDataException("MOBApi returned an invalid server fingerprint.");

        lock (_stateGate)
        {
            _httpClient?.Dispose();
            _expectedFingerprint = fingerprint;
            _baseUri = new Uri($"https://127.0.0.1:{hostHttpsPort}/", UriKind.Absolute);
            _httpClient = new HttpClient(CreatePinnedHttpMessageHandler())
            {
                BaseAddress = _baseUri,
                Timeout = TimeSpan.FromSeconds(5)
            };
        }
    }

    private void SetToken(HostTokenResponse token)
    {
        lock (_stateGate)
        {
            _credentialId = token.CredentialId;
            _accessToken = token.AccessToken;
            _accessTokenExpiresAt = token.AccessTokenExpiresAt;
            _renewalToken = token.RenewalToken;
        }
    }
}