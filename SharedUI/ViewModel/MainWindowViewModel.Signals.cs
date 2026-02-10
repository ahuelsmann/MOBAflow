// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.Input;
using Moba.Domain;
using Microsoft.Extensions.Logging;

/// <summary>
/// Partial class for Signal/Multiplex decoder control via Z21.
/// Handles setting signal aspects via extended accessory decoder protocol (LAN_X_SET_EXT_ACCESSORY).
/// </summary>
public partial class MainWindowViewModel
{
    /// <summary>
    /// Sets a multiplex signal aspect via Z21 extended accessory command.
    /// Automatically calculates the correct DCC address based on the multiplexer configuration and signal aspect.
    /// </summary>
    /// <param name="signal">The signal element with multiplex configuration (Multiplexer, MainSignal, BaseAddress)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SetSignalAspectAsync(SbSignal signal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signal);

        System.Diagnostics.Debug.WriteLine($"[MWVM.SetSignalAspect] START: Signal={signal.Name}, Aspect={signal.SignalAspect}");

        if (!signal.IsMultiplexed)
        {
            System.Diagnostics.Debug.WriteLine($"[MWVM.SetSignalAspect] ERROR: Signal not multiplexed");
            _logger?.LogWarning(
                "Signal '{SignalName}' (ID: {SignalId}) is not marked as multiplexed. Configure IsMultiplexed=true.",
                signal.Name, signal.Id.ToString()[..8]);
            return;
        }

        if (string.IsNullOrEmpty(signal.MultiplexerArticleNumber))
        {
            System.Diagnostics.Debug.WriteLine($"[MWVM.SetSignalAspect] ERROR: No multiplexer article number");
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
            // Get multiplexer definition
            var definition = Common.Multiplex.MultiplexerHelper.GetDefinition(signal.MultiplexerArticleNumber);
            System.Diagnostics.Debug.WriteLine($"[MWVM.SetSignalAspect] Multiplexer definition loaded: {definition.DisplayName}");

            // Calculate DCC address based on base address and signal aspect
            int dccAddress = definition.GetAddressForAspect(signal.BaseAddress, signal.SignalAspect);
            System.Diagnostics.Debug.WriteLine($"[MWVM.SetSignalAspect] DCC Address calculated: {dccAddress}");

            // Get command value for this aspect
            int commandValue = definition.GetCommandValue(signal.SignalAspect);
            System.Diagnostics.Debug.WriteLine($"[MWVM.SetSignalAspect] Command value calculated: {commandValue}");

            // Send to Z21
            System.Diagnostics.Debug.WriteLine($"[MWVM.SetSignalAspect] Calling Z21.SetExtAccessoryAsync(address={dccAddress}, value={commandValue})...");
            await _z21.SetExtAccessoryAsync(dccAddress, commandValue, cancellationToken).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[MWVM.SetSignalAspect] Z21.SetExtAccessoryAsync completed successfully");

            _logger?.LogInformation(
                "Signal '{SignalName}' set: Multiplexer={Multiplexer}, BaseAddress={BaseAddress}, " +
                "Aspect={Aspect} → DCC_Address={DccAddress}, CommandValue={CommandValue}",
                signal.Name, signal.MultiplexerArticleNumber, signal.BaseAddress,
                signal.SignalAspect, dccAddress, commandValue);

            _uiDispatcher.InvokeOnUi(() =>
            {
                StatusText = $"✅ Signal '{signal.Name}' gestellt (Adresse {dccAddress}, Wert {commandValue})";
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
