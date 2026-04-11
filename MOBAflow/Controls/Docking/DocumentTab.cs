// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls.Docking;

using Microsoft.UI.Xaml;
using System.Runtime.CompilerServices;

/// <summary>
/// Represents a single document tab.
/// Used by LayoutDocumentEx and DockingManager.
/// </summary>
public class DocumentTab : IEquatable<DocumentTab>
{
    public string DocumentId { get; set; } = string.Empty;

    public string Title { get; set; } = "Untitled";
    public string IconGlyph { get; set; } = "\uE71E";
    public UIElement? Content { get; set; }
    public bool IsModified { get; set; }
    /// <summary>Indicates whether the tab is pinned to the side.</summary>
    public bool IsPinned { get; set; }
    public object? Tag { get; set; }

    public override bool Equals(object? obj) => ReferenceEquals(this, obj);
    public bool Equals(DocumentTab? other) => ReferenceEquals(this, other);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}