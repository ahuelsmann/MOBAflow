// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Backend.Manager;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Domain;
using Moba.Test.Helpers;

using Moq;

/// <summary>
/// The runtime, its journey managers and their factories declare every collaborator; a missing one fails at
/// construction instead of being replaced by a default.
/// </summary>
[TestFixture]
internal sealed class RuntimeConstructionTests
{
    private static readonly IZ21 Z21 = new Mock<IZ21>().Object;
    private static readonly IWorkflowService Workflows = new Mock<IWorkflowService>().Object;
    private static readonly IEventBus Bus = new Mock<IEventBus>().Object;

    private static IEnumerable<TestCaseData> MissingDependencies()
    {
        yield return Case("runtime dependencies", _ => new MobaRuntimeService(null!, NullLogger<MobaRuntimeService>.Instance));
        yield return Case("runtime logger", counters => new MobaRuntimeService(Runtime(counters), null!));
        yield return Case("runtime event bus", counters => new MobaRuntimeService(
            WithoutEventBus(Runtime(counters)), NullLogger<MobaRuntimeService>.Instance));
        yield return Case("runtime interlocking", counters => new MobaRuntimeService(
            WithoutInterlocking(Runtime(counters)), NullLogger<MobaRuntimeService>.Instance));
        yield return Case("journey project", counters => new JourneyManager(
            null!, Context(), TestRuntimes.JourneyDependencies(counters), NullLogger<JourneyManager>.Instance));
        yield return Case("journey context", counters => new JourneyManager(
            new Project(), null!, TestRuntimes.JourneyDependencies(counters), NullLogger<JourneyManager>.Instance));
        yield return Case("journey dependencies", counters => new JourneyManager(
            new Project(), Context(), null!, NullLogger<JourneyManager>.Instance));
        yield return Case("journey logger", counters => new JourneyManager(
            new Project(), Context(), TestRuntimes.JourneyDependencies(counters), null!));
        yield return Case("factory workflows", _ => new JourneyManagerFactory(null!, new JourneyStopTransitionService(),
            new NullJourneyRuntimeStateStore(), Bus, TimeProvider.System, NullLoggerFactory.Instance));
        yield return Case("factory stop transitions", _ => new JourneyManagerFactory(Workflows, null!,
            new NullJourneyRuntimeStateStore(), Bus, TimeProvider.System, NullLoggerFactory.Instance));
        yield return Case("factory state store", _ => new JourneyManagerFactory(Workflows, new JourneyStopTransitionService(),
            null!, Bus, TimeProvider.System, NullLoggerFactory.Instance));
        yield return Case("factory event bus", _ => new JourneyManagerFactory(Workflows, new JourneyStopTransitionService(),
            new NullJourneyRuntimeStateStore(), null!, TimeProvider.System, NullLoggerFactory.Instance));
        yield return Case("factory time", _ => new JourneyManagerFactory(Workflows, new JourneyStopTransitionService(),
            new NullJourneyRuntimeStateStore(), Bus, null!, NullLoggerFactory.Instance));
        yield return Case("factory logger", _ => new JourneyManagerFactory(Workflows, new JourneyStopTransitionService(),
            new NullJourneyRuntimeStateStore(), Bus, TimeProvider.System, null!));
        yield return Case("platform context", _ => new PlatformManager(Z21, new Project(), new Station(), Workflows,
            null!, NullLogger<PlatformManager>.Instance));
        yield return Case("platform logger", _ => new PlatformManager(Z21, new Project(), new Station(), Workflows, Context(), null!));
        yield return Case("platform factory logger", _ => new PlatformManagerFactory(Z21, Workflows, null!));
    }

    [TestCaseSource(nameof(MissingDependencies))]
    public void Constructor_RejectsAMissingRuntimeDependency(Func<InPortCounterService, object> construct)
    {
        using var counters = new InPortCounterService(Z21, new AppSettings());

        Assert.Throws<ArgumentNullException>(() => construct(counters));
    }

    [Test]
    public void Factories_LogEachManagerUnderItsOwnCategory()
    {
        var loggers = new Mock<ILoggerFactory>();
        loggers.Setup(factory => factory.CreateLogger(It.IsAny<string>())).Returns(NullLogger.Instance);
        using var counters = new InPortCounterService(Z21, new AppSettings());
        var journeys = new JourneyManagerFactory(Workflows, new JourneyStopTransitionService(),
            new NullJourneyRuntimeStateStore(), Bus, TimeProvider.System, loggers.Object);
        var platforms = new PlatformManagerFactory(Z21, Workflows, loggers.Object);

        using (journeys.Create(new Project(), Context(), counters))
        using (platforms.Create(new Project(), new Station(), Context()))
        {
            loggers.Verify(factory => factory.CreateLogger(typeof(JourneyManager).FullName!), Times.Once);
            loggers.Verify(factory => factory.CreateLogger(typeof(PlatformManager).FullName!), Times.Once);
        }
    }

    [Test]
    public async Task RuntimeDispose_LeavesItsInjectedInPortCountersCounting()
    {
        var z21 = new Mock<IZ21>();
        var settings = CountingSettings();
        using var counters = new InPortCounterService(z21.Object, settings);
        using (var runtime = TestRuntimes.Create(z21.Object, counters, new TestRuntimeOptions { Settings = settings }))
        {
            await runtime.ActivateProjectAsync(new Project { Journeys = [new Journey { IsActive = true }] });
        }

        RaiseFeedbackOnInPortOne(z21);

        Assert.That(counters.GetSnapshot().Single(counter => counter.InPort == 1).Count, Is.EqualTo(1),
            "The runtime borrows its counters; disposing it must not dispose them.");
        Assert.DoesNotThrow(counters.ResetAll);
    }

    [Test]
    public void JourneyManagerDispose_DisposesItsCoordinatorOnceAndLeavesCountersCounting()
    {
        var z21 = new Mock<IZ21>();
        using var counters = new InPortCounterService(z21.Object, CountingSettings());
        var coordinator = new Mock<IWorkflowExecutionCoordinator>();
        var manager = TestRuntimes.JourneyManager(z21.Object, new Project(),
            TestRuntimes.JourneyDependencies(counters, coordinator: coordinator.Object));

        manager.Dispose();
        RaiseFeedbackOnInPortOne(z21);

        coordinator.Verify(value => value.Dispose(), Times.Once);
        Assert.That(counters.GetSnapshot().Single(counter => counter.InPort == 1).Count, Is.EqualTo(1),
            "The journey manager owns its coordinator but borrows its counters.");
    }

    private static AppSettings CountingSettings() =>
        new() { Counter = { CountOfFeedbackPoints = 1, UseTimerFilter = false } };

    private static void RaiseFeedbackOnInPortOne(Mock<IZ21> z21)
    {
        byte[] packet = [0x0F, 0x00, 0x80, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        z21.Raise(source => source.Received += null, new FeedbackResult(packet));
    }

    private static TestCaseData Case(string name, Func<InPortCounterService, object> construct) =>
        new TestCaseData(construct).SetArgDisplayNames(name);

    private static ActionExecutionContext Context() => new() { Z21 = Z21 };

    private static MobaRuntimeServiceDependencies Runtime(InPortCounterService counters) => new()
    {
        Z21 = Z21,
        ExecutionContextFactory = new ActionExecutionContextFactory(Context()),
        Settings = new AppSettings(),
        EventBus = Bus,
        JourneyManagerFactory = TestRuntimes.JourneyManagers(Workflows, Bus),
        InterlockingRuntime = new Mock<IInterlockingRuntime>().Object,
        InPortCounterService = counters
    };

    private static MobaRuntimeServiceDependencies WithoutEventBus(MobaRuntimeServiceDependencies dependencies) => new()
    {
        Z21 = dependencies.Z21,
        ExecutionContextFactory = dependencies.ExecutionContextFactory,
        Settings = dependencies.Settings,
        EventBus = null!,
        JourneyManagerFactory = dependencies.JourneyManagerFactory,
        InterlockingRuntime = dependencies.InterlockingRuntime,
        InPortCounterService = dependencies.InPortCounterService
    };

    private static MobaRuntimeServiceDependencies WithoutInterlocking(MobaRuntimeServiceDependencies dependencies) => new()
    {
        Z21 = dependencies.Z21,
        ExecutionContextFactory = dependencies.ExecutionContextFactory,
        Settings = dependencies.Settings,
        EventBus = dependencies.EventBus,
        JourneyManagerFactory = dependencies.JourneyManagerFactory,
        InterlockingRuntime = null!,
        InPortCounterService = dependencies.InPortCounterService
    };
}
