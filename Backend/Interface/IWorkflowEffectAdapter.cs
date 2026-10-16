// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

using Domain;
using Service;

/// <summary>
/// Executes a validated external intent. Isolated composition supplies a recording implementation.
/// Requests contain private execution data; adapters must project allow-listed fields before recording or logging them.
/// </summary>
public interface IWorkflowEffectAdapter
{
    /// <summary>Executes one intent using the invocation's context and cancellation boundary.</summary>
    Task ExecuteAsync(WorkflowEffectRequest request, ActionExecutionContext context, CancellationToken cancellationToken);
}

/// <summary>Private execution data, distinct from sanitized planned effects and trace output.</summary>
public abstract record WorkflowEffectRequest(Guid ActionId, string ActionName);

/// <summary>A validated command-station payload.</summary>
public sealed record WorkflowCommandEffectRequest(Guid ActionId, string ActionName, ReadOnlyMemory<byte> Bytes)
    : WorkflowEffectRequest(ActionId, ActionName);

/// <summary>An audio path whose availability is checked only by production execution.</summary>
public sealed record WorkflowAudioEffectRequest(Guid ActionId, string ActionName, string FilePath)
    : WorkflowEffectRequest(ActionId, ActionName);

/// <summary>An announcement template enriched with pure rendering and invocation state before execution.</summary>
public sealed record WorkflowAnnouncementEffectRequest(Guid ActionId, string ActionName, string? TemplateText)
    : WorkflowEffectRequest(ActionId, ActionName)
{
    /// <summary>Gets the text rendered against the invocation's station.</summary>
    public string? RenderedText { get; init; }

    /// <summary>Gets the current station from the owning execution context.</summary>
    public Station? Station { get; init; }

    /// <summary>Gets the one-based station index, including production's empty-journey fallback.</summary>
    public int StationIndex { get; init; }
}

/// <summary>A script intent; arguments and paths must never be copied into testbench outputs.</summary>
public sealed record WorkflowScriptEffectRequest(Guid ActionId, string ActionName, string ScriptPath, string? Arguments)
    : WorkflowEffectRequest(ActionId, ActionName)
{
    /// <summary>Returns only the opaque action identity, never executable arguments or a path.</summary>
    public override string ToString() => $"Script intent {ActionId:D}";
}

/// <summary>A signal aspect and its shared, resolved accessory command.</summary>
public sealed record WorkflowSignalEffectRequest(Guid ActionId, string ActionName, SignalAspect Aspect, ResolvedMultiplexerCommand Command)
    : WorkflowEffectRequest(ActionId, ActionName);

/// <summary>A validated display update; production delivery remains unconfigured.</summary>
public sealed record WorkflowDisplayEffectRequest(Guid ActionId, string ActionName, Guid DisplayDeviceId, bool ClearBeforeRender)
    : WorkflowEffectRequest(ActionId, ActionName);
