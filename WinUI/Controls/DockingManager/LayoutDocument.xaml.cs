// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;

/// <summary>
/// LayoutDocument Control für Tab-basierte Document Area.
/// Ähnlich dem Document-Tab-Interface in Visual Studio.
/// </summary>
public sealed partial class LayoutDocument : UserControl
{
    #region Dependency Properties

    public static readonly DependencyProperty DocumentsProperty =
        DependencyProperty.Register(
            nameof(Documents),
            typeof(ObservableCollection<DocumentTab>),
            typeof(LayoutDocument),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ActiveDocumentProperty =
        DependencyProperty.Register(
            nameof(ActiveDocument),
            typeof(DocumentTab),
            typeof(LayoutDocument),
            new PropertyMetadata(null));

    #endregion

    public LayoutDocument()
    {
        Documents = new ObservableCollection<DocumentTab>();
    }

    #region Properties

    public ObservableCollection<DocumentTab>? Documents
    {
        get => (ObservableCollection<DocumentTab>?)GetValue(DocumentsProperty);
        set => SetValue(DocumentsProperty, value);
    }

    public DocumentTab? ActiveDocument
    {
        get => (DocumentTab?)GetValue(ActiveDocumentProperty);
        set => SetValue(ActiveDocumentProperty, value);
    }

    #endregion

    /// <summary>
    /// Fügt ein neues Dokument zur Collection hinzu.
    /// </summary>
    public void AddDocument(DocumentTab document)
    {
        Documents?.Add(document);
        ActiveDocument = document;
    }

    /// <summary>
    /// Entfernt ein Dokument aus der Collection.
    /// </summary>
    public void RemoveDocument(DocumentTab document)
    {
        Documents?.Remove(document);
        if (ActiveDocument == document && Documents?.Count > 0)
        {
            ActiveDocument = Documents[Documents.Count - 1];
        }
    }
}

/// <summary>
/// Stellt ein einzelnes Dokument dar
/// </summary>
public class DocumentTab : IEquatable<DocumentTab>
{
    public string Title { get; set; } = "Untitled";
    public string IconGlyph { get; set; } = "\uE71E";
    public UIElement? Content { get; set; }
    public bool IsModified { get; set; }
    public bool IsPinned { get; set; }
    public object? Tag { get; set; }

    public override bool Equals(object? obj) => Equals(obj as DocumentTab);
    public bool Equals(DocumentTab? other) => other != null && Title == other.Title && Tag?.Equals(other.Tag) == true;
    public override int GetHashCode() => HashCode.Combine(Title, Tag);
}
