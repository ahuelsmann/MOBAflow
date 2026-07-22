// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

using Domain;

using Service.Interlocking;

/// <summary>
/// Narrow runtime boundary for immutable interlocking state and correlated route commands.
/// </summary>
public interface IInterlockingRuntime : IAsyncDisposable
{
    InterlockingRuntimeState Current { get; }

    bool IsSynchronized { get; }

    Task ActivateAsync(InterlockingDefinition definition, CancellationToken cancellationToken = default);

    Task SynchronizeAsync(CancellationToken cancellationToken = default);

    Task<RouteCoordinatorResult> PreviewRouteAsync(Guid routeId, Guid correlationId, CancellationToken cancellationToken = default);

    Task<RouteCoordinatorResult> SelectRouteAsync(Guid routeId, Guid correlationId, CancellationToken cancellationToken = default);

    Task<RouteCoordinatorResult> SetRouteAsync(Guid routeId, Guid correlationId, CancellationToken cancellationToken = default);

    Task<RouteCoordinatorResult> CancelRouteAsync(Guid routeId, Guid correlationId, CancellationToken cancellationToken = default);

    Task<RouteCoordinatorResult> SafeStopRouteAsync(Guid routeId, Guid correlationId, CancellationToken cancellationToken = default);

    Task<RouteCoordinatorResult> ReconcileRouteAsync(Guid routeId, Guid correlationId, CancellationToken cancellationToken = default);

    Task<RouteCoordinatorResult> ReleaseRouteAsync(Guid routeId, Guid correlationId, CancellationToken cancellationToken = default);

    Task WhenIdleAsync(CancellationToken cancellationToken = default);
}
