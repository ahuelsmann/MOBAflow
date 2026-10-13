#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAflow;

using Moba.Backend.Interface;
using Moba.Backend.Service;
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