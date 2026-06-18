// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Common.Discovery;

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

#if ANDROID
using Android.App;
using Android.Content;
using Android.Net;
#endif

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

#if ANDROID
        TryAddAndroidActiveNetworkAddresses(privateAddresses);
#endif

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

#if ANDROID
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

        return true;
#else
        return AllowedInterfaceTypes.Contains(ni.NetworkInterfaceType);
#endif
    }

#if ANDROID
    private static void TryAddAndroidActiveNetworkAddresses(List<IPAddress> privateAddresses)
    {
        try
        {
            var connectivity = (ConnectivityManager?)Application.Context.GetSystemService(Context.ConnectivityService);
            var activeNetwork = connectivity?.ActiveNetwork;
            if (activeNetwork == null || connectivity == null)
            {
                return;
            }

            var linkProperties = connectivity.GetLinkProperties(activeNetwork);
            if (linkProperties?.LinkAddresses == null)
            {
                return;
            }

            foreach (var linkAddress in linkProperties.LinkAddresses)
            {
                var javaAddress = linkAddress.Address;
                if (javaAddress == null)
                {
                    continue;
                }

                var host = javaAddress.HostAddress;
                if (string.IsNullOrWhiteSpace(host) || !IPAddress.TryParse(host, out var ip))
                {
                    continue;
                }

                TryAddPrivateAddress(privateAddresses, ip);
            }
        }
        catch
        {
            // Ignore: ConnectivityManager may be unavailable on some devices.
        }
    }
#endif
}
