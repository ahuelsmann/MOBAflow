// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Domain;
using Interface;
using Moba.Sound;
using System.Collections.ObjectModel;

/// <summary>
/// ViewModel wrapper for Solution model that maintains a hierarchical collection structure
/// for TreeView binding. This is the root of the TreeView hierarchy.
/// Must be manually refreshed when model changes (models don't fire events).
/// </summary>
public partial class SolutionViewModel : ObservableObject, IViewModelWrapper<Solution>
{
    #region Fields
    // Model
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

    public SolutionViewModel(Solution model, IUiDispatcher? dispatcher = null, IIoService? ioService = null, ISoundPlayer? soundPlayer = null)
    {
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
