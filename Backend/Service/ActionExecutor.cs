// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Domain;
using Domain.Enum;

using Interface;

using Microsoft.Extensions.Logging;

/// <summary>
/// Executes <see cref="WorkflowAction"/> instances (command, audio, announcement) using shared runtime dependencies.
/// </summary>
public class ActionExecutor : IActionExecutor
{
    private readonly IReadOnlyDictionary<ActionType, IWorkflowActionHandler> _handlers;
    private readonly ILogger<ActionExecutor>? _logger;

    public ActionExecutor(IEnumerable<IWorkflowActionHandler> handlers, ILogger<ActionExecutor>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        _handlers = handlers.ToDictionary(handler => handler.ActionType);
        _logger = logger;
    }

    /// <summary>
    /// Creates an executor with the built-in handler set. Intended for tests and manual wiring outside DI.
    /// </summary>
    public static ActionExecutor CreateWithDefaultHandlers(
        IAnnouncementService? announcementService = null,
        ILogger<ActionExecutor>? logger = null) =>
        new(CreateDefaultHandlers(announcementService), logger);

    /// <summary>
    /// Executes a WorkflowAction based on its type.
    /// </summary>
    public async Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(context);

        _logger?.LogDebug("Executing action #{Number}: {Name} (Type: {Type})", action.Number, action.Name, action.Type);

        if (!_handlers.TryGetValue(action.Type, out var handler))
            throw new NotSupportedException($"Action type '{action.Type}' is not supported");

        await handler.ExecuteAsync(action, context).ConfigureAwait(false);
    }

    private static IEnumerable<IWorkflowActionHandler> CreateDefaultHandlers(
        IAnnouncementService? announcementService) =>
        [
            new CommandWorkflowActionHandler(),
            new AudioWorkflowActionHandler(),
            new AnnouncementWorkflowActionHandler(announcementService),
            new ExecuteScriptWorkflowActionHandler(),
            new SelectSignalAspectWorkflowActionHandler(),
            new TrainDestinationDisplayWorkflowActionHandler()
        ];
}