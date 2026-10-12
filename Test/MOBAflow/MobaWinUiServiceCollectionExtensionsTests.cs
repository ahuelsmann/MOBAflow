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
using Moba.Sound;
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
    public async Task SettingsSpeechTest_KeepsFirstResolvedEngine_WhileAnnouncementsUseCurrentSelection()
    {
        var systemSpeaker = new Mock<ISpeakerEngine>();
        var piperSpeaker = new Mock<ISpeakerEngine>();
        systemSpeaker.Setup(engine => engine.AnnouncementAsync(It.IsAny<string>(), null))
            .Returns(Task.CompletedTask);
        piperSpeaker.Setup(engine => engine.AnnouncementAsync(
                It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var systemRegistration = new Mock<ISpeakerEngineRegistration>();
        var piperRegistration = new Mock<ISpeakerEngineRegistration>();
        systemRegistration.Setup(value => value.CanCreate(SpeechSpeakerEngineSelection.SystemSpeech)).Returns(true);
        piperRegistration.Setup(value => value.CanCreate(SpeechSpeakerEngineSelection.PiperTts)).Returns(true);
        systemRegistration.Setup(value => value.Create(It.IsAny<SpeechOptions>())).Returns(systemSpeaker.Object);
        piperRegistration.Setup(value => value.Create(It.IsAny<SpeechOptions>())).Returns(piperSpeaker.Object);
        var provider = CreateValidatorServiceProvider(services =>
        {
            services.RemoveAll<ISpeakerEngineRegistration>();
            services.AddSingleton(systemRegistration.Object);
            services.AddSingleton(piperRegistration.Object);
        });
        await using (provider.ConfigureAwait(false))
        {
            var settings = provider.GetRequiredService<AppSettings>();
            settings.Speech.SpeakerEngineName = SpeechSpeakerEngineSelection.SystemSpeech;
            settings.Speech.TestMessage = "Settings test";
            settings.Speech.VoiceName = "Configured voice";
            var viewModel = provider.GetRequiredService<SettingsPageViewModel>();

            await viewModel.TestSpeechCommand.ExecuteAsync(null).ConfigureAwait(false);
            settings.Speech.SpeakerEngineName = SpeechSpeakerEngineSelection.PiperTts;
            await viewModel.TestSpeechCommand.ExecuteAsync(null).ConfigureAwait(false);
            await provider.GetRequiredService<AnnouncementService>()
                .GenerateAndSpeakAnnouncementAsync("Workflow announcement", new Station(), 1).ConfigureAwait(false);

            systemSpeaker.Verify(engine => engine.AnnouncementAsync("Settings test", null), Times.Exactly(2));
            piperSpeaker.Verify(engine => engine.AnnouncementAsync(
                "Workflow announcement", null, CancellationToken.None), Times.Once);
            systemRegistration.Verify(value => value.Create(It.IsAny<SpeechOptions>()), Times.Once);
            piperRegistration.Verify(value => value.Create(It.IsAny<SpeechOptions>()), Times.Once);
            Assert.That(provider.GetRequiredService<ISpeakerEngine>(), Is.SameAs(systemSpeaker.Object));
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