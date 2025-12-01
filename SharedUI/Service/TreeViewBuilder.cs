// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using Moba.Domain;
using System.Collections.ObjectModel;
using ViewModel;

/// <summary>
/// Responsible for creating the TreeView structure from a SolutionViewModel.
/// ViewModels are provided by SolutionViewModel, not created here.
/// </summary>
public class TreeViewBuilder
{
    /// <summary>
    /// Creates the TreeView structure from a SolutionViewModel.
    /// Uses ViewModels from SolutionViewModel instead of creating new ones.
    /// </summary>
    /// <param name="solutionViewModel">The solution ViewModel containing hierarchical ViewModels</param>
    /// <returns>Observable collection of root tree nodes</returns>
    public ObservableCollection<TreeNodeViewModel> BuildTreeView(SolutionViewModel? solutionViewModel)
    {
        if (solutionViewModel == null)
            return new ObservableCollection<TreeNodeViewModel>();

        var rootNodes = new ObservableCollection<TreeNodeViewModel>();

        // Create Solution root node
        var solutionNode = new TreeNodeViewModel
        {
            DisplayName = solutionViewModel.Name,
            Icon = "\uE8F1", // Folder icon
            DataContext = solutionViewModel.Model,
            DataType = typeof(Solution),
            IsExpanded = true
        };

        // Add Projects
        foreach (var projectVM in solutionViewModel.Projects)
        {
            var projectNode = CreateProjectNode(projectVM);
            solutionNode.Children.Add(projectNode);
        }

        rootNodes.Add(solutionNode);
        return rootNodes;
    }

    private TreeNodeViewModel CreateProjectNode(ProjectViewModel projectVM)
    {
        var projectNode = new TreeNodeViewModel
        {
            DisplayName = projectVM.Name,
            Icon = "\uE8F1", // Project icon
            DataContext = projectVM.Model,
            DataType = typeof(Project),
            IsExpanded = true
        };

        // Journeys Folder
        if (projectVM.Journeys.Count > 0)
        {
            projectNode.Children.Add(CreateJourneysFolder(projectVM.Journeys));
        }

        // Workflows Folder
        if (projectVM.Workflows.Count > 0)
        {
            projectNode.Children.Add(CreateWorkflowsFolder(projectVM.Workflows));
        }

        // Trains Folder
        if (projectVM.Trains.Count > 0)
        {
            projectNode.Children.Add(CreateTrainsFolder(projectVM.Trains));
        }

        // Settings Node
        if (projectVM.Model.Name != null)
        {
            projectNode.Children.Add(CreateSettingsNode(projectVM.Model));
        }

        return projectNode;
    }

    private TreeNodeViewModel CreateJourneysFolder(ObservableCollection<JourneyViewModel> journeys)
    {
        var folderNode = new TreeNodeViewModel
        {
            DisplayName = "Journeys",
            Icon = "\uE8B7", // Train icon
            DataContext = null,
            DataType = null,
            IsExpanded = true
        };

        foreach (var journeyVM in journeys)
        {
            folderNode.Children.Add(CreateJourneyNode(journeyVM));
        }

        return folderNode;
    }

    private TreeNodeViewModel CreateJourneyNode(JourneyViewModel journeyVM)
    {
        var journeyNode = new TreeNodeViewModel
        {
            DisplayName = journeyVM.Name,
            Icon = "\uE8B7",
            DataContext = journeyVM,
            DataType = typeof(JourneyViewModel),
            IsExpanded = false
        };

        // Add Stations
        foreach (var stationVM in journeyVM.Stations)
        {
            journeyNode.Children.Add(CreateStationNode(stationVM));
        }

        return journeyNode;
    }

    private TreeNodeViewModel CreateStationNode(StationViewModel stationVM)
    {
        var stationNode = new TreeNodeViewModel
        {
            DisplayName = stationVM.Name,
            Icon = "\uE80F", // Location icon
            DataContext = stationVM,
            DataType = typeof(StationViewModel),
            IsExpanded = false
        };

        return stationNode;
    }

    private TreeNodeViewModel CreateWorkflowsFolder(ObservableCollection<WorkflowViewModel> workflows)
    {
        var folderNode = new TreeNodeViewModel
        {
            DisplayName = "Workflows",
            Icon = "\uE9D5", // Flow icon
            DataContext = null,
            DataType = null,
            IsExpanded = false
        };

        foreach (var workflowVM in workflows)
        {
            folderNode.Children.Add(CreateWorkflowNode(workflowVM));
        }

        return folderNode;
    }

    private TreeNodeViewModel CreateWorkflowNode(WorkflowViewModel workflowVM)
    {
        var workflowNode = new TreeNodeViewModel
        {
            DisplayName = workflowVM.Name,
            Icon = "\uE9D5",
            DataContext = workflowVM,
            DataType = typeof(WorkflowViewModel),
            IsExpanded = false
        };

        return workflowNode;
    }

    private TreeNodeViewModel CreateTrainsFolder(ObservableCollection<TrainViewModel> trains)
    {
        var folderNode = new TreeNodeViewModel
        {
            DisplayName = "Trains",
            Icon = "\uE7C0", // Train icon
            DataContext = null,
            DataType = null,
            IsExpanded = false
        };

        foreach (var trainVM in trains)
        {
            folderNode.Children.Add(CreateTrainNode(trainVM));
        }

        return folderNode;
    }

    private TreeNodeViewModel CreateTrainNode(TrainViewModel trainVM)
    {
        var trainNode = new TreeNodeViewModel
        {
            DisplayName = trainVM.Name,
            Icon = "\uE7C0",
            DataContext = trainVM,
            DataType = typeof(TrainViewModel),
            IsExpanded = false
        };

        return trainNode;
    }

    private TreeNodeViewModel CreateSettingsNode(Project project)
    {
        var settingsNode = new TreeNodeViewModel
        {
            DisplayName = "Settings",
            Icon = "\uE713", // Settings icon
            DataContext = project,
            DataType = typeof(Project),
            IsExpanded = false
        };

        return settingsNode;
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