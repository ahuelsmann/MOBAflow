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
        await _mobaRuntime.SetSignalAspectAsync(signal, cancellationToken).ConfigureAwait(false);
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
