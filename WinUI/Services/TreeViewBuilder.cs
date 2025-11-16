namespace Moba.WinUI.Services;

using Backend.Model;
using SharedUI.ViewModel;
using System.Collections.ObjectModel;

/// <summary>
/// WinUI-specific TreeView builder that creates WinUI JourneyViewModel instances
/// to handle UI thread dispatching with DispatcherQueue.
/// </summary>
public static class TreeViewBuilder
{
    /// <summary>
    /// Creates the TreeView structure from a Solution with WinUI-specific ViewModels.
    /// </summary>
    public static ObservableCollection<TreeNodeViewModel> BuildTreeView(Solution? solution)
    {
        var treeNodes = new ObservableCollection<TreeNodeViewModel>();

        if (solution == null)
            return treeNodes;

        var solutionNode = CreateSolutionNode(solution);

        foreach (var project in solution.Projects)
        {
            var projectNode = CreateProjectNode(project, solution.Projects.IndexOf(project));
            solutionNode.Children.Add(projectNode);
        }

        treeNodes.Add(solutionNode);
        return treeNodes;
    }

    private static TreeNodeViewModel CreateSolutionNode(Solution solution)
    {
        return new TreeNodeViewModel
        {
            DisplayName = "Solution",
            Icon = "\uE8F1",
            IsExpanded = true,
            DataContext = solution,
            DataType = typeof(Solution)
        };
    }

    private static TreeNodeViewModel CreateProjectNode(Project project, int index)
    {
        var displayName = string.IsNullOrEmpty(project.Name)
                 ? $"Project {index + 1}"
                 : project.Name;

        var projectNode = new TreeNodeViewModel
        {
            DisplayName = displayName,
            Icon = "\uE8B7",
            IsExpanded = true,
            DataContext = project,
            DataType = typeof(Project)
        };

        projectNode.Children.Add(CreateSettingNode(project.Settings));

        if (project.Journeys.Count > 0)
        {
            projectNode.Children.Add(CreateJourneysFolder(project.Journeys));
        }

        if (project.Workflows.Count > 0)
        {
            projectNode.Children.Add(CreateWorkflowsFolder(project.Workflows));
        }

        if (project.Locomotives.Count > 0)
        {
            projectNode.Children.Add(CreateLocomotivesFolder(project.Locomotives));
        }

        if (project.Trains.Count > 0)
        {
            projectNode.Children.Add(CreateTrainsFolder(project.Trains));
        }

        return projectNode;
    }

    private static TreeNodeViewModel CreateSettingNode(Settings settings)
    {
        return new TreeNodeViewModel
        {
            DisplayName = "Settings",
            Icon = "\uE713",
            DataContext = settings,
            DataType = typeof(Settings)
        };
    }

    private static TreeNodeViewModel CreateJourneysFolder(List<Journey> journeys)
    {
        var journeysFolder = new TreeNodeViewModel
        {
            DisplayName = "Journeys",
            Icon = "\uE8B7",
            IsExpanded = true
        };

        foreach (var journey in journeys)
        {
            // ✅ Use WinUI-specific JourneyViewModel with DispatcherQueue support
            var journeyViewModel = new Moba.SharedUI.ViewModel.WinUI.JourneyViewModel(journey);
            
            var journeyNode = new TreeNodeViewModel
            {
                DisplayName = journey.Name,
                Icon = "\uE81D",
                DataContext = journeyViewModel,
                DataType = typeof(Moba.SharedUI.ViewModel.WinUI.JourneyViewModel)
            };

            foreach (var station in journey.Stations)
            {
                var stationNode = new TreeNodeViewModel
                {
                    DisplayName = station.Name,
                    Icon = "\uE80F",
                    DataContext = station,
                    DataType = typeof(Station)
                };

                if (station.Platforms.Count > 0)
                {
                    foreach (var platform in station.Platforms)
                    {
                        stationNode.Children.Add(new TreeNodeViewModel
                        {
                            DisplayName = platform.Name,
                            Icon = "\uE81E",
                            DataContext = platform,
                            DataType = typeof(Platform)
                        });
                    }
                }

                journeyNode.Children.Add(stationNode);
            }

            journeysFolder.Children.Add(journeyNode);
        }

        return journeysFolder;
    }

    private static TreeNodeViewModel CreateWorkflowsFolder(List<Workflow> workflows)
    {
        var workflowsFolder = new TreeNodeViewModel
        {
            DisplayName = "Workflows",
            Icon = "\uE8B7",
            IsExpanded = true
        };

        foreach (var workflow in workflows)
        {
            workflowsFolder.Children.Add(new TreeNodeViewModel
            {
                DisplayName = workflow.Name,
                Icon = "\uE9D9",
                DataContext = workflow,
                DataType = typeof(Workflow)
            });
        }

        return workflowsFolder;
    }

    private static TreeNodeViewModel CreateLocomotivesFolder(List<Locomotive> locomotives)
    {
        var locomotivesFolder = new TreeNodeViewModel
        {
            DisplayName = "Locomotives",
            Icon = "\uE8B7",
            IsExpanded = false
        };

        foreach (var loco in locomotives)
        {
            locomotivesFolder.Children.Add(new TreeNodeViewModel
            {
                DisplayName = loco.Name,
                Icon = "\uE81D",
                DataContext = loco,
                DataType = loco.GetType()
            });
        }

        return locomotivesFolder;
    }

    private static TreeNodeViewModel CreateTrainsFolder(List<Train> trains)
    {
        var trainsFolder = new TreeNodeViewModel
        {
            DisplayName = "Trains",
            Icon = "\uE8B7",
            IsExpanded = false
        };

        foreach (var train in trains)
        {
            trainsFolder.Children.Add(new TreeNodeViewModel
            {
                DisplayName = train.Name,
                Icon = "\uE81D",
                DataContext = train,
                DataType = train.GetType()
            });
        }

        return trainsFolder;
    }
}
