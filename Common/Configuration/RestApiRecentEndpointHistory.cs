// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Configuration;

/// <summary>
/// Maintains a bounded, de-duplicated list of recently successful REST API host IPs for faster LAN discovery.
/// </summary>
public static class RestApiRecentEndpointHistory
{
    /// <summary>
    /// Maximum number of recent REST API IP addresses to retain.
    /// </summary>
    public const int MaxRecentAddresses = 12;

    /// <summary>
    /// Moves the given IP to the front of <see cref="RestApiSettings.RecentIpAddresses"/> (case-insensitive de-dupe).
    /// Does not modify <see cref="RestApiSettings.CurrentIpAddress"/> or <see cref="RestApiSettings.Port"/>.
    /// </summary>
    /// <param name="rest">REST settings whose recent list is updated.</param>
    /// <param name="ip">IPv4 address string to record.</param>
    /// <returns><c>true</c> if the list was mutated and should be persisted.</returns>
    public static bool RecordRecentIp(RestApiSettings rest, string ip)
    {
        ArgumentNullException.ThrowIfNull(rest);
        if (string.IsNullOrWhiteSpace(ip))
        {
            return false;
        }

        var trimmed = ip.Trim();
        var list = rest.RecentIpAddresses;

        if (list.Count > 0
            && string.Equals(list[0], trimmed, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        list.RemoveAll(x => string.Equals(x, trimmed, StringComparison.OrdinalIgnoreCase));
        list.Insert(0, trimmed);

        while (list.Count > MaxRecentAddresses)
        {
            list.RemoveAt(list.Count - 1);
        }

        return true;
    }
}