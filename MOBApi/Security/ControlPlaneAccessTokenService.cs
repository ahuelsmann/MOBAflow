// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Moba.MOBApi.Security;

/// <summary>
/// Issues and validates short-lived, purpose-protected access tokens.
/// </summary>
public interface IControlPlaneAccessTokenService
{
    Task<IssuedAccessToken?> IssueAsync(string credentialId, CancellationToken cancellationToken = default);

    Task<ClaimsPrincipal?> ValidateAsync(string token, CancellationToken cancellationToken = default);
}

/// <summary>
/// Contains an opaque bearer token and its fixed expiry time.
/// </summary>
public sealed record IssuedAccessToken(string Token, DateTimeOffset ExpiresAt);

internal sealed class ControlPlaneAccessTokenService : IControlPlaneAccessTokenService
{
    private const string TokenPurpose = "MOBApi.ControlPlane.AccessToken.v1";
    private const string Issuer = "mobaflow-control-plane";
    private const string Audience = "mobapi";
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);
    private readonly ICredentialRegistry _credentialRegistry;
    private readonly IDataProtector _protector;
    private readonly ControlPlaneSecurityOptions _options;
    private readonly TimeProvider _timeProvider;

    public ControlPlaneAccessTokenService(
        ICredentialRegistry credentialRegistry,
        IDataProtectionProvider dataProtectionProvider,
        IOptions<ControlPlaneSecurityOptions> options,
        TimeProvider timeProvider)
    {
        _credentialRegistry = credentialRegistry;
        _protector = dataProtectionProvider.CreateProtector(TokenPurpose);
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task<IssuedAccessToken?> IssueAsync(
        string credentialId,
        CancellationToken cancellationToken = default)
    {
        var state = await _credentialRegistry.GetAuthorizationStateAsync(credentialId, cancellationToken).ConfigureAwait(false);
        if (state is null)
            return null;

        var now = _timeProvider.GetUtcNow();
        var payload = new AccessTokenPayload
        {
            Issuer = Issuer,
            Audience = Audience,
            CredentialId = state.CredentialId,
            DisplayName = state.DisplayName,
            Role = state.Role,
            CapabilityVersion = state.CapabilityVersion,
            Capabilities = [.. state.Capabilities.Order(StringComparer.Ordinal)],
            TokenId = Guid.NewGuid().ToString("N"),
            IssuedAt = now,
            NotBefore = now.Subtract(ClockSkew),
            ExpiresAt = now.Add(_options.AccessTokenLifetime)
        };
        var token = _protector.Protect(JsonSerializer.Serialize(payload));
        return new IssuedAccessToken(token, payload.ExpiresAt);
    }

    public async Task<ClaimsPrincipal?> ValidateAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var payload = Unprotect(token);
        if (payload is null || !HasValidEnvelope(payload, _timeProvider.GetUtcNow()))
            return null;

        var state = await _credentialRegistry
            .GetAuthorizationStateAsync(payload.CredentialId, cancellationToken)
            .ConfigureAwait(false);
        if (!MatchesLiveState(payload, state))
            return null;

        return CreatePrincipal(payload);
    }

    private AccessTokenPayload? Unprotect(string token)
    {
        try
        {
            return JsonSerializer.Deserialize<AccessTokenPayload>(_protector.Unprotect(token));
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException)
        {
            return null;
        }
    }

    private static bool HasValidEnvelope(AccessTokenPayload payload, DateTimeOffset now) =>
        string.Equals(payload.Issuer, Issuer, StringComparison.Ordinal) &&
        string.Equals(payload.Audience, Audience, StringComparison.Ordinal) &&
        now >= payload.NotBefore &&
        now < payload.ExpiresAt.Add(ClockSkew) &&
        payload.ExpiresAt > payload.IssuedAt;

    private static bool MatchesLiveState(
        AccessTokenPayload payload,
        CredentialAuthorizationState? state) =>
        state is not null &&
        state.CapabilityVersion == payload.CapabilityVersion &&
        state.Role == payload.Role &&
        state.Capabilities.SetEquals(payload.Capabilities);

    private static ClaimsPrincipal CreatePrincipal(AccessTokenPayload payload)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, payload.CredentialId),
            new(ClaimTypes.Name, payload.DisplayName),
            new(ClaimTypes.Role, payload.Role.ToString()),
            new("mobaflow:capability_version", payload.CapabilityVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("jti", payload.TokenId)
        };
        claims.AddRange(payload.Capabilities.Select(capability => new Claim(ControlPlaneCapabilities.ClaimType, capability)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, ControlPlaneAuthenticationDefaults.Scheme));
    }

    private sealed class AccessTokenPayload
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string CredentialId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public ControlPlaneRole Role { get; set; }
        public long CapabilityVersion { get; set; }
        public List<string> Capabilities { get; set; } = [];
        public string TokenId { get; set; } = string.Empty;
        public DateTimeOffset IssuedAt { get; set; }
        public DateTimeOffset NotBefore { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }
}