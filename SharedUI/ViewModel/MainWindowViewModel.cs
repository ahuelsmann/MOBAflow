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
    private readonly TreeViewBuilder _treeViewBuilder;
    private Backend.Z21? _z21;
    private Backend.JourneyManager? _journeyManager;

    public MainWindowViewModel(IIoService ioService)
    {
        _ioService = ioService;
        _treeViewBuilder = new TreeViewBuilder();
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

    // Event für das Beenden der Anwendung
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
            // TODO: expose error to UI (add property or messaging)
            return;
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
            // TODO: expose error to UI
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

                    // ✅ Create execution context - only shared dependencies
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
        TreeNodes = _treeViewBuilder.BuildTreeView(Solution);
    }

    public void OnNodeSelected(TreeNodeViewModel? node)
    {
        // Alte PropertyViewModels aufräumen (Event-Handler entfernen)
        foreach (var prop in Properties)
        {
            prop.ValueChanged -= OnPropertyValueChanged;
            prop.Dispose(); // ✅ PropertyViewModel aufräumen
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
            && !p.PropertyType.IsGenericType // Keine Lists
            && (IsSimpleType(p.PropertyType) || IsReferenceType(p.PropertyType))); // Einfache Typen ODER Referenzen

        // Den Kontext (Project) ermitteln für Referenz-Lookups
        var contextProject = FindParentProject(node);

        foreach (var prop in props)
        {
            var propertyViewModel = new PropertyViewModel(prop, node.DataContext);

            // Für Referenz-Properties die verfügbaren Werte setzen
            if (IsReferenceType(prop.PropertyType))
            {
                propertyViewModel.ReferenceValues = GetReferenceValues(prop.PropertyType, contextProject);
            }

            // Event-Handler registrieren für Tree-Updates
            propertyViewModel.ValueChanged += OnPropertyValueChanged;

            Properties.Add(propertyViewModel);
        }
    }

    /// <summary>
    /// Wird aufgerufen, wenn ein Property-Wert im PropertyGrid geändert wurde.
    /// Aktualisiert den DisplayName des aktuellen TreeNodes.
    /// </summary>
    private void OnPropertyValueChanged(object? sender, EventArgs e)
    {
        CurrentSelectedNode?.RefreshDisplayName();
    }

    /// <summary>
    /// Findet das parent Project eines TreeNodes (z.B. für eine Station)
    /// </summary>
    private Project? FindParentProject(TreeNodeViewModel node)
    {
        // Prüfe ob der aktuelle Node ein Project ist
        if (node.DataContext is Project project)
        {
            return project;
        }

        // Durchsuche die Solution nach dem Project, das dieses Objekt enthält
        if (Solution != null && node.DataContext != null)
        {
            foreach (var proj in Solution.Projects)
            {
                // Prüfe ob das Objekt in einer der Listen des Projects ist
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

    /// <summary>
    /// Prüft ob eine Station in einem der Journeys eines Projects enthalten ist
    /// </summary>
    private static bool ContainsStation(Project project, Station station)
    {
        return project.Journeys.Any(j => j.Stations.Contains(station));
    }

    /// <summary>
    /// Prüft, ob ein Typ ein "einfacher" Typ ist, der im PropertyGrid angezeigt werden soll
    /// </summary>
    private static bool IsSimpleType(Type type)
    {
        // Nullable Types berücksichtigen
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

    /// <summary>
    /// Prüft, ob ein Typ eine Objektreferenz ist (z.B. Workflow, Train, etc.)
    /// </summary>
    private static bool IsReferenceType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        // Nur bestimmte Typen erlauben (Whitelist-Ansatz)
        return underlyingType == typeof(Workflow);
        // Hier können später weitere Typen hinzugefügt werden:
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

            // Nur Workflows aus dem AKTUELLEN Project
            var workflows = contextProject.Workflows.Cast<object>().ToList();

            // Null-Option hinzufügen (für optionale Workflows)
            return new List<object?> { null }.Concat(workflows);
        }

        // Hier können später weitere Typen hinzugefügt werden
        // if (underlyingType == typeof(Backend.Model.Train)) { ... }

        return null;
    }
}