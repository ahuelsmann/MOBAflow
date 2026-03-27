// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls.Docking.Model;

using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

/// <summary>
/// A terminal node in the docking hierarchy that contains actual DockPanels.
/// </summary>
internal sealed partial class DockPanelGroupNode : DockNode
{
    /// <summary>
    /// The collection of panels hosted in this group.
    /// </summary>
    public ObservableCollection<DockPanel> Panels { get; } = new();

    /// <summary>
    /// Indicates whether the panels should be displayed as tabs or split evenly.
    /// Default is Tabbed.
    /// </summary>
    [ObservableProperty]
    private DockGroupLayoutMode _layoutMode = DockGroupLayoutMode.Tabbed;

    public DockPanelGroupNode()
    {
        Panels.CollectionChanged += OnPanelsChanged;
    }

    private void OnPanelsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (DockPanel panel in e.NewItems)
            {
                panel.DockPosition = DockPosition;
            }
        }
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(DockPosition))
        {
            ApplyDockPositionToPanels();
        }
    }

    private void ApplyDockPositionToPanels()
    {
        foreach (var panel in Panels)
        {
            panel.DockPosition = DockPosition;
        }
    }
}
