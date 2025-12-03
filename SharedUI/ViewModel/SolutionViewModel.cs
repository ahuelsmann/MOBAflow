// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Moba.Domain;

using Moba.SharedUI.Interface;

using System.Collections.ObjectModel;

/// <summary>
/// ViewModel wrapper for Solution model that maintains a hierarchical collection structure
/// for TreeView binding. This is the root of the TreeView hierarchy.
/// Must be manually refreshed when model changes (models don't fire events).
/// </summary>
public partial class SolutionViewModel : ObservableObject
{
    public Solution Model { get; }

    private readonly IUiDispatcher? _dispatcher;

    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// Hierarchical collection of Project ViewModels.
    /// Manually synced with Model.Projects via Refresh().
    /// This is bound directly to TreeView.ItemsSource.
    /// </summary>
    public ObservableCollection<ProjectViewModel> Projects { get; } = new();

    public SolutionViewModel(Solution model, IUiDispatcher? dispatcher = null)
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

        // Smart sync: Only update if collections actually changed
        SyncCollection(Model.Projects, Projects, p => new ProjectViewModel(p, _dispatcher));
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

            // If ViewModel has Refresh method, call it
            if (existingVm is ProjectViewModel pvm)
            {
                pvm.Refresh();
            }
        }
    }

    private static object? GetModel(object vm)
    {
        return vm.GetType().GetProperty("Model")?.GetValue(vm);
    }
}
