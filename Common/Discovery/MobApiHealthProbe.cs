// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Discovery;

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

        return body.Contains("MOBAflow", StringComparison.OrdinalIgnoreCase)
               && body.Contains("healthy", StringComparison.OrdinalIgnoreCase);
    }
}
