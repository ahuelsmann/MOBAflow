// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

/// <summary>
/// Visual Studio-style PropertyGrid control.
/// Zeigt Eigenschaften eines Objekts in einer Name/Value-Liste mit optionaler Kategorisierung
/// und Beschreibung des selektierten Properties.
/// </summary>
public sealed partial class PropertyGrid : UserControl
{
    #region Dependency Properties

    /// <summary>
    /// Die angezeigten Property-Items.
    /// </summary>
    public static readonly DependencyProperty DisplayItemsProperty =
        DependencyProperty.Register(
            nameof(DisplayItems),
            typeof(ObservableCollection<PropertyGridItem>),
            typeof(PropertyGrid),
            new PropertyMetadata(null));

    /// <summary>
    /// Das aktuell selektierte Property.
    /// </summary>
    public static readonly DependencyProperty SelectedPropertyProperty =
        DependencyProperty.Register(
            nameof(SelectedProperty),
            typeof(PropertyGridItem),
            typeof(PropertyGrid),
            new PropertyMetadata(null));

    /// <summary>
    /// Verfügbare Objekte für den Object-Selector.
    /// </summary>
    public static readonly DependencyProperty AvailableObjectsProperty =
        DependencyProperty.Register(
            nameof(AvailableObjects),
            typeof(ObservableCollection<string>),
            typeof(PropertyGrid),
            new PropertyMetadata(null));

    /// <summary>
    /// Name des aktuell selektierten Objekts.
    /// </summary>
    public static readonly DependencyProperty SelectedObjectNameProperty =
        DependencyProperty.Register(
            nameof(SelectedObjectName),
            typeof(string),
            typeof(PropertyGrid),
            new PropertyMetadata(null, OnSelectedObjectNameChanged));

    /// <summary>
    /// Kategorisierte Ansicht aktiv.
    /// </summary>
    public static readonly DependencyProperty IsCategorizedProperty =
        DependencyProperty.Register(
            nameof(IsCategorized),
            typeof(bool),
            typeof(PropertyGrid),
            new PropertyMetadata(true, OnViewModeChanged));

    /// <summary>
    /// Alphabetische Ansicht aktiv.
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
    /// Wird ausgelöst, wenn ein anderes Objekt im Selector gewählt wird.
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
    /// Setzt die anzuzeigenden Properties und aktualisiert die Ansicht.
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
/// Stellt eine Zeile im PropertyGrid dar (Category-Header oder Property-Row).
/// </summary>
public sealed class PropertyGridItem
{
    /// <summary>
    /// Name der Property oder Kategorie.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Wert der Property.
    /// </summary>
    public string Value { get; set; } = "";

    /// <summary>
    /// Kategorie für Gruppierung.
    /// </summary>
    public string Category { get; set; } = "Misc";

    /// <summary>
    /// Beschreibungstext für das Description-Pane.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// True bei Category-Header.
    /// </summary>
    public bool IsCategoryHeader { get; set; }

    /// <summary>
    /// True bei Property-Zeile.
    /// </summary>
    public bool IsPropertyRow => !IsCategoryHeader;

    /// <summary>
    /// Ob das Property nur lesbar ist.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Erstellt einen Category-Header.
    /// </summary>
    public static PropertyGridItem CreateCategoryHeader(string categoryName) => new()
    {
        Name = categoryName,
        IsCategoryHeader = true,
        Category = categoryName
    };

    /// <summary>
    /// Erstellt ein Property-Item.
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
