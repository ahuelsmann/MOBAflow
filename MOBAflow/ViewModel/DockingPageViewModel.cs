// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Moba.WinUI.Controls.Docking;
using Moba.WinUI.Controls.Docking.Model;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

/// <summary>
/// ViewModel for DockingPage demonstrating LayoutDocument capabilities.
/// (Host-side: no plugin dependencies)
/// </summary>
internal sealed partial class DockingPageViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DocumentTab> _openDocuments = new();

    [ObservableProperty]
    private DocumentTab? _activeDocument;

    [ObservableProperty]
    private DockNode? _leftNode;

    [ObservableProperty]
    private DockNode? _rightNode;

    [ObservableProperty]
    private DockNode? _topNode;

    [ObservableProperty]
    private DockNode? _bottomNode;

    private int _documentCounter = 1;

    public DockingPageViewModel()
    {
        InitializePanels();
        InitializeDefaultDocuments();
    }

    private void InitializePanels()
    {
        LeftNode = CreateGroupNode(
            DockPosition.Left,
            CreatePanel("Solution Explorer", "\uEC50", "Project structure and files."),
            CreatePanel("Class View", "\uE8B8", "Types and symbols for the active solution."));

        RightNode = CreateGroupNode(
            DockPosition.Right,
            CreatePanel("Properties", "\uE946", "Details for the current selection."));

        var outputGroup = CreateGroupNode(
            DockPosition.Bottom,
            CreatePanel("Output", "\uE7BA", "Build output, diagnostics and execution logs."));
        var problemsGroup = CreateGroupNode(
            DockPosition.Bottom,
            CreatePanel("Problems", "\uEA39", "Warnings and validation results."));

        BottomNode = new DockSplitNode(outputGroup, problemsGroup, Orientation.Horizontal)
        {
            DockPosition = DockPosition.Bottom
        };
        TopNode = null;
    }

    private void InitializeDefaultDocuments()
    {
        var doc1 = new DocumentTab
        {
            Title = "Document 1",
            IconGlyph = "\uE8A5",
            Content = CreateDocumentContent("Document 1", "Welcome to the docking demo."),
            IsModified = false,
            Tag = "doc1"
        };

        OpenDocuments.Add(doc1);
        ActiveDocument = doc1;
        _documentCounter = 2;
    }

    [RelayCommand]
    private void AddNewDocument()
    {
        var docNum = _documentCounter++;
        var newDoc = new DocumentTab
        {
            Title = $"Document {docNum}.txt",
            IconGlyph = "\uE160",
            Content = CreateDocumentContent(
                $"Document {docNum}.txt",
                $"Generated sample content for document {docNum}."),
            IsModified = false,
            Tag = $"doc{docNum}"
        };

        OpenDocuments.Add(newDoc);
        ActiveDocument = newDoc;
    }

    [RelayCommand]
    private void MarkAsModified()
    {
        if (ActiveDocument != null)
        {
            ActiveDocument.IsModified = !ActiveDocument.IsModified;
        }
    }

    [RelayCommand]
    private void CloseCurrentDocument()
    {
        if (ActiveDocument != null && OpenDocuments.Count > 1)
        {
            var docToClose = ActiveDocument;
            OpenDocuments.Remove(docToClose);
            if (OpenDocuments.Count > 0)
            {
                ActiveDocument = OpenDocuments[OpenDocuments.Count - 1];
            }
        }
    }

    [RelayCommand]
    private void CloseAllModifiedDocuments()
    {
        var modifiedDocs = OpenDocuments.Where(d => d.IsModified).ToList();
        foreach (var doc in modifiedDocs)
        {
            OpenDocuments.Remove(doc);
        }

        if (ActiveDocument != null && !OpenDocuments.Contains(ActiveDocument))
        {
            ActiveDocument = OpenDocuments.FirstOrDefault();
        }
    }

    public void HandleDockedDocument(DocumentTab document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (!OpenDocuments.Remove(document))
        {
            return;
        }

        if (ReferenceEquals(ActiveDocument, document))
        {
            ActiveDocument = OpenDocuments.FirstOrDefault();
        }
    }

    private static DockPanelGroupNode CreateGroupNode(DockPosition side, params DockPanel[] panels)
    {
        var groupNode = new DockPanelGroupNode
        {
            DockPosition = side
        };

        foreach (var panel in panels)
        {
            groupNode.Panels.Add(panel);
        }

        return groupNode;
    }

    private static DockPanel CreatePanel(string title, string iconGlyph, string description)
    {
        return new DockPanel
        {
            PanelTitle = title,
            PanelIconGlyph = iconGlyph,
            PanelContent = new Border
            {
                Padding = new Thickness(12),
                Child = new TextBlock
                {
                    Text = description,
                    TextWrapping = TextWrapping.WrapWholeWords
                }
            }
        };
    }

    private static UIElement CreateDocumentContent(string title, string body)
    {
        return new Border
        {
            Padding = new Thickness(16),
            Child = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        FontSize = 20,
                        Text = title
                    },
                    new TextBlock
                    {
                        Text = body,
                        TextWrapping = TextWrapping.WrapWholeWords
                    }
                }
            }
        };
    }
}