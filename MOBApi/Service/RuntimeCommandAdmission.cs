// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Service;

using Common.Runtime;

/// <summary>
/// Outcome of admitting a remote runtime command.
/// </summary>
public enum RuntimeCommandAdmissionStatus
{
    Accepted,
    Invalid,
    QueueFull
}

/// <summary>
/// Result of admitting a remote runtime command, with an English reason when it was rejected.
/// </summary>
public sealed record RuntimeCommandAdmissionResult(RuntimeCommandAdmissionStatus Status, string? Error = null)
{
    public static RuntimeCommandAdmissionResult Accepted { get; } = new(RuntimeCommandAdmissionStatus.Accepted);

    public static RuntimeCommandAdmissionResult QueueFull { get; } =
        new(RuntimeCommandAdmissionStatus.QueueFull, "queue_full");

    public bool IsAccepted => Status == RuntimeCommandAdmissionStatus.Accepted;
}

/// <summary>
/// Single entry point for remote runtime commands from REST and SignalR: every command is validated
/// before it is forwarded to the host or queued.
/// </summary>
public interface IRuntimeCommandAdmission
{
    /// <summary>Checks a command that is about to be forwarded directly to the host.</summary>
    RuntimeCommandAdmissionResult Validate(RuntimeCommandEnvelope command);

    /// <summary>Validates a command and adds it to the bounded fallback queue.</summary>
    RuntimeCommandAdmissionResult Enqueue(RuntimeCommandEnvelope command);
}

/// <inheritdoc />
public sealed class RuntimeCommandAdmission(IRuntimeCommandQueue commandQueue, ISolutionCache solutionCache)
    : IRuntimeCommandAdmission
{
    public RuntimeCommandAdmissionResult Validate(RuntimeCommandEnvelope command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!RuntimeCommandValidator.TryValidate(command, out var error))
        {
            return new RuntimeCommandAdmissionResult(RuntimeCommandAdmissionStatus.Invalid, error);
        }

        // A command for a project the synchronized solution does not contain has no runtime to run in.
        return solutionCache.ContainsProject(command.ProjectId)
            ? RuntimeCommandAdmissionResult.Accepted
            : new RuntimeCommandAdmissionResult(RuntimeCommandAdmissionStatus.Invalid, "Unknown project.");
    }

    public RuntimeCommandAdmissionResult Enqueue(RuntimeCommandEnvelope command)
    {
        var validation = Validate(command);
        if (!validation.IsAccepted)
        {
            return validation;
        }

        return commandQueue.TryEnqueue(command)
            ? RuntimeCommandAdmissionResult.Accepted
            : RuntimeCommandAdmissionResult.QueueFull;
    }
}
