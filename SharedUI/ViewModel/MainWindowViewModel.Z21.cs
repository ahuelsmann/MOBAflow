// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend;
using Backend.Manager;
using Backend.Model;
using Backend.Service;

using Common.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;

/// <summary>
/// MainWindowViewModel - Z21 Connection and Control
/// Handles Z21 hardware connection, track power, feedback simulation, and system state updates.
/// </summary>
public partial class MainWindowViewModel
{
    #region JourneyManager Initialization
    /// <summary>
    /// Initializes the JourneyManager for the given project.
    /// Can be called at any time (with or without Z21 connection).
    /// </summary>
    private void InitializeJourneyManager(Domain.Project project)
    {
        if (_journeyManager != null)
        {
            _journeyManager.Dispose();
        }

        var executionContext = new ActionExecutionContext
        {
            Z21 = _z21
        };

        _journeyManager = new JourneyManager(_z21, project, _workflowService, executionContext);
        _journeyManager.StationChanged += OnJourneyStationChanged;
        _journeyManager.FeedbackReceived += OnJourneyFeedbackReceived;

        Debug.WriteLine($"✅ JourneyManager initialized for project '{project.Name}' with {project.Journeys.Count} journeys");
    }
    #endregion

    #region Z21 Traffic Monitor
    [ObservableProperty]
    private ObservableCollection<Z21TrafficPacket> trafficPackets = [];

    private void InitializeTrafficMonitor()
    {
        if (_z21.TrafficMonitor != null)
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
        _z21.TrafficMonitor?.Clear();
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

                // Traffic Monitor already initialized in constructor

                IsZ21Connected = true;
                Z21StatusText = $"Connected to {_settings.Z21.CurrentIpAddress}:{port}";

                // JourneyManager already initialized from Solution load
                // No need to recreate it - just keep using the existing one
                if (_journeyManager == null && Solution.Projects.Count > 0)
                {
                    // Fallback: if somehow not initialized, initialize now
                    InitializeJourneyManager(Solution.Projects[0]);
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
            // JourneyManager should already be initialized when solution loads
            // If not, something went wrong - log and return
            if (_journeyManager == null)
            {
                Z21StatusText = "Error: JourneyManager not initialized. Load a solution first.";
                Debug.WriteLine("❌ SimulateFeedback: JourneyManager is null");
                return;
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

    private bool CanResetJourney() => SelectedJourney != null;

    [RelayCommand(CanExecute = nameof(CanResetJourney))]
    private void ResetJourney()
    {
        if (SelectedJourney == null) return;

        try
        {
            // Delegate to JourneyViewModel.ResetCommand
            SelectedJourney.ResetCommand.Execute(null);

            // Also reset in JourneyManager if available
            if (_journeyManager != null)
            {
                _journeyManager.Reset(SelectedJourney.Model);
            }

            Z21StatusText = $"Journey '{SelectedJourney.Name}' reset";
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
    private void OnZ21SystemStateChanged(SystemState systemState)
    {
        _uiDispatcher.InvokeOnUi(() => UpdateZ21SystemState(systemState));
    }

    private void OnZ21VersionInfoChanged(Z21VersionInfo versionInfo)
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

    private void UpdateZ21SystemState(SystemState systemState)
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

    /// <summary>
    /// Handles JourneyManager.StationChanged events and updates the corresponding JourneyViewModel.
    /// This bridges the gap between JourneyManager (Backend) and JourneyViewModel (UI).
    /// </summary>
    private void OnJourneyStationChanged(object? sender, StationChangedEventArgs e)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            // Find the JourneyViewModel in the ENTIRE solution, not just selected project
            // This ensures updates occur even if the journey isn't in the selected project
            var journeyVM = SolutionViewModel?.Projects
                .SelectMany(p => p.Journeys)
                .FirstOrDefault(j => j.Id == e.JourneyId);
            
            if (journeyVM != null)
            {
                // Manually notify property changes since the ViewModel's SessionState 
                // is a dummy one and doesn't receive updates from JourneyManager
                journeyVM.UpdateFromSessionState(e.SessionState);
                
                // Update IsCurrentStation for all stations in this journey
                // This highlights the current station in the UI
                foreach (var stationVM in journeyVM.Stations)
                {
                    stationVM.IsCurrentStation = stationVM.Model.Id == e.Station.Id;
                }
                
                Debug.WriteLine($"📊 JourneyViewModel '{journeyVM.Name}' updated: Counter={e.SessionState.Counter}, Pos={e.SessionState.CurrentPos}, Station={e.SessionState.CurrentStationName}");
            }
            else
            {
                Debug.WriteLine($"⚠️ Journey {e.JourneyId} not found in any project for station change");
            }
        });
    }

    /// <summary>
    /// Handles JourneyManager.FeedbackReceived events and updates the corresponding JourneyViewModel counter.
    /// Fired on every feedback, not just when a station is reached.
    /// </summary>
    private void OnJourneyFeedbackReceived(object? sender, JourneyFeedbackEventArgs e)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            // Find the JourneyViewModel in the ENTIRE solution, not just selected project
            // This ensures counters update even if the journey isn't in the selected project
            var journeyVM = SolutionViewModel?.Projects
                .SelectMany(p => p.Journeys)
                .FirstOrDefault(j => j.Id == e.JourneyId);
            
            if (journeyVM != null)
            {
                // Update counter in UI
                journeyVM.UpdateFromSessionState(e.SessionState);
                
                Debug.WriteLine($"🔔 JourneyViewModel '{journeyVM.Name}' feedback: Counter={e.SessionState.Counter}");
            }
            else
            {
                Debug.WriteLine($"⚠️ Journey {e.JourneyId} not found in any project");
            }
        });
    }
    #endregion
}