#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAflow;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moba.Speech.Base;
using Moba.Test.Helpers;
using Moba.Test.Speech;
using Moba.WinUI.Extensions;
using Moba.WinUI.Service;

using Moq;

/// <summary>
/// The host work behind the main window: speech health in the status bar, the runtime observers and their shutdown.
/// </summary>
[TestFixture]
internal sealed class MainWindowLifecycleServiceTests
{
    [Test]
    public async Task HostRegistrationShowsSpeechHealthInTheStatusBarUntilTheHostServicesStop()
    {
        var provider = CreateProvider();
        await using var providerLifetime = provider.ConfigureAwait(false);
        var lifecycle = provider.GetRequiredService<MainWindowLifecycleService>();
        var health = provider.GetRequiredService<HealthCheckService>();
        var status = provider.GetRequiredService<SystemStatusViewModel>();

        lifecycle.InitializeHealthChecks();
        lifecycle.InitializeHealthChecks();
        await health.PerformHealthCheckAsync().ConfigureAwait(false);
        var whileRunning = status.SpeechHealthStatus;

        await lifecycle.StopAsync().ConfigureAwait(false);
        status.UpdateHealthStatus("Initializing...");
        await health.PerformHealthCheckAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(provider.GetRequiredService<MainWindowLifecycleService>(), Is.SameAs(lifecycle));
            Assert.That(whileRunning, Is.EqualTo("Speech: Ready"));
            Assert.That(status.SpeechHealthStatus, Is.EqualTo("Speech: Initializing..."),
                "A stopped host no longer forwards health results.");
        }
    }

    [Test]
    public async Task ConfirmedShutdownReleasesTheRuntimeObserversOnce()
    {
        var provider = CreateProvider();
        await using var providerLifetime = provider.ConfigureAwait(false);
        var observer = new Mock<IDisposable>();
        var lifecycle = CreateLifecycle(provider, observer);

        var confirmed = await lifecycle.ConfirmShutdownAsync().ConfigureAwait(false);
        var confirmedAgain = await lifecycle.ConfirmShutdownAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(confirmed, Is.True);
            Assert.That(confirmedAgain, Is.True);
            observer.Verify(value => value.Dispose(), Times.Once);
        }
    }

    [Test]
    public async Task CancelledShutdownKeepsTheRuntimeObserversSubscribed()
    {
        var io = TestFileServices.CreateMock();
        io.Setup(value => value.SaveAsAsync(It.IsAny<Solution>())).ReturnsAsync((false, null, null));
        var provider = CreateProvider(services => services.AddSingleton(io.Object));
        await using var providerLifetime = provider.ConfigureAwait(false);
        var observer = new Mock<IDisposable>();
        var lifecycle = CreateLifecycle(provider, observer);
        // An unnamed solution with unsaved changes asks for a path; the operator cancels the picker.
        await provider.GetRequiredService<MainWindowViewModel>().SaveSolutionInternalAsync().ConfigureAwait(false);

        var confirmed = await lifecycle.ConfirmShutdownAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(confirmed, Is.False);
            observer.Verify(value => value.Dispose(), Times.Never);
        }
    }

    [Test]
    public async Task AFailedShellWorkflowStillClosesAndStoppingReleasesTheRuntimeObservers()
    {
        var io = TestFileServices.CreateMock();
        io.Setup(value => value.SaveAsAsync(It.IsAny<Solution>())).ReturnsAsync((false, null, "disk full"));
        var provider = CreateProvider(services => services.AddSingleton(io.Object));
        await using var providerLifetime = provider.ConfigureAwait(false);
        var observer = new Mock<IDisposable>();
        var lifecycle = CreateLifecycle(provider, observer);
        await provider.GetRequiredService<MainWindowViewModel>().SaveSolutionInternalAsync().ConfigureAwait(false);

        var confirmed = await lifecycle.ConfirmShutdownAsync().ConfigureAwait(false);
        var releasedByTheWorkflow = observer.Invocations.Count;
        await lifecycle.StopAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(confirmed, Is.True, "A failed save must not keep the window open.");
            Assert.That(releasedByTheWorkflow, Is.Zero);
            observer.Verify(value => value.Dispose(), Times.Once);
        }
    }

    private static MainWindowLifecycleService CreateLifecycle(ServiceProvider provider, Mock<IDisposable> observer) =>
        new(
            provider.GetRequiredService<MainWindowViewModel>().PrepareForShutdownAsync,
            provider.GetRequiredService<SystemStatusViewModel>(),
            (IDisposable[])[observer.Object],
            provider.GetRequiredService<RestApiStatusService>(),
            provider.GetRequiredService<RestApiProcessService>(),
            provider.GetRequiredService<HealthCheckService>(),
            provider.GetRequiredService<IUiDispatcher>(),
            NullLogger<MainWindowLifecycleService>.Instance);

    private static ServiceProvider CreateProvider(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services
            .AddMobaWinUiConfiguration(new ConfigurationBuilder().AddInMemoryCollection([]).Build())
            .AddMobaWinUiPlatformServices()
            .AddMobaWinUiSpeechServices()
            .AddMobaWinUiBackendServices()
            .AddMobaWinUiIoAndNetworkServices()
            .AddMobaWinUiDomainServices()
            .AddMobaWinUiShellServices()
            .AddMobaWinUiViewModelsAndWindow()
            .AddMobaWinUiStartupServices();

        // Last registration wins: no WinUI dispatcher, speech engine or audio device in a headless run.
        services.AddSingleton<IUiDispatcher>(ImmediateUiDispatcher.Instance);
        services.AddSingleton(TestSpeechCache.Empty());
        services.RemoveAll<ISpeechProvider>();
        services.AddSingleton<ISpeechProvider>(new SpeechOutputTests.FakeProvider("system-speech"));
        services.AddSingleton<IWavePlayer>(new SpeechOutputTests.FakePlayer());
        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }
}
#endif
