// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Extensions;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Shared dependency injection helpers for MOBAflow UI hosts.
/// </summary>
public static class ServiceCollectionDependencyInjectionExtensions
{
    /// <summary>
    /// Registers a concrete service and exposes it through a shared singleton interface alias.
    /// </summary>
    public static IServiceCollection AddSingletonWithInterface<TService, TInterface>(this IServiceCollection services)
        where TService : class, TInterface
        where TInterface : class
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<TService>();
        services.AddSingleton<TInterface>(sp => sp.GetRequiredService<TService>());
        return services;
    }
}
