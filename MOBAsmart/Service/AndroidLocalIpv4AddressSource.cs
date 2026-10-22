// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Android.App;
using Android.Content;
using Android.Net;

using Common.Discovery;

using System.Net;

/// <summary>
/// Adds the addresses of Android's active network (ConnectivityManager) to the network-interface addresses,
/// because Android does not always report the Wi-Fi interface through <see cref="System.Net.NetworkInformation"/>.
/// </summary>
internal sealed class AndroidLocalIpv4AddressSource(NetworkInterfaceIpv4AddressSource interfaces) : ILocalIpv4AddressSource
{
    private readonly NetworkInterfaceIpv4AddressSource _interfaces = interfaces ?? throw new ArgumentNullException(nameof(interfaces));

    /// <inheritdoc />
    public IReadOnlyList<IPAddress> GetCandidateLocalIpv4Addresses() =>
        LanIpv4AddressHelper.MergePrivateIpv4Addresses(
            _interfaces.GetCandidateLocalIpv4Addresses(), GetActiveNetworkAddresses());

    private static List<IPAddress> GetActiveNetworkAddresses()
    {
        var addresses = new List<IPAddress>();
        try
        {
            var connectivity = (ConnectivityManager?)Application.Context.GetSystemService(Context.ConnectivityService);
            var activeNetwork = connectivity?.ActiveNetwork;
            if (activeNetwork == null || connectivity == null)
            {
                return addresses;
            }

            var linkProperties = connectivity.GetLinkProperties(activeNetwork);
            if (linkProperties?.LinkAddresses == null)
            {
                return addresses;
            }

            foreach (var linkAddress in linkProperties.LinkAddresses)
            {
                var host = linkAddress.Address?.HostAddress;
                if (!string.IsNullOrWhiteSpace(host) && IPAddress.TryParse(host, out var ip))
                {
                    addresses.Add(ip);
                }
            }
        }
        catch
        {
            // ConnectivityManager may be unavailable on some devices.
        }

        return addresses;
    }
}
