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
    /// Raised when runtime signal-box state was projected onto the active editor project.
    /// </summary>
    public event EventHandler? SignalBoxRuntimeStateChanged;

    /// <summary>
    /// Sends the signal's current aspect through the runtime command gateway.
    /// The runtime resolves the signal by id in the active project.
    /// </summary>
    /// <param name="signal">The signal whose <see cref="SbSignal.SignalAspect"/> is sent.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task SetSignalAspectAsync(SbSignal signal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signal);
        return _runtimeCommandGateway.SetSignalAspectAsync(signal.Id, signal.SignalAspect, cancellationToken);
    }

    /// <summary>
    /// Applies a property change made in the signal-box editor: persists the solution, refreshes the runtime
    /// project so it knows the changed configuration, and sends the signal aspect when requested.
    /// </summary>
    /// <param name="element">The changed signal-box element.</param>
    /// <param name="requiresPersistence">Whether the change alters stored configuration.</param>
    /// <param name="requiresSignalCommand">Whether the change requests a signal aspect on the layout.</param>
    public async Task ApplySignalBoxElementChangeAsync(SbElement element, bool requiresPersistence, bool requiresSignalCommand)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (requiresPersistence)
        {
            await SaveSolutionInternalAsync().ConfigureAwait(false);
            if (SelectedProject is { } project)
            {
                // The runtime executes a copy of the project; refresh it like station and journey edits do.
                await _runtimeConnection.ActivateProjectAsync(project.Model).ConfigureAwait(false);
            }
        }

        if (requiresSignalCommand && element is SbSignal signal)
        {
            await SetSignalAspectAsync(signal).ConfigureAwait(false);
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
            _logger.LogError(ex, "Error in SetSignalAspectCommand");
        }
    }
}