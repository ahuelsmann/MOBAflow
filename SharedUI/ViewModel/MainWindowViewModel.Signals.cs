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
            _logger?.LogWarning(
                "Signal '{SignalName}' (ID: {SignalId}) is not marked as multiplexed. Configure IsMultiplexed=true.",
                signal.Name, signal.Id.ToString()[..8]);
            return;
        }

        if (string.IsNullOrEmpty(signal.MultiplexerArticleNumber))
        {
            System.Diagnostics.Debug.WriteLine("[MWVM.SetSignalAspect] ERROR: No multiplexer article number");
            _logger?.LogWarning(
                "Signal '{SignalName}': Multiplexer article number not configured.",
                signal.Name);
            return;
        }

        if (signal.BaseAddress <= 0 || signal.BaseAddress > 2044)
        {
            System.Diagnostics.Debug.WriteLine($"[MWVM.SetSignalAspect] ERROR: Invalid base address {signal.BaseAddress}");
            _logger?.LogWarning(
                "Signal '{SignalName}': Invalid base address {Address}. Must be 1-2044.",
                signal.Name, signal.BaseAddress);
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
                _logger?.LogWarning(
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

            System.Diagnostics.Debug.WriteLine($"[MWVM.SetSignalAspect] DCC Address calculated: {dccAddress}, Output={turnoutCommand.Output}, Activate={turnoutCommand.Activate}");

            // Send activate pulse (A=1), then deactivate (A=0) to simulate button press
            System.Diagnostics.Debug.WriteLine($"[MWVM.SetSignalAspect] Sending activate pulse to address={dccAddress}, output={turnoutCommand.Output}");
            
            // Step 1: Send activate command (A=1) with queue=false for immediate execution
            await _z21.SetTurnoutAsync(dccAddress, turnoutCommand.Output, true, false, cancellationToken).ConfigureAwait(false);
            
            // Step 2: Short delay (100ms) to allow decoder to register the activate pulse
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            
            // Step 3: Send deactivate command (A=0) to complete the pulse
            await _z21.SetTurnoutAsync(dccAddress, turnoutCommand.Output, false, false, cancellationToken).ConfigureAwait(false);
            
            System.Diagnostics.Debug.WriteLine("[MWVM.SetSignalAspect] Z21.SetTurnoutAsync pulse completed successfully");

            _logger?.LogInformation(
                "Signal '{SignalName}' set: Multiplexer={Multiplexer}, BaseAddress={BaseAddress}, " +
                "Aspect={Aspect} → DCC_Address={DccAddress}, Output={Output}, Pulse sent (A=1→A=0)",
                signal.Name, signal.MultiplexerArticleNumber, signal.BaseAddress,
                signal.SignalAspect, dccAddress, turnoutCommand.Output);

            _uiDispatcher.InvokeOnUi(() =>
            {
                var signalColor = turnoutCommand.Activate ? "green (+)" : "red (-)";
                StatusText = $"Signal '{signal.Name}' set (Address {dccAddress}, {signalColor})";
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MWVM.SetSignalAspect] EXCEPTION: {ex.Message}");
            _logger?.LogError(ex, "Failed to set signal aspect for '{SignalName}'", signal.Name);

            _uiDispatcher.InvokeOnUi(() =>
            {
                StatusText = $"❌ Signal-Fehler: {ex.Message}";
            });

            throw;
        }
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
            _logger?.LogError(ex, "Error in SetSignalAspectCommand");
        }
    }
}
