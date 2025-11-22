// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using Backend.Model;

using System.Collections.ObjectModel;

using ViewModel;
using Moba.SharedUI.Interface; // ✅ Updated namespace

/// <summary>
/// Responsible for creating the TreeView structure from a Solution
/// </summary>
public class TreeViewBuilder
{
    private readonly IJourneyViewModelFactory _journeyViewModelFactory;
    private readonly IStationViewModelFactory _stationViewModelFactory;
    private readonly IWorkflowViewModelFactory _workflowViewModelFactory;
    private readonly ILocomotiveViewModelFactory _locomotiveViewModelFactory;
    private readonly ITrainViewModelFactory _trainViewModelFactory;
    private readonly IWagonViewModelFactory _wagonViewModelFactory;

    public TreeViewBuilder(
        IJourneyViewModelFactory journeyViewModelFactory,
        IStationViewModelFactory stationViewModelFactory,
        IWorkflowViewModelFactory workflowViewModelFactory,
        ILocomotiveViewModelFactory locomotiveViewModelFactory,
        ITrainViewModelFactory trainViewModelFactory,
        IWagonViewModelFactory wagonViewModelFactory)
    {
        _journeyViewModelFactory = journeyViewModelFactory;
        _stationViewModelFactory = stationViewModelFactory;
        _workflowViewModelFactory = workflowViewModelFactory;
        _locomotiveViewModelFactory = locomotiveViewModelFactory;
        _trainViewModelFactory = trainViewModelFactory;
        _wagonViewModelFactory = wagonViewModelFactory;
    }

    /// <summary>
    /// Creates the TreeView structure from a Solution
    /// </summary>
    /// <param name="solution">The Solution for which the TreeView should be created</param>
    /// <returns>An ObservableCollection with the root TreeNode</returns>
    public ObservableCollection<TreeNodeViewModel> BuildTreeView(Solution? solution)
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
        var solutionNode = new TreeNodeViewModel
        {
            DisplayName = "Solution",
            Icon = "\uE8F1", // Solution icon
            IsExpanded = true,
            DataContext = solution,
            DataType = typeof(Solution)
        };

        // ✅ Settings as direct child of Solution (global settings)
        solutionNode.Children.Add(CreateSettingNode(solution.Settings));

        return solutionNode;
    }

    private TreeNodeViewModel CreateProjectNode(Project project, int index)
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

        // ✅ Settings removed from Project (now at Solution level)

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

    private static TreeNodeViewModel CreateSettingNode(Settings settings)
    {
        return new TreeNodeViewModel
        {
            DisplayName = "Settings",
            Icon = "\uE713", // Settings icon
            DataContext = settings,
            DataType = typeof(Settings)
        };
    }

    private TreeNodeViewModel CreateJourneysFolder(List<Journey> journeys)
    {
        var journeysFolder = new TreeNodeViewModel
        {
            DisplayName = "Journeys",
            Icon = "\uE8B7",
            IsExpanded = true
        };

        foreach (var journey in journeys)
        {
            // Wrap Journey Model in JourneyViewModel for proper UI binding (using Factory)
            var journeyViewModel = _journeyViewModelFactory.Create(journey);
            
            var journeyNode = new TreeNodeViewModel
            {
                DisplayName = journey.Name,
                Icon = "\uE81D", // Train icon
                DataContext = journeyViewModel,  // ← Use ViewModel instead of Model
                DataType = journeyViewModel.GetType()  // ← Use actual ViewModel type
            };

            // Stations - use ViewModels from JourneyViewModel
            foreach (var stationVM in journeyViewModel.Stations)
            {
                var stationNode = new TreeNodeViewModel
                {
                    DisplayName = stationVM.Name,
                    Icon = "\uE80F", // Location icon
                    DataContext = stationVM,  // ← Use StationViewModel
                    DataType = typeof(StationViewModel)
                };

                journeyNode.Children.Add(stationNode);
            }

            journeysFolder.Children.Add(journeyNode);
        }

        return journeysFolder;
    }

    private TreeNodeViewModel CreateWorkflowsFolder(List<Workflow> workflows)
    {
        var workflowsFolder = new TreeNodeViewModel
        {
            DisplayName = "Workflows",
            Icon = "\uE8B7",
            IsExpanded = true
        };

        foreach (var workflow in workflows)
        {
            // ✅ Use Factory instead of direct instantiation
            var workflowVM = _workflowViewModelFactory.Create(workflow);

            workflowsFolder.Children.Add(new TreeNodeViewModel
            {
                DisplayName = workflow.Name,
                Icon = "\uE9D9", // Flow icon
                DataContext = workflowVM,  // ← Use WorkflowViewModel
                DataType = typeof(WorkflowViewModel)
            });
        }

        return workflowsFolder;
    }

    private TreeNodeViewModel CreateLocomotivesFolder(List<Locomotive> locomotives)
    {
        var locomotivesFolder = new TreeNodeViewModel
        {
            DisplayName = "Locomotives",
            Icon = "\uE8B7",
            IsExpanded = false
        };

        foreach (var loco in locomotives)
        {
            // ✅ Use Factory instead of direct instantiation
            var locoVM = _locomotiveViewModelFactory.Create(loco);

            locomotivesFolder.Children.Add(new TreeNodeViewModel
            {
                DisplayName = loco.Name,
                Icon = "\uE81D",
                DataContext = locoVM,  // ← Use LocomotiveViewModel
                DataType = typeof(LocomotiveViewModel)
            });
        }

        return locomotivesFolder;
    }

    private TreeNodeViewModel CreateTrainsFolder(List<Train> trains)
    {
        var trainsFolder = new TreeNodeViewModel
        {
            DisplayName = "Trains",
            Icon = "\uE8B7",
            IsExpanded = false
        };

        foreach (var train in trains)
        {
            // ✅ Use Factory instead of direct instantiation
            var trainVM = _trainViewModelFactory.Create(train);

            var trainNode = new TreeNodeViewModel
            {
                DisplayName = train.Name,
                Icon = "\uE81D",
                DataContext = trainVM,  // ← Use TrainViewModel
                DataType = typeof(TrainViewModel)
            };

            // Add Locomotives under Train (use Factory for each)
            foreach (var loco in train.Locomotives)
            {
                var locoVM = _locomotiveViewModelFactory.Create(loco);
                
                trainNode.Children.Add(new TreeNodeViewModel
                {
                    DisplayName = locoVM.Name,
                    Icon = "\uE81D",
                    DataContext = locoVM,
                    DataType = typeof(LocomotiveViewModel)
                });
            }

            // Add Wagons under Train (use Factory for each)
            foreach (var wagon in train.Wagons)
            {
                var wagonVM = _wagonViewModelFactory.Create(wagon);
                
                trainNode.Children.Add(new TreeNodeViewModel
                {
                    DisplayName = wagonVM.Name,
                    Icon = "\uE7C1", // Box icon for wagon
                    DataContext = wagonVM,
                    DataType = typeof(WagonViewModel)
                });
            }

            trainsFolder.Children.Add(trainNode);
        }

        return trainsFolder;
    }

    /// <summary>
    /// Updates an existing TreeNode's display name based on its DataContext.
    /// Used for real-time sync when properties change.
    /// </summary>
    public void UpdateNodeDisplayName(TreeNodeViewModel node)
    {
        if (node?.DataContext == null) return;

        // Update display name based on DataContext type
        var name = node.DataContext switch
        {
            JourneyViewModel jvm => jvm.Name,
            StationViewModel svm => svm.Name,
            WorkflowViewModel wvm => wvm.Name,
            TrainViewModel tvm => tvm.Name,
            LocomotiveViewModel lvm => lvm.Name,
            WagonViewModel wvm => wvm.Name,
            Journey j => j.Name,
            Station s => s.Name,
            Workflow w => w.Name,
            Train t => t.Name,
            Locomotive l => l.Name,
            Wagon w => w.Name,
            _ => node.DisplayName
        };

        if (!string.IsNullOrEmpty(name))
        {
            node.DisplayName = name;
        }
    }

    /// <summary>
    /// Recursively updates all display names in a tree structure.
    /// Useful after loading a solution or making bulk changes.
    /// </summary>
    public void UpdateAllDisplayNames(ObservableCollection<TreeNodeViewModel> nodes)
    {
        foreach (var node in nodes)
        {
            UpdateNodeDisplayName(node);
            
            if (node.Children.Count > 0)
            {
                UpdateAllDisplayNames(node.Children);
            }
        }
    }
}