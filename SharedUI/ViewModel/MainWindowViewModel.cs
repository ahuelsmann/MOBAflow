namespace Moba.SharedUI.ViewModel;

using Backend.Model;

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
    private Backend.Z21? _z21;
    private Backend.JourneyManager? _journeyManager;

    public MainWindowViewModel(IIoService ioService)
    {
        _ioService = ioService;
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

    // Event for application exit
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

        // Initialize Z21 if not already done
  _z21 ??= new Backend.Z21();

            if (project.Ips.Count > 0)
            {
            try
     {
             Z21StatusText = "Connecting...";
   var address = System.Net.IPAddress.Parse(project.Ips[0].Address);
             await _z21.ConnectAsync(address);

        IsZ21Connected = true;
   Z21StatusText = $"Connected to {project.Ips[0].Address}";

       // Create execution context with shared dependencies
                    var executionContext = new Moba.Backend.Model.Action.ActionExecutionContext
     {
       Z21 = _z21,
  // TODO: Add SpeakerEngine when needed
   // SpeakerEngine = _speakerEngine,
   Project = project
         };

          // Initialize JourneyManager with context
        _journeyManager?.Dispose();
  _journeyManager = new Moba.Backend.JourneyManager(_z21, project.Journeys, executionContext);

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

      if (_z21 != null)
{
        await _z21.DisconnectAsync();
             _z21.Dispose();
 _z21 = null;
            }

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
          if (int.TryParse(SimulateInPort, out int inPort))
 {
      _z21?.SimulateFeedback(inPort);
  Z21StatusText = $"Simulated feedback for InPort {inPort}";
   }
      else
         {
        Z21StatusText = "Invalid InPort number";
            }
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
        // Disconnect Z21 connection when exiting (non-blocking to avoid UI freeze)
        if (_z21 != null)
        {
            try
            {
       _journeyManager?.Dispose();

          var z21 = _z21; // capture local
     _z21 = null;

     // Run disconnect in background without blocking UI
  _ = Task.Run(async () =>
     {
   try
                 {
             await z21.DisconnectAsync();
     }
catch (TaskCanceledException)
             {
    // Expected during shutdown
    }
   catch (OperationCanceledException)
         {
       // Expected during shutdown
         }
         catch (Exception ex)
     {
       System.Diagnostics.Debug.WriteLine($"⚠️ Error during Z21 shutdown: {ex.Message}");
   }
      finally
 {
   z21.Dispose();
          }
        });
            }
            catch (Exception ex)
            {
      // Log other exceptions
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
       .Where(p => p.CanRead
   && !p.PropertyType.IsGenericType // No lists
         && (IsSimpleType(p.PropertyType) || IsReferenceType(p.PropertyType))); // Simple types OR references

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

    // Finds the parent Project of a TreeNode (e.g., for a Station)
 private Project? FindParentProject(TreeNodeViewModel node)
    {
        // Check if current node is a Project
        if (node.DataContext is Project project)
        {
 return project;
        }

        // Search the Solution for the Project that contains this object
        if (Solution != null && node.DataContext != null)
        {
     foreach (var proj in Solution.Projects)
       {
                // Check if object is in one of the Project's lists
      if (node.DataContext is Station station && ContainsStation(proj, station))
      return proj;
    if (node.DataContext is Workflow workflow && proj.Workflows.Contains(workflow))
         return proj;
     if (node.DataContext is Journey journey && proj.Journeys.Contains(journey))
    return proj;
     if (node.DataContext is Train train && proj.Trains.Contains(train))
        return proj;
      if (node.DataContext is Locomotive loco && proj.Locomotives.Contains(loco))
         return proj;
   if (node.DataContext is Setting setting && proj.Setting == setting)
      return proj;
      }
        }

        return null;
    }

 // Checks if a Station is contained in one of the Project's Journeys
    private static bool ContainsStation(Project project, Station station)
    {
        return project.Journeys.Any(j => j.Stations.Contains(station));
    }

    // Checks if a type is a "simple" type that should be displayed in the PropertyGrid
    private static bool IsSimpleType(Type type)
 {
        // Handle Nullable types
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType.IsPrimitive       // int, bool, byte, etc.
      || underlyingType.IsEnum   // Enums
   || underlyingType == typeof(string)  // string
       || underlyingType == typeof(decimal) // decimal
            || underlyingType == typeof(DateTime) // DateTime
       || underlyingType == typeof(DateTimeOffset) // DateTimeOffset
            || underlyingType == typeof(TimeSpan) // TimeSpan
            || underlyingType == typeof(Guid);    // Guid
    }

    // Checks if a type is an object reference (e.g., Workflow, Train, etc.)
    private static bool IsReferenceType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

     // Only allow specific types (whitelist approach)
        return underlyingType == typeof(Workflow);
     // Additional types can be added later:
        // || underlyingType == typeof(Backend.Model.Train)
        // || underlyingType == typeof(Backend.Model.Locomotive);
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

        // Additional types can be added later
        // if (underlyingType == typeof(Backend.Model.Train)) { ... }

        return null;
    }
}