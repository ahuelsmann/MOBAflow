// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Moba.Backend.Manager;
using Moba.Domain;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Moba.Backend.Interface;
using Moba.Common.Extensions;

using Service;

using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IIoService _ioService;
    private readonly IZ21 _z21;
    private readonly IJourneyManagerFactory _journeyManagerFactory;
    private readonly TreeViewBuilder _treeViewBuilder;
    private readonly IUiDispatcher _uiDispatcher;
    private JourneyManager? _journeyManager;

    // Primary ctor for DI
    public MainWindowViewModel(
        IIoService ioService,
        IZ21 z21,
        IJourneyManagerFactory journeyManagerFactory,
        TreeViewBuilder treeViewBuilder,
        IUiDispatcher uiDispatcher,
        Solution solution)
    {
        _ioService = ioService;
        _z21 = z21;
        _journeyManagerFactory = journeyManagerFactory;
        _treeViewBuilder = treeViewBuilder;
        _uiDispatcher = uiDispatcher;

        // Subscribe to Z21 events
        _z21.OnSystemStateChanged += OnZ21SystemStateChanged;

        // Use injected Solution from DI container
        // This allows for better testability and centralized configuration
        Solution = solution;
    }

    /// <summary>
    /// Application window title.
    /// </summary>
    [ObservableProperty]
    private string title = "MOBAflow";

    /// <summary>
    /// Current path to the loaded solution file (null if no solution is loaded).
    /// </summary>
    [ObservableProperty]
    private string? currentSolutionPath;

    /// <summary>
    /// Indicates whether a valid solution with at least one project is loaded.
    /// </summary>
    [ObservableProperty]
    private bool hasSolution;

    /// <summary>
    /// The currently loaded solution containing all projects, journeys, and configuration.
    /// </summary>
    [ObservableProperty]
    private Solution solution = null!; // Initialized in constructor

    /// <summary>
    /// ViewModel wrapper for Solution that provides hierarchical collections for TreeView binding.
    /// </summary>
    [ObservableProperty]
    private SolutionViewModel? solutionViewModel;

    [ObservableProperty]
    private bool hasUnsavedChanges = false;

    /// <summary>
    /// Tree view nodes representing the solution structure (projects, journeys, stations, etc.).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<TreeNodeViewModel> treeNodes = [];

    /// <summary>
    /// Property grid items for the currently selected tree node.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<PropertyViewModel> properties = [];

    /// <summary>
    /// Type name of the currently selected tree node (e.g., "JourneyViewModel", "StationViewModel").
    /// </summary>
    [ObservableProperty]
    private string selectedNodeType = string.Empty;

    /// <summary>
    /// The currently selected node in the TreeView.
    /// This is the root selection property that all views observe.
    /// </summary>
    [ObservableProperty]
    private TreeNodeViewModel? currentSelectedNode;
    
    /// <summary>
    /// Currently selected Journey across all views (Explorer, Editor, Configuration).
    /// Synchronized when TreeView selection changes to a Journey node.
    /// </summary>
    [ObservableProperty]
    private JourneyViewModel? selectedJourney;
    
    /// <summary>
    /// Currently selected Station across all views.
    /// Synchronized when TreeView selection changes to a Station node.
    /// </summary>
    [ObservableProperty]
    private StationViewModel? selectedStation;
    
    /// <summary>
    /// Currently selected Workflow across all views.
    /// Synchronized when TreeView selection changes to a Workflow node.
    /// </summary>
    [ObservableProperty]
    private WorkflowViewModel? selectedWorkflow;
    
    /// <summary>
    /// Currently selected Train across all views.
    /// Synchronized when TreeView selection changes to a Train node.
    /// </summary>
    [ObservableProperty]
    private TrainViewModel? selectedTrain;

    /// <summary>
    /// Indicates whether the Z21 is currently connected.
    /// </summary]
    [ObservableProperty]
    private bool isZ21Connected;

    /// <summary>
    /// Indicates whether the track power is currently ON.
    /// </summary>
    [ObservableProperty]
    private bool isTrackPowerOn;

    /// <summary>
    /// Current Z21 connection status text (e.g., "Connected to 192.168.0.111", "Disconnected").
    /// </summary]
    [ObservableProperty]
    private string z21StatusText = "Disconnected";

    /// <summary>
    /// InPort number for simulation (testing Z21 feedback without physical hardware).
    /// </summary>
    [ObservableProperty]
    private string simulateInPort = "1";

    /// <summary>
    /// Available cities with stations (loaded from master data).
    /// </summary]
    [ObservableProperty]
    private ObservableCollection<Moba.Domain.City> availableCities = [];

    /// <summary>
    /// Currently selected city for adding stations to journeys.
    /// </summary]
    [ObservableProperty]
    private Backend.Data.City? selectedCity;

    public event EventHandler? ExitApplicationRequested;

    partial void OnSolutionChanged(Solution? value)
    {
        // ✅ NULL-Check: value könnte während Initialization null sein
        if (value == null)
        {
            HasSolution = false;
            TreeNodes.Clear();
            Properties.Clear();
            AvailableCities.Clear();
            SolutionViewModel = null;
            return;
        }

        // Ensure Solution always has at least one project
        if (value.Projects.Count == 0)
        {
            value.Projects.Add(new Project { Name = "(Untitled Project)" });
            System.Diagnostics.Debug.WriteLine("⚠️ Solution had no projects, added empty project");
        }

        // ✅ Create SolutionViewModel wrapper for hierarchical TreeView binding
        SolutionViewModel = new SolutionViewModel(value, _uiDispatcher);

        HasSolution = value.Projects.Count > 0;
        SaveSolutionCommand.NotifyCanExecuteChanged();
        ConnectToZ21Command.NotifyCanExecuteChanged();
        BuildTreeView();
        LoadCities();
    }

    private void LoadCities()
    {
        AvailableCities.Clear();

        // ✅ NULL-Check: Solution könnte während des Initialisierungsprozesses noch null sein
        if (Solution?.Projects?.Count > 0)
        {
            var firstProject = Solution.Projects[0];
            if (firstProject?.Cities != null)
            {
                foreach (var city in firstProject.Cities)
                {
                    AvailableCities.Add(city);
                }
            }
        }
    }

    [RelayCommand]
    private async Task LoadCitiesFromFileAsync()
    {
        if (Solution.Projects.Count == 0) return;

        var (dataManager, _, error) = await _ioService.LoadDataManagerAsync();

        if (dataManager != null && Solution.Projects.Count > 0)
        {
            var firstProject = Solution.Projects[0];
            firstProject.Cities.Clear();
            // Note: City loading temporarily disabled due to Backend.Data.City vs Domain.City mismatch
            // TODO: Migrate CityDataManager to use Domain.City or create converter
            // firstProject.Cities.AddRange(dataManager.Cities);
            LoadCities();
        }
        else if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException($"Failed to load cities: {error}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddStationToJourney))]
    private void AddStationToJourney()
    {
        if (SelectedCity == null || CurrentSelectedNode?.DataContext is not JourneyViewModel journeyVM)
            return;

        // Take the first station from the selected city
        if (SelectedCity.Stations.Count > 0)
        {
            var stationToCopy = SelectedCity.Stations[0];

            // Create a new Station instance (deep copy)
            var newStation = new Station
            {
                Name = stationToCopy.Name,
                Description = stationToCopy.Description,
                NumberOfLapsToStop = 2,
                FeedbackInPort = stationToCopy.FeedbackInPort
            };

            // Add to Journey model
            journeyVM.Model.Stations.Add(newStation);

            // Add to ViewModel
            var stationVM = new StationViewModel(newStation);
            journeyVM.Stations.Add(stationVM);

            // Add to TreeView without rebuilding entire tree
            var stationNode = new TreeNodeViewModel
            {
                DisplayName = stationVM.Name,
                Icon = "\uE80F", // Location icon
                DataContext = stationVM,
                DataType = typeof(StationViewModel)
            };

            // Find the Journey node in the tree and add the station node
            if (CurrentSelectedNode != null)
            {
                CurrentSelectedNode.Children.Add(stationNode);
            }

            // ✅ Note: JourneyViewModel.Stations already updated, no SolutionViewModel refresh needed
            // TreeView auto-updates through TreeNode.Children.Add()

            HasUnsavedChanges = true;
        }
    }

    private bool CanAddStationToJourney()
    {
        return SelectedCity != null && CurrentSelectedNode?.DataContext is JourneyViewModel;
    }

    partial void OnSelectedCityChanged(Backend.Data.City? value)
    {
        AddStationToJourneyCommand.NotifyCanExecuteChanged();
    }

    partial void OnCurrentSelectedNodeChanged(TreeNodeViewModel? value)
    {
        AddStationToJourneyCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task LoadSolutionAsync()
    {
        System.Diagnostics.Debug.WriteLine("📂 LoadSolutionAsync START");
        (Solution? loadedSolution, string? path, string? error) = await _ioService.LoadAsync();

        System.Diagnostics.Debug.WriteLine($"LoadAsync returned: solution={loadedSolution != null}, path={path}, error={error}");

        if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException($"Failed to load solution with error {error}");
        }
        if (loadedSolution != null)
        {
            System.Diagnostics.Debug.WriteLine($"✅ Updating Solution with {loadedSolution.Projects.Count} projects");

            // ✅ CRITICAL: Update the existing Solution instance instead of replacing it
            // This ensures all ViewModels that have a reference to Solution see the changes
            MergeSolution(loadedSolution);

            // ✅ Refresh SolutionViewModel to sync with model changes
            SolutionViewModel?.Refresh();

            // Update current path and mark as saved
            CurrentSolutionPath = path;
            HasUnsavedChanges = false;

            // ✅ Manually trigger the same logic as OnSolutionChanged
            // because the Solution reference didn't change (only its content)
            HasSolution = Solution.Projects.Count > 0;
            SaveSolutionCommand.NotifyCanExecuteChanged();
            ConnectToZ21Command.NotifyCanExecuteChanged();
            BuildTreeView();
            LoadCities();

            // Notify that Solution content has changed (for EditorPageViewModel)
            OnPropertyChanged(nameof(Solution));

            System.Diagnostics.Debug.WriteLine($"✅ Solution updated. HasSolution={HasSolution}, Projects={Solution?.Projects.Count}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ loadedSolution is null - user cancelled or error");
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveSolution))]
    private async Task SaveSolutionAsync()
    {
        var (success, path, error) = await _ioService.SaveAsync(Solution, CurrentSolutionPath);
        if (success && path != null)
        {
            CurrentSolutionPath = path;
            HasUnsavedChanges = false;
        }
        else if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException($"Failed to save solution with error {error}");
        }
    }

    [RelayCommand]
    private void AddProject()
    {
        Solution.Projects.Add(new Project());
        
        // ✅ Refresh SolutionViewModel to sync new project
        SolutionViewModel?.Refresh();
        
        BuildTreeView();
        HasUnsavedChanges = true;
    }

    [RelayCommand(CanExecute = nameof(CanConnectToZ21))]
    private async Task ConnectToZ21Async()
    {
        // ✅ Use IP and Port from Solution.Settings
        if (Solution?.Settings != null && !string.IsNullOrEmpty(Solution.Settings.CurrentIpAddress))
        {
            try
            {
                Z21StatusText = "Connecting...";
                var address = System.Net.IPAddress.Parse(Solution.Settings.CurrentIpAddress);

                // Parse port from Settings.DefaultPort, fallback to 21105 if not set or invalid
                int port = 21105; // Default fallback
                if (!string.IsNullOrEmpty(Solution.Settings.DefaultPort) && int.TryParse(Solution.Settings.DefaultPort, out var parsedPort))
                {
                    port = parsedPort;
                }

                await _z21.ConnectAsync(address, port);
                IsZ21Connected = true;
                Z21StatusText = $"Connected to {Solution.Settings.CurrentIpAddress}:{port}";

                // Create execution context (requires first project)
                if (Solution.Projects.Count > 0)
                {
                    var project = Solution.Projects[0];
                    var executionContext = new Moba.Backend.Services.ActionExecutionContext
                    {
                        Z21 = _z21
                    };

                    _journeyManager?.Dispose();
                    _journeyManager = _journeyManagerFactory.Create(_z21, project.Journeys, executionContext);
                }

                ConnectToZ21Command.NotifyCanExecuteChanged();
                DisconnectFromZ21Command.NotifyCanExecuteChanged();
                // SimulateFeedbackCommand is always enabled, no need to notify
                SetTrackPowerCommand.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                Z21StatusText = $"Connection failed: {ex.Message}";
            }
        }
        else
        {
            Z21StatusText = "No IP address configured in Solution.Settings";
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
            System.Diagnostics.Debug.WriteLine("🔔 SimulateFeedback START");
            System.Diagnostics.Debug.WriteLine($"   Solution: {Solution != null}");
            System.Diagnostics.Debug.WriteLine($"   Projects: {Solution?.Projects.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"   CurrentSelectedNode: {CurrentSelectedNode != null}");
            System.Diagnostics.Debug.WriteLine($"   DataContext Type: {CurrentSelectedNode?.DataContext?.GetType().Name ?? "null"}");
            
            // ✅ Ensure JourneyManager exists (create if needed for simulation)
            if (_journeyManager == null && Solution?.Projects.Count > 0)
            {
                var project = Solution.Projects[0];
                System.Diagnostics.Debug.WriteLine($"   Creating JourneyManager with {project.Journeys.Count} journeys");
                
                var executionContext = new Moba.Backend.Services.ActionExecutionContext
                {
                    Z21 = _z21
                };
                _journeyManager = _journeyManagerFactory.Create(_z21, project.Journeys, executionContext);
                System.Diagnostics.Debug.WriteLine($"✅ JourneyManager created for simulation with {project.Journeys.Count} journeys");
            }
            else if (_journeyManager != null)
            {
                System.Diagnostics.Debug.WriteLine("✅ JourneyManager already exists");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ No solution or projects available for simulation");
            }

            // Prefer InPort from currently selected Journey (VM or model)
            uint? selectedInPort = null;
            if (CurrentSelectedNode?.DataContext is JourneyViewModel jvm)
            {
                selectedInPort = jvm.InPort;
                System.Diagnostics.Debug.WriteLine($"📍 Using InPort {selectedInPort} from selected JourneyViewModel");
            }
            else if (CurrentSelectedNode?.DataContext is Journey jm)
            {
                selectedInPort = jm.InPort;
                System.Diagnostics.Debug.WriteLine($"📍 Using InPort {selectedInPort} from selected Journey model");
            }

            int inPort;
            if (selectedInPort.HasValue)
            {
                inPort = unchecked((int)selectedInPort.Value);
            }
            else if (!int.TryParse(SimulateInPort, out inPort))
            {
                Z21StatusText = "Invalid InPort number";
                System.Diagnostics.Debug.WriteLine($"❌ Invalid InPort: {SimulateInPort}");
                return;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"📍 Using InPort {inPort} from text field");
            }

            System.Diagnostics.Debug.WriteLine($"🔔 Simulating feedback for InPort {inPort}");
            _z21.SimulateFeedback(inPort);
            Z21StatusText = $"Simulated feedback for InPort {inPort}";
            System.Diagnostics.Debug.WriteLine($"✅ SimulateFeedback COMPLETE");
        }
        catch (Exception ex)
        {
            Z21StatusText = $"Error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"❌ SimulateFeedback error: {ex}");
            System.Diagnostics.Debug.WriteLine($"   StackTrace: {ex.StackTrace}");
        }
    }

    private bool CanSaveSolution() => true; // Solution is always initialized

    private bool CanConnectToZ21() => !IsZ21Connected;

    private bool CanDisconnectFromZ21() => IsZ21Connected;

    private bool CanSimulateFeedback() => true; // Always enabled

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

    private bool CanToggleTrackPower() => IsZ21Connected;

    public void OnWindowClosing()
    {
        if (_journeyManager != null)
        {
            try
            {
                _journeyManager.Dispose();
                _journeyManager = null;

                var z21 = _z21;

                _ = Task.Run(async () =>
                {
                    try { await z21.DisconnectAsync(); }
                    catch (TaskCanceledException) { }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        this.LogError("Error during Z21 shutdown", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                this.LogError("Error scheduling Z21 shutdown", ex);
            }
        }

        // ✅ Unsubscribe from Z21 events
        _z21.OnSystemStateChanged -= OnZ21SystemStateChanged;

        // Trigger event so the view can close the application
        ExitApplicationRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Handles Z21 system state changes and updates UI status.
    /// IMPORTANT: Called from background thread (UDP callback), dispatches to UI thread via IUiDispatcher.
    /// </summary>
    private void OnZ21SystemStateChanged(Backend.SystemState systemState)
    {
        _uiDispatcher.InvokeOnUi(() => UpdateZ21SystemState(systemState));
    }

    /// <summary>
    /// Updates Z21 system state on UI thread.
    /// </summary>
    private void UpdateZ21SystemState(Backend.SystemState systemState)
    {
        // ✅ Synchronize track power status with Z21 actual state
        IsTrackPowerOn = systemState.IsTrackPowerOn;

        // Update status text with key metrics (using simple C instead of ° symbol to avoid encoding issues)
        var statusParts = new List<string>
        {
            "Connected",
            $"Current: {systemState.MainCurrent}mA",
            $"Temp: {systemState.Temperature}C"  // ✅ Use plain C instead of °C to avoid UTF-8 encoding issues
        };

        // Add warnings for special states
        if (systemState.IsEmergencyStop)
            statusParts.Add("WARNING: EMERGENCY STOP");

        if (systemState.IsShortCircuit)
            statusParts.Add("WARNING: SHORT CIRCUIT");

        if (systemState.IsProgrammingMode)
            statusParts.Add("Programming");

        Z21StatusText = string.Join(" | ", statusParts);

        this.Log($"Z21 System State: TrackPower={systemState.IsTrackPowerOn}, Current={systemState.MainCurrent}mA, Temp={systemState.Temperature}C, Voltage={systemState.SupplyVoltage}mV");
    }

    private void BuildTreeView()
    {
        // ✅ NULL-Check: Solution könnte während Initialization null sein
        if (Solution == null || SolutionViewModel == null)
        {
            TreeNodes.Clear();
            return;
        }

        System.Diagnostics.Debug.WriteLine($"🔧 BuildTreeView START - Current TreeNodes count: {TreeNodes.Count}");

        // ✅ ALWAYS save current expansion states before rebuild (even if TreeNodes is empty)
        var expansionStates = new Dictionary<string, bool>();
        if (TreeNodes.Count > 0)
        {
            SaveExpansionStates(TreeNodes, expansionStates, "");
            System.Diagnostics.Debug.WriteLine($"💾 Saved {expansionStates.Count} expansion states");
            System.Diagnostics.Debug.WriteLine($"💾 Expanded nodes: {expansionStates.Count(x => x.Value)}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"ℹ️ No previous TreeNodes to save expansion states from");
        }

        // Save path to currently selected node (so we can restore it after rebuild)
        string? selectedNodePath = null;
        if (CurrentSelectedNode != null)
        {
            selectedNodePath = GetNodePath(TreeNodes, CurrentSelectedNode, "");
            System.Diagnostics.Debug.WriteLine($"💾 Saving selected node path: {selectedNodePath}");
        }

        // ✅ Rebuild tree using SolutionViewModel (uses existing ViewModels!)
        TreeNodes = _treeViewBuilder.BuildTreeView(SolutionViewModel);

        System.Diagnostics.Debug.WriteLine($"🏗️ Rebuilt tree - New TreeNodes count: {TreeNodes.Count}");

        // ✅ If we have saved expansion states, restore them (otherwise keep defaults)
        if (expansionStates.Count > 0)
        {
            // First, collapse all nodes that TreeViewBuilder set to expanded
            CollapseAllNodes(TreeNodes);
            System.Diagnostics.Debug.WriteLine($"📁 Collapsed all nodes before restore");

            // Then restore the saved expansion states
            RestoreExpansionStates(TreeNodes, expansionStates, "");
            System.Diagnostics.Debug.WriteLine($"✅ Restore complete");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"ℹ️ No saved expansion states - keeping TreeViewBuilder defaults");
        }

        // Restore selected node (if it still exists after rebuild)
        if (!string.IsNullOrEmpty(selectedNodePath))
        {
            var restoredNode = FindNodeByPath(TreeNodes, selectedNodePath, "");
            if (restoredNode != null)
            {
                CurrentSelectedNode = restoredNode;

                // ✅ Expand all parent nodes to make the selected node visible
                ExpandParentNodes(restoredNode);

                System.Diagnostics.Debug.WriteLine($"✅ Restored selected node: {selectedNodePath}");
            }
            else
            {
                CurrentSelectedNode = null;
                Properties.Clear();
                System.Diagnostics.Debug.WriteLine($"⚠️ Could not restore selected node: {selectedNodePath}");
            }
        }

        // Subscribe to ModelChanged events for all JourneyViewModels
        SubscribeToJourneyChanges(TreeNodes);
    }

    /// <summary>
    /// Expands all parent nodes of a given node to make it visible in the tree.
    /// </summary>
    private void ExpandParentNodes(TreeNodeViewModel targetNode)
    {
        var parent = FindParentNodeInTree(TreeNodes, targetNode);
        while (parent != null)
        {
            parent.IsExpanded = true;
            System.Diagnostics.Debug.WriteLine($"📂 Expanded parent: {parent.DisplayName}");
            parent = FindParentNodeInTree(TreeNodes, parent);
        }
    }

    /// <summary>
    /// Finds the parent of a given node in the tree.
    /// </summary>
    private TreeNodeViewModel? FindParentNodeInTree(ObservableCollection<TreeNodeViewModel> nodes, TreeNodeViewModel targetNode)
    {
        foreach (var node in nodes)
        {
            if (node.Children.Contains(targetNode))
            {
                return node;
            }

            if (node.Children.Count > 0)
            {
                var parent = FindParentNodeInTree(node.Children, targetNode);
                if (parent != null)
                {
                    return parent;
                }
            }
        }

        return null;
    }

    private void SubscribeToJourneyChanges(ObservableCollection<TreeNodeViewModel> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.DataContext is JourneyViewModel journeyVM)
            {
                // Unsubscribe first to avoid double subscription
                journeyVM.ModelChanged -= OnJourneyModelChanged;
                journeyVM.ModelChanged += OnJourneyModelChanged;
            }

            if (node.Children.Count > 0)
            {
                SubscribeToJourneyChanges(node.Children);
            }
        }
    }

    private void OnJourneyModelChanged(object? sender, EventArgs e)
    {
        // A Journey was modified (Station added/deleted/reordered)
        HasUnsavedChanges = true;
    }

    private void SaveExpansionStates(ObservableCollection<TreeNodeViewModel> nodes, Dictionary<string, bool> states, string path)
    {
        // Group nodes by DataType to get correct indices per type
        var nodesByType = nodes.GroupBy(n => n.DataType?.Name ?? "Unknown").ToList();
        
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            var typeName = node.DataType?.Name ?? "Unknown";
            
            // Find index among siblings of the same type
            var siblingsOfSameType = nodes.Where(n => (n.DataType?.Name ?? "Unknown") == typeName).ToList();
            var indexInType = siblingsOfSameType.IndexOf(node);
            
            // ✅ Use DataType + index within same type for stable identification
            var nodeKey = $"{typeName}[{indexInType}]";
            if (!string.IsNullOrEmpty(path))
            {
                nodeKey = $"{path}/{nodeKey}";
            }
            
            states[nodeKey] = node.IsExpanded;

            if (node.IsExpanded)
            {
                System.Diagnostics.Debug.WriteLine($"💾 Saving expanded: {nodeKey}");
            }

            if (node.Children.Count > 0)
            {
                SaveExpansionStates(node.Children, states, nodeKey);
            }
        }
    }

    private void RestoreExpansionStates(ObservableCollection<TreeNodeViewModel> nodes, Dictionary<string, bool> states, string path)
    {
        // Group nodes by DataType to get correct indices per type
        var nodesByType = nodes.GroupBy(n => n.DataType?.Name ?? "Unknown").ToList();
        
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            var typeName = node.DataType?.Name ?? "Unknown";
            
            // Find index among siblings of the same type
            var siblingsOfSameType = nodes.Where(n => (n.DataType?.Name ?? "Unknown") == typeName).ToList();
            var indexInType = siblingsOfSameType.IndexOf(node);
            
            // ✅ Use DataType + index within same type for stable identification
            var nodeKey = $"{typeName}[{indexInType}]";
            if (!string.IsNullOrEmpty(path))
            {
                nodeKey = $"{path}/{nodeKey}";
            }

            if (states.TryGetValue(nodeKey, out var isExpanded))
            {
                // ✅ Set IsExpanded and explicitly notify (triggers PropertyChanged via ObservableProperty)
                node.IsExpanded = isExpanded;

                if (isExpanded)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Restoring expanded: {nodeKey} (IsExpanded={node.IsExpanded})");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ No saved state for: {nodeKey}");
            }

            // ✅ Restore children BEFORE expanding parent (important for WinUI!)
            if (node.Children.Count > 0)
            {
                RestoreExpansionStates(node.Children, states, nodeKey);
            }
        }
    }

    /// <summary>
    /// Gets a stable key for a node based on its type and index in the parent's children.
    /// This is more robust than using DisplayName, which can change.
    /// </summary>
    private string GetNodeKey(TreeNodeViewModel node, int index, string parentPath)
    {
        // Use DataType name + index as the identifier
        var typeName = node.DataType?.Name ?? "Unknown";
        var key = $"{typeName}[{index}]";

        return string.IsNullOrEmpty(parentPath) ? key : $"{parentPath}/{key}";
    }

    /// <summary>
    /// Collapses all nodes in the tree (used before restoring saved expansion states).
    /// </summary>
    private void CollapseAllNodes(ObservableCollection<TreeNodeViewModel> nodes)
    {
        foreach (var node in nodes)
        {
            node.IsExpanded = false;

            if (node.Children.Count > 0)
            {
                CollapseAllNodes(node.Children);
            }
        }
    }

    /// <summary>
    /// Gets the hierarchical path to a specific node (e.g., "Project1/Journeys/Journey1").
    /// </summary>
    private string GetNodePath(ObservableCollection<TreeNodeViewModel> nodes, TreeNodeViewModel targetNode, string currentPath)
    {
        foreach (var node in nodes)
        {
            // Check if this is the target node
            if (node == targetNode)
            {
                return currentPath + node.DisplayName;
            }

            // Recurse into children
            if (node.Children.Count > 0)
            {
                var childPath = GetNodePath(node.Children, targetNode, currentPath + node.DisplayName + "/");
                if (!string.IsNullOrEmpty(childPath))
                {
                    return childPath;
                }
            }
        }

        return string.Empty; // Not found
    }

    /// <summary>
    /// Finds a node by its hierarchical path (e.g., "Project1/Journeys/Journey1").
    /// </summary>
    private TreeNodeViewModel? FindNodeByPath(ObservableCollection<TreeNodeViewModel> nodes, string targetPath, string currentPath)
    {
        foreach (var node in nodes)
        {
            var nodePath = string.IsNullOrEmpty(currentPath) ? node.DisplayName : $"{currentPath}/{node.DisplayName}";

            if (nodePath == targetPath)
            {
                return node;
            }

            if (node.Children.Count > 0 && targetPath.StartsWith(nodePath + "/"))
            {
                var childNode = FindNodeByPath(node.Children, targetPath, nodePath);
                if (childNode != null)
                {
                    return childNode;
                }
            }
        }

        return null;
    }

    public void OnNodeSelected(TreeNodeViewModel? node)
    {
        // Clean up old PropertyViewModels (remove event handlers)
        foreach (var prop in Properties)
        {
            prop.ValueChanged -= OnPropertyValueChanged;
            prop.Dispose();
        }

        Properties.Clear();
        CurrentSelectedNode = node;
        
        // ✅ Reset all typed selections
        SelectedJourney = null;
        SelectedStation = null;
        SelectedWorkflow = null;
        SelectedTrain = null;

        if (node?.DataContext == null || node.DataType == null)
        {
            SelectedNodeType = string.Empty;
            return;
        }
        
        // ✅ Set typed selection based on DataContext type
        switch (node.DataContext)
        {
            case JourneyViewModel jvm:
                SelectedJourney = jvm;
                System.Diagnostics.Debug.WriteLine($"✅ SelectedJourney set: {jvm.Name}");
                break;
            case StationViewModel svm:
                SelectedStation = svm;
                System.Diagnostics.Debug.WriteLine($"✅ SelectedStation set: {svm.Name}");
                break;
            case WorkflowViewModel wvm:
                SelectedWorkflow = wvm;
                System.Diagnostics.Debug.WriteLine($"✅ SelectedWorkflow set: {wvm.Name}");
                break;
            case TrainViewModel tvm:
                SelectedTrain = tvm;
                System.Diagnostics.Debug.WriteLine($"✅ SelectedTrain set: {tvm.Name}");
                break;
        }

        SelectedNodeType = node.DataType.Name;

        var props = node.DataType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p is
            {
                CanRead: true,
                PropertyType.IsGenericType: false
            } // No lists
            && (IsSimpleType(p.PropertyType)
            || IsReferenceType(p.PropertyType))); // Simple types OR references

        // Get context (Project) for reference lookups
        var contextProject = FindParentProject(node);

        foreach (var prop in props)
        {
            var propertyViewModel = new PropertyViewModel(prop, node.DataContext);

            // Set available values for reference properties
            if (IsReferenceType(prop.PropertyType))
            {
                propertyViewModel.ReferenceValues = GetReferenceValues(prop.PropertyType, contextProject);
            }

            // Register event handler for tree updates
            propertyViewModel.ValueChanged += OnPropertyValueChanged;

            Properties.Add(propertyViewModel);
        }
    }

    // Updates the display name when a property value changes in the PropertyGrid
    private void OnPropertyValueChanged(object? sender, EventArgs e)
    {
        CurrentSelectedNode?.RefreshDisplayName();
        HasUnsavedChanges = true;
    }

    // Finds the parent Project of a TreeNode (e.g., for a Station or Platform)
    private Project? FindParentProject(TreeNodeViewModel node)
    {
        // Check if current node is a Project
        if (node.DataContext is Project project)
            return project;

        if (node.DataContext == null)
            return null;

        // Handle ViewModels - extract the Model first
        var dataContext = node.DataContext;

        // Extract Model from ViewModel
        Journey? journeyModel = dataContext switch
        {
            JourneyViewModel journeyVm => journeyVm.Model,
            _ => dataContext as Journey
        };

        Station? stationModel = dataContext switch
        {
            StationViewModel stationVm => stationVm.Model,
            _ => dataContext as Station
        };

        Platform? platformModel = dataContext switch
        {
            PlatformViewModel platformVm => platformVm.Model,
            _ => dataContext as Platform
        };

        Workflow? workflowModel = dataContext switch
        {
            WorkflowViewModel workflowVm => workflowVm.Model,
            _ => dataContext as Workflow
        };

        Train? trainModel = dataContext switch
        {
            TrainViewModel trainVm => trainVm.Model,
            _ => dataContext as Train
        };

        Locomotive? locomotiveModel = dataContext switch
        {
            LocomotiveViewModel locoVm => locoVm.Model,
            _ => dataContext as Locomotive
        };

        // Now search in projects with the extracted models
        if (journeyModel != null)
            return Solution.Projects.FirstOrDefault(p => p.Journeys.Contains(journeyModel));

        if (stationModel != null)
            return Solution.Projects.FirstOrDefault(p => ContainsStation(p, stationModel));

        if (platformModel != null)
            return Solution.Projects.FirstOrDefault(p => ContainsPlatform(p, platformModel));

        if (workflowModel != null)
            return Solution.Projects.FirstOrDefault(p => p.Workflows.Contains(workflowModel));

        if (trainModel != null)
            return Solution.Projects.FirstOrDefault(p => p.Trains.Contains(trainModel));

        if (locomotiveModel != null)
            return Solution.Projects.FirstOrDefault(p => p.Locomotives.Contains(locomotiveModel));

        if (dataContext is Settings setting)
            return Solution.Settings == setting ? Solution.Projects.FirstOrDefault() : null;

        return null;
    }

    // Checks if a Station is contained in one of the Project's Journeys
    private static bool ContainsStation(Project project, Station station)
    {
        return project.Journeys.Any(j => j.Stations.Contains(station));
    }

    // Checks if a Platform is contained in one of the Project's Journey Stations
    private static bool ContainsPlatform(Project project, Platform platform)
    {
        return project.Journeys.Any(j => j.Stations.Any(s => s.Platforms.Contains(platform)));
    }

    // Checks if a type is a "simple" type that should be displayed in the PropertyGrid
    private static bool IsSimpleType(Type type)
    {
        // Handle Nullable types
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType.IsPrimitive       // int, bool, byte, etc.
        || underlyingType.IsEnum                // Enums
        || underlyingType == typeof(string)     // string
        || underlyingType == typeof(decimal)    // decimal
        || underlyingType == typeof(DateTime)   // DateTime
        || underlyingType == typeof(DateTimeOffset) // DateTimeOffset
        || underlyingType == typeof(TimeSpan)   // TimeSpan
        || underlyingType == typeof(Guid);      // Guid
    }

    // Checks if a type is an object reference (e.g., Workflow, Train, etc.)
    private static bool IsReferenceType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        // Only allow specific types (whitelist approach)
        return underlyingType == typeof(Workflow)
            || underlyingType == typeof(Train);
    }

    private static IEnumerable<object?>? GetReferenceValues(Type type, Project? contextProject)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(Workflow))
        {
            if (contextProject == null)
                return new List<object?> { null };

            // Only Workflows from the CURRENT Project
            var workflows = contextProject.Workflows.Cast<object>().ToList();

            // Add null option (for optional workflows)
            return new List<object?> { null }.Concat(workflows);
        }

        if (underlyingType == typeof(Train))
        {
            if (contextProject == null)
                return new List<object?> { null };

            // Only Trains from the CURRENT Project
            var trains = contextProject.Trains.Cast<object>().ToList();

            // Add null option (for optional trains)
            return new List<object?> { null }.Concat(trains);
        }

        return null;
    }

    /// <summary>
    /// Finds the parent JourneyViewModel for a given TreeNode (e.g., for a StationViewModel).
    /// Used by UI for context menu operations and drag & drop.
    /// </summary>
    /// <param name="node">The tree node to search from</param>
    /// <returns>The parent JourneyViewModel if found, otherwise null</returns>
    public JourneyViewModel? FindParentJourneyViewModel(TreeNodeViewModel? node)
    {
        if (node == null) return null;

        // Search in entire tree for parent journey
        return FindJourneyViewModelRecursive(TreeNodes, node);
    }

    private JourneyViewModel? FindJourneyViewModelRecursive(
        ObservableCollection<TreeNodeViewModel> nodes,
        TreeNodeViewModel targetNode)
    {
        foreach (var node in nodes)
        {
            // Check if this node's children contain the target
            if (node.Children.Contains(targetNode) && node.DataContext is JourneyViewModel journeyVM)
            {
                return journeyVM;
            }

            // Recurse into children
            var result = FindJourneyViewModelRecursive(node.Children, targetNode);
            if (result != null) return result;
        }

        return null;
    }

    /// <summary>
    /// Finds a JourneyViewModel by its model instance.
    /// </summary>
    public JourneyViewModel? FindJourneyViewModel(Journey journey)
    {
        return FindJourneyViewModelByModelRecursive(TreeNodes, journey);
    }

    private JourneyViewModel? FindJourneyViewModelByModelRecursive(
        ObservableCollection<TreeNodeViewModel> nodes,
        Journey journey)
    {
        foreach (var node in nodes)
        {
            if (node.DataContext is JourneyViewModel journeyVM && journeyVM.Model == journey)
            {
                return journeyVM;
            }

            var result = FindJourneyViewModelByModelRecursive(node.Children, journey);
            if (result != null) return result;
        }

        return null;
    }

    /// <summary>
    /// Refreshes the TreeView without losing expansion state.
    /// </summary>
    public void RefreshTreeView()
    {
        BuildTreeView();
    }

    [RelayCommand]
    private async Task NewSolutionAsync()
    {
        System.Diagnostics.Debug.WriteLine("📄 NewSolutionAsync START");
        
        var (success, userCancelled, error) = await _ioService.NewSolutionAsync(HasUnsavedChanges);
        
        if (userCancelled)
        {
            System.Diagnostics.Debug.WriteLine("ℹ️ User cancelled new solution creation");
            return;
        }
        
        if (!success)
        {
            // Check if user wants to save first
            if (error == "SAVE_REQUESTED")
            {
                System.Diagnostics.Debug.WriteLine("💾 Saving current solution before creating new one");
                
                // Execute save command
                if (SaveSolutionCommand.CanExecute(null))
                {
                    await SaveSolutionCommand.ExecuteAsync(null);
                }
                
                // After save, create new solution
                System.Diagnostics.Debug.WriteLine("📄 Creating new solution after save");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to create new solution: {error}");
                return;
            }
        }
        
        // Create a new empty Solution
        var newSolution = new Solution
        {
            Name = "New Solution"
        };
        
        // Add a default project
        newSolution.Projects.Add(new Project
        {
            Name = "New Project",
            Journeys = new System.Collections.Generic.List<Journey>(),
            Workflows = new System.Collections.Generic.List<Workflow>(),
            Trains = new System.Collections.Generic.List<Train>()
        });
        
        System.Diagnostics.Debug.WriteLine($"✅ Updating Solution with new empty solution");
        
        // Update the existing Solution singleton instance
        MergeSolution(newSolution);
        
        // ✅ Refresh SolutionViewModel to sync with new solution
        SolutionViewModel?.Refresh();
        
        // Clear the current path (unsaved new solution)
        CurrentSolutionPath = null;
        
        // Mark as having unsaved changes (new solution not yet saved)
        HasUnsavedChanges = true;
        
        // Rebuild TreeView
        SaveSolutionCommand.NotifyCanExecuteChanged();
        ConnectToZ21Command.NotifyCanExecuteChanged();
        BuildTreeView();
        LoadCities();
        
        System.Diagnostics.Debug.WriteLine("✅ NewSolutionAsync COMPLETE");
    }

    /// <summary>
    /// Updates the current Solution instance with data from another Solution.
    /// This preserves the DI singleton reference while updating its content.
    /// </summary>
    private void MergeSolution(Solution source)
    {
        if (source == null) return;

        // Clear and update Projects
        Solution.Projects.Clear();
        foreach (var project in source.Projects)
        {
            Solution.Projects.Add(project);
        }

        // Update Settings
        Solution.Settings = source.Settings ?? new Settings();

        // Update Name
        Solution.Name = source.Name;
    }
}