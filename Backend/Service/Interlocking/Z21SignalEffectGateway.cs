// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Interlocking;

using Common.Configuration;

using Domain;

using Interface;

/// <summary>
/// Live signal effect adapter that reuses the established semantic multiplexer mapping.
/// </summary>
public sealed class Z21SignalEffectGateway : ISignalEffectGateway
{
    private readonly IReadOnlyDictionary<Guid, SignalDefinition> _signals;
    private readonly IZ21 _z21;
    private readonly AppSettings _appSettings;

    public Z21SignalEffectGateway(
        InterlockingDefinition definition,
        IZ21 z21,
        AppSettings appSettings)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(z21);
        ArgumentNullException.ThrowIfNull(appSettings);

        _signals = definition.Signals.ToDictionary(signal => signal.Id);
        _z21 = z21;
        _appSettings = appSettings;
    }

    public async Task<SignalEffectResult> ExecuteAsync(
        SignalEffectCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!_signals.TryGetValue(command.SignalId, out var signal))
            return Failed("The signal does not exist in the active interlocking definition.");
        if (!signal.IsMultiplexed || string.IsNullOrWhiteSpace(signal.MultiplexerArticleNumber))
            return Failed("The signal has no supported live multiplexer mapping.");
        if (!_z21.IsConnected)
            return new SignalEffectResult(SignalEffectStatus.Offline, "Z21 is not connected.");

        try
        {
            var resolved = global::Moba.Backend.Service.MultiplexerCommandResolver.Resolve(
                signal.BaseAddress,
                signal.MultiplexerArticleNumber,
                signal.MainSignalArticleNumber,
                command.Aspect,
                _appSettings.SignalBox);
            await _z21.SetTurnoutAsync(
                resolved.DccAddress,
                resolved.Output,
                resolved.Activate,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return new SignalEffectResult(SignalEffectStatus.Succeeded);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Failed(ex.Message);
        }
    }

    private static SignalEffectResult Failed(string message) =>
        new(SignalEffectStatus.Failed, message);
}
