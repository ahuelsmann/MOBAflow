// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Discovery;

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

/// <summary>
/// Filters local IPv4 addresses and network interfaces for the /24 subnet scans of Z21 and MOBApi discovery.
/// </summary>
public static class LanIpv4AddressHelper
{
    private static readonly HashSet<NetworkInterfaceType> AllowedInterfaceTypes =
    [
        NetworkInterfaceType.Wireless80211,
        NetworkInterfaceType.Ethernet
    ];

    /// <summary>Filters supplied addresses to distinct private IPv4 addresses without querying the network.</summary>
    public static IReadOnlyList<IPAddress> SelectPrivateIpv4Addresses(IEnumerable<IPAddress> addresses)
    {
        ArgumentNullException.ThrowIfNull(addresses);

        var candidates = new List<IPAddress>();
        foreach (var address in addresses)
        {
            TryAddPrivateAddress(candidates, address);
        }

        return candidates;
    }

    private static void TryAddPrivateAddress(List<IPAddress> addresses, IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(address))
        {
            return;
        }

        if (!SubnetCandidateBuilder.IsPrivateIPv4(address))
        {
            return;
        }

        if (addresses.Any(existing => existing.Equals(address)))
        {
            return;
        }

        addresses.Add(address);
    }

    /// <summary>Checks a supplied interface and platform flag without enumerating operating-system interfaces.</summary>
    public static bool IsDiscoveryCapableInterface(NetworkInterface ni, bool isAndroid)
    {
        ArgumentNullException.ThrowIfNull(ni);

        if (ni.OperationalStatus != OperationalStatus.Up)
        {
            return false;
        }

        if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
        {
            return false;
        }

        var name = ni.Name ?? string.Empty;
        if (name.Contains("tun", StringComparison.OrdinalIgnoreCase)
            || name.Contains("ppp", StringComparison.OrdinalIgnoreCase)
            || name.Contains("vpn", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return AllowedInterfaceTypes.Contains(ni.NetworkInterfaceType)
               || isAndroid;
    }
}
