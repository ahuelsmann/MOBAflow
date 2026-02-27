// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls.Docking;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System;
using System.Collections.ObjectModel;

/// <summary>
/// Extended LayoutDocument with tab groups, binding support and window management.
/// Supports grouped tabs (Modified, Pinned, Regular) and floating windows.
/// </summary>
internal sealed partial class LayoutDocument
{
    #region Dependency Properties

    /// <summary>
    /// All available documents (binding property)
    /// </summary>
    public static readonly DependencyProperty DocumentsProperty =
        DependencyProperty.Register(
            nameof(Documents),
            typeof(ObservableCollection<DocumentTab>),
            typeof(LayoutDocument), new PropertyMetadata(null));

    /// <summary>
    /// Currently active document
    /// </summary>
    public static readonly DependencyProperty ActiveDocumentProperty =
        DependencyProperty.Register(
            nameof(ActiveDocument),
            typeof(DocumentTab),
            typeof(LayoutDocument), new PropertyMetadata(null));

    /// <summary>
    /// Template for tab rendering (ItemTemplate support)
    /// </summary>
    public static readonly DependencyProperty TabTemplateProperty =
        DependencyProperty.Register(
            nameof(TabTemplate),
            typeof(DataTemplate),
            typeof(LayoutDocument), new PropertyMetadata(null));

    /// <summary>
    /// Template for content rendering
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
    /// Enables floating windows for tabs
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
    /// All available documents
    /// </summary>
    public ObservableCollection<DocumentTab>? Documents
    {
        get => (ObservableCollection<DocumentTab>?)GetValue(DocumentsProperty);
        set => SetValue(DocumentsProperty, value);
    }

    /// <summary>
    /// Currently active document
    /// </summary>
    public DocumentTab? ActiveDocument
    {
        get => (DocumentTab?)GetValue(ActiveDocumentProperty);
        set => SetValue(ActiveDocumentProperty, value);
    }

    /// <summary>
    /// Custom template for tab rendering
    /// </summary>
    public DataTemplate? TabTemplate
    {
        get => (DataTemplate?)GetValue(TabTemplateProperty);
        set => SetValue(TabTemplateProperty, value);
    }

    /// <summary>
    /// Custom template for content rendering
    /// </summary>
    public DataTemplate? ContentTemplate
    {
        get => (DataTemplate?)GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    /// <summary>
    /// Enable tab grouping (Modified, Pinned, Regular)
    /// </summary>
    public bool EnableTabGrouping
    {
        get => (bool)GetValue(EnableTabGroupingProperty);
        set => SetValue(EnableTabGroupingProperty, value);
    }

    /// <summary>
    /// Allow floating windows for tabs
    /// </summary>
    public bool AllowFloatingTabs
    {
        get => (bool)GetValue(AllowFloatingTabsProperty);
        set => SetValue(AllowFloatingTabsProperty, value);
    }
    #endregion
}

/// <summary>
/// Floating window for tabs
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
/// EventArgs for a tab that was dragged out of the TabView.
/// </summary>
internal abstract class DocumentTabDraggedOutEventArgs(DocumentTab document) : EventArgs
{
    public DocumentTab Document { get; } = document;
}