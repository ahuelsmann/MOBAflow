// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Discovery;

/// <summary>
/// Discovers a Z21 command station on the local network (e.g. by scanning the subnet on port 21105).
/// </summary>
public interface IZ21DiscoveryService
{
    /// <summary>
    /// Attempts to discover a Z21 on the local network.
    /// </summary>
    /// <param name="preferredIpAddress">Optional saved or MOBAflow-provided IP to probe before scanning the subnet.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The IP address of the first responding Z21, or null if none found.</returns>
    Task<string?> DiscoverZ21Async(string? preferredIpAddress = null, CancellationToken cancellationToken = default);
}
