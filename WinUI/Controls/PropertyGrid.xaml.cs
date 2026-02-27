// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

/// <summary>
/// Visual Studio-style PropertyGrid control.
/// Displays object properties in a Name/Value list with optional categorization
/// and description of the selected property.
/// </summary>
internal sealed partial class PropertyGrid
{
    #region Dependency Properties

    /// <summary>
    /// The displayed property items.
    /// </summary>
    public static readonly DependencyProperty DisplayItemsProperty =
        DependencyProperty.Register(
            nameof(DisplayItems),
            typeof(ObservableCollection<PropertyGridItem>),
            typeof(PropertyGrid),
            new PropertyMetadata(null));

    /// <summary>
    /// The currently selected property.
    /// </summary>
    public static readonly DependencyProperty SelectedPropertyProperty =
        DependencyProperty.Register(
            nameof(SelectedProperty),
            typeof(PropertyGridItem),
            typeof(PropertyGrid),
            new PropertyMetadata(null));

    /// <summary>
    /// Available objects for the object selector.
    /// </summary>
    public static readonly DependencyProperty AvailableObjectsProperty =
        DependencyProperty.Register(
            nameof(AvailableObjects),
            typeof(ObservableCollection<string>),
            typeof(PropertyGrid),
            new PropertyMetadata(null));

    /// <summary>
    /// Name of the currently selected object.
    /// </summary>
    public static readonly DependencyProperty SelectedObjectNameProperty =
        DependencyProperty.Register(
            nameof(SelectedObjectName),
            typeof(string),
            typeof(PropertyGrid),
            new PropertyMetadata(null, OnSelectedObjectNameChanged));

    /// <summary>
    /// Categorized view active.
    /// </summary>
    public static readonly DependencyProperty IsCategorizedProperty =
        DependencyProperty.Register(
            nameof(IsCategorized),
            typeof(bool),
            typeof(PropertyGrid),
            new PropertyMetadata(true, OnViewModeChanged));

    /// <summary>
    /// Alphabetical view active.
    /// </summary>
    public static readonly DependencyProperty IsAlphabeticalProperty =
        DependencyProperty.Register(
            nameof(IsAlphabetical),
            typeof(bool),
            typeof(PropertyGrid),
            new PropertyMetadata(false, OnViewModeChanged));

    #endregion

    #region Events

    /// <summary>
    /// Raised when a different object is selected in the selector.
    /// </summary>
    public event EventHandler<string>? SelectedObjectChanged;

    #endregion

    private List<PropertyGridItem> _allProperties = [];

    public PropertyGrid()
    {
        InitializeComponent();
        DisplayItems = new ObservableCollection<PropertyGridItem>();
        AvailableObjects = new ObservableCollection<string>();
    }

    #region Properties

    public ObservableCollection<PropertyGridItem>? DisplayItems
    {
        get => (ObservableCollection<PropertyGridItem>?)GetValue(DisplayItemsProperty);
        set => SetValue(DisplayItemsProperty, value);
    }

    public PropertyGridItem? SelectedProperty
    {
        get => (PropertyGridItem?)GetValue(SelectedPropertyProperty);
        set => SetValue(SelectedPropertyProperty, value);
    }

    public ObservableCollection<string>? AvailableObjects
    {
        get => (ObservableCollection<string>?)GetValue(AvailableObjectsProperty);
        set => SetValue(AvailableObjectsProperty, value);
    }

    public string? SelectedObjectName
    {
        get => (string?)GetValue(SelectedObjectNameProperty);
        set => SetValue(SelectedObjectNameProperty, value);
    }

    public bool IsCategorized
    {
        get => (bool)GetValue(IsCategorizedProperty);
        set => SetValue(IsCategorizedProperty, value);
    }

    public bool IsAlphabetical
    {
        get => (bool)GetValue(IsAlphabeticalProperty);
        set => SetValue(IsAlphabeticalProperty, value);
    }

    #endregion

    /// <summary>
    /// Sets the properties to display and refreshes the view.
    /// </summary>
    public void SetProperties(IEnumerable<PropertyGridItem> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        _allProperties = properties.ToList();
        RefreshView();
    }

    private void RefreshView()
    {
        if (DisplayItems is null)
        {
            return;
        }

        DisplayItems.Clear();

        if (IsCategorized)
        {
            var grouped = _allProperties
                .Where(p => p.IsPropertyRow)
                .GroupBy(p => p.Category)
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                DisplayItems.Add(PropertyGridItem.CreateCategoryHeader(group.Key));
                foreach (var item in group.OrderBy(i => i.Name))
                {
                    DisplayItems.Add(item);
                }
            }
        }
        else
        {
            var sorted = _allProperties
                .Where(p => p.IsPropertyRow)
                .OrderBy(p => p.Name);

            foreach (var item in sorted)
            {
                DisplayItems.Add(item);
            }
        }
    }

    private static void OnSelectedObjectNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PropertyGrid grid && e.NewValue is string name)
        {
            grid.SelectedObjectChanged?.Invoke(grid, name);
        }
    }

    private static void OnViewModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PropertyGrid grid)
        {
            if (e.Property == IsCategorizedProperty && (bool)e.NewValue)
            {
                grid.IsAlphabetical = false;
            }
            else if (e.Property == IsAlphabeticalProperty && (bool)e.NewValue)
            {
                grid.IsCategorized = false;
            }

            grid.RefreshView();
        }
    }
}

/// <summary>
/// Represents a row in the PropertyGrid (category header or property row).
/// </summary>
internal sealed class PropertyGridItem
{
    /// <summary>
    /// Name of the property or category.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Value of the property.
    /// </summary>
    public string Value { get; set; } = "";

    /// <summary>
    /// Category for grouping.
    /// </summary>
    public string Category { get; set; } = "Misc";

    /// <summary>
    /// Description text for the description pane.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// True for category header.
    /// </summary>
    public bool IsCategoryHeader { get; set; }

    /// <summary>
    /// True for property row.
    /// </summary>
    public bool IsPropertyRow => !IsCategoryHeader;

    /// <summary>
    /// Whether the property is read-only.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Creates a category header.
    /// </summary>
    public static PropertyGridItem CreateCategoryHeader(string categoryName) => new()
    {
        Name = categoryName,
        IsCategoryHeader = true,
        Category = categoryName
    };

    /// <summary>
    /// Creates a property item.
    /// </summary>
    public static PropertyGridItem CreateProperty(
        string name,
        string value,
        string category = "Misc",
        string description = "",
        bool isReadOnly = false) => new()
    {
        Name = name,
        Value = value,
        Category = category,
        Description = description,
        IsReadOnly = isReadOnly
    };
}
