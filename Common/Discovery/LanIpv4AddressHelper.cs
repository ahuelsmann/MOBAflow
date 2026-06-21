// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Discovery;

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

/// <summary>
/// Resolves local IPv4 addresses used to derive /24 subnet scan lists (Z21 and MOBApi discovery).
/// </summary>
public static class LanIpv4AddressHelper
{
    private static readonly HashSet<NetworkInterfaceType> AllowedInterfaceTypes =
    [
        NetworkInterfaceType.Wireless80211,
        NetworkInterfaceType.Ethernet
    ];

    /// <summary>
    /// Optional host-specific augmenter (e.g. Android ConnectivityManager fallback) registered at startup.
    /// </summary>
    public static Action<List<IPAddress>>? AugmentAddresses { get; set; }

    /// <summary>
    /// Gets candidate local IPv4 addresses whose /24 subnets should be scanned on the LAN.
    /// </summary>
    public static List<IPAddress> GetCandidateLocalIpv4Addresses()
    {
        var privateAddresses = new List<IPAddress>();

        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (!IsDiscoveryCapableInterface(ni))
                {
                    continue;
                }

                var props = ni.GetIPProperties();
                foreach (var ua in props.UnicastAddresses)
                {
                    TryAddPrivateAddress(privateAddresses, ua.Address);
                }
            }
        }
        catch
        {
            // Ignore: e.g. permission or platform
        }

        AugmentAddresses?.Invoke(privateAddresses);

        return privateAddresses;
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

    private static bool IsDiscoveryCapableInterface(NetworkInterface ni)
    {
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
               || OperatingSystem.IsAndroid();
    }
}
