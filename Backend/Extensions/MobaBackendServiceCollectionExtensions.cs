// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend;

using Data;
using Interface;
using Network;
using Service;

using Common.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Sound;

/// <summary>
/// Dependency injection registrations shared by all MOBAflow hosts that use the backend runtime.
/// </summary>
public static class MobaBackendServiceCollectionExtensions
{
    /// <summary>
    /// Registers the platform-neutral backend runtime, Z21 transport and workflow services.
    /// </summary>
    public static IServiceCollection AddMobaBackendServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<MasterDataStore>();
        services.TryAddSingleton<Z21Monitor>();
        services.TryAddSingleton<IUdpClientWrapper, UdpWrapper>();
        services.TryAddSingleton<IZ21, Z21>();
        services.TryAddSingleton<IProjectValidator, ProjectValidator>();
        services.TryAddSingleton<AnnouncementService>();
        services.TryAddSingleton<IActionExecutor>(sp => new ActionExecutor(
            sp.GetRequiredService<AnnouncementService>(),
            sp.GetService<ITrainDestinationDisplayService>(),
            sp.GetService<ILogger<ActionExecutor>>()));
        services.TryAddSingleton<IWorkflowService, WorkflowService>();
        services.TryAddSingleton(sp => new ActionExecutionContext
        {
            Z21 = sp.GetRequiredService<IZ21>(),
            SpeakerEngine = sp.GetService<ISpeakerEngine>(),
            SoundPlayer = sp.GetService<ISoundPlayer>()
        });
        services.TryAddSingleton<IMobaRuntime, MobaRuntimeService>();

        return services;
    }
}
