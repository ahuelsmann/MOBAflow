// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Domain;
using Interface;
using System.Collections.ObjectModel;

/// <summary>
/// ViewModel wrapper for Project model that maintains a hierarchical collection structure
/// for TreeView binding. Must be manually refreshed when model changes (models don't fire events).
/// </summary>
public partial class ProjectViewModel : ObservableObject, IViewModelWrapper<Project>
{
    #region Fields
    // Model
    public Project Model { get; }
    
    // Optional Services
    private readonly IUiDispatcher? _dispatcher;
    
    // Properties (ObservableProperty fields)
    [ObservableProperty]
    private string _name;
    #endregion

    partial void OnNameChanged(string value)
    {
        // Synchronize back to Model
        Model.Name = value;
    }

    /// <summary>
    /// Hierarchical collection of Journey ViewModels.
    /// Manually synced with Model.Journeys via Refresh().
    /// </summary>
    public ObservableCollection<JourneyViewModel> Journeys { get; } = new();

    /// <summary>
    /// Hierarchical collection of Workflow ViewModels.
    /// Manually synced with Model.Workflows via Refresh().
    /// </summary>
    public ObservableCollection<WorkflowViewModel> Workflows { get; } = new();

    /// <summary>
    /// Hierarchical collection of Train ViewModels.
    /// Manually synced with Model.Trains via Refresh().
    /// </summary>
    public ObservableCollection<TrainViewModel> Trains { get; } = new();

    /// <summary>
    /// Hierarchical collection of Locomotive ViewModels.
    /// Manually synced with Model.Locomotives via Refresh().
    /// </summary>
    public ObservableCollection<LocomotiveViewModel> Locomotives { get; } = new();

    /// <summary>
    /// Hierarchical collection of Wagon ViewModels (combined PassengerWagons and GoodsWagons).
    /// Manually synced with Model via Refresh().
    /// </summary>
    public ObservableCollection<WagonViewModel> Wagons { get; } = new();

    /// <summary>
    /// Separate collection for PassengerWagon ViewModels (for Train Tab UI).
    /// </summary>
    public ObservableCollection<PassengerWagonViewModel> PassengerWagons { get; } = new();

    /// <summary>
    /// Separate collection for GoodsWagon ViewModels (for Train Tab UI).
    /// </summary>
    public ObservableCollection<GoodsWagonViewModel> GoodsWagons { get; } = new();

    /// <summary>
    /// Observable collection of FeedbackPoints for UI binding.
    /// Manually synced with Model.FeedbackPoints via Refresh().
    /// </summary>
    public ObservableCollection<FeedbackPointOnTrack> FeedbackPoints { get; } = new();

    public ProjectViewModel(Project model, IUiDispatcher? dispatcher = null)
    {
        Model = model;
        _dispatcher = dispatcher;
        _name = model.Name;  // Initialize from Model
        Refresh();
    }

    /// <summary>
    /// Refreshes all collections from the model. Call this after model changes.
    /// Simple rebuild approach - performance is not critical (called rarely on Load/Save).
    /// </summary>
    public void Refresh()
    {
        // Update scalar properties from Model
        Name = Model.Name;
        
        // Clear and rebuild all collections
        Journeys.Clear();
        foreach (var j in Model.Journeys)
            Journeys.Add(new JourneyViewModel(j, Model, _dispatcher));

        Workflows.Clear();
        foreach (var w in Model.Workflows)
            Workflows.Add(new WorkflowViewModel(w));

        Trains.Clear();
        foreach (var t in Model.Trains)
            Trains.Add(new TrainViewModel(t, Model));

        Locomotives.Clear();
        foreach (var l in Model.Locomotives)
            Locomotives.Add(new LocomotiveViewModel(l));

        // Wagons: Combine PassengerWagons and GoodsWagons
        Wagons.Clear();
        foreach (var pw in Model.PassengerWagons)
            Wagons.Add(new PassengerWagonViewModel(pw));
        foreach (var gw in Model.GoodsWagons)
            Wagons.Add(new GoodsWagonViewModel(gw));

        // Separate collections for Train Tab UI
        PassengerWagons.Clear();
        foreach (var pw in Model.PassengerWagons)
            PassengerWagons.Add(new PassengerWagonViewModel(pw));

        GoodsWagons.Clear();
        foreach (var gw in Model.GoodsWagons)
            GoodsWagons.Add(new GoodsWagonViewModel(gw));

        FeedbackPoints.Clear();
        foreach (var fp in Model.FeedbackPoints)
            FeedbackPoints.Add(fp);
    }
}
