// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Domain;
using Interface;
using Sound;
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

    // Optional Services
    private readonly IUiDispatcher? _dispatcher;
    private readonly IIoService? _ioService;
    private readonly ISoundPlayer? _soundPlayer;

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
    /// <param name="dispatcher">Optional UI dispatcher used by nested project ViewModels.</param>
    /// <param name="ioService">Optional IO service used by nested ViewModels.</param>
    /// <param name="soundPlayer">Optional sound player used by nested ViewModels.</param>
    public SolutionViewModel(Solution model, IUiDispatcher? dispatcher = null, IIoService? ioService = null, ISoundPlayer? soundPlayer = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
        _dispatcher = dispatcher;
        _ioService = ioService;
        _soundPlayer = soundPlayer;
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
            Projects.Add(new ProjectViewModel(project, _dispatcher, _ioService, _soundPlayer));
        }
    }
}
