namespace Moba.SharedUI.ViewModel;

using Backend.Model;
using Backend.Manager;
using Moba.Backend.Interface;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
    private JourneyManager? _journeyManager;

    // Primary ctor for DI
    public MainWindowViewModel(IIoService ioService, IZ21 z21, IJourneyManagerFactory journeyManagerFactory)
    {
        _ioService = ioService;
        _z21 = z21;
        _journeyManagerFactory = journeyManagerFactory;
    }

    // Secondary ctor for tests (legacy)
    public MainWindowViewModel(IIoService ioService)
        : this(ioService, new Backend.Z21(null, null), new JourneyManagerFactory())
    {
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

    public event EventHandler? ExitApplicationRequested;

    partial void OnSolutionChanged(Solution? value)
    {
        HasSolution = value is { Projects.Count: > 0 };
        SaveSolutionCommand.NotifyCanExecuteChanged();
        ConnectToZ21Command.NotifyCanExecuteChanged();
        BuildTreeView();
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
                        Z21 = _z21 as Backend.Z21,
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
                        System.Diagnostics.Debug.WriteLine($"⚠️ Error during Z21 shutdown: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error scheduling Z21 shutdown: {ex.Message}");
            }
        }

        // Trigger event so the view can close the application
        ExitApplicationRequested?.Invoke(this, EventArgs.Empty);
    }

    private void BuildTreeView()
    {
        TreeNodes = TreeViewBuilder.BuildTreeView(Solution);
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
    }

    // Finds the parent Project of a TreeNode (e.g., for a Station or Platform)
    private Project? FindParentProject(TreeNodeViewModel node)
    {
        // Check if current node is a Project
        if (node.DataContext is Project project)
            return project;

        if (Solution == null || node.DataContext == null)
            return null;

        // Handle JourneyViewModel - extract the Model
        var dataContext = node.DataContext;
        if (dataContext is JourneyViewModel journeyVm)
        {
            return Solution.Projects.FirstOrDefault(p => p.Journeys.Contains(journeyVm.Model));
        }

        // Type-check once, then search in projects
        return dataContext switch
        {
            Station station => Solution.Projects.FirstOrDefault(p => ContainsStation(p, station)),
            Platform platform => Solution.Projects.FirstOrDefault(p => ContainsPlatform(p, platform)),
            Workflow workflow => Solution.Projects.FirstOrDefault(p => p.Workflows.Contains(workflow)),
            Journey journey => Solution.Projects.FirstOrDefault(p => p.Journeys.Contains(journey)),
            Train train => Solution.Projects.FirstOrDefault(p => p.Trains.Contains(train)),
            Locomotive loco => Solution.Projects.FirstOrDefault(p => p.Locomotives.Contains(loco)),
            Settings setting => Solution.Projects.FirstOrDefault(p => p.Settings == setting),
            _ => null
        };
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
        return underlyingType == typeof(Workflow);
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

        return null;
    }
}