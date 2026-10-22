// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Discovery;

using System.Net;

/// <summary>
/// Supplies the local private IPv4 addresses whose /24 subnets Z21 and MOBApi discovery scan.
/// Hosts register the source that fits their platform.
/// </summary>
public interface ILocalIpv4AddressSource
{
    /// <summary>Gets distinct private, non-loopback IPv4 addresses of the active LAN interfaces.</summary>
    IReadOnlyList<IPAddress> GetCandidateLocalIpv4Addresses();
}
