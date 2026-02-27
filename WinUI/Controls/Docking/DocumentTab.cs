// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls.Docking;

using Microsoft.UI.Xaml;

using System;

/// <summary>
/// Represents a single document tab.
/// Used by LayoutDocumentEx and DockingManager.
/// </summary>
internal class DocumentTab : IEquatable<DocumentTab>
{
    public string Title { get; set; } = "Untitled";
    public string IconGlyph { get; set; } = "\uE71E";
    public UIElement? Content { get; set; }
    public bool IsModified { get; set; }
    /// <summary>Indicates whether the tab is pinned to the side.</summary>
    public bool IsPinned { get; set; }
    public object? Tag { get; set; }

    public override bool Equals(object? obj) => Equals(obj as DocumentTab);
    public bool Equals(DocumentTab? other) => other != null && Title == other.Title && Tag?.Equals(other.Tag) == true;
    public override int GetHashCode() => HashCode.Combine(Title, Tag);
}