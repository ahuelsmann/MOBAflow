// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Extensions;

using Domain;
using Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Network;
using Service;
using Sound;

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
    /// - Workflow: WorkflowService, ActionExecutor, ActionExecutionContext
    /// - Domain: Solution
    /// 
    /// Audio Services (ISoundPlayer, ISpeakerEngine):
    /// - Backend expects these services from platform-specific registrations
    /// - If not registered by platform, ActionExecutor uses null-safe execution
    /// - WinUI: WindowsSoundPlayer, SystemSpeechEngine/CognitiveSpeechEngine
    /// - MAUI: NullSoundPlayer, NullSpeakerEngine (or platform-specific implementations)
    /// - WebApp: NullSoundPlayer, NullSpeakerEngine
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
            var logger = sp.GetService<ILogger<Z21>>();
            var trafficMonitor = sp.GetRequiredService<Z21Monitor>();
            return new Z21(udp, logger, trafficMonitor);
        });

        // ✅ ActionExecutionContext (Backend owns this, platforms provide audio implementations)
        services.AddSingleton<ActionExecutionContext>(sp => new ActionExecutionContext
        {
            Z21 = sp.GetRequiredService<IZ21>(),
            SpeakerEngine = sp.GetService<ISpeakerEngine>(),  // Optional - from platform
            SoundPlayer = sp.GetService<ISoundPlayer>()       // Optional - from platform
        });

        // ✅ AnnouncementService (optional - only if ISpeakerEngine is available)
        services.AddSingleton<AnnouncementService>(sp =>
        {
            var speakerEngine = sp.GetService<ISpeakerEngine>();
            var logger = sp.GetService<ILogger<AnnouncementService>>();
            return new AnnouncementService(speakerEngine, logger);
        });

        // ✅ ActionExecutor (uses AnnouncementService if available)
        services.AddSingleton<IActionExecutor>(sp =>
        {
            var announcementService = sp.GetService<AnnouncementService>();
            return new ActionExecutor(announcementService);
        });

        // Workflow & Actions
        services.AddSingleton<WorkflowService>();

        // Domain
        services.AddSingleton<Solution>();

        return services;
    }
}
