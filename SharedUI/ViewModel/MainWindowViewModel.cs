namespace Moba.SharedUI.ViewModel;

using Backend.Model;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Service;

using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

public partial class MainWindowViewModel(IIoService ioService) : ObservableObject
{
    private readonly IIoService _ioService = ioService;

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

    private void BuildTreeView()
    {
        TreeNodes.Clear();

        if (Solution == null) return;

        var solutionNode = new TreeNodeViewModel
        {
            DisplayName = "Solution",
            Icon = "\uE8F1", // Solution icon
            IsExpanded = true,
            DataContext = Solution,
            DataType = typeof(Solution)
        };

        foreach (var project in Solution.Projects)
        {
            var projectNode = new TreeNodeViewModel
            {
                DisplayName = $"Project {Solution.Projects.IndexOf(project) + 1}",
                Icon = "\uE8B7", // Folder icon
                IsExpanded = true,
                DataContext = project,
                DataType = typeof(Project)
            };

            // Journeys
            if (project.Journeys.Count > 0)
            {
                var journeysFolder = new TreeNodeViewModel
                {
                    DisplayName = "Journeys",
                    Icon = "\uE8B7",
                    IsExpanded = true
                };

                foreach (var journey in project.Journeys)
                {
                    var journeyNode = new TreeNodeViewModel
                    {
                        DisplayName = journey.Name,
                        Icon = "\uE81D", // Train icon
                        DataContext = journey,
                        DataType = typeof(Journey)
                    };

                    // Stations
                    foreach (var station in journey.Stations)
                    {
                        journeyNode.Children.Add(new TreeNodeViewModel
                        {
                            DisplayName = station.Name,
                            Icon = "\uE80F", // Location icon
                            DataContext = station,
                            DataType = typeof(Station)
                        });
                    }

                    journeysFolder.Children.Add(journeyNode);
                }

                projectNode.Children.Add(journeysFolder);
            }

            // Workflows
            if (project.Workflows.Count > 0)
            {
                var workflowsFolder = new TreeNodeViewModel
                {
                    DisplayName = "Workflows",
                    Icon = "\uE8B7",
                    IsExpanded = true
                };

                foreach (var workflow in project.Workflows)
                {
                    workflowsFolder.Children.Add(new TreeNodeViewModel
                    {
                        DisplayName = workflow.Name,
                        Icon = "\uE9D9", // Flow icon
                        DataContext = workflow,
                        DataType = typeof(Workflow)
                    });
                }

                projectNode.Children.Add(workflowsFolder);
            }

            // Locomotives
            if (project.Locomotives.Count > 0)
            {
                var locomotivesFolder = new TreeNodeViewModel
                {
                    DisplayName = "Locomotives",
                    Icon = "\uE8B7",
                    IsExpanded = false
                };

                foreach (var loco in project.Locomotives)
                {
                    locomotivesFolder.Children.Add(new TreeNodeViewModel
                    {
                        DisplayName = loco.Name,
                        Icon = "\uE81D",
                        DataContext = loco,
                        DataType = loco.GetType()
                    });
                }

                projectNode.Children.Add(locomotivesFolder);
            }

            // Trains
            if (project.Trains.Count > 0)
            {
                var trainsFolder = new TreeNodeViewModel
                {
                    DisplayName = "Trains",
                    Icon = "\uE8B7",
                    IsExpanded = false
                };

                foreach (var train in project.Trains)
                {
                    trainsFolder.Children.Add(new TreeNodeViewModel
                    {
                        DisplayName = train.Name,
                        Icon = "\uE81D",
                        DataContext = train,
                        DataType = train.GetType()
                    });
                }

                projectNode.Children.Add(trainsFolder);
            }

            solutionNode.Children.Add(projectNode);
        }

        TreeNodes.Add(solutionNode);
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
            .Where(p => p.CanRead && !p.PropertyType.IsGenericType);

        foreach (var prop in props)
        {
            Properties.Add(new PropertyViewModel(prop, node.DataContext));
        }
    }
}