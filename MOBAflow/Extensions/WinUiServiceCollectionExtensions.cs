// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Extensions;

using Backend.Data;
using Backend.Extensions;
using Backend.Interface;
using Backend.Service;
using Common.Configuration;
using Common.Events;
using Common.Extension;
using Common.Navigation;
using Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moba.SharedUI.ViewModel;
using Serilog;
using Service;
using SharedUI.Extensions;
using SharedUI.Interface;
using SharedUI.Shell;
using Sound;
using TrackLibrary.PikoA;
using TrackPlan.Renderer;
using View;
using ViewModel;

/// <summary>
/// Central DI registration for the WinUI host (extracted from <see cref="Moba.WinUI.App"/> for readability).
/// </summary>
internal static class WinUiServiceCollectionExtensions
{
    /// <summary>
    /// Registers MOBAflow WinUI services, view models, and shell types. Requires Serilog, <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>, and logging to be configured first.
    /// </summary>
    internal static IServiceCollection AddMobaWinUiApplicationServices(this IServiceCollection services)
    {
        services.AddUiDispatcher();

        services.AddEventBusWithUiDispatch();

        var corePages = NavigationRegistration.RegisterPages(services);
        services.AddSingleton(corePages);

        services.AddSingleton<IFeatureTogglePageProvider>(sp =>
            new FeatureTogglePageProvider(
                sp.GetRequiredService<List<PageMetadata>>(),
                sp.GetRequiredService<AppSettings>()));

        services.AddSingleton<SpeakerEngineFactory>();

        services.AddSingleton<ISpeakerEngine>(sp =>
        {
            var factory = sp.GetRequiredService<SpeakerEngineFactory>();
            return factory.CreateEngineFromOptions();
        });

        services.AddMobaBackendServices();
        services.AddSingleton<IMobaRuntime, MobaRuntimeService>();

        services.AddSingleton<IIoService, IoService>();
        services.AddSingleton<IUiDispatcher, UiDispatcher>();
        services.AddSingleton<PhotoHubClient>();

        services.AddHttpClient();

        services.AddSingleton<RestApiStatusService>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new RestApiStatusService(
                factory.CreateClient(),
                sp.GetRequiredService<AppSettings>(),
                sp.GetRequiredService<RestApiProcessService>(),
                sp.GetRequiredService<PhotoHubClient>(),
                sp.GetRequiredService<IRestApiStatusSink>(),
                sp.GetRequiredService<IUiDispatcher>(),
                sp.GetRequiredService<ILogger<RestApiStatusService>>());
        });

        services.AddSingleton<RestApiProcessService>();

        services.AddSingleton<ICityService>(sp => new CityService(
            sp.GetRequiredService<MasterDataStore>(),
            sp.GetRequiredService<ILogger<CityService>>()));

        services.AddSingleton<ILocomotiveService>(sp => new LocomotiveService(
            sp.GetRequiredService<MasterDataStore>(),
            sp.GetRequiredService<ILogger<LocomotiveService>>()));

        services.AddSingleton<ViessmannSignalService>(sp =>
            new ViessmannSignalService(sp.GetRequiredService<MasterDataStore>()));

        services.AddSingleton<ISettingsService>(sp => new SettingsService(
            sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<ILogger<SettingsService>>()));

        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());
        services.AddSingleton<IShellService, ShellService>();

        services.AddSingleton<ISoundPlayer, WindowsSoundPlayer>();
        services.AddSingleton<HealthCheckService>();

        services.AddSingleton<LayoutColumnWidthsViewModel>();
        services.AddSingleton(sp => new MainWindowViewModel(
            sp.GetRequiredService<LayoutColumnWidthsViewModel>(),
            sp.GetRequiredService<IMobaRuntime>(),
            sp.GetRequiredService<IEventBus>(),
            sp.GetRequiredService<IUiDispatcher>(),
            sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<Solution>(),
            sp.GetRequiredService<ActionExecutionContext>(),
            sp.GetRequiredService<ILogger<MainWindowViewModel>>(),
            sp.GetRequiredService<IIoService>(),
            sp.GetRequiredService<ICityService>(),
            sp.GetRequiredService<ISettingsService>(),
            sp.GetRequiredService<AnnouncementService>(),
            sp.GetRequiredService<PhotoHubClient>(),
            sp.GetService<IFeatureTogglePageProvider>(),
            loggerFactory: sp.GetRequiredService<ILoggerFactory>()
        ));

        services.AddSingleton<IRestApiStatusSink>(sp => sp.GetRequiredService<MainWindowViewModel>());

        services.AddSingleton<JourneyMapViewModel>();
        services.AddTransient<MonitorPageViewModel>();
        services.AddSingleton<SkinSelectorViewModel>();
        services.AddSingleton(sp => new TrainControlViewModel(
            sp.GetRequiredService<IMobaRuntime>(),
            sp.GetRequiredService<ISettingsService>(),
            sp.GetRequiredService<MainWindowViewModel>(),
            sp.GetService<ILogger<TrainControlViewModel>>(),
            sp.GetService<IUiDispatcher>()
        ));

        services.AddSingleton<TrackPlan>();
        services.AddSingleton(sp => new TrackPlanViewModel(
            sp.GetRequiredService<TrackPlan>(),
            sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<ISettingsService>(),
            sp.GetRequiredService<ILogger<TrackPlanViewModel>>()));
        services.AddSingleton<EditableTrackPlan>();

        services.AddSingleton<ISkinProvider, SkinProvider>();

        services.AddSingleton(sp => new MainWindow(
            sp.GetRequiredService<MainWindowViewModel>(),
            sp.GetRequiredService<NavigationService>(),
            sp.GetRequiredService<IUiDispatcher>(),
            sp.GetRequiredService<IIoService>(),
            sp.GetRequiredService<List<PageMetadata>>(),
            sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<ISkinProvider>(),
            sp.GetRequiredService<RestApiStatusService>(),
            sp.GetRequiredService<RestApiProcessService>(),
            sp.GetRequiredService<ILogger<MainWindow>>()));

        services.AddSingleton<PostStartupInitializationService>();
        services.AddSingleton<SpeechHealthCheck>();

        return services;
    }
}
