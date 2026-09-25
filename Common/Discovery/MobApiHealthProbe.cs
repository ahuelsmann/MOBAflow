// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Discovery;

using System.Text.Json;

/// <summary>
/// Identifies MOBApi health responses during LAN discovery probes.
/// </summary>
public static class MobApiHealthProbe
{
    /// <summary>GET path used by MOBAsmart and MOBApi for reachability checks.</summary>
    public const string HealthPath = "/api/photos/health";

    /// <summary>
    /// Returns true when the HTTP body looks like a MOBApi health payload (not just any 200 OK).
    /// </summary>
    public static bool IsHealthyResponse(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("status", out var status)
                || status.ValueKind != JsonValueKind.String
                || !string.Equals(status.GetString(), "healthy", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!root.TryGetProperty("service", out var service)
                || service.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var serviceName = service.GetString();
            return string.Equals(serviceName, "MOBAflow MOBApi", StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
