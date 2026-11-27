// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Extensions;

using Interface;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering UiDispatcher services in the dependency injection container.
/// </summary>
public static class UiDispatcherServiceCollectionExtensions
{
    /// <summary>
    /// Registers a fallback <see cref="IUiDispatcher"/> that runs actions on the calling thread.
    /// WinUI and MAUI hosts must register their platform dispatcher explicitly instead
    /// (see <c>MobaWinUiServiceCollectionExtensions</c> and <c>MobaMauiServiceCollectionExtensions</c>),
    /// because SharedUI is compiled without platform-specific symbols.
    ///
    /// IMPORTANT: This method MUST be called BEFORE any services that depend on IUiDispatcher.
    /// </summary>
    /// <param name="services">The service collection to register with.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddUiDispatcher();
    /// </code>
    /// </example>
    public static IServiceCollection AddUiDispatcher(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the synchronous DefaultUiDispatcher fallback as a singleton.
        // Do not call this after a host has registered its platform IUiDispatcher,
        // because AddSingleton appends and the last registration wins.
        services.AddSingleton(GetPlatformDispatcher());

        return services;
    }

    /// <summary>
    /// Returns the synchronous fallback <see cref="IUiDispatcher"/> implementation.
    /// </summary>
    private static IUiDispatcher GetPlatformDispatcher()
    {
        // Default fallback (e.g., Blazor, console tests)
        // WinUI and MAUI hosts register their platform dispatcher explicitly.
        return new DefaultUiDispatcher();
    }

    /// <summary>
    /// Fallback dispatcher for environments without dedicated UI thread management.
    /// Executes all actions immediately on the calling thread.
    /// </summary>
    private sealed class DefaultUiDispatcher : IUiDispatcher
    {
        public void InvokeOnUi(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);
            action();
        }

        public void InvokeOnUiHighPriority(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);
            action();
        }

        public void InvokeOnUiLowPriority(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);
            action();
        }

        public Task InvokeOnUiAsync(Func<Task> asyncAction)
        {
            ArgumentNullException.ThrowIfNull(asyncAction);
            return asyncAction();
        }

        public Task InvokeOnUiAsync(Func<Task> asyncAction, UiPriority priority)
        {
            _ = priority;
            ArgumentNullException.ThrowIfNull(asyncAction);
            return asyncAction();
        }

        public Task<T> InvokeOnUiAsync<T>(Func<Task<T>> asyncFunc)
        {
            ArgumentNullException.ThrowIfNull(asyncFunc);
            return asyncFunc();
        }

        public Task<T> InvokeOnUiAsync<T>(Func<Task<T>> asyncFunc, UiPriority priority)
        {
            _ = priority;
            ArgumentNullException.ThrowIfNull(asyncFunc);
            return asyncFunc();
        }
    }
}
