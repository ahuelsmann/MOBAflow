// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Common.Extension;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Windows.ApplicationModel.DataTransfer;

using SharedUI.Interface;
using SharedUI.ViewModel;

/// <summary>
/// Solution page displaying projects list, the Z21 finder and the properties panel.
/// DeleteProject logic moved to MainWindowViewModel with IDialogService.
/// </summary>
internal sealed partial class SolutionPage
{
    private const string Z21DragKey = "Z21";

    public MainWindowViewModel ViewModel { get; }

    public Z21AssignmentViewModel Z21Assignment { get; }

    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private readonly ILogger<SolutionPage>? _logger;

    private GridLength _projectsExpandedWidth = new(1, GridUnitType.Star);
    private GridLength _z21FinderExpandedWidth = new(1, GridUnitType.Star);
    private GridLength _propertiesExpandedWidth = new(2.2, GridUnitType.Star);

    public SolutionPage(
        MainWindowViewModel viewModel,
        Z21AssignmentViewModel z21Assignment,
        AppSettings settings,
        ISettingsService? settingsService = null,
        ILogger<SolutionPage>? logger = null)
    {
        ViewModel = viewModel;
        Z21Assignment = z21Assignment;
        _settings = settings;
        _settingsService = settingsService;
        _logger = logger;
        InitializeComponent();

        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        Z21Assignment.PropertyChanged -= Z21Assignment_PropertyChanged;
        Z21Assignment.PropertyChanged += Z21Assignment_PropertyChanged;
        RestoreLayout();
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        HandlePageUnloadedAsync().Observe(ex => _logger?.LogWarning(ex, "Persist layout on unload failed"));
    }

    private async Task HandlePageUnloadedAsync()
    {
        try
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            Z21Assignment.PropertyChanged -= Z21Assignment_PropertyChanged;
            SaveLayout();
            if (_settingsService != null)
            {
                await _settingsService.SaveSettingsAsync(_settings);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Persist layout on unload failed");
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsProjectListExpanded))
        {
            ApplyColumnState(ViewModel.IsProjectListExpanded, ColProjects, ref _projectsExpandedWidth);
        }
        else if (e.PropertyName == nameof(ViewModel.IsProjectPropertiesExpanded))
        {
            ApplyColumnState(ViewModel.IsProjectPropertiesExpanded, ColProperties, ref _propertiesExpandedWidth);
        }
    }

    private void Z21Assignment_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Z21Assignment.IsFinderExpanded))
        {
            ApplyColumnState(Z21Assignment.IsFinderExpanded, ColZ21Finder, ref _z21FinderExpandedWidth);
        }
    }

    private void Z21ListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        // A running search rebuilds the list; only current entries can be dragged.
        if (Z21Assignment.IsSearching || e.Items.FirstOrDefault() is not Z21AssignmentCandidate z21)
        {
            e.Cancel = true;
            return;
        }

        e.Data.Properties.Add(Z21DragKey, z21);
        e.Data.RequestedOperation = DataPackageOperation.Link;
        e.Data.SetText(z21.Title);
    }

    private void Z21ListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { DataContext: Z21AssignmentCandidate z21 })
        {
            Z21Assignment.AssignToSelectedProjectCommand.Execute(z21);
        }
    }

    private void ProjectListView_DragOver(object sender, DragEventArgs e)
    {
        if (!e.DataView.Properties.TryGetValue(Z21DragKey, out var value) || value is not Z21AssignmentCandidate z21)
        {
            return;
        }

        var project = ProjectAt(e.OriginalSource);
        var canAssign = project is not null && Z21Assignment.CanAssignToProject(z21, project);
        e.AcceptedOperation = canAssign ? DataPackageOperation.Link : DataPackageOperation.None;
        e.DragUIOverride.Caption = (project, canAssign) switch
        {
            (null, _) => "Drop on a project",
            (_, false) => "Another project uses this Z21",
            _ => $"Assign to {project.Name}"
        };
        e.DragUIOverride.IsCaptionVisible = true;
        e.DragUIOverride.IsContentVisible = true;
        e.DragUIOverride.IsGlyphVisible = true;
    }

    private void ProjectListView_Drop(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.TryGetValue(Z21DragKey, out var value)
            && value is Z21AssignmentCandidate z21
            && ProjectAt(e.OriginalSource) is { } project)
        {
            Z21Assignment.AssignToProject(z21, project);
        }
    }

    private static ProjectViewModel? ProjectAt(object source) =>
        source is FrameworkElement { DataContext: ProjectViewModel project } ? project : null;

    private void RestoreLayout()
    {
        var layout = _settings.Layout.SolutionPage;

        _projectsExpandedWidth = ToStarGridLength(layout.ProjectListColumnStarValue, _projectsExpandedWidth);
        _z21FinderExpandedWidth = ToStarGridLength(layout.Z21FinderColumnStarValue, _z21FinderExpandedWidth);
        _propertiesExpandedWidth = ToStarGridLength(layout.PropertiesColumnStarValue, _propertiesExpandedWidth);

        RestoreColumnState(layout.IsProjectListExpanded, ColProjects, _projectsExpandedWidth);
        RestoreColumnState(layout.IsZ21FinderExpanded, ColZ21Finder, _z21FinderExpandedWidth);
        RestoreColumnState(layout.IsPropertiesExpanded, ColProperties, _propertiesExpandedWidth);

        if (ViewModel.IsProjectListExpanded != layout.IsProjectListExpanded)
        {
            ViewModel.IsProjectListExpanded = layout.IsProjectListExpanded;
        }
        if (ViewModel.IsProjectPropertiesExpanded != layout.IsPropertiesExpanded)
        {
            ViewModel.IsProjectPropertiesExpanded = layout.IsPropertiesExpanded;
        }
        if (Z21Assignment.IsFinderExpanded != layout.IsZ21FinderExpanded)
        {
            Z21Assignment.IsFinderExpanded = layout.IsZ21FinderExpanded;
        }
    }

    private void SaveLayout()
    {
        var layout = _settings.Layout.SolutionPage;

        layout.IsProjectListExpanded = ViewModel.IsProjectListExpanded;
        layout.IsPropertiesExpanded = ViewModel.IsProjectPropertiesExpanded;
        layout.IsZ21FinderExpanded = Z21Assignment.IsFinderExpanded;
        layout.Z21FinderColumnStarValue = GetCurrentStarValue(ColZ21Finder, _z21FinderExpandedWidth);
        layout.ProjectListColumnStarValue = GetCurrentStarValue(ColProjects, _projectsExpandedWidth);
        layout.PropertiesColumnStarValue = GetCurrentStarValue(ColProperties, _propertiesExpandedWidth);
    }

    private static void ApplyColumnState(bool isExpanded, ColumnDefinition column, ref GridLength rememberedWidth)
    {
        if (!isExpanded)
        {
            if (!column.Width.IsAuto)
            {
                rememberedWidth = column.Width;
            }

            column.Width = GridLength.Auto;
        }
        else
        {
            column.Width = rememberedWidth;
        }
    }

    private static GridLength ToStarGridLength(double starValue, GridLength fallback)
    {
        return starValue > 0
            ? new GridLength(starValue, GridUnitType.Star)
            : fallback;
    }

    private static void RestoreColumnState(bool isExpanded, ColumnDefinition column, GridLength rememberedWidth)
    {
        column.Width = isExpanded ? rememberedWidth : GridLength.Auto;
    }

    private static double GetCurrentStarValue(ColumnDefinition column, GridLength fallback)
    {
        if (column.Width.IsStar)
        {
            return column.Width.Value;
        }

        return fallback.IsStar ? fallback.Value : 1;
    }
}