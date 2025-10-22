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

    // Event für das Beenden der Anwendung
    public event EventHandler? ExitApplicationRequested;

    partial void OnSolutionChanged(Solution? value)
    {
        HasSolution = value is { Projects.Count: > 0 };
        SaveSolutionCommand.NotifyCanExecuteChanged();
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

    private bool CanSaveSolution() => Solution != null;

    public void OnWindowClosing()
    {
        // Hier können Sie zukünftig Aufräumarbeiten durchführen
        // z.B. ungespeicherte Änderungen prüfen, Zustand speichern, etc.

        // Event auslösen, damit die View die Anwendung beenden kann
        ExitApplicationRequested?.Invoke(this, EventArgs.Empty);
    }

    private void BuildTreeView()
    {
        TreeNodes = _treeViewBuilder.BuildTreeView(Solution);
    }

    public void OnNodeSelected(TreeNodeViewModel? node)
    {
        Properties.Clear();

        if (node?.DataContext == null || node.DataType == null)
        {
            SelectedNodeType = string.Empty;
            return;
        }

        SelectedNodeType = node.DataType.Name;

        var props = node.DataType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead && !p.PropertyType.IsGenericType && IsSimpleType(p.PropertyType)); // Nur einfache Typen

        foreach (var prop in props)
        {
            Properties.Add(new PropertyViewModel(prop, node.DataContext));
        }
    }

    /// <summary>
    /// Prüft, ob ein Typ ein "einfacher" Typ ist, der im PropertyGrid angezeigt werden soll
    /// </summary>
    private static bool IsSimpleType(Type type)
    {
        // Nullable Types berücksichtigen
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType.IsPrimitive       // int, bool, byte, etc.
            || underlyingType.IsEnum             // Enums
            || underlyingType == typeof(string)  // string
            || underlyingType == typeof(decimal) // decimal
            || underlyingType == typeof(DateTime) // DateTime
            || underlyingType == typeof(DateTimeOffset) // DateTimeOffset
            || underlyingType == typeof(TimeSpan) // TimeSpan
            || underlyingType == typeof(Guid);    // Guid
    }
}