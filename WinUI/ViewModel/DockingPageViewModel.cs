// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Moba.WinUI.Controls;

using System.Collections.ObjectModel;

/// <summary>
/// ViewModel for DockingPage demonstrating LayoutDocumentEx capabilities.
/// (Host-seitig: ohne Plugin-Abhängigkeiten)
/// </summary>
public partial class DockingPageViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DocumentTab> openDocuments = new();

    [ObservableProperty]
    private DocumentTab? activeDocument;

    [ObservableProperty]
    private ObservableCollection<DockPanel> leftPanels = new();

    [ObservableProperty]
    private ObservableCollection<DockPanel> rightPanels = new();

    [ObservableProperty]
    private ObservableCollection<DockPanel> topPanels = new();

    [ObservableProperty]
    private ObservableCollection<DockPanel> bottomPanels = new();

    private int _documentCounter = 1;

    public DockingPageViewModel()
    {
        InitializePanels();
        InitializeDefaultDocuments();
    }

    private void InitializePanels()
    {
        LeftPanels =
        [
            new DockPanel
            {
                PanelIconGlyph = "\uEC50",
                PanelTitle = "Solution Explorer"
            }
        ];

        RightPanels =
        [
            new DockPanel
            {
                PanelIconGlyph = "\uE946",
                PanelTitle = "Properties"
            }
        ];

        BottomPanels =
        [
            new DockPanel
            {
                PanelIconGlyph = "\uE7BA",
                PanelTitle = "Output"
            }
        ];

        TopPanels = [];
    }

    private void InitializeDefaultDocuments()
    {
        var doc1 = new DocumentTab
        {
            Title = "Document 1",
            IconGlyph = "\uE8A5",
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
}