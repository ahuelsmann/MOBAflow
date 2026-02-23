// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls.Docking;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System;
using System.Collections.ObjectModel;

/// <summary>
/// Erweiterte LayoutDocument mit Tab-Gruppen, Binding-Support und Window-Management.
/// Unterstützt gruppierte Tabs (Modified, Pinned, Regular) und Floating Windows.
/// </summary>
internal sealed partial class LayoutDocument
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
}

/// <summary>
/// Floating Window für Tabs
/// </summary>
internal abstract class FloatingTabWindow : Window
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

internal abstract class DocumentTabChangedEventArgs : EventArgs
{
    public DocumentTabChangedEventArgs(DocumentTab document)
    {
        Document = document;
    }
    public DocumentTab Document { get; }
}

internal abstract class DocumentTabClosingEventArgs : EventArgs
{
    public DocumentTabClosingEventArgs(DocumentTab document)
    {
        Document = document;
    }
    public DocumentTab Document { get; }
    public bool Cancel { get; set; }
}

internal abstract class DocumentTabMovedEventArgs : EventArgs
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
internal abstract class DocumentTabDraggedOutEventArgs(DocumentTab document) : EventArgs
{
    public DocumentTab Document { get; } = document;
}