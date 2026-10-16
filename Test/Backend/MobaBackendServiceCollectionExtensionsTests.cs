// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moba.Backend;
using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Backend.Service.Interlocking;
using Moba.Backend.Service.Validation;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Sound;
using Moba.Speech.Base;
using Moba.Domain;
using Moba.Domain.Enum;
using Moq;

/// <summary>
/// Tests for <see cref="MobaBackendServiceCollectionExtensions.AddMobaBackendServices"/>.
/// Verifies that shared backend services register once and resolve to the expected implementations.
/// </summary>
[TestFixture]
internal sealed class MobaBackendServiceCollectionExtensionsTests
{
    [Test]
    public async Task AddMobaBackendServices_PreservesExplicitAdapterAndRoutesResolvedHandlersThroughIt()
    {
        var adapter = new Mock<IWorkflowEffectAdapter>();
        adapter.Setup(value => value.ExecuteAsync(It.IsAny<WorkflowEffectRequest>(), It.IsAny<ActionExecutionContext>(),
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(adapter.Object);
        services.AddMobaBackendServices();
        services.AddMobaBackendServices();
        using var provider = services.BuildServiceProvider();
        var z21 = new Mock<IZ21>(MockBehavior.Strict);
        var action = new WorkflowAction { Type = ActionType.Command, Command = new() { BytesBase64 = "AQID" } };
        await provider.GetRequiredService<IActionExecutor>().ExecuteAsync(action, new ActionExecutionContext { Z21 = z21.Object }).ConfigureAwait(false);
        Assert.That(provider.GetRequiredService<IWorkflowEffectAdapter>(), Is.SameAs(adapter.Object));
        adapter.Verify(value => value.ExecuteAsync(It.Is<WorkflowCommandEffectRequest>(request => request.ActionId == action.Id),
            It.IsAny<ActionExecutionContext>(), CancellationToken.None), Times.Once);
        z21.VerifyNoOtherCalls();
    }

    [Test]
    public void AddMobaBackendServices_NullCollection_Throws()
    {
        IServiceCollection? services = null;

        Assert.Throws<ArgumentNullException>(() => services!.AddMobaBackendServices());
    }

    [Test]
    public void AddMobaBackendServices_RegistersCoreRuntimeServices()
    {
        var provider = BuildServiceProvider();

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetService<IZ21>(), Is.InstanceOf<Z21>());
            Assert.That(provider.GetService<IMobaRuntime>(), Is.InstanceOf<MobaRuntimeService>());
            Assert.That(provider.GetService<IInterlockingRuntime>(), Is.InstanceOf<InterlockingRuntimeService>());
            Assert.That(provider.GetService<IWorkflowService>(), Is.InstanceOf<WorkflowService>());
            Assert.That(provider.GetService<IWorkflowEffectAdapter>(), Is.InstanceOf<ProductionWorkflowEffectAdapter>());
            Assert.That(provider.GetService<IProjectValidator>(), Is.InstanceOf<ProjectValidator>());
            Assert.That(provider.GetService<IProjectDiagnosticsService>(), Is.InstanceOf<ProjectDiagnosticsService>());
            Assert.That(provider.GetService<ILocomotiveLibraryService>(), Is.InstanceOf<LocomotiveLibraryService>());
            Assert.That(provider.GetService<ILocomotivePassportHtmlRenderer>(), Is.InstanceOf<LocomotivePassportHtmlRenderer>());
        });
    }

    [Test]
    public void AddMobaBackendServices_RegistersAllWorkflowActionHandlers()
    {
        var handlers = BuildServiceProvider().GetServices<IWorkflowActionHandler>().ToList();

        Assert.That(handlers, Has.Count.EqualTo(7));
        Assert.That(handlers.Select(h => h.GetType().Name), Is.Unique);
    }

    [Test]
    public void AddMobaBackendServices_IsIdempotentForSingletonRegistrations()
    {
        var services = CreateServiceCollection();
        services.AddMobaBackendServices();
        services.AddMobaBackendServices();

        var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<IZ21>();
        var second = provider.GetRequiredService<IZ21>();

        Assert.That(second, Is.SameAs(first));
    }

    private static ServiceProvider BuildServiceProvider()
        => CreateServiceCollection().BuildServiceProvider();

    private static ServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton(new AppSettings());
        services.AddSingleton<IEventBus>(sp => new EventBus(sp.GetRequiredService<ILogger<EventBus>>()));
        services.AddSingleton<ISpeechOutput, NullSpeechOutput>();
        services.AddSingleton<ISoundPlayer, NullSoundPlayer>();
        services.AddMobaBackendServices();
        return services;
    }
}
