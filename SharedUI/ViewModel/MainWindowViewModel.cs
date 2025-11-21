namespace Moba.SharedUI.ViewModel;

using Backend.Data;
using Backend.Manager;
using Backend.Model;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Moba.Backend.Interface;
using Moba.SharedUI.Extensions;

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
    private readonly UndoRedoManager _undoRedoManager;

    // Primary ctor for DI
    public MainWindowViewModel(IIoService ioService, IZ21 z21, IJourneyManagerFactory journeyManagerFactory, TreeViewBuilder treeViewBuilder, IUiDispatcher uiDispatcher)
    {
        _ioService = ioService;
        _z21 = z21;
        _journeyManagerFactory = journeyManagerFactory;
        _treeViewBuilder = treeViewBuilder;
        _uiDispatcher = uiDispatcher;
        
        // Initialize undo/redo manager with temp directory
        var historyPath = Path.Combine(Path.GetTempPath(), "MOBAflow", "History");
        _undoRedoManager = new UndoRedoManager(historyPath);
        
        // ✅ Subscribe to Z21 system state events (for status display)
        _z21.OnSystemStateChanged += OnZ21SystemStateChanged;
    }

    [ObservableProperty]
    private string title = "MOBAflow";

    [ObservableProperty]
    private string? currentSolutionPath;

    [ObservableProperty]
    private bool hasSolution;

    [ObservableProperty]
    private Solution? solution;

    [ObservableProperty]
    private ObservableCollection<TreeNodeViewModel> treeNodes = [];

    [ObservableProperty]
    private ObservableCollection<PropertyViewModel> properties = [];

    [ObservableProperty]
    private string selectedNodeType = string.Empty;

    [ObservableProperty]
    private TreeNodeViewModel? currentSelectedNode;

    [ObservableProperty]
    private bool isZ21Connected;

    [ObservableProperty]
    private string z21StatusText = "Disconnected";

    [ObservableProperty]
    private string simulateInPort = "1";

    [ObservableProperty]
    private ObservableCollection<Backend.Data.City> availableCities = [];

    [ObservableProperty]
    private Backend.Data.City? selectedCity;

    [ObservableProperty]
    private bool canUndo;

    [ObservableProperty]
    private bool canRedo;

    public event EventHandler? ExitApplicationRequested;

    partial void OnSolutionChanged(Solution? value)
    {
        HasSolution = value is { Projects.Count: > 0 };
        SaveSolutionCommand.NotifyCanExecuteChanged();
        ConnectToZ21Command.NotifyCanExecuteChanged();
        BuildTreeView();
        LoadCities();
        
        // Save initial state
        if (value != null)
        {
            _ = _undoRedoManager.SaveStateImmediateAsync(value);
            UpdateUndoRedoState();
        }
    }

    private void LoadCities()
    {
        AvailableCities.Clear();
        
        if (Solution?.Projects.Count > 0)
        {
            var firstProject = Solution.Projects[0];
            foreach (var city in firstProject.Cities)
            {
                AvailableCities.Add(city);
            }
        }
    }

    [RelayCommand]
    private async Task LoadCitiesFromFileAsync()
    {
        if (Solution?.Projects.Count == 0) return;

        var (dataManager, path, error) = await _ioService.LoadDataManagerAsync();
        
        if (dataManager != null && Solution.Projects.Count > 0)
        {
            var firstProject = Solution.Projects[0];
            firstProject.Cities.Clear();
            firstProject.Cities.AddRange(dataManager.Cities);
            LoadCities();

            // Save state for undo/redo
            await _undoRedoManager.SaveStateImmediateAsync(Solution);
            UpdateUndoRedoState();
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
                Track = stationToCopy.Track,
                IsExitOnLeft = stationToCopy.IsExitOnLeft,
                NumberOfLapsToStop = 2, // Default for journey
                Number = (uint)journeyVM.Model.Stations.Count + 1
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

            // Save state for undo/redo
            if (Solution != null)
            {
                _ = _undoRedoManager.SaveStateImmediateAsync(Solution);
                UpdateUndoRedoState();
            }
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
        (Solution? _solution, string? path, string? error) = await _ioService.LoadAsync();
        if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException($"Failed to load solution with error {error}");
        }
        if (_solution != null)
        {
            Solution = _solution;
            CurrentSolutionPath = path;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveSolution))]
    private async Task SaveSolutionAsync()
    {
        if (Solution == null) return;
        var (success, path, error) = await _ioService.SaveAsync(Solution, CurrentSolutionPath);
        if (success && path != null)
        {
            CurrentSolutionPath = path;
        }
        else if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException($"Failed to save solution with error {error}");
        }
    }

    [RelayCommand]
    private void AddProject()
    {
        if (Solution == null) return;
        Solution.Projects.Add(new Project());
        BuildTreeView();

        // Save state for undo/redo
        _ = _undoRedoManager.SaveStateImmediateAsync(Solution);
        UpdateUndoRedoState();
    }

    [RelayCommand(CanExecute = nameof(CanConnectToZ21))]
    private async Task ConnectToZ21Async()
    {
        if (Solution?.Projects.Count > 0)
        {
            var project = Solution.Projects[0];

            if (project.IpAddresses.Count > 0)
            {
                try
                {
                    Z21StatusText = "Connecting...";
                    var address = System.Net.IPAddress.Parse(project.IpAddresses[0]);
                    await _z21.ConnectAsync(address);

                    IsZ21Connected = true;
                    Z21StatusText = $"Connected to {project.IpAddresses[0]}";

                    var executionContext = new Backend.Model.Action.ActionExecutionContext
                    {
                        Z21 = _z21,
                        Project = project
                    };

                    _journeyManager?.Dispose();
                    _journeyManager = _journeyManagerFactory.Create(_z21, project.Journeys, executionContext);

                    ConnectToZ21Command.NotifyCanExecuteChanged();
                    DisconnectFromZ21Command.NotifyCanExecuteChanged();
                    SimulateFeedbackCommand.NotifyCanExecuteChanged();
                }
                catch (Exception ex)
                {
                    Z21StatusText = $"Connection failed: {ex.Message}";
                }
            }
            else
            {
                Z21StatusText = "No IP address configured";
            }
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
            Z21StatusText = "Disconnected";
            ConnectToZ21Command.NotifyCanExecuteChanged();
            SimulateFeedbackCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            Z21StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanSimulateFeedback))]
    private void SimulateFeedback()
    {
        try
        {
            // Prefer InPort from currently selected Journey (VM or model)
            uint? selectedInPort = null;
            if (CurrentSelectedNode?.DataContext is JourneyViewModel jvm)
                selectedInPort = jvm.InPort;
            else if (CurrentSelectedNode?.DataContext is Journey jm)
                selectedInPort = jm.InPort;

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

    private bool CanSaveSolution() => Solution != null;

    private bool CanConnectToZ21() => Solution != null && !IsZ21Connected;

    private bool CanDisconnectFromZ21() => IsZ21Connected;

    private bool CanSimulateFeedback() => IsZ21Connected;

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
        // Update status text with key metrics
        Z21StatusText = $"Connected | Current: {systemState.MainCurrent}mA | Temp: {systemState.Temperature}°C";
        
        this.Log($"📊 Z21 System State: Current={systemState.MainCurrent}mA, Temp={systemState.Temperature}°C, Voltage={systemState.SupplyVoltage}mV");
    }
    
    private void BuildTreeView()
    {
        // Save current expansion states
        var expansionStates = new Dictionary<string, bool>();
        SaveExpansionStates(TreeNodes, expansionStates, "");

        // Rebuild tree
        TreeNodes = _treeViewBuilder.BuildTreeView(Solution);

        // Restore expansion states
        RestoreExpansionStates(TreeNodes, expansionStates, "");

        // Subscribe to ModelChanged events for all JourneyViewModels
        SubscribeToJourneyChanges(TreeNodes);
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
        if (Solution != null)
        {
            _ = _undoRedoManager.SaveStateImmediateAsync(Solution);
            UpdateUndoRedoState();
        }
    }

    private void SaveExpansionStates(ObservableCollection<TreeNodeViewModel> nodes, Dictionary<string, bool> states, string path)
    {
        foreach (var node in nodes)
        {
            var nodePath = string.IsNullOrEmpty(path) ? node.DisplayName : $"{path}/{node.DisplayName}";
            states[nodePath] = node.IsExpanded;

            if (node.Children.Count > 0)
            {
                SaveExpansionStates(node.Children, states, nodePath);
            }
        }
    }

    private void RestoreExpansionStates(ObservableCollection<TreeNodeViewModel> nodes, Dictionary<string, bool> states, string path)
    {
        foreach (var node in nodes)
        {
            var nodePath = string.IsNullOrEmpty(path) ? node.DisplayName : $"{path}/{node.DisplayName}";
            
            if (states.TryGetValue(nodePath, out var isExpanded))
            {
                node.IsExpanded = isExpanded;
            }

            if (node.Children.Count > 0)
            {
                RestoreExpansionStates(node.Children, states, nodePath);
            }
        }
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

        if (node?.DataContext == null || node.DataType == null)
        {
            SelectedNodeType = string.Empty;
            return;
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
        
        // Throttled auto-save after property changes
        if (Solution != null)
        {
            _undoRedoManager.SaveStateThrottled(Solution);
        }
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private async Task UndoAsync()
    {
        var previousSolution = await _undoRedoManager.UndoAsync();
        if (previousSolution != null)
        {
            Solution = previousSolution;
            UpdateUndoRedoState();
        }
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private async Task RedoAsync()
    {
        var nextSolution = await _undoRedoManager.RedoAsync();
        if (nextSolution != null)
        {
            Solution = nextSolution;
            UpdateUndoRedoState();
        }
    }

    private void UpdateUndoRedoState()
    {
        try
        {
            CanUndo = _undoRedoManager.CanUndo;
            CanRedo = _undoRedoManager.CanRedo;
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        }
        catch
        {
            // Ignore errors during initial state update
            CanUndo = false;
            CanRedo = false;
        }
    }

    // Finds the parent Project of a TreeNode (e.g., for a Station or Platform)
    private Project? FindParentProject(TreeNodeViewModel node)
    {
        // Check if current node is a Project
        if (node.DataContext is Project project)
            return project;

        if (Solution == null || node.DataContext == null)
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
        });

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
            return Solution.Projects.FirstOrDefault(p => p.Settings == setting);

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
}