// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Discovery;

/// <summary>
/// Null-object implementation of <see cref="IZ21DiscoveryService"/> used when Z21 discovery is not available.
/// </summary>
public sealed class NullZ21DiscoveryService : IZ21DiscoveryService
{
    /// <inheritdoc />
    public Task<string?> DiscoverZ21Async(string? preferredIpAddress = null, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);
}
