// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Service;

using Contracts.Runtime;

using System.Diagnostics.Metrics;

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
public sealed class RuntimeCommandAdmission(
    IRuntimeCommandQueue commandQueue,
    ISolutionCache solutionCache,
    ILogger<RuntimeCommandAdmission> logger)
    : IRuntimeCommandAdmission
{
    /// <summary>Meter of the MOBApi counters, readable live with <c>dotnet-counters monitor --counters Moba.MOBApi</c>.</summary>
    public const string MeterName = "Moba.MOBApi";

    /// <summary>Counter of rejected remote commands, tagged with <c>reason</c> and <c>command</c>.</summary>
    public const string RejectedCommandsName = "moba.runtime_commands.rejected";

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> Rejected = Meter.CreateCounter<long>(
        RejectedCommandsName,
        unit: "{command}",
        description: "Remote runtime commands rejected as invalid or because the bounded queue was full.");

    private static readonly Action<ILogger, RuntimeCommandType, int?, Exception?> LogQueueFull =
        LoggerMessage.Define<RuntimeCommandType, int?>(
            LogLevel.Warning,
            new EventId(1, nameof(LogQueueFull)),
            "Remote runtime command {Command} for locomotive {Address} rejected: the command queue is full");

    private static readonly Action<ILogger, RuntimeCommandType, string?, Exception?> LogInvalid =
        LoggerMessage.Define<RuntimeCommandType, string?>(
            LogLevel.Debug,
            new EventId(2, nameof(LogInvalid)),
            "Remote runtime command {Command} rejected as invalid: {Reason}");

    public RuntimeCommandAdmissionResult Validate(RuntimeCommandEnvelope command) => Record(command, ValidateCore(command));

    public RuntimeCommandAdmissionResult Enqueue(RuntimeCommandEnvelope command)
    {
        var validation = ValidateCore(command);
        if (!validation.IsAccepted)
        {
            return Record(command, validation);
        }

        return Record(
            command,
            commandQueue.TryEnqueue(command) ? RuntimeCommandAdmissionResult.Accepted : RuntimeCommandAdmissionResult.QueueFull);
    }

    private RuntimeCommandAdmissionResult ValidateCore(RuntimeCommandEnvelope command)
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

    // Every rejection is counted; an overloaded queue is also a warning, an invalid client request only a debug entry.
    private RuntimeCommandAdmissionResult Record(RuntimeCommandEnvelope command, RuntimeCommandAdmissionResult result)
    {
        if (result.IsAccepted)
        {
            return result;
        }

        Rejected.Add(
            1,
            new KeyValuePair<string, object?>("reason", result.Status == RuntimeCommandAdmissionStatus.QueueFull ? "queue_full" : "invalid"),
            new KeyValuePair<string, object?>("command", command.Type.ToString()));
        if (result.Status == RuntimeCommandAdmissionStatus.QueueFull)
        {
            LogQueueFull(logger, command.Type, command.LocomotiveAddress, null);
        }
        else
        {
            LogInvalid(logger, command.Type, result.Error, null);
        }

        return result;
    }
}
