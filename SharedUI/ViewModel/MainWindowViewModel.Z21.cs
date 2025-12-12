// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Moba.Backend.Manager;
using Moba.Backend.Model;
using Moba.Common.Extensions;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

/// <summary>
/// MainWindowViewModel - Z21 Connection and Control
/// Handles Z21 hardware connection, track power, feedback simulation, and system state updates.
/// </summary>
public partial class MainWindowViewModel
{
    #region Z21 Traffic Monitor
    [ObservableProperty]
    private ObservableCollection<Z21TrafficPacket> trafficPackets = [];

    private void InitializeTrafficMonitor()
    {
        if (_z21?.TrafficMonitor != null)
        {
            _z21.TrafficMonitor.PacketLogged += OnTrafficPacketLogged;

            // Load existing packets (if any)
            var existingPackets = _z21.TrafficMonitor.GetPackets();
            foreach (var packet in existingPackets)
            {
                TrafficPackets.Add(packet);
            }
        }
    }

    private void OnTrafficPacketLogged(object? sender, Z21TrafficPacket packet)
    {
        // Ensure UI updates happen on UI thread
        _uiDispatcher.InvokeOnUi(() =>
        {
            TrafficPackets.Insert(0, packet); // Add to top (newest first)

            // Keep only last 100 packets in UI
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
        _z21?.TrafficMonitor?.Clear();
    }
    #endregion

    #region Z21 Connection Commands
    [RelayCommand(CanExecute = nameof(CanConnectToZ21))]
    private async Task ConnectToZ21Async()
    {
        if (!string.IsNullOrEmpty(_settings.Z21.CurrentIpAddress))
        {
            try
            {
                Z21StatusText = "Connecting...";
                var address = IPAddress.Parse(_settings.Z21.CurrentIpAddress);

                int port = 21105;
                if (!string.IsNullOrEmpty(_settings.Z21.DefaultPort) && int.TryParse(_settings.Z21.DefaultPort, out var parsedPort))
                {
                    port = parsedPort;
                }

                await _z21.ConnectAsync(address, port);

                // Initialize Traffic Monitor
                InitializeTrafficMonitor();

                IsZ21Connected = true;
                Z21StatusText = $"Connected to {_settings.Z21.CurrentIpAddress}:{port}";

                if (Solution.Projects.Count > 0)
                {
                    var project = Solution.Projects[0];
                    var executionContext = new Backend.Services.ActionExecutionContext
                    {
                        Z21 = _z21
                    };

                    _journeyManager?.Dispose();
                    _journeyManager = new JourneyManager(_z21, project, _workflowService, executionContext);
                }

                ConnectToZ21Command.NotifyCanExecuteChanged();
                DisconnectFromZ21Command.NotifyCanExecuteChanged();
                SetTrackPowerCommand.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                Z21StatusText = $"Connection failed: {ex.Message}";
            }
        }
        else
        {
            Z21StatusText = "No IP address configured in AppSettings";
        }
    }

    [RelayCommand(CanExecute = nameof(CanDisconnectFromZ21))]
    private async Task DisconnectFromZ21Async()
    {
        try
        {
            Z21StatusText = "Disconnecting...";

            _journeyManager?.Dispose();
            _journeyManager = null;

            await _z21.DisconnectAsync();

            IsZ21Connected = false;
            IsTrackPowerOn = false;
            Z21StatusText = "Disconnected";

            ConnectToZ21Command.NotifyCanExecuteChanged();
            SetTrackPowerCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            Z21StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SimulateFeedback()
    {
        try
        {
            // Create JourneyManager if needed for simulation
            if (_journeyManager == null && Solution?.Projects.Count > 0)
            {
                var project = Solution.Projects[0];
                
                Debug.WriteLine($"🔍 [DEBUG] Creating ExecutionContext for JourneyManager");
                
                var executionContext = new Backend.Services.ActionExecutionContext
                {
                    Z21 = _z21,
                    SpeakerEngine = null  // Will be set via MainWindowViewModel.SpeakerEngine property
                };
                
                Debug.WriteLine($"   - ExecutionContext.SpeakerEngine: {executionContext.SpeakerEngine?.Name ?? "NULL (not implemented yet)"}");
                
                _journeyManager = new JourneyManager(_z21, project, _workflowService, executionContext);
                
                Debug.WriteLine($"✅ [DEBUG] JourneyManager created");
            }

            // Get InPort from selected journey or text field
            uint? selectedInPort = SelectedJourney?.InPort;

            int inPort;
            if (selectedInPort.HasValue)
            {
                inPort = unchecked((int)selectedInPort.Value);
            }
            else if (!int.TryParse(SimulateInPort, out inPort))
            {
                Z21StatusText = "Invalid InPort number";
                return;
            }

            _z21.SimulateFeedback(inPort);
            Z21StatusText = $"Simulated feedback for InPort {inPort}";
        }
        catch (Exception ex)
        {
            Z21StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanToggleTrackPower))]
    private async Task SetTrackPowerAsync(bool turnOn)
    {
        try
        {
            if (turnOn)
            {
                await _z21.SetTrackPowerOnAsync();
                Z21StatusText = "Track power ON";
                IsTrackPowerOn = true;
            }
            else
            {
                await _z21.SetTrackPowerOffAsync();
                Z21StatusText = "Track power OFF";
                IsTrackPowerOn = false;
            }
        }
        catch (Exception ex)
        {
            Z21StatusText = $"Track power error: {ex.Message}";
        }
    }

    private bool CanConnectToZ21() => !IsZ21Connected;
    private bool CanDisconnectFromZ21() => IsZ21Connected;
    private bool CanToggleTrackPower() => IsZ21Connected;
    #endregion

    #region Z21 Event Handlers
    private void OnZ21SystemStateChanged(Backend.SystemState systemState)
    {
        _uiDispatcher.InvokeOnUi(() => UpdateZ21SystemState(systemState));
    }

    private void OnZ21VersionInfoChanged(Backend.Z21VersionInfo versionInfo)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            Z21SerialNumber = versionInfo.SerialNumber.ToString();
            Z21FirmwareVersion = versionInfo.FirmwareVersion;
            Z21HardwareType = versionInfo.HardwareType;
            Z21HardwareVersion = versionInfo.HardwareVersion.ToString();

            this.Log($"Z21 Version Info: S/N={Z21SerialNumber}, HW={Z21HardwareType}, FW={Z21FirmwareVersion}");
        });
    }

    private void UpdateZ21SystemState(Backend.SystemState systemState)
    {
        // If we're receiving system state updates, we're connected
        if (!IsZ21Connected)
        {
            IsZ21Connected = true;
            ConnectToZ21Command.NotifyCanExecuteChanged();
            DisconnectFromZ21Command.NotifyCanExecuteChanged();
            SetTrackPowerCommand.NotifyCanExecuteChanged();
        }

        IsTrackPowerOn = systemState.IsTrackPowerOn;

        var statusParts = new List<string>
        {
            "Connected",
            $"Current: {systemState.MainCurrent}mA",
            $"Temp: {systemState.Temperature}C"
        };

        if (systemState.IsEmergencyStop)
            statusParts.Add("WARNING: EMERGENCY STOP");

        if (systemState.IsShortCircuit)
            statusParts.Add("WARNING: SHORT CIRCUIT");

        if (systemState.IsProgrammingMode)
            statusParts.Add("Programming");

        Z21StatusText = string.Join(" | ", statusParts);

        this.Log($"Z21 System State: TrackPower={systemState.IsTrackPowerOn}, Current={systemState.MainCurrent}mA");
    }

    private void HandleConnectionLost()
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            IsZ21Connected = false;
            IsTrackPowerOn = false;
            Z21StatusText = "Connection lost - reconnect required";

            ConnectToZ21Command.NotifyCanExecuteChanged();
            DisconnectFromZ21Command.NotifyCanExecuteChanged();
            SetTrackPowerCommand.NotifyCanExecuteChanged();
        });
    }
    #endregion
}
