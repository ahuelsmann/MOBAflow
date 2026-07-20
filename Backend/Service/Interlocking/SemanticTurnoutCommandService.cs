// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Interlocking;

using Domain;

using Interface;

public sealed record TurnoutEffectCommand(
    Guid TurnoutId,
    TurnoutPosition Position,
    int DecoderAddress,
    int Output,
    bool Activate,
    bool Queue,
    int SequenceIndex,
    Guid CorrelationId);

public enum TurnoutEffectStatus
{
    Succeeded,
    Offline,
    Failed
}

public sealed record TurnoutEffectResult(TurnoutEffectStatus Status, string? Message = null);

/// <summary>
/// Narrow replaceable effect boundary used by live and simulation turnout execution.
/// </summary>
public interface ITurnoutEffectGateway
{
    Task<TurnoutEffectResult> ExecuteAsync(TurnoutEffectCommand command, CancellationToken cancellationToken = default);
}

public enum TurnoutCommandExecutionStatus
{
    Succeeded,
    Rejected,
    Offline,
    Failed,
    Cancelled
}

public sealed record TurnoutCommandExecutionResult(
    TurnoutCommandExecutionStatus Status,
    string Code,
    string Message,
    IReadOnlyList<TurnoutEffectCommand> DispatchedCommands)
{
    public bool RequiresReconciliation =>
        DispatchedCommands.Count > 0 && Status is not TurnoutCommandExecutionStatus.Succeeded;
}

/// <summary>
/// Maps stable turnout IDs and semantic positions to fully validated raw accessory commands.
/// </summary>
public sealed class SemanticTurnoutCommandService
{
    private readonly IReadOnlyDictionary<Guid, TurnoutCommandDefinition> _turnouts;
    private readonly ITurnoutEffectGateway _gateway;

    public SemanticTurnoutCommandService(
        InterlockingDefinition definition,
        ITurnoutEffectGateway gateway)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(gateway);

        _gateway = gateway;
        _turnouts = definition.Turnouts.ToDictionary(
            turnout => turnout.Id,
            turnout => new TurnoutCommandDefinition(
                turnout.Id,
                turnout.DecoderAddress,
                turnout.Commands
                    .GroupBy(mapping => mapping.Position)
                    .ToDictionary(
                        group => group.Key,
                        group => new PositionCommandDefinition(
                            group.Count() > 1,
                            group.SelectMany(mapping => mapping.Commands)
                                .Select(command => new RawCommandDefinition(
                                    command.AddressOffset,
                                    command.Output,
                                    command.Activate,
                                    command.Queue))
                                .ToArray()))));
    }

    public async Task<TurnoutCommandExecutionResult> ExecuteAsync(
        Guid turnoutId,
        TurnoutPosition position,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
            return Rejected("turnout.correlation.empty", "A semantic turnout command requires a non-empty correlation ID.");
        if (!_turnouts.TryGetValue(turnoutId, out var turnout))
            return Rejected("turnout.missing", "The requested turnout does not exist.");
        if (!turnout.Commands.TryGetValue(position, out var positionDefinition) || positionDefinition.Commands.Length == 0)
            return Rejected("turnout.mapping.missing", "The requested semantic turnout position has no command mapping.");
        if (positionDefinition.IsAmbiguous)
            return Rejected("turnout.mapping.ambiguous", "The requested semantic turnout position has multiple competing mappings.");

        var commandDefinitions = positionDefinition.Commands;
        var commands = new List<TurnoutEffectCommand>(commandDefinitions.Length);
        for (var index = 0; index < commandDefinitions.Length; index++)
        {
            var definition = commandDefinitions[index];
            var address = (long)turnout.DecoderAddress + definition.AddressOffset;
            if (definition.AddressOffset < 0 || address is < 1 or > 2044 || definition.Output is < 0 or > 1)
                return Rejected("turnout.mapping.invalid", "The semantic turnout mapping contains an unsupported address or output.");

            commands.Add(new TurnoutEffectCommand(
                turnoutId,
                position,
                (int)address,
                definition.Output,
                definition.Activate,
                definition.Queue,
                index,
                correlationId));
        }

        var dispatched = new List<TurnoutEffectCommand>(commands.Count);
        try
        {
            foreach (var command in commands)
            {
                cancellationToken.ThrowIfCancellationRequested();
                dispatched.Add(command);
                var effect = await _gateway.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
                if (effect.Status == TurnoutEffectStatus.Succeeded)
                    continue;

                if (effect.Status == TurnoutEffectStatus.Offline)
                    dispatched.RemoveAt(dispatched.Count - 1);

                var status = effect.Status == TurnoutEffectStatus.Offline
                    ? TurnoutCommandExecutionStatus.Offline
                    : TurnoutCommandExecutionStatus.Failed;
                var code = effect.Status == TurnoutEffectStatus.Offline
                    ? "turnout.gateway.offline"
                    : "turnout.gateway.failed";
                return new TurnoutCommandExecutionResult(
                    status,
                    code,
                    effect.Message ?? "The turnout effect gateway did not complete the command.",
                    dispatched);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new TurnoutCommandExecutionResult(
                TurnoutCommandExecutionStatus.Cancelled,
                "turnout.command.cancelled",
                "The semantic turnout command was cancelled.",
                dispatched);
        }
        catch (Exception ex)
        {
            return new TurnoutCommandExecutionResult(
                TurnoutCommandExecutionStatus.Failed,
                "turnout.gateway.exception",
                ex.Message,
                dispatched);
        }

        return new TurnoutCommandExecutionResult(
            TurnoutCommandExecutionStatus.Succeeded,
            "turnout.command.succeeded",
            "The semantic turnout command sequence completed.",
            dispatched);
    }

    private static TurnoutCommandExecutionResult Rejected(string code, string message) =>
        new(TurnoutCommandExecutionStatus.Rejected, code, message, []);

    private sealed record TurnoutCommandDefinition(
        Guid TurnoutId,
        int DecoderAddress,
        IReadOnlyDictionary<TurnoutPosition, PositionCommandDefinition> Commands);

    private sealed record PositionCommandDefinition(bool IsAmbiguous, RawCommandDefinition[] Commands);

    private sealed record RawCommandDefinition(int AddressOffset, int Output, bool Activate, bool Queue);
}

/// <summary>
/// Live Z21 adapter. It rejects commands while disconnected and exposes no interlocking policy.
/// </summary>
public sealed class Z21TurnoutEffectGateway : ITurnoutEffectGateway
{
    private readonly IZ21 _z21;

    public Z21TurnoutEffectGateway(IZ21 z21)
    {
        ArgumentNullException.ThrowIfNull(z21);
        _z21 = z21;
    }

    public async Task<TurnoutEffectResult> ExecuteAsync(
        TurnoutEffectCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!_z21.IsConnected)
            return new TurnoutEffectResult(TurnoutEffectStatus.Offline, "Z21 is not connected.");

        try
        {
            await _z21.SetTurnoutAsync(
                command.DecoderAddress,
                command.Output,
                command.Activate,
                command.Queue,
                cancellationToken).ConfigureAwait(false);
            return new TurnoutEffectResult(TurnoutEffectStatus.Succeeded);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TurnoutEffectResult(TurnoutEffectStatus.Failed, ex.Message);
        }
    }
}

/// <summary>
/// Simulation adapter that records commands and can inject deterministic outcomes without live hardware.
/// </summary>
public sealed class RecordingTurnoutEffectGateway : ITurnoutEffectGateway
{
    private readonly object _sync = new();
    private readonly Func<TurnoutEffectCommand, TurnoutEffectResult> _resultFactory;
    private readonly List<TurnoutEffectCommand> _commands = [];

    public RecordingTurnoutEffectGateway(Func<TurnoutEffectCommand, TurnoutEffectResult>? resultFactory = null)
    {
        _resultFactory = resultFactory ?? (_ => new TurnoutEffectResult(TurnoutEffectStatus.Succeeded));
    }

    public IReadOnlyList<TurnoutEffectCommand> Commands
    {
        get
        {
            lock (_sync)
                return _commands.ToArray();
        }
    }

    public Task<TurnoutEffectResult> ExecuteAsync(
        TurnoutEffectCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
            _commands.Add(command);
        return Task.FromResult(_resultFactory(command));
    }
}
