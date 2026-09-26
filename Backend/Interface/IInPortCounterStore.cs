// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Interface;

/// <summary>Persists one application's local input counts independently of solutions.</summary>
public interface IInPortCounterStore
{
    /// <summary>Loads saved counts; a missing file represents the first application start.</summary>
    Task<IReadOnlyDictionary<uint, ulong>> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Atomically replaces the complete saved counter inventory.</summary>
    Task SaveAsync(IReadOnlyDictionary<uint, ulong> counts, CancellationToken cancellationToken = default);
}
