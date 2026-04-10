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
    /// <summary>
    /// Gets candidate local IPv4 addresses whose /24 subnets should be scanned on the LAN.
    /// </summary>
    public static List<IPAddress> GetCandidateLocalIpv4Addresses()
    {
        try
        {
            var privateAddresses = new List<IPAddress>();
            var fallbackAddresses = new List<IPAddress>();

            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
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
                    else
                    {
                        fallbackAddresses.Add(address);
                    }
                }
            }

            if (privateAddresses.Count > 0)
            {
                return privateAddresses;
            }

#if ANDROID
            return fallbackAddresses;
#else
            return [];
#endif
        }
        catch
        {
            // Ignore: e.g. permission or platform
        }

        return [];
    }
}
