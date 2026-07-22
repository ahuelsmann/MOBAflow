// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Discovery;

/// <summary>
/// Parses the MOBAflow REST API discovery response format so the protocol is defined and testable in one place.
/// Legacy format: "MOBAFLOW_REST_API|{ip}|{httpPort}".
/// Version 2 appends the HTTPS endpoint and persistent server identity metadata.
/// </summary>
public static class DiscoveryResponseParser
{
    /// <summary>UDP multicast discovery request sent by MAUI clients.</summary>
    public const string RequestMessage = "MOBAFLOW_DISCOVER";

    /// <summary>Expected prefix of the discovery response.</summary>
    public const string ResponsePrefix = "MOBAFLOW_REST_API";

    /// <summary>Current discovery response protocol version.</summary>
    public const int CurrentProtocolVersion = 2;

    /// <summary>UDP port for MOBAflow REST API discovery (multicast). Not the Z21 command-station port.</summary>
    public const int MulticastPort = 21106;

    /// <summary>Multicast group address for LAN discovery.</summary>
    public const string MulticastAddress = "239.255.42.99";

    /// <summary>
    /// Tries to parse a discovery response string into IP and port.
    /// </summary>
    /// <param name="response">Raw response string (e.g. "MOBAFLOW_REST_API|192.168.0.100|5001").</param>
    /// <param name="ip">Parsed IP address, or null if parsing failed.</param>
    /// <param name="port">Parsed port (1-65535), or null if parsing failed.</param>
    /// <returns>True if the response was valid and parsed successfully.</returns>
    public static bool TryParse(string? response, out string? ip, out int? port)
    {
        var success = TryParse(response, out var endpoint);
        ip = endpoint?.IpAddress;
        port = endpoint?.HttpPort;
        return success;
    }

    /// <summary>
    /// Tries to parse legacy or current discovery metadata.
    /// </summary>
    public static bool TryParse(string? response, out MobApiDiscoveryEndpoint? endpoint)
    {
        endpoint = null;

        if (string.IsNullOrWhiteSpace(response))
            return false;

        var trimmed = response.TrimEnd('\0').Trim();
        if (!trimmed.StartsWith(ResponsePrefix, StringComparison.Ordinal))
            return false;

        var parts = trimmed.Split('|');
        if (parts.Length is not (3 or 7))
            return false;

        var ipPart = parts[1].Trim();
        if (string.IsNullOrEmpty(ipPart))
            return false;

        if (!int.TryParse(parts[2].Trim(), out var portValue) || portValue <= 0 || portValue >= 65536)
            return false;

        if (parts.Length == 3)
        {
            endpoint = new MobApiDiscoveryEndpoint(ipPart, portValue, null, null, null, 1);
            return true;
        }

        if (!int.TryParse(parts[3].Trim(), out var protocolVersion) ||
            protocolVersion != CurrentProtocolVersion ||
            !TryParsePort(parts[4], out var httpsPort) ||
            !Guid.TryParseExact(parts[5].Trim(), "N", out _) ||
            !IsSha256Fingerprint(parts[6]))
        {
            return false;
        }

        endpoint = new MobApiDiscoveryEndpoint(
            ipPart,
            portValue,
            httpsPort,
            parts[5].Trim(),
            parts[6].Trim().ToUpperInvariant(),
            protocolVersion);
        return true;
    }

    /// <summary>
    /// Creates a version 2 response while retaining the legacy IP and HTTP port fields first.
    /// </summary>
    public static string CreateResponse(
        string ipAddress,
        int httpPort,
        int httpsPort,
        string serverInstanceId,
        string serverPublicKeyFingerprint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ipAddress);
        if (!IsValidPort(httpPort))
            throw new ArgumentOutOfRangeException(nameof(httpPort));
        if (!IsValidPort(httpsPort))
            throw new ArgumentOutOfRangeException(nameof(httpsPort));
        if (!Guid.TryParseExact(serverInstanceId, "N", out _))
            throw new ArgumentException("Server instance ID must be a GUID in N format.", nameof(serverInstanceId));
        if (!IsSha256Fingerprint(serverPublicKeyFingerprint))
            throw new ArgumentException("Server public-key fingerprint must be a SHA-256 hexadecimal value.", nameof(serverPublicKeyFingerprint));

        return string.Join(
            '|',
            ResponsePrefix,
            ipAddress.Trim(),
            httpPort,
            CurrentProtocolVersion,
            httpsPort,
            serverInstanceId.Trim(),
            serverPublicKeyFingerprint.Trim().ToUpperInvariant());
    }

    private static bool TryParsePort(string value, out int port) =>
        int.TryParse(value.Trim(), out port) && IsValidPort(port);

    private static bool IsValidPort(int port) => port is > 0 and < 65536;

    private static bool IsSha256Fingerprint(string value) =>
        value.Trim().Length == 64 && value.Trim().All(Uri.IsHexDigit);
}

/// <summary>
/// Describes a discovered MOBApi endpoint without treating discovery data as trusted identity.
/// </summary>
public sealed record MobApiDiscoveryEndpoint(
    string IpAddress,
    int HttpPort,
    int? HttpsPort,
    string? ServerInstanceId,
    string? ServerPublicKeyFingerprint,
    int ProtocolVersion);