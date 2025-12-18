// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.Input;
using Domain;

/// <summary>
/// MainWindowViewModel - Feedback Points Management
/// Handles FeedbackPoint CRUD operations.
/// </summary>
public partial class MainWindowViewModel
{
    #region FeedbackPoint CRUD Commands
    [RelayCommand(CanExecute = nameof(CanAddFeedbackPoint))]
    private void AddFeedbackPoint()
    {
        if (SelectedProject == null) return;

        // Find the next available InPort (start from 1)
        uint nextInPort = 1;
        while (SelectedProject.Model.FeedbackPoints.Any(fp => fp.InPort == nextInPort))
        {
            nextInPort++;
        }

        var newFeedbackPoint = new FeedbackPointOnTrack
        {
            InPort = nextInPort,
            Name = $"Feedback Point {nextInPort}",
            Description = null
        };

        // Add to Domain model
        SelectedProject.Model.FeedbackPoints.Add(newFeedbackPoint);
        
        // Add to ProjectViewModel's ObservableCollection for UI update
        SelectedProject.FeedbackPoints.Add(newFeedbackPoint);

        SaveSolutionCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanDeleteFeedbackPoint))]
    private void DeleteFeedbackPoint()
    {
        if (SelectedProject == null || SelectedFeedbackPoint == null) return;

        // Remove from Domain model
        SelectedProject.Model.FeedbackPoints.Remove(SelectedFeedbackPoint);
        
        // Remove from ProjectViewModel's ObservableCollection for UI update
        SelectedProject.FeedbackPoints.Remove(SelectedFeedbackPoint);
        
        SelectedFeedbackPoint = null;

        SaveSolutionCommand.NotifyCanExecuteChanged();
    }

    private bool CanAddFeedbackPoint() => SelectedProject != null;
    private bool CanDeleteFeedbackPoint() => SelectedFeedbackPoint != null;
    #endregion
}
