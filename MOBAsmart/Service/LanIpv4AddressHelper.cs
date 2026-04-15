// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Common.Discovery;

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

/// <summary>
/// Resolves local IPv4 addresses used to derive /24 subnet scan lists (Z21 and MOBApi discovery).
/// </summary>
internal static class LanIpv4AddressHelper
{
    private static readonly HashSet<NetworkInterfaceType> AllowedInterfaceTypes =
    [
        NetworkInterfaceType.Wireless80211,
        NetworkInterfaceType.Ethernet
    ];

    /// <summary>
    /// Gets candidate local IPv4 addresses whose /24 subnets should be scanned on the LAN.
    /// </summary>
    public static List<IPAddress> GetCandidateLocalIpv4Addresses()
    {
        try
        {
            var privateAddresses = new List<IPAddress>();

            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (!IsDiscoveryCapableInterface(ni))
                {
                    continue;
                }

                var props = ni.GetIPProperties();
                foreach (var ua in props.UnicastAddresses)
                {
                    var address = ua.Address;
                    if (address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(address))
                    {
                        continue;
                    }

                    if (SubnetCandidateBuilder.IsPrivateIPv4(address))
                    {
                        privateAddresses.Add(address);
                    }
                }
            }

            return privateAddresses;
        }
        catch
        {
            // Ignore: e.g. permission or platform
        }

        return [];
    }

    private static bool IsDiscoveryCapableInterface(NetworkInterface ni)
    {
        // Restrict discovery to active Wi-Fi and wired LAN interfaces.
        // This avoids scanning VPN, virtual adapters, and tunnel interfaces.
        return ni.OperationalStatus == OperationalStatus.Up
               && AllowedInterfaceTypes.Contains(ni.NetworkInterfaceType);
    }
}
