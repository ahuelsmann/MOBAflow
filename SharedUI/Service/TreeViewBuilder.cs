namespace Moba.SharedUI.Service;

using Backend.Model;

using System.Collections.ObjectModel;

using ViewModel;

/// <summary>
/// Responsible for creating the TreeView structure from a Solution
/// </summary>
public static class TreeViewBuilder
{
    /// <summary>
    /// Creates the TreeView structure from a Solution
    /// </summary>
    /// <param name="solution">The Solution for which the TreeView should be created</param>
    /// <returns>An ObservableCollection with the root TreeNode</returns>
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
            Icon = "\uE8F1", // Solution icon
            IsExpanded = true,
            DataContext = solution,
            DataType = typeof(Solution)
        };
    }

    private static TreeNodeViewModel CreateProjectNode(Project project, int index)
    {
        // Show Project name if available, otherwise "Project {index + 1}"
        var displayName = string.IsNullOrEmpty(project.Name)
                 ? $"Project {index + 1}"
                 : project.Name;

        var projectNode = new TreeNodeViewModel
        {
            DisplayName = displayName,
            Icon = "\uE8B7", // Folder icon
            IsExpanded = true,
            DataContext = project,
            DataType = typeof(Project)
        };

        // Setting as first child element
        projectNode.Children.Add(CreateSettingNode(project.Setting));

        // Journeys
        if (project.Journeys.Count > 0)
        {
            projectNode.Children.Add(CreateJourneysFolder(project.Journeys));
        }

        // Workflows
        if (project.Workflows.Count > 0)
        {
            projectNode.Children.Add(CreateWorkflowsFolder(project.Workflows));
        }

        // Locomotives
        if (project.Locomotives.Count > 0)
        {
            projectNode.Children.Add(CreateLocomotivesFolder(project.Locomotives));
        }

        // Trains
        if (project.Trains.Count > 0)
        {
            projectNode.Children.Add(CreateTrainsFolder(project.Trains));
        }

        return projectNode;
    }

    private static TreeNodeViewModel CreateSettingNode(Setting setting)
    {
        return new TreeNodeViewModel
        {
            DisplayName = "Setting",
            Icon = "\uE713", // Settings icon
            DataContext = setting,
            DataType = typeof(Setting)
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
                Icon = "\uE9D9", // Flow icon
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