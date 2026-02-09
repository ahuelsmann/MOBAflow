// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Moba.WinUI.Behavior;
using Moba.WinUI.Controls;
using Moba.WinUI.ViewModel;

public sealed partial class DockingPage : Page
{
    private readonly DockingPageViewModel _viewModel;
    private readonly PropertyGrid _propertiesGrid = new() { IsCategorized = true };
    private bool _isInitialized;

    public DockingPage(DockingPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
        {
            return;
        }

        DockManager.PanelUndocked += OnPanelUndocked;
        DockManager.PanelClosed += OnPanelClosed;

        InitializeDockPanels();
        _isInitialized = true;
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        DockManager.PanelUndocked -= OnPanelUndocked;
        DockManager.PanelClosed -= OnPanelClosed;
    }

    private void InitializeDockPanels()
    {
        // Left: Solution Explorer
        var solutionExplorer = new Controls.DockPanel
        {
            PanelIconGlyph = "\uEC50",
            PanelTitle = "Solution Explorer",
            PanelContent = CreateSolutionExplorerContent()
        };
        DockManager.DockPanel(solutionExplorer, DockPosition.Left);

        // Right: Properties
        var propertiesPanel = new Controls.DockPanel
        {
            PanelIconGlyph = "\uE946",
            PanelTitle = "Properties",
            PanelContent = _propertiesGrid
        };
        DockManager.DockPanel(propertiesPanel, DockPosition.Right);

        // Bottom: Output
        var outputPanel = new Controls.DockPanel
        {
            PanelIconGlyph = "\uE7BA",
            PanelTitle = "Output",
            PanelContent = CreateOutputContent()
        };
        DockManager.DockPanel(outputPanel, DockPosition.Bottom);

        PopulatePropertyGrid();
    }

    private static TreeView CreateSolutionExplorerContent()
    {
        var tree = new TreeView
        {
            Margin = new Microsoft.UI.Xaml.Thickness(0),
            Padding = new Microsoft.UI.Xaml.Thickness(4),
            SelectionMode = TreeViewSelectionMode.Single
        };

        var solutionNode = new TreeViewNode { Content = "Solution 'MOBAflow' (5 projects)", IsExpanded = true };

        var domain = new TreeViewNode { Content = "Domain", IsExpanded = true };
        domain.Children.Add(new TreeViewNode { Content = "Solution.cs" });
        domain.Children.Add(new TreeViewNode { Content = "Journey.cs" });
        domain.Children.Add(new TreeViewNode { Content = "Train.cs" });
        domain.Children.Add(new TreeViewNode { Content = "Workflow.cs" });
        solutionNode.Children.Add(domain);

        var backend = new TreeViewNode { Content = "Backend" };
        backend.Children.Add(new TreeViewNode { Content = "Z21Wrapper.cs" });
        backend.Children.Add(new TreeViewNode { Content = "WorkflowService.cs" });
        solutionNode.Children.Add(backend);

        var winui = new TreeViewNode { Content = "WinUI", IsExpanded = true };
        var controls = new TreeViewNode { Content = "Controls" };
        controls.Children.Add(new TreeViewNode { Content = "DockingManager.xaml" });
        controls.Children.Add(new TreeViewNode { Content = "DockPanel.xaml" });
        controls.Children.Add(new TreeViewNode { Content = "LayoutDocumentEx.xaml" });
        winui.Children.Add(controls);
        solutionNode.Children.Add(winui);

        tree.RootNodes.Add(solutionNode);
        return tree;
    }

    private static Microsoft.UI.Xaml.UIElement CreateOutputContent()
    {
        return new TextBlock
        {
            Text = "Build succeeded - 0 errors, 6 warnings",
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Cascadia Code,Consolas"),
            FontSize = 12,
            Padding = new Microsoft.UI.Xaml.Thickness(8, 4, 8, 4)
        };
    }

    private void PopulatePropertyGrid()
    {
        _propertiesGrid.AvailableObjects?.Add("WinUI Projekteigenschaften");
        _propertiesGrid.AvailableObjects?.Add("DockingPage.xaml Page");
        _propertiesGrid.SelectedObjectName = "WinUI Projekteigenschaften";

        _propertiesGrid.SetProperties(
        [
            PropertyGridItem.CreateProperty("Dateiname", "WinUI.csproj", "Allgemein", "Name der Projektdatei."),
            PropertyGridItem.CreateProperty("Projektordner", @"C:\Repo\ahuelsmann\MOBAflow\WinUI", "Allgemein", "Pfad zum Projektordner."),
            PropertyGridItem.CreateProperty("Ausgabetyp", "WinExe", "Allgemein", "Art der Ausgabe."),
            PropertyGridItem.CreateProperty("Zielframework", "net10.0-windows10.0.17763.0", "Allgemein", "Das .NET-Zielframework."),
            PropertyGridItem.CreateProperty("RootNamespace", "Moba.WinUI", "Allgemein", "Standard-Namespace."),
            PropertyGridItem.CreateProperty("AssemblyName", "MOBAflow", "Allgemein", "Name der Assembly."),
        ]);
    }

    private void OnTabDraggedOutside(object? sender, DocumentTabDraggedOutEventArgs e)
    {
    }

    private void OnTabDockedToSide(object sender, DocumentTabDockedEventArgs e)
    {
        _viewModel.OpenDocuments.Remove(e.Document);
    }

    private void OnPanelUndocked(object? sender, DockPanelUndockedEventArgs e)
    {
        var tab = new DocumentTab
        {
            Title = e.Panel.PanelTitle,
            IconGlyph = e.Panel.PanelIconGlyph,
            Content = e.Panel.PanelContent,
            Tag = e.Panel.PanelTitle.ToLowerInvariant()
        };

        _viewModel.OpenDocuments.Add(tab);
        _viewModel.ActiveDocument = tab;
    }

    private void OnPanelClosed(object? sender, DockPanelClosedEventArgs e)
    {
    }
}
