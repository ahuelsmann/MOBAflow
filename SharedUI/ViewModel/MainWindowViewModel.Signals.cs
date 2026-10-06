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

    // Signal-box editor changes run one after another so an aspect never overtakes a configuration update.
    private Task _signalBoxChanges = Task.CompletedTask;

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
    /// Applies a property change made in the signal-box editor: updates the runtime's signal-box configuration,
    /// sends the signal aspect when requested and persists the solution. Changes run in call order.
    /// </summary>
    /// <param name="element">The changed signal-box element.</param>
    /// <param name="requiresPersistence">Whether the change alters stored configuration.</param>
    /// <param name="requiresSignalCommand">Whether the change requests a signal aspect on the layout.</param>
    public Task ApplySignalBoxElementChangeAsync(SbElement element, bool requiresPersistence, bool requiresSignalCommand)
    {
        ArgumentNullException.ThrowIfNull(element);
        var change = ApplySignalBoxElementChangeAfterAsync(
            _signalBoxChanges,
            element,
            requiresPersistence,
            SelectedProject?.Model,
            requiresSignalCommand);
        _signalBoxChanges = change;
        return change;
    }

    private async Task ApplySignalBoxElementChangeAfterAsync(
        Task previousChange,
        SbElement element,
        bool requiresPersistence,
        Project? project,
        bool requiresSignalCommand)
    {
        // A failed earlier change was reported to its own caller; it must not block later changes.
        await Task.WhenAny(previousChange).ConfigureAwait(false);

        if (requiresPersistence && project is not null)
        {
            // The runtime executes a copy of the project; update only its signal-box configuration.
            await _runtimeConnection.UpdateSignalBoxAsync(project).ConfigureAwait(false);
        }

        if (requiresSignalCommand && element is SbSignal signal)
        {
            await SetSignalAspectAsync(signal).ConfigureAwait(false);
        }

        if (requiresPersistence)
        {
            await SaveSolutionInternalAsync().ConfigureAwait(false);
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
            await ApplySignalBoxElementChangeAsync(signal, requiresPersistence: false, requiresSignalCommand: true)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SetSignalAspectCommand");
        }
    }
}