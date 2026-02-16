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
    /// Registers the platform-specific IUiDispatcher implementation as a singleton.
    /// 
    /// The actual implementation is determined at compile-time based on the target platform:
    /// - WinUI: Uses Microsoft.UI.Dispatching.DispatcherQueue for thread marshalling
    /// - MAUI: Uses MainThread API for cross-platform UI thread dispatch
    /// - Blazor: Uses BlazorUiDispatcher for server-side rendering
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

        // Register as singleton because:
        // 1. DispatcherQueue (WinUI) / MainThread (MAUI) are static, not per-request
        // 2. No state to manage, safe to share across entire app lifetime
        // 3. Improves performance by avoiding repeated instantiation
        services.AddSingleton(GetPlatformDispatcher());

        return services;
    }

    /// <summary>
    /// Returns the platform-specific IUiDispatcher implementation.
    /// </summary>
    private static IUiDispatcher GetPlatformDispatcher()
    {
#if WINDOWS
        // WinUI 3 desktop application
        return new Moba.WinUI.Service.UiDispatcher();
#elif ANDROID || IOS || MACCATALYST
        // .NET MAUI mobile/tablet application
        return new Moba.MAUI.Service.UiDispatcher();
#else
        // Default fallback (e.g., Blazor, console tests)
        // Returns a no-op dispatcher that executes immediately
        return new DefaultUiDispatcher();
#endif
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

        public Task InvokeOnUiAsync(Func<Task> asyncAction)
        {
            ArgumentNullException.ThrowIfNull(asyncAction);
            return asyncAction();
        }

        public Task<T> InvokeOnUiAsync<T>(Func<Task<T>> asyncFunc)
        {
            ArgumentNullException.ThrowIfNull(asyncFunc);
            return asyncFunc();
        }
    }
}
