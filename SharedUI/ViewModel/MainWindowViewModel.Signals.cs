// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.Input;

using Domain;

using Microsoft.Extensions.Logging;

/// <summary>
/// Partial class for Signal/Multiplex decoder control via Z21.
/// Handles setting signal aspects via turnout commands based on 5229.md mappings.
/// </summary>
public partial class MainWindowViewModel
{
    /// <summary>
    /// Sets a multiplex signal aspect via Z21 turnout commands.
    /// Automatically calculates the correct DCC address and polarity based on the multiplexer mapping.
    /// </summary>
    /// <param name="signal">The signal element with multiplex configuration (Multiplexer, MainSignal, BaseAddress)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SetSignalAspectAsync(SbSignal signal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signal);

        System.Diagnostics.Debug.WriteLine($"[MWVM.SetSignalAspect] START: Signal={signal.Name}, Aspect={signal.SignalAspect}");

        if (!signal.IsMultiplexed)
        {
            System.Diagnostics.Debug.WriteLine("[MWVM.SetSignalAspect] ERROR: Signal not multiplexed");
            _logger.LogWarning(
                "Signal '{SignalName}' (ID: {SignalId}) is not marked as multiplexed. Configure IsMultiplexed=true.",
                signal.Name, signal.Id.ToString()[..8]);
            return;
        }

        if (string.IsNullOrEmpty(signal.MultiplexerArticleNumber))
        {
            System.Diagnostics.Debug.WriteLine("[MWVM.SetSignalAspect] ERROR: No multiplexer article number");
            _logger.LogWarning(
                "Signal '{SignalName}': Multiplexer article number not configured.",
                signal.Name);
            return;
        }

        if (signal.BaseAddress <= 0 || signal.BaseAddress > 2044)
        {
            System.Diagnostics.Debug.WriteLine($"[MWVM.SetSignalAspect] ERROR: Invalid base address {signal.BaseAddress}");
            _logger.LogWarning(
                "Signal '{SignalName}': Invalid base address {Address}. Must be 1-2044.",
                signal.Name, signal.BaseAddress);
            return;
        }

        if (!_z21.IsConnected)
        {
            System.Diagnostics.Debug.WriteLine("[MWVM.SetSignalAspect] ERROR: Z21 not connected");
            _logger.LogWarning("Signal '{SignalName}': Z21 not connected; skipping command send.", signal.Name);
            return;
        }

        try
        {
            if (!Common.Multiplex.MultiplexerHelper.TryGetTurnoutCommand(
                    signal.MultiplexerArticleNumber,
                    signal.MainSignalArticleNumber,
                    signal.SignalAspect,
                    out var turnoutCommand))
            {
                System.Diagnostics.Debug.WriteLine("[MWVM.SetSignalAspect] ERROR: Aspect not supported by multiplexer mapping");
                _logger.LogWarning(
                    "Signal '{SignalName}': Aspect {Aspect} not supported by multiplexer mapping.",
                    signal.Name, signal.SignalAspect);
                return;
            }

            var dccAddress = signal.BaseAddress + turnoutCommand.AddressOffset;
            if (dccAddress is < 1 or > 2044)
            {
                throw new ArgumentOutOfRangeException(nameof(signal.BaseAddress),
                    $"Calculated DCC address {dccAddress} is outside the valid range (1-2044).");
            }

            // Optional polarity inversion per address (offset 0..3 for e.g. 201, 202, 203, 204)
            var activate = turnoutCommand.Activate;
            if (ShouldInvertPolarityForOffset(turnoutCommand.AddressOffset))
            {
                activate = !activate;
                System.Diagnostics.Debug.WriteLine($"[MWVM.SetSignalAspect] InvertPolarity Offset {turnoutCommand.AddressOffset}: Activate flipped to {activate}");
            }

            System.Diagnostics.Debug.WriteLine($"[MWVM.SetSignalAspect] DCC Address calculated: {dccAddress}, Output={turnoutCommand.Output}, Activate={activate}");

            // For Viessmann multiplex decoders (e.g., 5229): use classic turnout command (0x53)
            // Each signal aspect maps to a different DCC address via AddressOffset
            await _z21.SetTurnoutAsync(dccAddress, turnoutCommand.Output, activate, false, cancellationToken).ConfigureAwait(false);

            System.Diagnostics.Debug.WriteLine("[MWVM.SetSignalAspect] Z21.SetTurnoutAsync completed successfully");

            _logger.LogInformation(
                "Signal '{SignalName}' set: Multiplexer={Multiplexer}, BaseAddress={BaseAddress}, " +
                "Aspect={Aspect} → Turnout_Address={TurnoutAddress}, Output={Output}, Activate={Activate}",
                signal.Name, signal.MultiplexerArticleNumber, signal.BaseAddress,
                signal.SignalAspect, dccAddress, turnoutCommand.Output, activate);

            _uiDispatcher.InvokeOnUi(() =>
            {
                StatusText = $"Signal '{signal.Name}' gestellt: DCC-Adresse {dccAddress}, Ausgang {turnoutCommand.Output}, Activate={activate}";
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MWVM.SetSignalAspect] EXCEPTION: {ex.Message}");
            _logger.LogError(ex, "Failed to set signal aspect for '{SignalName}'", signal.Name);

            _uiDispatcher.InvokeOnUi(() =>
            {
                StatusText = $"❌ Signal-Fehler: {ex.Message}";
            });

            throw;
        }
    }

    /// <summary>
    /// Checks whether polarity should be inverted for the specified address offset (0..3).
    /// </summary>
    private bool ShouldInvertPolarityForOffset(int addressOffset)
    {
        var sb = _settings.SignalBox;
        if (sb == null) return false;
        return addressOffset switch
        {
            0 => sb.InvertPolarityOffset0,
            1 => sb.InvertPolarityOffset1,
            2 => sb.InvertPolarityOffset2,
            3 => sb.InvertPolarityOffset3,
            _ => false
        };
    }

    /// <summary>
    /// Relay command version for XAML binding: Set signal aspect via Z21.
    /// </summary>
    [RelayCommand]
    private async Task SetSignalAspectCommand(SbSignal? signal)
    {
        if (signal == null) return;

        try
        {
            await SetSignalAspectAsync(signal).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SetSignalAspectCommand");
        }
    }
}
