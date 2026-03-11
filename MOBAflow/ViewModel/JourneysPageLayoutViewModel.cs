using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Moba.Common.Configuration;
using SharedUI.ViewModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Moba.WinUI.ViewModel;

/// <summary>
/// WinUI-specific implementation of PageLayoutViewModel for JourneysPage.
/// Handles VisualStateManager integration and GridSplitter-specific operations.
/// </summary>
public class JourneysPageLayoutViewModel : PageLayoutViewModel
{
    private Grid? _associatedGrid;
    private Control? _associatedControl;

    /// <summary>
    /// Gets or sets the associated Grid for layout operations.
    /// </summary>
    public Grid? AssociatedGrid
    {
        get => _associatedGrid;
        set
        {
            if (SetProperty(ref _associatedGrid, value))
            {
                UpdateColumnAssociations();
            }
        }
    }

    /// <summary>
    /// Gets or sets the associated control for VisualStateManager operations.
    /// </summary>
    public Control? AssociatedControl
    {
        get => _associatedControl;
        set
        {
            if (SetProperty(ref _associatedControl, value))
            {
                UpdateVisualState();
            }
        }
    }

    public JourneysPageLayoutViewModel(AppSettings settings) : base("JourneysPage", settings)
    {
        // Initialize columns for JourneysPage layout
        AddColumn("CityLibrary", 300, 200, 500);
        AddColumn("Splitter1", 16, 16, 16);
        AddColumn("MainContent", 400, 300, double.NaN);
        AddColumn("Splitter2", 16, 16, 16);
        AddColumn("WorkflowLibrary", 250, 200, 400);
    }

    /// <summary>
    /// Applies column states to the associated WinUI Grid.
    /// </summary>
    public void ApplyToWinUIGrid()
    {
        if (_associatedGrid == null) return;

        base.ApplyToGrid(_associatedGrid);
        UpdateVisualState();
    }

    /// <summary>
    /// Updates column states from the associated WinUI Grid.
    /// </summary>
    public void UpdateFromWinUIGrid()
    {
        if (_associatedGrid == null) return;

        base.UpdateFromGrid(_associatedGrid);
    }

    /// <summary>
    /// Updates the VisualStateManager state based on current column states.
    /// </summary>
    private void UpdateVisualState()
    {
        if (_associatedControl == null) return;

        var stateName = GenerateStateName();
        VisualStateManager.GoToState(_associatedControl, stateName, true);
    }

    /// <summary>
    /// Updates column associations with the WinUI Grid.
    /// </summary>
    private void UpdateColumnAssociations()
    {
        if (_associatedGrid == null) return;

        // Update all columns with WinUI-specific associations
        foreach (var column in Columns.Values)
        {
            // Column-specific associations can be added here if needed
        }
    }

    /// <summary>
    /// Sets the expanded state of a column and updates the visual state.
    /// </summary>
    /// <param name="columnName">The name of the column to update.</param>
    /// <param name="isExpanded">Whether the column should be expanded.</param>
    public new void SetColumnExpanded(string columnName, bool isExpanded)
    {
        base.SetColumnExpanded(columnName, isExpanded);
        UpdateVisualState();
    }

    /// <summary>
    /// Toggles the expanded state of a column and updates the visual state.
    /// </summary>
    /// <param name="columnName">The name of the column to toggle.</param>
    public new void ToggleColumn(string columnName)
    {
        base.ToggleColumn(columnName);
        UpdateVisualState();
    }

    /// <summary>
    /// Sets the width of a column, persists the change, and updates the WinUI Grid.
    /// </summary>
    /// <param name="columnName">The name of the column to update.</param>
    /// <param name="width">The new width for the column.</param>
    public new void SetColumnWidth(string columnName, double width)
    {
        base.SetColumnWidth(columnName, width);
        ApplyToWinUIGrid();
    }

    /// <summary>
    /// Gets the column name by its index in the WinUI Grid.
    /// </summary>
    /// <param name="index">The column index.</param>
    /// <returns>The column name, or null if not found.</returns>
    protected override string? GetColumnNameByIndex(int index)
    {
        return index switch
        {
            0 => "CityLibrary",
            1 => "Splitter1", 
            2 => "MainContent",
            3 => "Splitter2",
            4 => "WorkflowLibrary",
            _ => null
        };
    }

    /// <summary>
    /// Handles property changes from columns to trigger visual state updates.
    /// </summary>
    protected override void OnColumnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ColumnViewModel.IsExpanded) || 
            e.PropertyName == nameof(ColumnViewModel.Width))
        {
            UpdateVisualState();
            PersistLayout();
        }
    }
}
