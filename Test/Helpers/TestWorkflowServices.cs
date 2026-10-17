// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Helpers;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Events;
using Moba.Common.IO;
using Moba.Speech.Base;

/// <summary>
/// Builds workflow execution services with explicit test collaborators: null loggers, a silent speech output,
/// a local event bus and the real file system unless a test passes its own.
/// </summary>
internal static class TestWorkflowServices
{
    /// <summary>An announcement service that renders text and speaks through <paramref name="speechOutput"/>.</summary>
    public static AnnouncementService Announcements(ISpeechOutput? speechOutput = null) =>
        new(speechOutput ?? new NullSpeechOutput(), NullLogger<AnnouncementService>.Instance);

    /// <summary>The production effect adapter over the given announcement service and file system.</summary>
    public static ProductionWorkflowEffectAdapter ProductionEffects(
        IAnnouncementService? announcements = null,
        IFileSystem? fileSystem = null) =>
        new(announcements ?? Announcements(), fileSystem ?? SystemFileSystem.Instance,
            NullLogger<ProductionWorkflowEffectAdapter>.Instance);

    /// <summary>The built-in handlers for direct workflow actions, all sending effects to <paramref name="effects"/>.</summary>
    public static IWorkflowActionHandler[] Handlers(IWorkflowEffectAdapter effects, IAnnouncementService? announcements = null) =>
    [
        new CommandWorkflowActionHandler(NullLogger<CommandWorkflowActionHandler>.Instance, effects),
        new AudioWorkflowActionHandler(effects),
        new AnnouncementWorkflowActionHandler(announcements ?? Announcements(),
            NullLogger<AnnouncementWorkflowActionHandler>.Instance, effects),
        new ExecuteScriptWorkflowActionHandler(effects),
        new SelectSignalAspectWorkflowActionHandler(effects),
        new TrainDestinationDisplayWorkflowActionHandler(NullLogger<TrainDestinationDisplayWorkflowActionHandler>.Instance, effects)
    ];

    /// <summary>An executor with the built-in handlers and production effects.</summary>
    public static ActionExecutor Executor(IAnnouncementService? announcements = null)
    {
        var resolved = announcements ?? Announcements();
        return new ActionExecutor(Handlers(ProductionEffects(resolved), resolved), NullLogger<ActionExecutor>.Instance);
    }

    /// <summary>A journey stop handler backed by the production transition rules.</summary>
    public static ChangeJourneyStopWorkflowActionHandler ChangeJourneyStop() => new(new JourneyStopTransitionService());

    /// <summary>Workflow sequence collaborators with a fresh trace store and a local event bus.</summary>
    public static WorkflowServiceDependencies Dependencies(
        TimeProvider? timeProvider = null,
        IWorkflowTraceStore? traceStore = null,
        IEventBus? eventBus = null) => new()
        {
            Validator = new WorkflowValidator(),
            EffectPlanner = new WorkflowEffectPlanner(),
            EventBus = eventBus ?? new EventBus(NullLogger<EventBus>.Instance),
            TraceStore = traceStore ?? new WorkflowTraceStore(),
            TimeProvider = timeProvider ?? TimeProvider.System,
            Logger = NullLogger<WorkflowService>.Instance
        };

    /// <summary>A workflow service over <paramref name="executor"/> with <see cref="Dependencies"/>.</summary>
    public static WorkflowService Workflow(IActionExecutor executor, TimeProvider? timeProvider = null) =>
        new(executor, Dependencies(timeProvider));
}
