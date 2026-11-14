#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAflow;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Backend.Service.Recording;
using Moba.Backend.Service.ProjectRuntimes;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Display.Transport;
using Moba.Domain;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.TrainControl;
using Moba.SharedUI.ViewModel;
using Moba.WinUI.Extensions;
using Moba.WinUI.Service;
using Moba.Speech.Base;
using Moba.Speech.Piper;
using Moba.Sound;
using Moba.Test.Helpers;
using Moba.Test.Speech;
using Moq;

/// <summary>
/// Tests for MOBAflow DI extension methods and startup validation.
/// </summary>
[TestFixture]
internal sealed class MobaWinUiServiceCollectionExtensionsTests
{
    private static readonly string[] SpeechProviderIds = ["piper", "system-speech"];

    [Test]
    public async Task UsageReaderAndProjection_ResolveTheExistingDesktopOwnerAndSharedPageInstance()
    {
        var provider = CreateValidatorServiceProvider();
        await using var lifetime = provider.ConfigureAwait(false);
        var owner = provider.GetRequiredService<ProjectRuntimeHost>();
        var usage = provider.GetRequiredService<LocomotiveTrainUsageViewModel>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(provider.GetRequiredService<ILocomotiveTrainRuntimeUsageReader>(), Is.SameAs(owner));
            Assert.That(provider.GetRequiredService<IProjectRuntimeHost>(), Is.SameAs(owner));
            Assert.That(provider.GetRequiredService<IProjectRuntimeSnapshots>(), Is.SameAs(owner));
            Assert.That(provider.GetRequiredService<LocomotivesPageViewModel>().Usage, Is.SameAs(usage));
            Assert.That(provider.GetRequiredService<MainWindowViewModel>().LocomotivesPage.Usage, Is.SameAs(usage));
        }
    }

    [Test]
    public void AddMobaWinUiPlatformServices_RegistersUiDispatcherAndEventBus()
    {
        var provider = CreatePlatformServiceProvider();

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetRequiredService<IUiDispatcher>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<UiThreadEventBusDecorator>(), Is.Not.Null);
            Assert.That(
                provider.GetRequiredService<List<PageMetadata>>(),
                Has.One.Matches<PageMetadata>(page =>
                    page is not null && page.Tag == "recorder"
                    && page.Title == "Recorder"
                    && page.Category == global::Moba.Common.Navigation.NavigationCategory.Monitoring
                    && page.Order == 20));
        });
    }

    [Test]
    public void AddMobaWinUiBackendServices_ResolvesIMobaRuntime()
    {
        var provider = CreateBackendServiceProvider();
        var replayService = provider.GetRequiredService<IRecordingReplayService>();

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetRequiredService<IMobaRuntime>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<IEventBus>(), Is.InstanceOf<RecordingEventBusDecorator>(),
                "The application bus records published events, so it needs the backend recording services.");
            Assert.That(replayService, Is.Not.Null);
            Assert.That(provider.GetRequiredService<IRecordingReplayStatusSource>(), Is.SameAs(replayService));
        });
    }

    [Test]
    public void AddMobaWinUiIoAndNetworkServices_ResolvesInterfaceAliases()
    {
        var provider = CreateNetworkServiceProvider();

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetRequiredService<ISolutionIoService>().CanPersist, Is.True);
            Assert.That(provider.GetRequiredService<IFilePickerService>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<IPhotoStorageService>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<PhotoAssignmentService>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<IRecordingFileService>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<IRuntimeCommandGateway>(), Is.InstanceOf<RecordingRuntimeCommandGateway>());
            Assert.That(provider.GetRequiredService<LocalRuntimeCommandGateway>(), Is.Not.Null);
        });
    }

    [Test]
    public void WinUiDiContainerValidator_Passes_ForCoreServices()
    {
        var provider = CreateValidatorServiceProvider();

        Assert.DoesNotThrow(() => WinUiDiContainerValidator.ValidateCoreServices(provider));
        Assert.That(provider.GetRequiredService<RecorderPageViewModel>(), Is.Not.Null);
    }

    [Test]
    public async Task EveryRegistrationOfTheHostResolvesExceptTheTypesThatNeedTheXamlWindow()
    {
        var services = CreateValidatorServices(services => services.AddMobaWinUiStartupServices());
        // The real dispatcher needs the WinUI thread; the appended test double must be the only registration.
        services.RemoveAll<IUiDispatcher>();
        services.AddSingleton(Mock.Of<IUiDispatcher>());
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var providerLifetime = provider.ConfigureAwait(false);

        var failures = ServiceRegistrations.ResolveAll(services, provider, NeedsTheXamlWindow, out var resolved);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(failures, Is.Empty);
            Assert.That(resolved, Is.GreaterThan(150), "The host registers its backend, services and ViewModels.");
        }
    }

    /// <summary>
    /// XAML creates the window and its pages on the UI thread of a running app, so a headless test cannot
    /// construct them; the startup service takes the window and the matrix ViewModel creates a WinUI brush.
    /// The FastDebug build compiles their constructors.
    /// </summary>
    private static bool NeedsTheXamlWindow(Type serviceType) =>
        typeof(Microsoft.UI.Xaml.Window).IsAssignableFrom(serviceType)
        || typeof(Microsoft.UI.Xaml.UIElement).IsAssignableFrom(serviceType)
        || serviceType == typeof(PostStartupInitializationService)
        || serviceType == typeof(Moba.WinUI.ViewModel.MatrixPageViewModel);
    [Test]
    public async Task ShellTrainControlAndLocomotiveManagementResolveTheirRequiredDesktopServices()
    {
        var provider = CreateValidatorServiceProvider();
        await using var providerLifetime = provider.ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(provider.GetRequiredService<IRuntimeRoutingState>(), Is.InstanceOf<NullRuntimeRoutingState>());
            Assert.That(provider.GetRequiredService<TrainControlViewModel>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<MainWindowViewModel>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<LocomotiveManagementViewModel>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<SettingsPageViewModel>().FeatureToggleItems, Is.Not.Empty,
                "The desktop page provider lists the toggleable pages.");
        }
    }

    [Test]
    public async Task HostSpeechBackendNetworkAndDisplayServicesResolveTheirRequiredDependencies()
    {
        var provider = CreateValidatorServiceProvider();
        await using var providerLifetime = provider.ConfigureAwait(false);
        var context = provider.GetRequiredService<ActionExecutionContext>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(provider.GetRequiredService<IPiperProcessRunner>(), Is.InstanceOf<PiperProcessRunner>());
            Assert.That(provider.GetServices<ISpeechProvider>().Select(speechProvider => speechProvider.Id),
                Is.EqualTo(SpeechProviderIds));
            Assert.That(provider.GetRequiredService<IWavePlayer>(), Is.InstanceOf<CancellableWavePlayer>());
            Assert.That(context.SpeechOutput, Is.SameAs(provider.GetRequiredService<ISpeechOutput>()));
            Assert.That(context.SoundPlayer, Is.SameAs(provider.GetRequiredService<ISoundPlayer>()));
            Assert.That(provider.GetRequiredService<IZ21>().TrafficMonitor,
                Is.SameAs(provider.GetRequiredService<Z21Monitor>()));
            Assert.That(provider.GetRequiredService<InPortCounterService>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<ILocomotiveWhistleAutomationService>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<IRuntimeHubHostClient>(), Is.InstanceOf<RuntimeHubHostClient>());
            Assert.That(provider.GetRequiredService<IPhotoHubClient>(), Is.InstanceOf<PhotoHubClient>());
            Assert.That(provider.GetRequiredService<RestApiProcessService>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<RestApiStatusService>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<SystemStatusViewModel>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<IFrameSender>(), Is.InstanceOf<UdpDisplayFrameSender>());
            Assert.That(provider.GetRequiredService<IDisplayDeviceClient>(), Is.InstanceOf<UdpDisplayDeviceClient>());
        }
    }

    [Test]
    public void AddMobaWinUiViewModelsAndWindow_WiresWorkflowDryRunServices()
    {
        var provider = CreateValidatorServiceProvider();
        var workflows = provider.GetRequiredService<WorkflowsPageViewModel>();
        provider.GetRequiredService<ISolutionSession>().SelectedProject = new ProjectViewModel(new Project
        {
            Workflows = [new Workflow { Name = "Preview" }]
        }, TestViewModelServices.Default);

        Assert.That(workflows.WorkflowLibrary.CanDryRun, Is.True);
    }

    [Test]
    public async Task SettingsSpeechTest_AndAnnouncementsUseCurrentProviderAndOptions()
    {
        var system = new SpeechOutputTests.FakeProvider("system-speech");
        var piper = new SpeechOutputTests.FakeProvider("piper");
        var player = new SpeechOutputTests.FakePlayer();
        var savedSettings = new List<string>();
        var provider = CreateValidatorServiceProvider(services =>
        {
            services.RemoveAll<ISpeechProvider>();
            services.AddSingleton<ISpeechProvider>(system);
            services.AddSingleton<ISpeechProvider>(piper);
            services.AddSingleton<IWavePlayer>(player);
            services.AddSingleton<ISettingsService>(sp =>
            {
                var settingsService = new Mock<ISettingsService>();
                settingsService.Setup(value => value.GetSettings()).Returns(sp.GetRequiredService<AppSettings>());
                settingsService.Setup(value => value.SaveSettingsAsync(It.IsAny<AppSettings>()))
                    .Callback<AppSettings>(value => savedSettings.Add(System.Text.Json.JsonSerializer.Serialize(value)))
                    .Returns(Task.CompletedTask);
                return settingsService.Object;
            });
        });
        await using (provider.ConfigureAwait(false))
        {
            var settings = provider.GetRequiredService<AppSettings>();
            settings.Speech.TestMessage = "Settings test";
            var viewModel = provider.GetRequiredService<SettingsPageViewModel>();
            await viewModel.TestSpeechCommand.ExecuteAsync(null).ConfigureAwait(false);
            viewModel.SystemSpeechDefaultVoiceName = "Windows voice";
            viewModel.SelectedSpeechProviderId = "piper";
            viewModel.PiperModelPath = "selected.onnx";
            viewModel.SpeechRate = 2;
            viewModel.SpeechVolume = 65;
            await viewModel.TestSpeechCommand.ExecuteAsync(null).ConfigureAwait(false);
            await provider.GetRequiredService<AnnouncementService>()
                .GenerateAndSpeakAnnouncementAsync("Workflow announcement", new Station(), 1).ConfigureAwait(false);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(system.Requests, Has.One.Matches<SpeechRequest>(request => request is not null && request.Text == "Settings test"));
                Assert.That(piper.Requests, Has.Count.EqualTo(2));
                Assert.That(piper.Requests[0].Text, Is.EqualTo("Settings test"));
                Assert.That(piper.Requests[1].Text, Is.EqualTo("Workflow announcement"));
                Assert.That(piper.Requests.All(request => request.Rate == 2 && request.Volume == 65), Is.True);
                Assert.That(player.PlayCount, Is.EqualTo(3));
                Assert.That(savedSettings, Has.Some.Contains("selected.onnx"));
                Assert.That(savedSettings, Has.Some.Contains("Windows voice"));
                Assert.That(settings.Speech.ProviderId, Is.EqualTo("piper"));
            }
        }
    }

    [Test]
    public async Task AddMobaWinUiViewModelsAndWindowSharesSettingsStateWithShell()
    {
        var provider = CreateValidatorServiceProvider();
        await using var providerLifetime = provider.ConfigureAwait(false);
        var settings = provider.GetRequiredService<SettingsPageViewModel>();
        var shell = provider.GetRequiredService<MainWindowViewModel>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(shell.SettingsPage, Is.SameAs(settings));
            Assert.That(provider.GetRequiredService<SettingsPageViewModel>(), Is.SameAs(settings));
            Assert.That(settings.Settings, Is.SameAs(provider.GetRequiredService<ISettingsService>().GetSettings()));
        }
    }

    [Test]
    public async Task AddMobaWinUiViewModelsAndWindowSharesLocomotiveStateWithShellAndManagement()
    {
        var provider = CreateValidatorServiceProvider();
        await using var providerLifetime = provider.ConfigureAwait(false);
        var locomotives = provider.GetRequiredService<LocomotivesPageViewModel>();
        var shell = provider.GetRequiredService<MainWindowViewModel>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(shell.LocomotivesPage, Is.SameAs(locomotives));
            Assert.That(provider.GetRequiredService<ISettingsService>().GetSettings(), Is.SameAs(shell.SettingsPage.Settings));
            Assert.That(provider.GetRequiredService<LocomotivesPageViewModel>(), Is.SameAs(locomotives));
            Assert.That(locomotives.Management, Is.SameAs(provider.GetRequiredService<LocomotiveManagementViewModel>()));
            Assert.That(locomotives.LayoutColumnWidths, Is.SameAs(provider.GetRequiredService<LayoutPersistenceService>().ColumnWidths));
        }
    }

    [Test]
    public async Task AddMobaWinUiViewModelsAndWindowSharesWagonStateWithInventoryAndTrainLibraries()
    {
        var provider = CreateValidatorServiceProvider();
        await using var providerLifetime = provider.ConfigureAwait(false);
        var wagons = provider.GetRequiredService<WagonsPageViewModel>();
        var shell = provider.GetRequiredService<MainWindowViewModel>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(shell.WagonsPage, Is.SameAs(wagons));
            Assert.That(provider.GetRequiredService<WagonsPageViewModel>(), Is.SameAs(wagons));
            Assert.That(wagons.LayoutColumnWidths, Is.SameAs(provider.GetRequiredService<LayoutPersistenceService>().ColumnWidths));
            Assert.That(provider.GetRequiredService<ISettingsService>().GetSettings(), Is.SameAs(shell.SettingsPage.Settings));
        }
        shell.SelectedProject = new ProjectViewModel(new Project(), TestViewModelServices.Default);
        wagons.AddPassengerWagonCommand.Execute(null);
        wagons.AddGoodsWagonCommand.Execute(null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(shell.WagonsPage.FilteredPassengerWagonLibrary.Single(), Is.SameAs(wagons.SelectedPassengerWagon));
            Assert.That(shell.WagonsPage.FilteredGoodsWagonLibrary.Single(), Is.SameAs(wagons.SelectedGoodsWagon));
        }
    }
    [Test]
    public async Task TrainCompositionSharesSessionInventoryAndLayoutThroughHostRegistration()
    {
        var provider = CreateValidatorServiceProvider();
        await using var providerLifetime = provider.ConfigureAwait(false);
        var trains = provider.GetRequiredService<TrainsPageViewModel>();
        var shell = provider.GetRequiredService<MainWindowViewModel>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(shell.TrainsPage, Is.SameAs(trains));
            Assert.That(trains.LocomotivesPage, Is.SameAs(provider.GetRequiredService<LocomotivesPageViewModel>()));
            Assert.That(trains.WagonsPage, Is.SameAs(provider.GetRequiredService<WagonsPageViewModel>()));
        }
        shell.SelectedProject = new ProjectViewModel(new Project(), TestViewModelServices.Default);
        trains.AddTrainCommand.Execute(null);
        Assert.That(trains.SelectedTrain, Is.SameAs(provider.GetRequiredService<ISolutionSession>().SelectedProject!.Trains.Single()));
    }

    [Test]
    public async Task StationEditorSharesHostSessionLayoutAndWorkflowLibraryWithTheShell()
    {
        var provider = CreateValidatorServiceProvider();
        await using var providerLifetime = provider.ConfigureAwait(false);
        using var assertions = Assert.EnterMultipleScope();
        var stations = provider.GetRequiredService<StationsPageViewModel>();
        var shell = provider.GetRequiredService<MainWindowViewModel>();
        var library = provider.GetRequiredService<WorkflowLibraryViewModel>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(shell.StationsPage, Is.SameAs(stations));
            Assert.That(stations.WorkflowLibrary, Is.SameAs(library));
        }
        shell.SelectedProject = new ProjectViewModel(new Project(), TestViewModelServices.Default);
        stations.AddProjectStationCommand.Execute(null);
        Assert.That(stations.SelectedProjectStation,
            Is.SameAs(provider.GetRequiredService<ISolutionSession>().SelectedProject!.Stations.Single()));
    }

    [Test]
    public async Task WorkflowEditorSharesTheHostLibraryWithStationsAndEventManager()
    {
        var provider = CreateValidatorServiceProvider();
        await using var providerLifetime = provider.ConfigureAwait(false);
        using var assertions = Assert.EnterMultipleScope();
        var workflows = provider.GetRequiredService<WorkflowsPageViewModel>();
        var library = provider.GetRequiredService<WorkflowLibraryViewModel>();
        var session = provider.GetRequiredService<ISolutionSession>();
        var shell = provider.GetRequiredService<MainWindowViewModel>();
        var editor = provider.GetRequiredService<EventManagerViewModel>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(provider.GetRequiredService<WorkflowsPageViewModel>(), Is.SameAs(workflows));
            Assert.That(workflows.WorkflowLibrary, Is.SameAs(library));
            Assert.That(provider.GetRequiredService<StationsPageViewModel>().WorkflowLibrary, Is.SameAs(library));
            Assert.That(editor.WorkflowLibrary, Is.SameAs(library));
            Assert.That(provider.GetRequiredService<ISettingsService>().GetSettings().Layout.WorkflowsPage.IsActionsExpanded,
                Is.EqualTo(workflows.IsWorkflowActionsExpanded));
        }
        session.SelectedProject = new ProjectViewModel(new Project
        {
            Workflows = [new Workflow { Name = "Departure" }, new Workflow { Name = "Arrival" }]
        }, TestViewModelServices.Default);
        library.SearchText = "Arr";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(workflows.WorkflowLibrary.SelectedWorkflow, Is.SameAs(session.SelectedProject!.Workflows[0]));
            Assert.That(workflows.WorkflowLibrary.FilteredWorkflows.Single().Name, Is.EqualTo("Arrival"));
            Assert.That(editor.WorkflowLibrary.FilteredWorkflows.Single().Name, Is.EqualTo("Arrival"));
        }
        Assert.That(await shell.PrepareForShutdownAsync().ConfigureAwait(false), Is.True);
        session.SelectedProject = new ProjectViewModel(new Project { Workflows = [new Workflow { Name = "Late" }] }, TestViewModelServices.Default);
        Assert.That(library.SelectedWorkflow, Is.Null, "Shutdown disposed the shared library.");
    }

    [Test]
    public async Task JourneyEditorSharesHostSelectionLayoutAndStatusWithoutShellDependency()
    {
        using var assertions = Assert.EnterMultipleScope();
        var gateway = new Mock<IRuntimeCommandGateway>();
        gateway.Setup(value => value.ResetJourneyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var io = TestFileServices.CreateMock();
        io.Setup(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()))
            .ReturnsAsync((Solution _, string path) => (true, (string?)path, (string?)null));
        var provider = CreateValidatorServiceProvider(services =>
        {
            services.AddSingleton(gateway.Object);
            services.AddSingleton(io.Object);
            // Saves take their snapshot through the dispatcher, so the shutdown save needs one that runs its work.
            services.AddSingleton<IUiDispatcher>(new ImmediateUiDispatcher());
        });
        await using var providerLifetime = provider.ConfigureAwait(false);
        var journeys = provider.GetRequiredService<JourneysPageViewModel>();
        var shell = provider.GetRequiredService<MainWindowViewModel>();
        var session = provider.GetRequiredService<ISolutionSession>();
        session.CurrentSolutionPath = "host-journeys-test.json";
        var editor = provider.GetRequiredService<EventManagerViewModel>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(shell.JourneysPage, Is.SameAs(journeys));
            Assert.That(provider.GetRequiredService<JourneysPageViewModel>(), Is.SameAs(journeys));
            Assert.That(editor.JourneysPage, Is.SameAs(journeys));
            Assert.That(editor.ResetCountersCommand, Is.SameAs(provider.GetRequiredService<InPortCounterViewModel>().ResetCountersCommand));
            Assert.That(editor.WorkflowLibrary, Is.SameAs(provider.GetRequiredService<WorkflowLibraryViewModel>()));
        }
        journeys.AddJourneyCommand.Execute(null);
        var selected = journeys.SelectedJourney!;
        Assert.That(session.SelectedJourney, Is.SameAs(selected));
        Assert.That(editor.SelectedJourney, Is.SameAs(selected));
        await journeys.ResetJourneyCommand.ExecuteAsync(null).ConfigureAwait(false);
        gateway.Verify(value => value.ResetJourneyAsync(selected.Id, It.IsAny<CancellationToken>()), Times.Once);
        var notifications = new List<string?>();
        journeys.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        Assert.That(await shell.PrepareForShutdownAsync().ConfigureAwait(false), Is.True);
        session.SelectedJourney = null;
        Assert.That(notifications, Is.Empty);
    }

    [Test]
    public async Task LayoutPersistenceAndTheSolutionAndSignalBoxPagesAreHostSingletonsWithoutAShellDependency()
    {
        var provider = CreateValidatorServiceProvider();
        await using var providerLifetime = provider.ConfigureAwait(false);
        var layout = provider.GetRequiredService<LayoutPersistenceService>();
        var settings = provider.GetRequiredService<ISettingsService>().GetSettings();
        var session = provider.GetRequiredService<ISolutionSession>();
        var solutionPage = provider.GetRequiredService<SolutionPageViewModel>();
        var signalBoxPage = provider.GetRequiredService<SignalBoxPageViewModel>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(provider.GetRequiredService<LayoutPersistenceService>(), Is.SameAs(layout));
            Assert.That(layout.Layout, Is.SameAs(settings.Layout));
            Assert.That(layout.ColumnWidths, Is.SameAs(provider.GetRequiredService<LayoutColumnWidthsViewModel>()));
            Assert.That(provider.GetRequiredService<SolutionPageViewModel>(), Is.SameAs(solutionPage));
            Assert.That(provider.GetRequiredService<SignalBoxPageViewModel>(), Is.SameAs(signalBoxPage));
            Assert.That(solutionPage.SolutionViewModel, Is.SameAs(session.SolutionViewModel));
            Assert.That(signalBoxPage.SelectedProject, Is.SameAs(session.SelectedProject));
        }

        // Page states and the Settings reset write the same saved layout.
        solutionPage.IsProjectListExpanded = !solutionPage.IsProjectListExpanded;
        signalBoxPage.IsSignalBoxToolboxExpanded = !signalBoxPage.IsSignalBoxToolboxExpanded;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings.Layout.SolutionPage.IsProjectListExpanded, Is.EqualTo(solutionPage.IsProjectListExpanded));
            Assert.That(settings.Layout.SignalBoxPage.IsToolboxExpanded, Is.EqualTo(signalBoxPage.IsSignalBoxToolboxExpanded));
        }
    }

    [Test]
    public async Task CounterAndMonitorStateAreHostSingletonsWithoutAShellDependency()
    {
        var provider = CreateValidatorServiceProvider();
        await using var providerLifetime = provider.ConfigureAwait(false);
        var counters = provider.GetRequiredService<InPortCounterViewModel>();
        var monitor = provider.GetRequiredService<MonitorPageViewModel>();
        var settings = provider.GetRequiredService<SettingsPageViewModel>();
        var session = provider.GetRequiredService<ISolutionSession>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(provider.GetRequiredService<InPortCounterViewModel>(), Is.SameAs(counters));
            Assert.That(provider.GetRequiredService<MonitorPageViewModel>(), Is.SameAs(monitor));
            Assert.That(provider.GetRequiredService<EventManagerViewModel>().ResetCountersCommand, Is.SameAs(counters.ResetCountersCommand));
            Assert.That(counters.GlobalTargetLapCount, Is.EqualTo(settings.Settings.Counter.TargetLapCount));
            Assert.That(monitor.IsMonitorTrafficExpanded, Is.EqualTo(settings.Settings.Layout.MonitorPage.IsTrafficExpanded));
        }

        // The counters follow the shared settings state and the shared session without the shell.
        settings.CountOfFeedbackPoints = 2;
        Assert.That(counters.Statistics, Has.Count.EqualTo(2));
        session.SelectedProject = new ProjectViewModel(new Project(), TestViewModelServices.Default);
        Assert.That(monitor.ConnectionStatus, Is.EqualTo("❌ Not connected"));
        monitor.Dispose();
        counters.Dispose();
    }

    [Test]
    public async Task DiagnosticsAndSystemStatusAreHostSingletonsSharedWithTheShell()
    {
        var provider = CreateValidatorServiceProvider();
        await using var providerLifetime = provider.ConfigureAwait(false);
        var diagnostics = provider.GetRequiredService<ProjectDiagnosticsViewModel>();
        var status = provider.GetRequiredService<SystemStatusViewModel>();
        var shell = provider.GetRequiredService<MainWindowViewModel>();
        var session = provider.GetRequiredService<ISolutionSession>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(provider.GetRequiredService<ProjectDiagnosticsViewModel>(), Is.SameAs(diagnostics));
            Assert.That(provider.GetRequiredService<SystemStatusViewModel>(), Is.SameAs(status));
            Assert.That(shell.Diagnostics, Is.SameAs(diagnostics));
            Assert.That(shell.SystemStatus, Is.SameAs(status));
            Assert.That(status.SolutionLoadedStatusText, Is.EqualTo("In-memory only, no solution file loaded"));
        }

        // The status follows the shared session and the diagnostics the shared usage query without the shell.
        session.CurrentSolutionPath = "host-status-test.json";
        session.SelectedProject = new ProjectViewModel(new Project
        {
            Name = "Conflicting",
            Locomotives = [new Locomotive { Name = "One", DigitalAddress = 9 }, new Locomotive { Name = "Two", DigitalAddress = 9 }]
        }, TestViewModelServices.Default);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(status.SolutionLoadedStatusText, Is.EqualTo("Loaded from host-status-test.json, project Conflicting"));
            Assert.That(diagnostics.DiagnosticWarningCount, Is.EqualTo(1), "The duplicate address of the selected project is reported.");
            Assert.That(diagnostics.LocomotiveUsageCoverageSummary,
                Is.EqualTo(provider.GetRequiredService<LocomotiveTrainUsageViewModel>().CoverageSummary));
        }
    }

    private static ServiceProvider CreatePlatformServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddMobaWinUiConfiguration(CreateTestConfiguration());
        services.AddMobaWinUiPlatformServices();
        return services.BuildServiceProvider();
    }

    private static ServiceProvider CreateBackendServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services
            .AddMobaWinUiConfiguration(CreateTestConfiguration())
            .AddMobaWinUiPlatformServices()
            .AddMobaWinUiSpeechServices()
            .AddMobaWinUiBackendServices();
        return services.BuildServiceProvider();
    }

    private static ServiceProvider CreateNetworkServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services
            .AddMobaWinUiConfiguration(CreateTestConfiguration())
            .AddMobaWinUiPlatformServices()
            .AddMobaWinUiSpeechServices()
            .AddMobaWinUiBackendServices()
            .AddMobaWinUiIoAndNetworkServices()
            .AddMobaWinUiDomainServices();
        return services.BuildServiceProvider();
    }

    [Test]
    public async Task SpeechServicesShareOneOutputAndContainerDisposalCancelsItsSynthesis()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var speaker = new SpeechOutputTests.FakeProvider("system-speech")
        {
            Synthesize = async (_, token) =>
            {
                started.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token).ConfigureAwait(false);
                return SpeechResult.Success(TestWaveAudio.Create([1]));
            }
        };
        var player = new SpeechOutputTests.FakePlayer();
        var provider = CreateValidatorServiceProvider(services =>
        {
            services.RemoveAll<ISpeechProvider>();
            services.AddSingleton<ISpeechProvider>(speaker);
            services.AddSingleton<IWavePlayer>(player);
        });
        await using var providerLifetime = provider.ConfigureAwait(false);
        var output = provider.GetRequiredService<ISpeechOutput>();
        Assert.That(output, Is.SameAs(provider.GetRequiredService<SpeechOutput>()));
        var speech = output.SpeakAsync("Shutdown through production DI");
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        await provider.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        await Assert.CatchAsync<OperationCanceledException>(() => speech).ConfigureAwait(false);
        Assert.That(player.PlayCount, Is.Zero);
    }
    private static ServiceProvider CreateValidatorServiceProvider(Action<IServiceCollection>? configure = null) =>
        CreateValidatorServices(configure).BuildServiceProvider();

    private static ServiceCollection CreateValidatorServices(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services
            .AddMobaWinUiConfiguration(CreateTestConfiguration())
            .AddMobaWinUiPlatformServices()
            .AddMobaWinUiSpeechServices()
            .AddMobaWinUiBackendServices()
            .AddMobaWinUiIoAndNetworkServices()
            .AddMobaWinUiDomainServices()
            .AddMobaWinUiShellServices()
            .AddMobaWinUiViewModelsAndWindow();

        // The container test validates registrations, not WinUI COM activation.
        // Last-registration-wins keeps production DI intact while preventing a
        // headless runner from touching DispatcherQueue during ViewModel creation.
        services.AddSingleton(Moq.Mock.Of<IUiDispatcher>());
        services.AddSingleton(TestSpeechCache.Empty());
        configure?.Invoke(services);

        return services;
    }

    private static IConfiguration CreateTestConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
    }

    private sealed class ImmediateUiDispatcher : IUiDispatcher
    {
        public void InvokeOnUi(Action action) => action();

        public Task InvokeOnUiAsync(Func<Task> asyncAction) => asyncAction();

        public Task<T> InvokeOnUiAsync<T>(Func<Task<T>> asyncFunc) => asyncFunc();

        public void InvokeOnUiHighPriority(Action action) => action();

        public void InvokeOnUiLowPriority(Action action) => action();

        public Task InvokeOnUiAsync(Func<Task> asyncAction, UiPriority priority)
        {
            _ = priority;
            return asyncAction();
        }
    }
}
#endif
