// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Discovery;

/// <summary>
/// A Z21 command station that answered a network search.
/// </summary>
/// <param name="IpAddress">IP address of the Z21.</param>
/// <param name="Port">UDP port the Z21 answered on.</param>
/// <param name="SerialNumber">Serial number reported by the Z21.</param>
public sealed record DiscoveredZ21(string IpAddress, int Port, uint SerialNumber);
