// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Domain;

using Interface;

using Manager;

using Microsoft.Extensions.Logging;

using Model;

/// <summary>
/// Public <see cref="IMobaRuntime"/> command and query surface for <see cref="MobaRuntimeService"/>.
/// </summary>
public sealed partial class MobaRuntimeService
{
    /// <inheritdoc />
    public Task ActivateProjectAsync(Project editableProject, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(editableProject);

        var activeProject = editableProject;
        var journeyManager = new JourneyManager(_z21, activeProject, _workflowService, _executionContextFactory.Create());
        journeyManager.StationChanged += OnJourneyRuntimeChanged;
        journeyManager.FeedbackReceived += OnJourneyRuntimeChanged;

        var nextContext = new ActiveProjectContext(activeProject, journeyManager);
        ReplaceActiveProjectContext(nextContext);

        _logger.LogInformation(
            "Activated project '{ProjectName}' for runtime with {JourneyCount} journeys",
            activeProject.Name,
            activeProject.Journeys.Count);

        PublishSnapshot();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (!TryGetConfiguredEndpoint(out var address, out var port, out var errorMessage))
        {
            _isZ21Connecting = false;
            _statusText = errorMessage;
            PublishSnapshot();
            return;
        }

        try
        {
            _isZ21Connecting = true;
            _isManualDisconnectRequested = false;
            _statusText = "Connecting...";
            PublishSnapshot();

            _z21.SetSystemStatePollingInterval(_settings.Z21.SystemStatePollingIntervalSeconds);
            await _z21.ConnectAsync(address!, port, cancellationToken).ConfigureAwait(false);

            if (!_isConnected)
            {
                _statusText = $"Waiting for Z21 at {_settings.Z21.CurrentIpAddress}:{port}...";
            }

            PublishSnapshot();
        }
        catch (Exception ex)
        {
            _isZ21Connecting = false;
            _statusText = $"Connection failed: {ex.Message}";
            PublishSnapshot();
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _isManualDisconnectRequested = true;
            _isZ21Connecting = false;
            _isOperatorAckRequired = false;
            _statusText = "Disconnecting...";
            PublishSnapshot();

            await _z21.DisconnectAsync().ConfigureAwait(false);

            _isConnected = false;
            _isTrackPowerOn = false;
            _statusText = "Disconnected";
            PublishSnapshot();
        }
        catch (Exception ex)
        {
            _statusText = $"Error: {ex.Message}";
            PublishSnapshot();
        }
    }

    /// <inheritdoc />
    public async Task SetTrackPowerAsync(bool isOn, CancellationToken cancellationToken = default)
    {
        try
        {
            if (isOn)
            {
                await _z21.SetTrackPowerOnAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _z21.SetTrackPowerOffAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _statusText = $"Track power error: {ex.Message}";
            PublishSnapshot();
        }
    }

    /// <inheritdoc />
    public void SetSystemStatePollingInterval(int intervalSeconds)
    {
        _settings.Z21.SystemStatePollingIntervalSeconds = Math.Max(intervalSeconds, 0);
        _z21.SetSystemStatePollingInterval(_settings.Z21.SystemStatePollingIntervalSeconds);
    }

    /// <inheritdoc />
    public async Task SetLocomotiveDriveAsync(int address, int speed, bool forward, CancellationToken cancellationToken = default)
    {
        await _z21.SetLocoDriveAsync(address, speed, forward, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetLocomotiveFunctionAsync(int address, int functionIndex, bool isOn, CancellationToken cancellationToken = default)
    {
        await _z21.SetLocoFunctionAsync(address, functionIndex, isOn, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetAllLocomotiveFunctionsOffAsync(int address, CancellationToken cancellationToken = default)
    {
        await _z21.SetAllLocoFunctionsOffAsync(address, cancellationToken).ConfigureAwait(false);

        // Reflect the commanded all-off state in the runtime model so consumers (and the next
        // snapshot) see Functions=0 immediately, instead of stale decoder bits from earlier info.
        var existingState = _locomotiveStates.TryGetValue(address, out var current) ? current : null;
        _locomotiveStates[address] = new Common.Runtime.LocomotiveRuntimeSnapshot
        {
            Address = address,
            Speed = existingState?.Speed ?? 0,
            IsForward = existingState?.IsForward ?? true,
            Functions = 0
        };

        PublishSnapshot();
    }

    /// <inheritdoc />
    public async Task RequestLocomotiveInfoAsync(int address, CancellationToken cancellationToken = default)
    {
        await _z21.GetLocoInfoAsync(address, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task AcknowledgeFailSafeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_isConnected)
        {
            return Task.CompletedTask;
        }

        _isOperatorAckRequired = false;
        _lastFailSafeReason = "Operator released the system for normal operation.";
        PublishSnapshot();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SimulateFeedbackAsync(int inPort, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_activeProjectContext == null)
        {
            _statusText = "Error: No active project. Load or select a project first.";
            PublishSnapshot();
            return Task.CompletedTask;
        }

        try
        {
            _z21.SimulateFeedback(inPort);
            _statusText = $"Simulated feedback for InPort {inPort}";
        }
        catch (Exception ex)
        {
            _statusText = $"Error: {ex.Message}";
        }

        PublishSnapshot();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ResetJourneyAsync(Guid journeyId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_activeProjectContext == null)
        {
            return Task.CompletedTask;
        }

        var journey = _activeProjectContext.ActiveProject.Journeys.FirstOrDefault(j => j.Id == journeyId);
        if (journey == null)
        {
            return Task.CompletedTask;
        }

        _activeProjectContext.JourneyManager.Reset(journey);
        _statusText = $"Journey '{journey.Name}' reset";
        PublishSnapshot();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SetSignalAspectAsync(SbSignal signal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signal);

        if (!signal.IsMultiplexed)
        {
            _logger.LogWarning(
                "Signal '{SignalName}' (ID: {SignalId}) is not marked as multiplexed. Configure IsMultiplexed=true.",
                signal.Name,
                signal.Id.ToString()[..8]);
            return;
        }

        if (string.IsNullOrEmpty(signal.MultiplexerArticleNumber))
        {
            _logger.LogWarning("Signal '{SignalName}': Multiplexer article number not configured.", signal.Name);
            return;
        }

        if (!_z21.IsConnected)
        {
            _logger.LogWarning("Signal '{SignalName}': Z21 not connected; skipping command send.", signal.Name);
            return;
        }

        try
        {
            var command = MultiplexerCommandResolver.Resolve(
                signal.BaseAddress,
                signal.MultiplexerArticleNumber,
                signal.MainSignalArticleNumber,
                signal.SignalAspect,
                _settings.SignalBox);

            // Warn if any signal aspect resolves to a pure deactivate command before optional polarity inversion.
            if (!command.OriginalActivate)
            {
                _logger.LogWarning(
                    "Signal '{SignalName}': Aspect {Aspect} mapped to Activate=false. " +
                    "This typically does not switch the Viessmann multiplexer; verify hardware behavior.",
                    signal.Name,
                    signal.SignalAspect);
            }

            await _z21.SetTurnoutAsync(
                    command.DccAddress,
                    command.Output,
                    command.Activate,
                    false,
                    cancellationToken)
                .ConfigureAwait(false);

            _statusText = $"Signal '{signal.Name}' gestellt: DCC-Adresse {command.DccAddress}, Ausgang {command.Output}, Activate={command.Activate}";
            PublishSnapshot();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Signal '{SignalName}': Multiplexer command could not be resolved.", signal.Name);
        }
        catch (Exception ex)
        {
            _statusText = $"❌ Signal-Fehler: {ex.Message}";
            PublishSnapshot();
            _logger.LogError(ex, "Failed to set signal aspect for '{SignalName}'", signal.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SendTurnoutCommandAsync(int decoderAddress, int output, bool activate, bool queue = false, CancellationToken cancellationToken = default)
    {
        if (!_z21.IsConnected)
        {
            _logger.LogWarning("Raw turnout command skipped because Z21 is not connected");
            _statusText = "⚠️ Z21 nicht verbunden";
            PublishSnapshot();
            return;
        }

        await _z21.SetTurnoutAsync(decoderAddress, output, activate, queue, cancellationToken).ConfigureAwait(false);
        _statusText = $"Turnout gestellt: DCC-Adresse {decoderAddress}, Ausgang {output}, Activate={activate}, Queue={queue}";
        PublishSnapshot();
    }

    /// <inheritdoc />
    public IReadOnlyList<Z21TrafficPacket> GetTrafficPackets()
    {
        return [.. (_z21.TrafficMonitor?.GetPackets() ?? Enumerable.Empty<Z21TrafficPacket>())];
    }

    /// <inheritdoc />
    public void ClearTrafficMonitor()
    {
        _z21.TrafficMonitor?.Clear();
    }

    /// <inheritdoc />
    public async Task RequestSystemStateAsync(CancellationToken cancellationToken = default)
    {
        if (_z21.IsConnected)
        {
            // Trigger a status request - this will update the snapshot via events
            await _z21.GetStatusAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
