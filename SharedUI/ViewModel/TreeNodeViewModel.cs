// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using CommunityToolkit.Mvvm.ComponentModel;

using System.Collections.ObjectModel;

namespace Moba.SharedUI.ViewModel;

public partial class TreeNodeViewModel : ObservableObject
{
    [ObservableProperty]
    private string displayName = string.Empty;

    [ObservableProperty]
    private string icon = "\uE8B7"; // Document icon

    [ObservableProperty]
    private bool isExpanded;

    [ObservableProperty]
    private ObservableCollection<TreeNodeViewModel> children = [];

    public object? DataContext { get; set; }
    public Type? DataType { get; set; }

    /// <summary>
    /// Updates the DisplayName based on the DataContext.
    /// Called when properties in the PropertyGrid are changed.
    /// </summary>
    public void RefreshDisplayName()
    {
        if (DataContext == null)
            return;

        // Try to read the "Name" property
        var nameProp = DataContext.GetType().GetProperty("Name");
        if (nameProp != null)
        {
            var newName = nameProp.GetValue(DataContext)?.ToString();
            if (!string.IsNullOrEmpty(newName) && newName != DisplayName)
            {
                DisplayName = newName;
            }
        }
    }
}