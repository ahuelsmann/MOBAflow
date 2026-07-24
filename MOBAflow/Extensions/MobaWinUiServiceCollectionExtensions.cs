// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Extensions;

using Backend.Data;
using Backend.Interface;
using Backend.Service;
using Backend.Service.TrackPlan;
using Backend.Service.Validation;
using Common.Configuration;
using Common.Events;
using Common.Multiplex;
using Common.Navigation;
using Display.Rendering;
using Display.Runtime;
using Display.Transport;
using Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moba.Backend;
using Serilog;
using Service;
using SharedUI.Extensions;
using SharedUI.Interface;
using SharedUI.Service;
using SharedUI.Shell;
using SharedUI.ViewModel;
using Sound;
using TrackLibrary.PikoA;
using TrackPlan.Renderer;
using View;

/// <summary>
/// Dependency injection registrations for the MOBAflow WinUI host.
/// </summary>
public static class MobaWinUiServiceCollectionExtensions
{
    /// <summary>
    /// Registers configuration, AppSettings, SpeechOptions, and Serilog logging.
    /// </summary>
    public static IServiceCollection AddMobaWinUiConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton(configuration);
        services.Configure<AppSettings>(configuration);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);
        services.Configure<SpeechOptions>(configuration.GetSection("Speech"));
        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(Log.Logger, dispose: true));

        return services;
    }

    /// <summary>
    /// Registers UI dispatcher, EventBus, navigation pages, and feature toggle provider.
    /// </summary>
    public static IServiceCollection AddMobaWinUiPlatformServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // SharedUI's AddUiDispatcher() falls back to a no-op dispatcher because SharedUI
        // is compiled without the WINDOWS symbol. Register the WinUI dispatcher explicitly.
        services.AddSingleton<IUiDispatcher, UiDispatcher>();
        services.AddEventBusWithUiDispatch();

        var corePages = NavigationRegistration.RegisterPages(services);
        services.AddSingleton(corePages);

        services.AddSingleton<IFeatureTogglePageProvider>(sp =>
            new FeatureTogglePageProvider(
                sp.GetRequiredService<List<PageMetadata>>(),
                sp.GetRequiredService<AppSettings>()));

        return services;
    }

    /// <summary>
    /// Registers speaker engine factory and the configured engine singleton.
    /// </summary>
    public static IServiceCollection AddMobaWinUiSpeechServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ISpeakerEngineRegistration, PiperSpeakerEngineRegistration>();
        services.AddSingleton<ISpeakerEngineRegistration, SystemSpeechEngineRegistration>();
        services.AddSingleton<SpeakerEngineFactory>();
        services.AddSingleton<ISpeakerEngineFactory>(sp => sp.GetRequiredService<SpeakerEngineFactory>());
        services.AddSingleton<ISpeakerEngine>(sp =>
            sp.GetRequiredService<SpeakerEngineFactory>().CreateEngineFromOptions());

        return services;
    }

    /// <summary>
    /// Registers shared backend services and announcement pipeline.
    /// </summary>
    public static IServiceCollection AddMobaWinUiBackendServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<Solution>();
        services.AddMobaBackendServices();
        services.AddSingleton(sp => new AnnouncementService(
            sp.GetRequiredService<ISpeakerEngineFactory>(),
            sp.GetRequiredService<ILogger<AnnouncementService>>()));

        return services;
    }

    /// <summary>
    /// Registers file I/O, HTTP clients, REST API, photo hub, and runtime hub services.
    /// </summary>
    public static IServiceCollection AddMobaWinUiIoAndNetworkServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingletonWithInterface<IoService, IIoService>();
        services.AddSingleton<ISolutionIoService>(sp => sp.GetRequiredService<IIoService>());
        services.AddSingleton<IFilePickerService>(sp => sp.GetRequiredService<IIoService>());
        services.AddSingleton<IPhotoStorageService>(sp => sp.GetRequiredService<IIoService>());
        services.AddSingleton<IRecordingFileService, RecordingFileService>();
        services.AddSingletonWithInterface<PhotoHubClient, IPhotoHubClient>();
        services.AddSingleton<HostControlPlaneSession>();
        services.AddSingletonWithInterface<RuntimeHubHostClient, IRuntimeHubHostClient>();
        services.AddSingleton<RestApiRuntimeHubService>();
        services.AddSingleton<RestApiRuntimeCommandConsumerService>();
        services.AddSingleton<LocalRuntimeCommandGateway>();
        // The explicit factory keeps the production command route as the decorator's concrete inner gateway.
        services.AddSingleton(sp => new RecordingRuntimeCommandGateway(
            sp.GetRequiredService<LocalRuntimeCommandGateway>(),
            sp.GetRequiredService<IRecordingSessionService>()));
        services.AddSingleton<IRuntimeCommandGateway>(sp => sp.GetRequiredService<RecordingRuntimeCommandGateway>());

        services.AddHttpClient();

        services.AddSingleton(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new RestApiStatusService(
                factory.CreateClient(),
                sp.GetRequiredService<AppSettings>(),
                sp.GetRequiredService<RestApiProcessService>(),
                sp.GetRequiredService<IPhotoHubClient>(),
                sp.GetRequiredService<RestApiRuntimeHubService>(),
                sp.GetRequiredService<RestApiSolutionSyncService>(),
                sp.GetRequiredService<IRuntimeHubHostClient>(),
                sp.GetRequiredService<IMobaRuntime>(),
                sp.GetRequiredService<IEventBus>(),
                sp.GetRequiredService<ILogger<RestApiStatusService>>());
        });

        services.AddSingleton<RestApiProcessService>();
        services.AddSingleton<RestApiSolutionSyncService>();

        return services;
    }

    /// <summary>
    /// Registers domain-specific master data and settings services.
    /// </summary>
    public static IServiceCollection AddMobaWinUiDomainServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ICityService>(sp => new CityService(
            sp.GetRequiredService<MasterDataStore>(),
            sp.GetRequiredService<ILogger<CityService>>()));

        services.AddSingleton<ILocomotiveService>(sp => new LocomotiveService(
            sp.GetRequiredService<MasterDataStore>(),
            sp.GetRequiredService<ILogger<LocomotiveService>>()));

        services.AddSingleton<IMultiplexerProvider, DefaultMultiplexerProvider>();
        services.AddSingleton<ISignalArticleCatalog>(sp =>
            new ViessmannSignalService(sp.GetRequiredService<MasterDataStore>()));
        services.AddTransient<SignalBoxPropertiesViewModel>();

        services.AddSingleton<ISettingsService>(sp => new SettingsService(
            sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<ILogger<SettingsService>>()));

        return services;
    }

    /// <summary>
    /// Registers navigation shell, dialog, health check, display, and track plan services.
    /// </summary>
    public static IServiceCollection AddMobaWinUiShellServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingletonWithInterface<NavigationService, INavigationService>();
        services.AddSingleton<IShellService, ShellService>();

        // DialogService resolves XamlRoot lazily to avoid startup deadlocks.
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IFunctionAppearancePicker, WinUiFunctionAppearancePicker>();

        services.AddSingleton<ISoundPlayer, WindowsSoundPlayer>();
        services.AddSingleton<HealthCheckService>();

        services.AddSingleton<IFrameRenderer, SkiaFrameRenderer>();
        services.AddSingletonWithInterface<UdpLineFrameSender, IFrameSender>();
        services.AddTransient<FrameLoopScheduler>();

        services.AddSingleton<TrackPlan>();
        services.AddSingleton<EditableTrackPlan>();
        services.AddSingleton<TrackPlanInteractionService>();
        services.AddSingleton<ITrackFeedbackLookup>(sp => sp.GetRequiredService<EditableTrackPlan>());
        services.AddSingleton<RailroadState>();
        services.AddSingleton<TrackPlanRailroadStateProjector>();
        services.AddSingleton<ITrackLibrary, PikoATrackLibrary>();
        services.AddSingleton<TrackLibraryRegistry>();
        services.AddSingleton<LayoutService>();
        services.AddSingleton<GraphService>();
        services.AddSingleton<SelectionService>();
        services.AddSingleton<UndoRedoService<TrackPlanEditorDocument>>();
        services.AddSingleton<TrackPlanEditorService>();
        services.AddSingleton<TrackPlanSolutionBinder>();
        services.AddSingleton<TrackPlanFeedbackHighlighter>();

        services.AddSingleton(sp => new TrackPlanViewModel(
            sp.GetRequiredService<TrackPlan>(),
            sp.GetRequiredService<TrackPlanEditorService>(),
            sp.GetRequiredService<IDialogService>(),
            sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<ISettingsService>(),
            sp.GetRequiredService<ILogger<TrackPlanViewModel>>()));

        return services;
    }

    /// <summary>
    /// Registers ViewModels and the main application window.
    /// </summary>
    public static IServiceCollection AddMobaWinUiViewModelsAndWindow(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<LayoutColumnWidthsViewModel>();
        services.AddSingleton<LocomotiveManagementViewModel>();
        services.AddTransient<RollingStockMaintenanceViewModel>();
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
            featureTogglePageProvider: sp.GetService<IFeatureTogglePageProvider>(),
            loggerFactory: sp.GetRequiredService<ILoggerFactory>(),
            dialogService: sp.GetService<IDialogService>(),
            speechTestAction: async message =>
            {
                var speakerEngine = sp.GetRequiredService<ISpeakerEngine>();
                await speakerEngine.AnnouncementAsync(message, voiceName: null).ConfigureAwait(false);
            },
            locomotiveWhistleAutomation: sp.GetService<ILocomotiveWhistleAutomationService>(),
            projectDiagnosticsService: sp.GetRequiredService<IProjectDiagnosticsService>(),
            runtimeCommandGateway: sp.GetRequiredService<IRuntimeCommandGateway>(),
            workflowService: sp.GetRequiredService<IWorkflowService>(),
            workflowTraceStore: sp.GetRequiredService<IWorkflowTraceStore>()));

        services.AddSingleton<IJourneySelectionContext>(sp => sp.GetRequiredService<MainWindowViewModel>());
        services.AddSingleton<IProjectContext>(sp => sp.GetRequiredService<MainWindowViewModel>());
        services.AddSingleton<IRecordingContextProvider, WinUiRecordingContextProvider>();
        services.AddSingleton<JourneyMapViewModel>();
        services.AddSingleton<MonitorPageViewModel>();
        services.AddSingleton<RecorderPageViewModel>();

        services.AddSingleton(new TrainControlViewModelOptions
        {
            Host = TrainControlHost.WinUi
        });
        services.AddSingleton(sp => new TrainControlViewModel(
            sp.GetRequiredService<IMobaRuntime>(),
            sp.GetRequiredService<ISettingsService>(),
            sp.GetRequiredService<IProjectContext>(),
            sp.GetService<ILogger<TrainControlViewModel>>(),
            sp.GetService<IUiDispatcher>(),
            sp.GetRequiredService<IEventBus>(),
            sp.GetRequiredService<IRuntimeCommandGateway>(),
            functionAppearancePicker: sp.GetRequiredService<IFunctionAppearancePicker>(),
            options: sp.GetRequiredService<TrainControlViewModelOptions>()));

        services.AddSingleton(sp => new MainWindow(
            sp.GetRequiredService<MainWindowViewModel>(),
            sp.GetRequiredService<NavigationService>(),
            sp.GetRequiredService<IUiDispatcher>(),
            sp.GetRequiredService<IIoService>(),
            sp.GetRequiredService<List<PageMetadata>>(),
            sp.GetRequiredService<AppSettings>(),
            sp.GetRequiredService<RestApiStatusService>(),
            sp.GetRequiredService<RestApiProcessService>(),
            sp.GetRequiredService<ILogger<WindowActivationService>>(),
            sp.GetRequiredService<ILogger<MainWindow>>()));

        return services;
    }

    /// <summary>
    /// Registers deferred startup and speech health check services.
    /// </summary>
    public static IServiceCollection AddMobaWinUiStartupServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<PostStartupInitializationService>();
        services.AddSingleton<SpeechHealthCheck>();

        return services;
    }
}
