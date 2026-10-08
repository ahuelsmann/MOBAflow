// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// The Z21 command station assigned to a project. Every project controls its own Z21; two projects never share one.
/// Pure data object without behavior.
/// </summary>
public class Z21Endpoint
{
    /// <summary>The Z21 default UDP port.</summary>
    public const int DefaultPort = 21105;

    /// <summary>
    /// Gets or sets the IP address of the Z21; empty when no Z21 is assigned.
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UDP port of the Z21.
    /// </summary>
    public int Port { get; set; } = DefaultPort;

    /// <summary>
    /// Gets or sets the serial number reported by the Z21 when it was assigned; helps to tell devices apart.
    /// </summary>
    public uint? SerialNumber { get; set; }
}
