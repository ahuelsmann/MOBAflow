// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;

using Backend.Interface;

using Common.Recording;

using Domain;

using Interface;

using System.Text.Json;

/// <summary>Records sanitized command intent and outcomes around an explicit runtime command gateway.</summary>
public sealed class RecordingRuntimeCommandGateway : IRuntimeCommandGateway
{
    private const string Category = "command";
    private const string Source = "runtime-command-gateway";
    private readonly IRuntimeCommandGateway _inner;
    private readonly IRecordingSessionService _recordingSessionService;

    /// <summary>Initializes a recording decorator around one concrete command route.</summary>
    public RecordingRuntimeCommandGateway(
        IRuntimeCommandGateway inner,
        IRecordingSessionService recordingSessionService)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _recordingSessionService = recordingSessionService ?? throw new ArgumentNullException(nameof(recordingSessionService));
    }

    /// <inheritdoc />
    public Task SetTrackPowerAsync(bool isOn, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            Command(
                "command.track-power",
                JsonSerializer.SerializeToElement(new { isOn }),
                $"Set track power {(isOn ? "on" : "off")}"),
            token => _inner.SetTrackPowerAsync(isOn, token),
            cancellationToken);

    /// <inheritdoc />
    public Task SimulateFeedbackAsync(int inPort, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            Command(
                "command.simulate-feedback",
                JsonSerializer.SerializeToElement(new { inPort }),
                $"Simulate feedback {inPort}"),
            token => _inner.SimulateFeedbackAsync(inPort, token),
            cancellationToken);

    /// <inheritdoc />
    public Task ResetJourneyAsync(Guid journeyId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            Command(
                "command.journey-reset",
                JsonSerializer.SerializeToElement(new { journeyId }),
                "Reset journey",
                [new RecordingEntityReference("journey", journeyId)]),
            token => _inner.ResetJourneyAsync(journeyId, token),
            cancellationToken);

    /// <inheritdoc />
    public Task SetSignalAspectAsync(
        Guid signalId,
        SignalAspect aspect,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            Command(
                "command.signal-aspect",
                JsonSerializer.SerializeToElement(new { signalId, aspect = aspect.ToString() }),
                $"Set signal aspect {aspect}",
                [new RecordingEntityReference("signal", signalId)]),
            token => _inner.SetSignalAspectAsync(signalId, aspect, token),
            cancellationToken);

    /// <inheritdoc />
    public Task SetLocomotiveDriveAsync(
        int address,
        int speed,
        bool forward,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            Command(
                "command.locomotive-drive",
                JsonSerializer.SerializeToElement(new { address, speed, forward }),
                $"Set locomotive {address} drive"),
            token => _inner.SetLocomotiveDriveAsync(address, speed, forward, token),
            cancellationToken);

    /// <inheritdoc />
    public Task SetLocomotiveFunctionAsync(
        int address,
        int functionIndex,
        bool isOn,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            Command(
                "command.locomotive-function",
                JsonSerializer.SerializeToElement(new { address, functionIndex, isOn }),
                $"Set locomotive {address} function {functionIndex}"),
            token => _inner.SetLocomotiveFunctionAsync(address, functionIndex, isOn, token),
            cancellationToken);

    /// <inheritdoc />
    public Task SendTurnoutCommandAsync(
        int decoderAddress,
        int output,
        bool activate,
        bool queue = false,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            Command(
                "command.turnout",
                JsonSerializer.SerializeToElement(new { decoderAddress, output, activate, queue }),
                $"Set turnout decoder {decoderAddress} output {output}"),
            token => _inner.SendTurnoutCommandAsync(decoderAddress, output, activate, queue, token),
            cancellationToken);

    private async Task ExecuteAsync(
        CommandDescriptor command,
        Func<CancellationToken, Task> execute,
        CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid();
        Record(command, "request", "information", correlationId, command.Payload, RecordingReplayApplicability.ReplayApplicable);
        try
        {
            await execute(cancellationToken).ConfigureAwait(false);
            RecordOutcome(command, "result", "information", correlationId, "succeeded");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            RecordOutcome(command, "failure", "warning", correlationId, "cancelled");
            throw;
        }
        catch
        {
            RecordOutcome(command, "failure", "error", correlationId, "failed");
            throw;
        }
    }

    private void RecordOutcome(
        CommandDescriptor command,
        string suffix,
        string severity,
        Guid correlationId,
        string outcome) =>
        Record(
            command,
            suffix,
            severity,
            correlationId,
            JsonSerializer.SerializeToElement(new { outcome }),
            RecordingReplayApplicability.DisplayOnly);

    private void Record(
        CommandDescriptor command,
        string suffix,
        string severity,
        Guid correlationId,
        JsonElement payload,
        RecordingReplayApplicability replayApplicability)
    {
        _recordingSessionService.TryRecord(new RecordingEntryProjection(
            Category,
            Source,
            $"{command.TypeKey}.{suffix}",
            severity,
            correlationId,
            command.EntityReferences,
            payload,
            $"{command.DisplayText}: {suffix}",
            replayApplicability));
    }

    private static CommandDescriptor Command(
        string typeKey,
        JsonElement payload,
        string displayText,
        IReadOnlyList<RecordingEntityReference>? entityReferences = null) =>
        new(typeKey, payload, displayText, entityReferences ?? []);

    private sealed record CommandDescriptor(
        string TypeKey,
        JsonElement Payload,
        string DisplayText,
        IReadOnlyList<RecordingEntityReference> EntityReferences);
}
