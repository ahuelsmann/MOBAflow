// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Moba.WinUI.Behavior;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Windows.ApplicationModel.DataTransfer;

/// <summary>
/// Erweiterte LayoutDocument mit Tab-Gruppen, Binding-Support und Window-Management.
/// Unterstützt gruppierte Tabs (Modified, Pinned, Regular) und Floating Windows.
/// </summary>
public sealed partial class LayoutDocument : UserControl
{
    #region Dependency Properties

    /// <summary>
    /// Alle verfügbaren Dokumente (Binding-Property)
    /// </summary>
    public static readonly DependencyProperty DocumentsProperty =
        DependencyProperty.Register(
            nameof(Documents),
            typeof(ObservableCollection<DocumentTab>),
            typeof(LayoutDocument), new PropertyMetadata(null));

    /// <summary>
    /// Aktuell aktives Dokument
    /// </summary>
    public static readonly DependencyProperty ActiveDocumentProperty =
        DependencyProperty.Register(
            nameof(ActiveDocument),
            typeof(DocumentTab),
            typeof(LayoutDocument), new PropertyMetadata(null));

    /// <summary>
    /// Template für Tab-Renderung (ItemTemplate-Support)
    /// </summary>
    public static readonly DependencyProperty TabTemplateProperty =
        DependencyProperty.Register(
            nameof(TabTemplate),
            typeof(DataTemplate),
            typeof(LayoutDocument), new PropertyMetadata(null));

    /// <summary>
    /// Template für Content-Renderung
    /// </summary>
    public static readonly DependencyProperty ContentTemplateProperty =
        DependencyProperty.Register(
            nameof(ContentTemplate),
            typeof(DataTemplate),
            typeof(LayoutDocument), new PropertyMetadata(null));

    /// <summary>
    /// Enable Tab-Grouping (Modified, Pinned, Regular)
    /// </summary>
    public static readonly DependencyProperty EnableTabGroupingProperty =
        DependencyProperty.Register(
            nameof(EnableTabGrouping),
            typeof(bool),
            typeof(LayoutDocument), new PropertyMetadata(false));

    /// <summary>
    /// Ermöglicht Floating-Windows für Tabs
    /// </summary>
    public static readonly DependencyProperty AllowFloatingTabsProperty =
        DependencyProperty.Register(
            nameof(AllowFloatingTabs),
            typeof(bool),
            typeof(LayoutDocument), new PropertyMetadata(false));

    #endregion

    #region Events

    public event EventHandler<DocumentTabChangedEventArgs>? DocumentSelected;
    public event EventHandler<DocumentTabClosingEventArgs>? DocumentClosing;
    public event EventHandler? NewTabRequested;
    public event EventHandler<DocumentTabMovedEventArgs>? TabMovedToFloatingWindow;

    /// <summary>
    /// Wird ausgelöst, wenn ein Tab aus dem TabView herausgezogen wird.
    /// Der DockingManager kann diesen Tab dann an einer Dock-Position andocken.
    /// </summary>
    public event EventHandler<DocumentTabDraggedOutEventArgs>? TabDraggedOutside;

    #endregion

    private const string DocumentTabDataKey = "DocumentTab";

    private Dictionary<DocumentTab, FloatingTabWindow> _floatingWindows = new();

    public LayoutDocument()
    {
        InitializeComponent();
        Documents = new ObservableCollection<DocumentTab>();
    }

    #region Properties

    /// <summary>
    /// Alle verfügbaren Dokumente
    /// </summary>
    public ObservableCollection<DocumentTab>? Documents
    {
        get => (ObservableCollection<DocumentTab>?)GetValue(DocumentsProperty);
        set => SetValue(DocumentsProperty, value);
    }

    /// <summary>
    /// Aktuell aktives Dokument
    /// </summary>
    public DocumentTab? ActiveDocument
    {
        get => (DocumentTab?)GetValue(ActiveDocumentProperty);
        set => SetValue(ActiveDocumentProperty, value);
    }

    /// <summary>
    /// Custom Template für Tab-Renderung
    /// </summary>
    public DataTemplate? TabTemplate
    {
        get => (DataTemplate?)GetValue(TabTemplateProperty);
        set => SetValue(TabTemplateProperty, value);
    }

    /// <summary>
    /// Custom Template für Content-Renderung
    /// </summary>
    public DataTemplate? ContentTemplate
    {
        get => (DataTemplate?)GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    /// <summary>
    /// Tab-Grouping aktivieren (Modified, Pinned, Regular)
    /// </summary>
    public bool EnableTabGrouping
    {
        get => (bool)GetValue(EnableTabGroupingProperty);
        set => SetValue(EnableTabGroupingProperty, value);
    }

    /// <summary>
    /// Floating-Windows für Tabs ermöglichen
    /// </summary>
    public bool AllowFloatingTabs
    {
        get => (bool)GetValue(AllowFloatingTabsProperty);
        set => SetValue(AllowFloatingTabsProperty, value);
    }

    #endregion

    #region Tab Management

    /// <summary>
    /// Fügt ein neues Dokument hinzu
    /// </summary>
    public void AddDocument(DocumentTab document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Documents?.Add(document);
        ActiveDocument = document;
    }

    /// <summary>
    /// Entfernt ein Dokument
    /// </summary>
    public void RemoveDocument(DocumentTab document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var closingArgs = new DocumentTabClosingEventArgs(document);
        DocumentClosing?.Invoke(this, closingArgs);

        if (!closingArgs.Cancel)
        {
            Documents?.Remove(document);
            if (ActiveDocument == document && Documents?.Count > 0)
            {
                ActiveDocument = Documents[Documents.Count - 1];
            }
        }
    }

    /// <summary>
    /// Markiert ein Dokument als "Modified"
    /// </summary>
    public void MarkAsModified(DocumentTab document, bool isModified)
    {
        ArgumentNullException.ThrowIfNull(document);
        document.IsModified = isModified;
    }
    #endregion

    #region Tab Grouping

    /// <summary>
    /// Gibt gruppierte Tabs zurück (Modified, Pinned, Regular)
    /// </summary>
    public IEnumerable<TabGroup> GetGroupedTabs()
    {
        if (Documents == null || Documents.Count == 0)
            yield break;

        var modifiedTabs = Documents.Where(d => d.IsModified).ToList();
        var regularTabs = Documents.Where(d => !d.IsModified).ToList();

        if (modifiedTabs.Any())
            yield return new TabGroup { Name = "Modified", Tabs = modifiedTabs };

        if (regularTabs.Any())
            yield return new TabGroup { Name = "Open", Tabs = regularTabs };
    }

    #endregion

    #region TabView Event Handlers

    private void OnAddTabButtonClick(TabView sender, object args)
    {
        NewTabRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Item is DocumentTab document)
        {
            RemoveDocument(document);
        }
    }

    private void OnTabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
    {
        if (args.Item is DocumentTab tab)
        {
            args.Data.Properties[DocumentTabDataKey] = tab;
            args.Data.RequestedOperation = DataPackageOperation.Move;
        }
    }

    private void OnTabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
    {
        if (args.Item is not DocumentTab tab)
        {
            return;
        }

        TabDraggedOutside?.Invoke(this, new DocumentTabDraggedOutEventArgs(tab));
    }

    #endregion

    #region Tab Context Menu (Qt-ADS 4.1: "Pin to...", Close Others, Close All)

    private DocumentTab? _contextMenuTab;

    private void OnTabRightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        if (sender is TabViewItem tabViewItem && tabViewItem.Content is DocumentTab tab)
        {
            _contextMenuTab = tab;
        }
    }

    private void OnContextClose(object sender, RoutedEventArgs e)
    {
        if (_contextMenuTab is not null)
        {
            RemoveDocument(_contextMenuTab);
        }
    }

    private void OnContextCloseOthers(object sender, RoutedEventArgs e)
    {
        if (_contextMenuTab is null || Documents is null)
        {
            return;
        }

        var toClose = Documents.Where(d => d != _contextMenuTab).ToList();
        foreach (var doc in toClose)
        {
            Documents.Remove(doc);
        }

        ActiveDocument = _contextMenuTab;
    }

    private void OnContextCloseAll(object sender, RoutedEventArgs e)
    {
        Documents?.Clear();
        ActiveDocument = null;
    }
    #endregion
}

/// <summary>
/// Gruppiert Tabs (z.B. Modified, Pinned, Open)
/// </summary>
public class TabGroup
{
    public string Name { get; set; } = "";
    public List<DocumentTab> Tabs { get; set; } = new();
}

/// <summary>
/// Floating Window für Tabs
/// </summary>
public class FloatingTabWindow : Window
{
    private readonly DocumentTab _tab;

    public FloatingTabWindow(DocumentTab tab)
    {
        _tab = tab;
        Title = $"[Floating] {tab.Title}";

        var rootGrid = new Grid();
        rootGrid.Children.Add(_tab.Content ?? new TextBlock { Text = "No Content" });

        Content = rootGrid;

        // Default size
        AppWindow.ResizeClient(new Windows.Graphics.SizeInt32 { Width = 800, Height = 600 });
    }

    public DocumentTab Tab => _tab;
}

// Event Args

public class DocumentTabChangedEventArgs : EventArgs
{
    public DocumentTabChangedEventArgs(DocumentTab document)
    {
        Document = document;
    }
    public DocumentTab Document { get; }
}

public class DocumentTabClosingEventArgs : EventArgs
{
    public DocumentTabClosingEventArgs(DocumentTab document)
    {
        Document = document;
    }
    public DocumentTab Document { get; }
    public bool Cancel { get; set; }
}

public class DocumentTabMovedEventArgs : EventArgs
{
    public DocumentTabMovedEventArgs(DocumentTab document, FloatingTabWindow window)
    {
        Document = document;
        Window = window;
    }
    public DocumentTab Document { get; }
    public FloatingTabWindow Window { get; }
}

/// <summary>
/// EventArgs für einen Tab, der aus dem TabView herausgezogen wurde.
/// </summary>
public sealed class DocumentTabDraggedOutEventArgs(DocumentTab document) : EventArgs
{
    public DocumentTab Document { get; } = document;
}

/// <summary>
/// EventArgs für einen Tab, der über das Kontextmenü an eine Dock-Seite gepinnt wird (Qt-ADS: "Pin To...").
/// </summary>
public sealed class DocumentTabPinToSideEventArgs(DocumentTab document, DockPosition side) : EventArgs
{
    public DocumentTab Document { get; } = document;
    public DockPosition Side { get; } = side;
}