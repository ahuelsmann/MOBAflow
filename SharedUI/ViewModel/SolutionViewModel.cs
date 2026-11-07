// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Domain;

using Interface;

using Service;

using System.Collections.ObjectModel;

/// <summary>
/// ViewModel wrapper for Solution model that maintains a hierarchical collection structure
/// for TreeView binding. This is the root of the TreeView hierarchy.
/// Must be manually refreshed when model changes (models don't fire events).
/// </summary>
public sealed partial class SolutionViewModel : ObservableObject, IViewModelWrapper<Solution>
{
    #region Fields
    // Model
    /// <summary>
    /// Gets the underlying solution domain model represented by this ViewModel.
    /// </summary>
    public Solution Model { get; }

    // Services passed to the nested project and workflow ViewModels
    private readonly SolutionViewModelServices _services;

    // Properties
    [ObservableProperty]
    private string _name = string.Empty;
    #endregion

    /// <summary>
    /// Hierarchical collection of Project ViewModels.
    /// Manually synced with Model.Projects via Refresh().
    /// This is bound directly to TreeView.ItemsSource.
    /// </summary>
    public ObservableCollection<ProjectViewModel> Projects { get; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionViewModel"/> class.
    /// </summary>
    /// <param name="model">The solution domain model.</param>
    /// <param name="services">Services used by the nested project and workflow ViewModels.</param>
    public SolutionViewModel(Solution model, SolutionViewModelServices services)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(services);
        Model = model;
        _services = services;
        Refresh();
    }

    /// <summary>
    /// Refreshes all collections from the model. Call this after model changes.
    /// </summary>
    public void Refresh()
    {
        Name = Model.Name;

        // Simple rebuild: Clear and recreate all ViewModels
        // Performance is not critical here (called rarely on Load/Save)
        Projects.Clear();
        foreach (var project in Model.Projects)
        {
            Projects.Add(new ProjectViewModel(project, _services));
        }
    }
}