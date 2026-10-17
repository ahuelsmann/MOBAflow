// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Discovery;

using System.Net;
using System.Net.NetworkInformation;

/// <summary>
/// Reads candidate addresses from the operating system's network interfaces.
/// </summary>
public sealed class NetworkInterfaceIpv4AddressSource : ILocalIpv4AddressSource
{
    /// <inheritdoc />
    public IReadOnlyList<IPAddress> GetCandidateLocalIpv4Addresses()
    {
        var addresses = new List<IPAddress>();

        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (!LanIpv4AddressHelper.IsDiscoveryCapableInterface(ni, OperatingSystem.IsAndroid()))
                {
                    continue;
                }

                var props = ni.GetIPProperties();
                foreach (var ua in props.UnicastAddresses)
                {
                    addresses.Add(ua.Address);
                }
            }
        }
        catch
        {
            // Ignore: e.g. permission or platform
        }

        return LanIpv4AddressHelper.SelectPrivateIpv4Addresses(addresses);
    }
}
