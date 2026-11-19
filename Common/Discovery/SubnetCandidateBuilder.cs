// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Discovery;

using System.Net;
using System.Net.Sockets;

/// <summary>
/// Builds IPv4 subnet scan candidates for discovery services.
/// </summary>
public static class SubnetCandidateBuilder
{
    /// <summary>
    /// Builds scan candidates for every distinct /24 subnet represented by the provided local IPv4 addresses.
    /// </summary>
    /// <param name="localAddresses">Local IPv4 addresses that represent reachable subnets.</param>
    /// <returns>Ordered list of candidate host addresses excluding the local host addresses themselves.</returns>
    public static IReadOnlyList<IPAddress> BuildCandidates(IEnumerable<IPAddress> localAddresses)
    {
        ArgumentNullException.ThrowIfNull(localAddresses);

        var candidates = new List<IPAddress>();
        var ipv4Addresses = localAddresses
            .Where(address => address.AddressFamily == AddressFamily.InterNetwork)
            .ToList();
        var excludedHosts = new HashSet<uint>(ipv4Addresses.Select(BuildAddressKey));
        var seenSubnets = new HashSet<uint>();

        foreach (var address in ipv4Addresses)
        {
            var bytes = address.GetAddressBytes();
            var subnetKey = BuildSubnetKey(bytes[0], bytes[1], bytes[2]);
            if (!seenSubnets.Add(subnetKey))
            {
                continue;
            }

            for (var lastOctet = 1; lastOctet <= 254; lastOctet++)
            {
                var candidateKey = BuildAddressKey(bytes[0], bytes[1], bytes[2], (byte)lastOctet);
                if (excludedHosts.Contains(candidateKey))
                {
                    continue;
                }

                candidates.Add(new IPAddress(new[] { bytes[0], bytes[1], bytes[2], (byte)lastOctet }));
            }
        }

        return candidates;
    }

    /// <summary>
    /// Returns true when the given address is in a private IPv4 range.
    /// </summary>
    public static bool IsPrivateIPv4(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] == 10
               || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
               || (bytes[0] == 192 && bytes[1] == 168);
    }

    private static uint BuildSubnetKey(byte first, byte second, byte third)
        => ((uint)first << 16) | ((uint)second << 8) | third;

    private static uint BuildAddressKey(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return BuildAddressKey(bytes[0], bytes[1], bytes[2], bytes[3]);
    }

    private static uint BuildAddressKey(byte first, byte second, byte third, byte fourth)
        => ((uint)first << 24) | ((uint)second << 16) | ((uint)third << 8) | fourth;
}
