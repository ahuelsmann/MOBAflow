// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Events;
using Common.Extension;
using Contracts.Runtime;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Service;

/// <summary>
/// MainWindowViewModel - runtime-backed Z21 connection, status and telemetry projection.
/// </summary>
public partial class MainWindowViewModel
{
    #region Z21 Telemetry
    private MobaRuntimeSnapshot _latestRuntimeSnapshot = MobaRuntimeSnapshot.Empty;

    /// <summary>
    /// Main current in mA (from Z21 SystemState).
    /// </summary>
    [ObservableProperty]
    public partial int MainCurrent { get; private set; }

    /// <summary>
    /// Temperature in Celsius (from Z21 SystemState).
    /// </summary>
    [ObservableProperty]
    public partial int Temperature { get; private set; }

    /// <summary>
    /// Supply voltage in mV (from Z21 SystemState).
    /// </summary>
    [ObservableProperty]
    public partial int SupplyVoltage { get; private set; }

    /// <summary>
    /// VCC voltage in mV (from Z21 SystemState).
    /// </summary>
    [ObservableProperty]
    public partial int VccVoltage { get; private set; }
    #endregion

    #region Z21 Connection Commands
    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        await _runtimeConnection.ConnectAsync().ConfigureAwait(false);
    }

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private async Task DisconnectAsync()
    {
        await _runtimeConnection.DisconnectAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task SimulateFeedback()
    {
        if (!int.TryParse(SimulateInPort, out var inPort))
        {
            StatusText = "Invalid InPort number";
            return;
        }

        await _runtimeCommandGateway.SimulateFeedbackAsync(inPort).ConfigureAwait(false);
    }

    [RelayCommand(CanExecute = nameof(CanToggleTrackPower))]
    private async Task SetTrackPowerAsync(bool turnOn)
    {
        await _runtimeCommandGateway.SetTrackPowerAsync(turnOn).ConfigureAwait(false);
    }

    private bool CanConnect() => !IsConnected;
    private bool CanDisconnect() => IsConnected;
    private bool CanToggleTrackPower() => IsOperationalControlEnabled;

    /// <summary>
    /// Refreshes Z21 status explicitly (called when window is activated).
    /// </summary>
    [RelayCommand]
    private void RefreshZ21Status()
    {
        // Trigger a status update request to the Z21
        if (IsConnected)
        {
            _runtimeConnection.RequestSystemStateAsync()
                .Observe(ex => _logger.LogWarning(ex, "Requesting Z21 system state failed"));
        }
    }
    #endregion

    #region Runtime Snapshot Projection
    private void OnRuntimeSnapshotChanged(RuntimeSnapshotChangedEvent e)
    {
        if (_isShuttingDown)
        {
            return;
        }

        ApplyRuntimeSnapshot(e.Snapshot);
    }

    private void ApplyRuntimeSnapshot(MobaRuntimeSnapshot snapshot)
    {
        if (_isShuttingDown)
        {
            return;
        }

        SuppressOperatingStateRecompute = true;
        try
        {
            _latestRuntimeSnapshot = snapshot;
            var status = RuntimeSnapshotProjector.ProjectStatus(snapshot);
            IsConnected = status.IsConnected;
            IsTrackPowerOn = status.IsTrackPowerOn;
            StatusText = status.StatusText;
            SerialNumber = status.SerialNumber;
            FirmwareVersion = status.FirmwareVersion;
            HardwareType = status.HardwareType;
            MainCurrent = status.MainCurrent;
            Temperature = status.Temperature;
            SupplyVoltage = status.SupplyVoltage;
            VccVoltage = status.VccVoltage;

            _isZ21Connecting = status.IsZ21Connecting;
            _hasSeenSuccessfulZ21Connection = status.HasSeenSuccessfulConnection;
            _isManualDisconnectRequested = status.IsManualDisconnectRequested;
            _isEmergencyStopActive = status.IsEmergencyStopActive;
            _isShortCircuitActive = status.IsShortCircuitActive;
            _isProgrammingModeActive = status.IsProgrammingModeActive;
            _lastFailSafeReason = status.LastFailSafeReason;
            _lastFailSafeAt = status.LastFailSafeAt;
            IsOperatorAckRequired = status.IsOperatorAckRequired;

            ApplyJourneyRuntimeSnapshots(snapshot.JourneyStates);

            if (SignalBoxRuntimeSync.ApplyToPlan(SelectedProject?.Model.SignalBoxPlan, snapshot.SignalBoxElements))
            {
                SignalBoxRuntimeStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        finally
        {
            SuppressOperatingStateRecompute = false;
        }

        NotifyRuntimeCommandStatesChanged();
        RecomputeOperatingState();
    }

    private void ApplyJourneyRuntimeSnapshots(IReadOnlyDictionary<Guid, JourneyRuntimeSnapshot> journeyStates)
    {
        var journeyViewModels = SolutionViewModel?.Projects.SelectMany(project => project.Journeys)
            ?? Enumerable.Empty<JourneyViewModel>();

        foreach (var journeyVm in journeyViewModels)
        {
            if (journeyStates.TryGetValue(journeyVm.Id, out var snapshot))
            {
                journeyVm.UpdateFromRuntimeSnapshot(snapshot);
            }
            else
            {
                journeyVm.ResetRuntimeState();
            }
        }
    }

    private void NotifyRuntimeCommandStatesChanged()
    {
        ConnectCommand.NotifyCanExecuteChanged();
        DisconnectCommand.NotifyCanExecuteChanged();
        SetTrackPowerCommand.NotifyCanExecuteChanged();
        AcknowledgeOperatingStateCommand.NotifyCanExecuteChanged();
    }
    #endregion
}
