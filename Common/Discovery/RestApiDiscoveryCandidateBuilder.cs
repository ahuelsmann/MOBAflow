// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Discovery;

using Configuration;

using System.Net;
using System.Net.Sockets;

/// <summary>
/// Builds host probe order for MOBApi HTTP discovery on the LAN.
/// </summary>
public static class RestApiDiscoveryCandidateBuilder
{
    /// <summary>Factory default that must not be prioritized before subnet scan (often wrong on real networks).</summary>
    public const string LegacyFactoryDefaultIp = "192.168.0.79";

    /// <summary>
    /// Hosts near the device's own IPv4 address in the same /24 (fast path before a full subnet scan).
    /// </summary>
    public static IReadOnlyList<IPAddress> BuildQuickWindowCandidates(
        IEnumerable<IPAddress> localAddresses,
        int radius = 24)
    {
        ArgumentNullException.ThrowIfNull(localAddresses);

        var seen = new HashSet<uint>();
        var result = new List<IPAddress>();
        var clampedRadius = Math.Clamp(radius, 1, 127);

        foreach (var local in localAddresses)
        {
            if (local.AddressFamily != AddressFamily.InterNetwork)
            {
                continue;
            }

            var bytes = local.GetAddressBytes();
            if (bytes.Length != 4 || !SubnetCandidateBuilder.IsPrivateIPv4(local))
            {
                continue;
            }

            var selfLast = bytes[3];
            for (var offset = 1; offset <= clampedRadius; offset++)
            {
                TryAddHost(bytes[0], bytes[1], bytes[2], (byte)(selfLast + offset));
                TryAddHost(bytes[0], bytes[1], bytes[2], (byte)(selfLast - offset));
            }
        }

        return result;

        void TryAddHost(byte a, byte b, byte c, byte d)
        {
            if (d is 0 or 255)
            {
                return;
            }

            var key = ((uint)a << 24) | ((uint)b << 16) | ((uint)c << 8) | d;
            if (!seen.Add(key))
            {
                return;
            }

            result.Add(new IPAddress(new[] { a, b, c, d }));
        }
    }

    /// <summary>
    /// All host addresses in the same /24 as <paramref name="anchor"/> (excluding network/broadcast).
    /// Used when Z21 is reachable: MOBAflow is typically on the same subnet.
    /// </summary>
    public static IReadOnlyList<IPAddress> BuildSubnetFromAnchor(IPAddress anchor)
    {
        ArgumentNullException.ThrowIfNull(anchor);

        if (anchor.AddressFamily != AddressFamily.InterNetwork)
        {
            return [];
        }

        var bytes = anchor.GetAddressBytes();
        if (bytes.Length != 4 || !SubnetCandidateBuilder.IsPrivateIPv4(anchor))
        {
            return [];
        }

        var result = new List<IPAddress>(253);
        for (var lastOctet = 1; lastOctet <= 254; lastOctet++)
        {
            if (lastOctet == bytes[3])
            {
                continue;
            }

            result.Add(new IPAddress(new[] { bytes[0], bytes[1], bytes[2], (byte)lastOctet }));
        }

        return result;
    }

    /// <summary>
    /// Full probe order: recent successful IPs, proximity-ordered subnet, then saved IP (if any).
    /// Does not prioritize the legacy factory default before LAN scan.
    /// </summary>
    public static IReadOnlyList<IPAddress> BuildFullProbeOrder(
        RestApiSettings settings,
        IReadOnlyList<IPAddress> localAddresses,
        IReadOnlyList<IPAddress> subnetCandidates)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(localAddresses);
        ArgumentNullException.ThrowIfNull(subnetCandidates);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<IPAddress>();

        foreach (var recent in settings.RecentIpAddresses ?? [])
        {
            TryAddString(recent, seen, result);
        }

        foreach (var candidate in OrderByProximity(subnetCandidates, localAddresses))
        {
            TryAddAddress(candidate, seen, result);
        }

        if (ShouldProbeSavedIp(settings.CurrentIpAddress))
        {
            TryAddString(settings.CurrentIpAddress, seen, result);
        }

        return result;
    }

    public static bool ShouldProbeSavedIp(string? savedIp)
    {
        if (string.IsNullOrWhiteSpace(savedIp))
        {
            return false;
        }

        var trimmed = savedIp.Trim();
        if (string.Equals(trimmed, LegacyFactoryDefaultIp, StringComparison.Ordinal))
        {
            return false;
        }

        return IPAddress.TryParse(trimmed, out _);
    }

    private static IEnumerable<IPAddress> OrderByProximity(
        IReadOnlyList<IPAddress> candidates,
        IReadOnlyList<IPAddress> localAddresses)
    {
        var localBytes = localAddresses
            .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
            .Select(a => a.GetAddressBytes())
            .Where(b => b.Length == 4)
            .ToList();

        const int unmatchedSubnetScore = 512;

        int ProximityScore(IPAddress candidate)
        {
            var c = candidate.GetAddressBytes();
            if (c.Length != 4)
            {
                return unmatchedSubnetScore;
            }

            var best = unmatchedSubnetScore;
            foreach (var l in localBytes)
            {
                if (c[0] != l[0] || c[1] != l[1] || c[2] != l[2])
                {
                    continue;
                }

                var distance = Math.Abs(c[3] - l[3]);
                if (distance < best)
                {
                    best = distance;
                }
            }

            return best;
        }

        return candidates
            .OrderBy(ProximityScore)
            .ThenBy(ip =>
            {
                var b = ip.GetAddressBytes();
                return b.Length == 4 ? b[3] : 0;
            });
    }

    private static void TryAddString(string? value, HashSet<string> seen, List<IPAddress> result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!IPAddress.TryParse(value.AsSpan().Trim(), out var ip))
        {
            return;
        }

        TryAddAddress(ip, seen, result);
    }

    private static void TryAddAddress(IPAddress ip, HashSet<string> seen, List<IPAddress> result)
    {
        if (ip.AddressFamily != AddressFamily.InterNetwork || !SubnetCandidateBuilder.IsPrivateIPv4(ip))
        {
            return;
        }

        var key = ip.ToString();
        if (!seen.Add(key))
        {
            return;
        }

        result.Add(ip);
    }
}
