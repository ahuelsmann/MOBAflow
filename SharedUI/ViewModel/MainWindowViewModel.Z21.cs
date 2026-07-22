// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Events;
using Backend.Model;
using Common.Events;
using Common.Extension;
using Common.Runtime;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Service;
using System.Collections.ObjectModel;

/// <summary>
/// MainWindowViewModel - runtime-backed Z21 connection and control.
/// </summary>
public partial class MainWindowViewModel
{
    #region Z21 Traffic Monitor
    [ObservableProperty]
    private ObservableCollection<Z21TrafficPacket> _trafficPackets = [];

    private void InitializeTrafficMonitor()
    {
        _eventBusSubscriptions.Add(_eventBus.Subscribe<Z21TrafficPacketLoggedEvent>(OnTrafficPacketLogged));

        foreach (var packet in _mobaRuntime.GetTrafficPackets())
        {
            TrafficPackets.Add(packet);
        }
    }

    private void OnTrafficPacketLogged(Z21TrafficPacketLoggedEvent e)
    {
        var packet = e.Packet;

        if (_isShuttingDown)
        {
            return;
        }

        TrafficPackets.Insert(0, packet);

        while (TrafficPackets.Count > 100)
        {
            TrafficPackets.RemoveAt(TrafficPackets.Count - 1);
        }
    }

    [RelayCommand]
    private void ClearTrafficMonitor()
    {
        TrafficPackets.Clear();
        _mobaRuntime.ClearTrafficMonitor();
    }
    #endregion

    #region Z21 Connection Commands
    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        await _mobaRuntime.ConnectAsync().ConfigureAwait(false);
    }

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private async Task DisconnectAsync()
    {
        await _mobaRuntime.DisconnectAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task SimulateFeedback()
    {
        uint? selectedInPort = SelectedJourney?.NextFeedbackInPort;

        int inPort;
        if (selectedInPort.HasValue)
        {
            inPort = unchecked((int)selectedInPort.Value);
        }
        else if (!int.TryParse(SimulateInPort, out inPort))
        {
            StatusText = "Invalid InPort number";
            return;
        }

        await _runtimeCommandGateway.SimulateFeedbackAsync(inPort).ConfigureAwait(false);
    }

    private bool CanResetJourney() => SelectedJourney != null;

    [RelayCommand(CanExecute = nameof(CanResetJourney))]
    private async Task ResetJourney()
    {
        if (SelectedJourney == null) return;

        SelectedJourney.ResetCommand.Execute(null);
        await _runtimeCommandGateway.ResetJourneyAsync(SelectedJourney.Model.Id).ConfigureAwait(false);
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
            _mobaRuntime.RequestSystemStateAsync()
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
        ResetJourneyCommand.NotifyCanExecuteChanged();
        ResetJourneyCounterCommand.NotifyCanExecuteChanged();
        AcknowledgeOperatingStateCommand.NotifyCanExecuteChanged();
    }
    #endregion
}
