// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Moba.Domain;

using Moba.SharedUI.Interface;

using System.Collections.ObjectModel;

/// <summary>
/// ViewModel wrapper for Project model that maintains a hierarchical collection structure
/// for TreeView binding. Must be manually refreshed when model changes (models don't fire events).
/// </summary>
public partial class ProjectViewModel : ObservableObject
{
    public Project Model { get; }

    private readonly IUiDispatcher? _dispatcher;

    [ObservableProperty]
    private string _name = string.Empty;

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

    public ProjectViewModel(Project model, IUiDispatcher? dispatcher = null)
    {
        Model = model;
        _dispatcher = dispatcher;
        Refresh();
    }

    /// <summary>
    /// Refreshes all collections from the model. Call this after model changes.
    /// </summary>
    public void Refresh()
    {
        Name = Model.Name;

        // Smart sync: Reuse existing ViewModels where possible
        SyncCollection(Model.Journeys, Journeys, j => new JourneyViewModel(j, _dispatcher));
        SyncCollection(Model.Workflows, Workflows, w => new WorkflowViewModel(w));
        SyncCollection(Model.Trains, Trains, t => new TrainViewModel(t, _dispatcher));
        SyncCollection(Model.Locomotives, Locomotives, l => new LocomotiveViewModel(l));
        
        // Wagons: Combine PassengerWagons and GoodsWagons
        var allWagons = Model.PassengerWagons.Cast<Wagon>()
            .Concat(Model.GoodsWagons.Cast<Wagon>())
            .ToList();
        
        SyncCollection(allWagons, Wagons, w =>
        {
            return w switch
            {
                PassengerWagon pw => (WagonViewModel)new PassengerWagonViewModel(pw),
                GoodsWagon gw => new GoodsWagonViewModel(gw),
                _ => new WagonViewModel(w)
            };
        });
    }

    /// <summary>
    /// Smart collection sync: Updates ViewModel collection to match Model collection.
    /// Reuses existing ViewModels where possible (by Model reference).
    /// </summary>
    private void SyncCollection<TModel, TViewModel>(
        List<TModel> modelCollection,
        ObservableCollection<TViewModel> vmCollection,
        Func<TModel, TViewModel> createVm)
        where TViewModel : class
    {
        // Remove ViewModels for models that no longer exist
        for (int i = vmCollection.Count - 1; i >= 0; i--)
        {
            var vm = vmCollection[i];
            var model = GetModel(vm);
            if (model == null || !modelCollection.Contains((TModel)model))
            {
                vmCollection.RemoveAt(i);
            }
        }

        // Add or update ViewModels for each model
        for (int i = 0; i < modelCollection.Count; i++)
        {
            var model = modelCollection[i];
            var existingVm = vmCollection.FirstOrDefault(vm => EqualityComparer<TModel>.Default.Equals((TModel)GetModel(vm)!, model));

            if (existingVm == null)
            {
                // Insert new ViewModel at correct index
                if (i < vmCollection.Count)
                {
                    vmCollection.Insert(i, createVm(model));
                }
                else
                {
                    vmCollection.Add(createVm(model));
                }
            }
            else if (vmCollection.IndexOf(existingVm) != i)
            {
                // Reorder if needed
                vmCollection.Move(vmCollection.IndexOf(existingVm), i);
            }
        }
    }

    private static object? GetModel(object vm)
    {
        return vm.GetType().GetProperty("Model")?.GetValue(vm);
    }
}
