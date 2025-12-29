// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend;
using Backend.Manager;
using Backend.Model;
using Backend.Protocol;
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
            // Unsubscribe from previous instance
            _workflowService.ActionExecutionError -= OnActionExecutionError;
            _journeyManager.Dispose();
        }

        _journeyManager = new JourneyManager(_z21, project, _workflowService, _executionContext);
        _journeyManager.StationChanged += OnJourneyStationChanged;
        _journeyManager.FeedbackReceived += OnJourneyFeedbackReceived;
        
        // ✅ Subscribe directly to WorkflowService (simplified event chain)
        _workflowService.ActionExecutionError += OnActionExecutionError;

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
    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        if (!string.IsNullOrEmpty(_settings.Z21.CurrentIpAddress))
        {
            try
            {
                StatusText = "Connecting...";
                var address = IPAddress.Parse(_settings.Z21.CurrentIpAddress);

                int port = 21105;
                if (!string.IsNullOrEmpty(_settings.Z21.DefaultPort) && int.TryParse(_settings.Z21.DefaultPort, out var parsedPort))
                {
                    port = parsedPort;
                }

                // Apply system state polling interval from settings before connecting
                _z21.SetSystemStatePollingInterval(_settings.Z21.SystemStatePollingIntervalSeconds);

                await _z21.ConnectAsync(address, port);

                // Note: IsConnected will be set when Z21 responds (via OnConnectedChanged event)
                StatusText = $"Waiting for Z21 at {_settings.Z21.CurrentIpAddress}:{port}...";
            }
            catch (Exception ex)
            {
                StatusText = $"Connection failed: {ex.Message}";
            }
        }
        else
        {
            StatusText = "No IP address configured in AppSettings";
        }
    }

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private async Task DisconnectAsync()
    {
        try
        {
            StatusText = "Disconnecting...";

            _journeyManager?.Dispose();
            _journeyManager = null;

            await _z21.DisconnectAsync();

            IsConnected = false;
            IsTrackPowerOn = false;
            StatusText = "Disconnected";

            ConnectCommand.NotifyCanExecuteChanged();
            SetTrackPowerCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
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
                StatusText = "Error: JourneyManager not initialized. Load a solution first.";
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
                StatusText = "Invalid InPort number";
                return;
            }

            _z21.SimulateFeedback(inPort);
            StatusText = $"Simulated feedback for InPort {inPort}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
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

            StatusText = $"Journey '{SelectedJourney.Name}' reset";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
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
                IsTrackPowerOn = true;
            }
            else
            {
                await _z21.SetTrackPowerOffAsync();
                IsTrackPowerOn = false;
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Track power error: {ex.Message}";
        }
    }

    private bool CanConnect() => !IsConnected;
    private bool CanDisconnect() => IsConnected;
    private bool CanToggleTrackPower() => IsConnected;
    
    /// <summary>
    /// Attempts to auto-connect to Z21 at startup.
    /// Non-blocking: returns immediately, connection status updated via OnConnectedChanged event.
    /// Starts a retry timer that attempts to reconnect every 10 seconds if Z21 is not reachable.
    /// </summary>
    private async Task TryAutoConnectToZ21Async()
    {
        if (string.IsNullOrEmpty(_settings.Z21.CurrentIpAddress))
        {
            StatusText = "No Z21 IP configured";
            Debug.WriteLine("⚠️ Z21 Auto-Connect: No IP address configured");
            return;
        }

        // Set initial status
        StatusText = $"Connecting to {_settings.Z21.CurrentIpAddress}...";
        
        // Initial connection attempt
        await AttemptZ21ConnectionAsync();
        
        // Start retry timer (checks periodically if not connected)
        var retryInterval = TimeSpan.FromSeconds(_settings.Z21.AutoConnectRetryIntervalSeconds);
        _z21AutoConnectTimer = new Timer(
            _ => { _ = AttemptZ21ConnectionIfDisconnectedAsync(); },
            null,
            retryInterval,  // First retry after configured interval
            retryInterval   // Subsequent retries at same interval
        );
        
        Debug.WriteLine($"🔄 Z21 Auto-Connect retry timer started ({_settings.Z21.AutoConnectRetryIntervalSeconds}s interval)");
    }
    
    /// <summary>
    /// Attempts to connect to Z21 only if currently disconnected.
    /// Called by retry timer.
    /// </summary>
    private async Task AttemptZ21ConnectionIfDisconnectedAsync()
    {
        if (IsConnected) return;  // Already connected, skip
        await AttemptZ21ConnectionAsync();
    }
    
    /// <summary>
    /// Performs a single connection attempt to Z21.
    /// </summary>
    private async Task AttemptZ21ConnectionAsync()
    {
        if (string.IsNullOrEmpty(_settings.Z21.CurrentIpAddress)) return;

        // Don't overwrite status if already connected
        if (IsConnected) return;

        try
        {
            StatusText = "Connecting to Z21...";
            var address = IPAddress.Parse(_settings.Z21.CurrentIpAddress);

            int port = 21105;
            if (!string.IsNullOrEmpty(_settings.Z21.DefaultPort) && int.TryParse(_settings.Z21.DefaultPort, out var parsedPort))
            {
                port = parsedPort;
            }

            Debug.WriteLine($"🔄 Z21 Auto-Connect: Attempting connection to {address}:{port}...");
            await _z21.ConnectAsync(address, port);
            
            // Note: Status will be updated by OnConnectedChanged event when Z21 responds
            // Don't set "Waiting for..." status here - it gets overwritten by the event!
        }
        catch (Exception ex)
        {
            StatusText = $"Z21 unavailable: {ex.Message}";
            Debug.WriteLine($"⚠️ Z21 Auto-Connect failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Handles Z21 connected state changes from the backend.
    /// Called when Z21 starts or stops responding.
    /// </summary>
    private void OnZ21ConnectedChanged(bool isConnected)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            IsConnected = isConnected;
            
            if (isConnected)
            {
                StatusText = $"Connected to {_settings.Z21.CurrentIpAddress}";
                Debug.WriteLine("✅ Z21 connection confirmed - Z21 is responding");
                
                // Initialize JourneyManager if needed
                if (_journeyManager == null)
                {
                    var firstProject = Solution.Projects.FirstOrDefault();
                    if (firstProject != null)
                    {
                        InitializeJourneyManager(firstProject);
                    }
                }
            }
            else
            {
                StatusText = "Z21 disconnected";
                Debug.WriteLine("❌ Z21 disconnected");
            }
            
            ConnectCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
            SetTrackPowerCommand.NotifyCanExecuteChanged();
        });
    }
    #endregion

    #region Z21 Event Handlers
    private void OnZ21SystemStateChanged(SystemState systemState)
    {
        _uiDispatcher.InvokeOnUi(() => UpdateZ21SystemState(systemState));
    }

    private void OnZ21XBusStatusChanged(XBusStatus xBusStatus)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            // XBusStatus.TrackOff is the OPPOSITE of IsTrackPowerOn
            IsTrackPowerOn = !xBusStatus.TrackOff;
            
            this.Log($"📊 XBus status updated: Track Power {(IsTrackPowerOn ? "ON" : "OFF")}, EmergencyStop={xBusStatus.EmergencyStop}, ShortCircuit={xBusStatus.ShortCircuit}");
        });
    }

    private void OnZ21VersionInfoChanged(Z21VersionInfo versionInfo)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            SerialNumber = versionInfo.SerialNumber.ToString();
            FirmwareVersion = versionInfo.FirmwareVersion;
            HardwareType = versionInfo.HardwareType;

            this.Log($"Z21 Version Info: S/N={SerialNumber}, HW={HardwareType}, FW={FirmwareVersion}");
        });
    }

    private void UpdateZ21SystemState(SystemState systemState)
    {
        // If we're receiving system state updates, we're connected
        if (!IsConnected)
        {
            IsConnected = true;
            ConnectCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
            SetTrackPowerCommand.NotifyCanExecuteChanged();
        }

        IsTrackPowerOn = systemState.IsTrackPowerOn;

        // Update System State properties (for OverviewPage/WebApp)
        MainCurrent = systemState.MainCurrent;
        Temperature = systemState.Temperature;
        SupplyVoltage = systemState.SupplyVoltage;
        VccVoltage = systemState.VccVoltage;

        // Only show warnings/special states in StatusText - normal state is just "Connected"
        var warnings = new List<string>();

        if (systemState.IsEmergencyStop)
            warnings.Add("EMERGENCY STOP");

        if (systemState.IsShortCircuit)
            warnings.Add("SHORT CIRCUIT");

        if (systemState.IsProgrammingMode)
            warnings.Add("Programming");

        StatusText = warnings.Count > 0 
            ? $"Connected | {string.Join(" | ", warnings)}" 
            : "Connected";

        this.Log($"Z21 System State: Track Power {(systemState.IsTrackPowerOn ? "ON" : "OFF")}, Current={systemState.MainCurrent}mA");
    }

    private void HandleConnectionLost()
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            IsConnected = false;
            IsTrackPowerOn = false;
            StatusText = "Connection lost - reconnect required";

            ConnectCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
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

