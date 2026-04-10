// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Model;

using Common.Runtime;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
        _mobaClient.TrafficPacketLogged += OnTrafficPacketLogged;

        foreach (var packet in _mobaClient.GetTrafficPackets())
        {
            TrafficPackets.Add(packet);
        }
    }

    private void OnTrafficPacketLogged(object? sender, Z21TrafficPacket packet)
    {
        _ = sender;

        ExecuteOnUiWhenActive(() =>
        {
            TrafficPackets.Insert(0, packet);

            while (TrafficPackets.Count > 100)
            {
                TrafficPackets.RemoveAt(TrafficPackets.Count - 1);
            }
        });
    }

    [RelayCommand]
    private void ClearTrafficMonitor()
    {
        TrafficPackets.Clear();
        _mobaClient.ClearTrafficMonitor();
    }
    #endregion

    #region Z21 Connection Commands
    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        await _mobaClient.ConnectAsync().ConfigureAwait(false);
    }

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private async Task DisconnectAsync()
    {
        await _mobaClient.DisconnectAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task SimulateFeedback()
    {
        uint? selectedInPort = SelectedJourney?.InPort;

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

        await _mobaClient.SimulateFeedbackAsync(inPort).ConfigureAwait(false);
    }

    private bool CanResetJourney() => SelectedJourney != null;

    [RelayCommand(CanExecute = nameof(CanResetJourney))]
    private async Task ResetJourney()
    {
        if (SelectedJourney == null) return;

        SelectedJourney.ResetCommand.Execute(null);
        await _mobaClient.ResetJourneyAsync(SelectedJourney.Model.Id).ConfigureAwait(false);
    }

    [RelayCommand(CanExecute = nameof(CanToggleTrackPower))]
    private async Task SetTrackPowerAsync(bool turnOn)
    {
        await _mobaClient.SetTrackPowerAsync(turnOn).ConfigureAwait(false);
    }

    private bool CanConnect() => !IsConnected;
    private bool CanDisconnect() => IsConnected;
    private bool CanToggleTrackPower() => IsOperationalControlEnabled;
    #endregion

    #region Runtime Snapshot Projection
    private void OnMobaRuntimeSnapshotChanged(object? sender, MobaRuntimeSnapshot snapshot)
    {
        _ = sender;

        ExecuteOnUiWhenActive(() =>
        {
            ApplyRuntimeSnapshot(snapshot);
        });
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
            IsConnected = snapshot.IsConnected;
            IsTrackPowerOn = snapshot.IsTrackPowerOn;
            StatusText = snapshot.StatusText;
            SerialNumber = snapshot.SerialNumber;
            FirmwareVersion = snapshot.FirmwareVersion;
            HardwareType = snapshot.HardwareType;
            MainCurrent = snapshot.MainCurrent;
            Temperature = snapshot.Temperature;
            SupplyVoltage = snapshot.SupplyVoltage;
            VccVoltage = snapshot.VccVoltage;

            _isZ21Connecting = snapshot.IsZ21Connecting;
            _hasSeenSuccessfulZ21Connection = snapshot.HasSeenSuccessfulConnection;
            _isManualDisconnectRequested = snapshot.IsManualDisconnectRequested;
            _isEmergencyStopActive = snapshot.IsEmergencyStopActive;
            _isShortCircuitActive = snapshot.IsShortCircuitActive;
            _isProgrammingModeActive = snapshot.IsProgrammingModeActive;
            _lastFailSafeReason = snapshot.LastFailSafeReason;
            _lastFailSafeAt = snapshot.LastFailSafeAt;
            IsOperatorAckRequired = snapshot.IsOperatorAckRequired;

            ApplyJourneyRuntimeSnapshots(snapshot.JourneyStates);
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

    private void ExecuteOnUiWhenActive(System.Action action)
    {
        if (_isShuttingDown)
        {
            return;
        }

        _uiDispatcher.InvokeOnUi(() =>
        {
            if (_isShuttingDown)
            {
                return;
            }

            action();
        });
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
