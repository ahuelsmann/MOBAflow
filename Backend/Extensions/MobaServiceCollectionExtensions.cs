// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Extensions;

using Interface;
using Network;
using Service;
using Microsoft.Extensions.DependencyInjection;
using Domain;

/// <summary>
/// Extension methods for registering shared backend services across all platforms.
/// This ensures consistent service registration in WinUI, MAUI, and WebApp.
/// </summary>
public static class MobaServiceCollectionExtensions
{
    /// <summary>
    /// Registers all shared backend services (Z21, ActionExecutor, WorkflowService, etc).
    /// Use in WinUI/App.xaml.cs, MAUI/MauiProgram.cs, and WebApp/Program.cs
    /// 
    /// Includes:
    /// - Network: IUdpClientWrapper, Z21Monitor, IZ21
    /// - Workflow: WorkflowService, ActionExecutor
    /// - Domain: Solution
    /// </summary>
    /// <param name="services">The service collection to register services in</param>
    /// <returns>The service collection for fluent chaining</returns>
    public static IServiceCollection AddMobaBackendServices(
        this IServiceCollection services)
    {
        // Network & Monitoring
        services.AddSingleton<Z21Monitor>();
        services.AddSingleton<IUdpClientWrapper, UdpWrapper>();
        services.AddSingleton<IZ21, Z21>(sp =>
        {
            var udp = sp.GetRequiredService<IUdpClientWrapper>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Z21>>();
            var trafficMonitor = sp.GetRequiredService<Z21Monitor>();
            return new Z21(udp, logger, trafficMonitor);
        });

        // Workflow & Actions
        services.AddSingleton<WorkflowService>();
        services.AddSingleton<IActionExecutor, ActionExecutor>();

        // Domain
        services.AddSingleton<Solution>();

        return services;
    }
}
