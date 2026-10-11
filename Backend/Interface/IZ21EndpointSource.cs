// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Interface;

using System.Net;

/// <summary>
/// Supplies the Z21 a runtime connects to.
/// </summary>
public interface IZ21EndpointSource
{
    /// <summary>
    /// Gets the configured address for status messages; empty when none is known yet.
    /// </summary>
    string DisplayAddress { get; }

    /// <summary>
    /// Resolves the Z21 to connect to.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Z21EndpointResolution> ResolveAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// The Z21 address and port to connect to, or the reason why there is none.
/// </summary>
/// <param name="Address">The Z21 address; null when <paramref name="Error"/> explains why there is none.</param>
/// <param name="Port">The UDP port.</param>
/// <param name="Error">A user-facing reason when no address is available.</param>
public sealed record Z21EndpointResolution(IPAddress? Address, int Port, string? Error)
{
    /// <summary>Identifies the project supplying this endpoint, so late connection attempts cannot cross projects.</summary>
    public Guid? ProjectId { get; init; }

    /// <summary>Creates a resolution without an address.</summary>
    public static Z21EndpointResolution Unavailable(string error) => new(null, 0, error);
}
