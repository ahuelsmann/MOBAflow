#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAflow;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Backend.Service.ProjectRuntimes;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Domain;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;
using Moba.WinUI.Extensions;
using Moba.WinUI.Service;
using Moba.Speech.Base;
using Moba.Test.Speech;
using Moq;

/// <summary>
/// Tests for MOBAflow DI extension methods and startup validation.
/// </summary>
[TestFixture]
internal sealed class MobaWinUiServiceCollectionExtensionsTests
{
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
            Assert.That(provider.GetRequiredService<IEventBus>(), Is.Not.Null);
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
            var ioService = provider.GetRequiredService<IIoService>();
            Assert.That(ioService, Is.Not.Null);
            Assert.That(provider.GetRequiredService<ISolutionIoService>(), Is.SameAs(ioService));
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
    public void AddMobaWinUiViewModelsAndWindow_WiresWorkflowDryRunServices()
    {
        var provider = CreateValidatorServiceProvider();
        var viewModel = provider.GetRequiredService<MainWindowViewModel>();
        viewModel.SelectedProject = new ProjectViewModel(new Project
        {
            Workflows = [new Workflow { Name = "Preview" }]
        });

        Assert.That(viewModel.WorkflowLibrary.CanDryRun, Is.True);
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
            Assert.That(locomotives.LayoutColumnWidths, Is.SameAs(shell.LayoutColumnWidths));
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
            Assert.That(wagons.LayoutColumnWidths, Is.SameAs(shell.LayoutColumnWidths));
            Assert.That(provider.GetRequiredService<ISettingsService>().GetSettings(), Is.SameAs(shell.SettingsPage.Settings));
        }
        shell.SelectedProject = new ProjectViewModel(new Project());
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
            Assert.That(trains.LayoutColumnWidths, Is.SameAs(shell.LayoutColumnWidths));
        }
        shell.SelectedProject = new ProjectViewModel(new Project());
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
            Assert.That(shell.WorkflowLibrary, Is.SameAs(library));
            Assert.That(stations.LayoutColumnWidths, Is.SameAs(shell.LayoutColumnWidths));
        }
        shell.SelectedProject = new ProjectViewModel(new Project());
        stations.AddProjectStationCommand.Execute(null);
        Assert.That(stations.SelectedProjectStation,
            Is.SameAs(provider.GetRequiredService<ISolutionSession>().SelectedProject!.Stations.Single()));
        var searchNotifications = 0;
        shell.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.WorkflowSearchText)) searchNotifications++;
        };
        library.SearchText = "Station filter";
        Assert.That(shell.WorkflowSearchText, Is.EqualTo("Station filter"));
        shell.WorkflowSearchText = "Shell filter";
        Assert.That(library.SearchText, Is.EqualTo("Shell filter"));
        Assert.That(searchNotifications, Is.EqualTo(2));
    }

    [Test]
    public async Task JourneyEditorSharesHostSelectionLayoutAndStatusWithoutShellDependency()
    {
        using var assertions = Assert.EnterMultipleScope();
        var gateway = new Mock<IRuntimeCommandGateway>();
        gateway.Setup(value => value.ResetJourneyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var io = new Mock<IIoService>();
        io.Setup(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()))
            .ReturnsAsync((Solution _, string path) => (true, (string?)path, (string?)null));
        var provider = CreateValidatorServiceProvider(services =>
        {
            services.AddSingleton(gateway.Object);
            services.AddSingleton(io.Object);
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
            Assert.That(editor.ResetCountersCommand, Is.SameAs(shell.ResetCountersCommand));
            Assert.That(editor.WorkflowLibrary, Is.SameAs(provider.GetRequiredService<WorkflowLibraryViewModel>()));
            Assert.That(journeys.LayoutColumnWidths, Is.SameAs(shell.LayoutColumnWidths));
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
    private static ServiceProvider CreateValidatorServiceProvider(Action<IServiceCollection>? configure = null)
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

        return services.BuildServiceProvider();
    }

    private static IConfiguration CreateTestConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
    }
}
#endif
