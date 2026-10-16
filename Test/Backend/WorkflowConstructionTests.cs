// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Events;
using Moba.Common.IO;
using Moba.Speech.Base;

using Moq;

/// <summary>
/// Workflow execution services declare every collaborator; a missing one fails at construction instead of
/// silently falling back to a default implementation.
/// </summary>
[TestFixture]
internal sealed class WorkflowConstructionTests
{
    private static readonly IWorkflowEffectAdapter Effects = new Mock<IWorkflowEffectAdapter>().Object;
    private static readonly IAnnouncementService Announcements = new Mock<IAnnouncementService>().Object;

    private static IEnumerable<TestCaseData> MissingDependencies()
    {
        yield return Case("command logger", () => new CommandWorkflowActionHandler(null!, Effects));
        yield return Case("command effects", () => new CommandWorkflowActionHandler(NullLogger<CommandWorkflowActionHandler>.Instance, null!));
        yield return Case("audio effects", () => new AudioWorkflowActionHandler(null!));
        yield return Case("announcement service", () => new AnnouncementWorkflowActionHandler(
            null!, NullLogger<AnnouncementWorkflowActionHandler>.Instance, Effects));
        yield return Case("announcement logger", () => new AnnouncementWorkflowActionHandler(Announcements, null!, Effects));
        yield return Case("announcement effects", () => new AnnouncementWorkflowActionHandler(
            Announcements, NullLogger<AnnouncementWorkflowActionHandler>.Instance, null!));
        yield return Case("display logger", () => new TrainDestinationDisplayWorkflowActionHandler(null!, Effects));
        yield return Case("display effects", () => new TrainDestinationDisplayWorkflowActionHandler(
            NullLogger<TrainDestinationDisplayWorkflowActionHandler>.Instance, null!));
        yield return Case("signal effects", () => new SelectSignalAspectWorkflowActionHandler(null!));
        yield return Case("script effects", () => new ExecuteScriptWorkflowActionHandler(null!));
        yield return Case("journey stop transitions", () => new ChangeJourneyStopWorkflowActionHandler(null!));
        yield return Case("adapter announcements", () => new ProductionWorkflowEffectAdapter(
            null!, SystemFileSystem.Instance, NullLogger<ProductionWorkflowEffectAdapter>.Instance));
        yield return Case("adapter file system", () => new ProductionWorkflowEffectAdapter(
            Announcements, null!, NullLogger<ProductionWorkflowEffectAdapter>.Instance));
        yield return Case("adapter logger", () => new ProductionWorkflowEffectAdapter(Announcements, SystemFileSystem.Instance, null!));
        yield return Case("executor logger", () => new ActionExecutor([], null!));
        yield return Case("speech output", () => new AnnouncementService(null!, NullLogger<AnnouncementService>.Instance));
        yield return Case("announcement service logger", () => new AnnouncementService(new NullSpeechOutput(), null!));
        yield return Case("workflow event bus", () => new WorkflowService(
            new Mock<IActionExecutor>().Object, Dependencies(eventBus: null, logger: NullLogger<WorkflowService>.Instance)));
        yield return Case("workflow logger", () => new WorkflowService(
            new Mock<IActionExecutor>().Object, Dependencies(new Mock<IEventBus>().Object, logger: null)));
    }

    [TestCaseSource(nameof(MissingDependencies))]
    public void Constructor_RejectsAMissingDependency(Func<object> construct)
    {
        Assert.Throws<ArgumentNullException>(() => construct());
    }

    private static TestCaseData Case(string name, Func<object> construct) => new TestCaseData(construct).SetArgDisplayNames(name);

    private static WorkflowServiceDependencies Dependencies(IEventBus? eventBus, Microsoft.Extensions.Logging.ILogger<WorkflowService>? logger) => new()
    {
        Validator = new WorkflowValidator(),
        EffectPlanner = new WorkflowEffectPlanner(),
        EventBus = eventBus!,
        TraceStore = new WorkflowTraceStore(),
        TimeProvider = TimeProvider.System,
        Logger = logger!
    };
}
