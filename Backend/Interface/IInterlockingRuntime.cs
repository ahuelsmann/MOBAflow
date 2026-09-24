// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

using Domain;

using Service.Interlocking;

/// <summary>
/// Runtime boundary for immutable operational observations and direct turnout commands.
/// </summary>
public interface IInterlockingRuntime : IAsyncDisposable
{
    InterlockingRuntimeState Current { get; }

    bool IsSynchronized { get; }

    Task ActivateAsync(InterlockingDefinition definition, CancellationToken cancellationToken = default);

    Task SynchronizeAsync(CancellationToken cancellationToken = default);

    Task<TurnoutCoordinatorResult> SetTurnoutAsync(
        Guid turnoutId,
        TurnoutPosition position,
        Guid correlationId,
        CancellationToken cancellationToken = default);

    Task WhenIdleAsync(CancellationToken cancellationToken = default);
}