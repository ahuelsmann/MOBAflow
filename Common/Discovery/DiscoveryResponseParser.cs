// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Discovery;

/// <summary>
/// Parses the MOBAflow REST API discovery response format so the protocol is defined and testable in one place.
/// Format: "MOBAFLOW_REST_API|{ip}|{port}"
/// </summary>
public static class DiscoveryResponseParser
{
    /// <summary>Expected prefix of the discovery response.</summary>
    public const string ResponsePrefix = "MOBAFLOW_REST_API";

    /// <summary>
    /// Tries to parse a discovery response string into IP and port.
    /// </summary>
    /// <param name="response">Raw response string (e.g. "MOBAFLOW_REST_API|192.168.0.100|5001").</param>
    /// <param name="ip">Parsed IP address, or null if parsing failed.</param>
    /// <param name="port">Parsed port (1-65535), or null if parsing failed.</param>
    /// <returns>True if the response was valid and parsed successfully.</returns>
    public static bool TryParse(string? response, out string? ip, out int? port)
    {
        ip = null;
        port = null;

        if (string.IsNullOrWhiteSpace(response))
            return false;

        var trimmed = response.TrimEnd('\0').Trim();
        if (!trimmed.StartsWith(ResponsePrefix, StringComparison.Ordinal))
            return false;

        var parts = trimmed.Split('|');
        if (parts.Length < 3)
            return false;

        var ipPart = parts[1].Trim();
        if (string.IsNullOrEmpty(ipPart))
            return false;

        if (!int.TryParse(parts[2].Trim(), out var portValue) || portValue <= 0 || portValue >= 65536)
            return false;

        ip = ipPart;
        port = portValue;
        return true;
    }
}
