// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Events;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension method for registering EventBus in DI container.
/// </summary>
public static class EventBusServiceCollectionExtensions
{
    /// <summary>
    /// Registers the central EventBus as a singleton.
    /// Must be called during application startup before services are built.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddEventBus(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, EventBus>();
        return services;
    }
}
