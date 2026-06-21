// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Android.App;
using Android.Content;
using Android.Net;

using Common.Discovery;

using System.Net;

/// <summary>
/// Registers Android ConnectivityManager addresses with <see cref="LanIpv4AddressHelper"/>.
/// </summary>
internal static class LanIpv4AddressHelperAndroidBootstrap
{
    public static void Register()
    {
        LanIpv4AddressHelper.AugmentAddresses = AugmentFromActiveNetwork;
    }

    private static void AugmentFromActiveNetwork(List<IPAddress> privateAddresses)
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

                if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork
                    || IPAddress.IsLoopback(ip)
                    || !SubnetCandidateBuilder.IsPrivateIPv4(ip))
                {
                    continue;
                }

                if (privateAddresses.All(existing => !existing.Equals(ip)))
                {
                    privateAddresses.Add(ip);
                }
            }
        }
        catch
        {
            // ConnectivityManager may be unavailable on some devices.
        }
    }
}
