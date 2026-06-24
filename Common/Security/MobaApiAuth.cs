// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Security;

using System.Net;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Shared API-key authentication for MOBApi and its clients.
/// </summary>
public static class MobaApiAuth
{
    /// <summary>HTTP header carrying the MOBApi pairing key.</summary>
    public const string ApiKeyHeaderName = "X-MOBAflow-Api-Key";

    /// <summary>Environment variable WinUI passes when starting the MOBApi process.</summary>
    public const string ApiKeyEnvironmentVariable = "MOBAFLOW_API_KEY";

    /// <summary>HttpContext.Items key set when a request passed API-key auth.</summary>
    public const string AuthenticatedItemKey = "MobaApiAuthenticated";

    /// <summary>Public health probe used by LAN discovery (no API key required).</summary>
    public const string PublicHealthPath = "/api/photos/health";

    /// <summary>
    /// Generates a new cryptographically secure API key for MOBApi pairing.
    /// </summary>
    public static string GenerateApiKey()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', 'x')
            .Replace('/', 'y');
    }

    /// <summary>
    /// Ensures <see cref="RestApiSettings.ApiKey"/> is populated and returns the active key.
    /// </summary>
    /// <returns>True when a new key was generated.</returns>
    public static bool TryEnsureApiKey(Configuration.RestApiSettings settings, out string apiKey)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            apiKey = settings.ApiKey.Trim();
            return false;
        }

        apiKey = GenerateApiKey();
        settings.ApiKey = apiKey;
        return true;
    }

    /// <summary>
    /// Reads the API key from a raw header value.
    /// </summary>
    public static bool TryGetProvidedApiKey(string? headerValue, out string apiKey)
    {
        var candidate = headerValue?.Trim();
        if (!string.IsNullOrEmpty(candidate))
        {
            apiKey = candidate;
            return true;
        }

        apiKey = string.Empty;
        return false;
    }

    /// <summary>
    /// Adds the API key header when a key is configured.
    /// </summary>
    public static void ApplyApiKeyHeader(System.Net.Http.Headers.HttpHeaders headers, string? apiKey)
    {
        ArgumentNullException.ThrowIfNull(headers);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return;
        }

        headers.Remove(ApiKeyHeaderName);
        headers.TryAddWithoutValidation(ApiKeyHeaderName, apiKey.Trim());
    }

    /// <summary>
    /// Constant-time comparison for API keys.
    /// </summary>
    public static bool KeysMatch(string? provided, string? expected)
    {
        if (string.IsNullOrEmpty(provided) || string.IsNullOrEmpty(expected))
        {
            return false;
        }

        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return providedBytes.Length == expectedBytes.Length
               && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }

    /// <summary>
    /// Returns true for loopback or same-machine connections (WinUI host traffic).
    /// </summary>
    public static bool IsLocalConnection(IPAddress? remoteIp, IPAddress? localIp)
    {
        if (remoteIp == null)
        {
            return false;
        }

        if (IPAddress.IsLoopback(remoteIp))
        {
            return true;
        }

        return localIp != null && remoteIp.Equals(localIp);
    }

    /// <summary>
    /// Returns true when the path is reachable without an API key.
    /// </summary>
    public static bool IsPublicPath(string? path)
    {
        return !string.IsNullOrEmpty(path)
               && path.StartsWith(PublicHealthPath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Reads the configured API key from the MOBApi process environment.
    /// </summary>
    public static string? ReadConfiguredApiKey()
    {
        var value = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
