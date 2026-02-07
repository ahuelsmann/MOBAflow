// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Moba.WinUI.Controls;
using System;
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

    private int _documentCounter = 1;

    public DockingPageViewModel()
    {
        InitializeDefaultDocuments();
    }

    private void InitializeDefaultDocuments()
    {
        var doc1 = new DocumentTab
        {
            Title = "Welcome.md",
            IconGlyph = "\uE745",
            IsModified = false,
            IsPinned = true,
            Content = CreateWelcomeContent(),
            Tag = "welcome"
        };

        var doc2 = new DocumentTab
        {
            Title = "Example.txt",
            IconGlyph = "\uE160",
            IsModified = true,
            IsPinned = false,
            Content = CreateExampleContent(),
            Tag = "example"
        };

        var doc3 = new DocumentTab
        {
            Title = "Test File.xml",
            IconGlyph = "\uE7AC",
            IsModified = false,
            IsPinned = false,
            Content = CreateXmlContent(),
            Tag = "test"
        };

        OpenDocuments.Add(doc1);
        OpenDocuments.Add(doc2);
        OpenDocuments.Add(doc3);

        ActiveDocument = doc1;
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
            IsPinned = false,
            Content = CreateNewDocumentContent(docNum),
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
    private void PinCurrentDocument()
    {
        if (ActiveDocument != null)
        {
            ActiveDocument.IsPinned = !ActiveDocument.IsPinned;
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

    private UIElement CreateWelcomeContent()
    {
        return new StackPanel
        {
            Padding = new Thickness(16),
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = "Welcome to DockingManager Test",
                    FontSize = 24,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold
                },
                new TextBlock
                {
                    Text = "This is a demonstration of the LayoutDocumentEx control.",
                    FontSize = 14
                },
                new TextBlock
                {
                    Text = "Features demonstrated:",
                    FontSize = 14,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Thickness(0, 12, 0, 0)
                },
                new TextBlock
                {
                    Text = "• Tab Grouping (Modified, Pinned, Open)\n" +
                           "• Custom Tab Templates\n" +
                           "• Modified & Pinned Indicators\n" +
                           "• Tab Management (Add, Remove, Pin)\n" +
                           "• Full MVVM Binding Support\n" +
                           "• Floating Windows (coming soon)",
                    FontSize = 13
                }
            }
        };
    }

    private UIElement CreateExampleContent()
    {
        return new StackPanel
        {
            Padding = new Thickness(16),
            Spacing = 8,
            Children =
            {
                new TextBlock
                {
                    Text = "Modified Document Example",
                    FontSize = 18,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                },
                new TextBlock
                {
                    Text = "This document is marked as modified (unsaved).",
                    FontSize = 13,
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCautionBrush"]
                },
                new TextBlock
                {
                    Text = "Notice the red dot (●) indicator in the tab header.",
                    FontSize = 13
                },
                new TextBlock
                {
                    Text = "Content: Example modified text file",
                    FontSize = 12,
                    Margin = new Thickness(0, 16, 0, 0)
                }
            }
        };
    }

    private UIElement CreateXmlContent()
    {
        var codeBlock = new TextBlock
        {
            Text = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                   "<root>\n" +
                   "  <item>Test Data</item>\n" +
                   "  <config>\n" +
                   "    <enabled>true</enabled>\n" +
                   "  </config>\n" +
                   "</root>",
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Courier New"),
            FontSize = 11
        };

        return new ScrollViewer
        {
            Padding = new Thickness(16),
            Content = codeBlock
        };
    }

    private UIElement CreateNewDocumentContent(int number)
    {
        return new StackPanel
        {
            Padding = new Thickness(16),
            Spacing = 8,
            Children =
            {
                new TextBlock
                {
                    Text = $"Document {number}",
                    FontSize = 18,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                },
                new TextBlock
                {
                    Text = "This is a newly created document.",
                    FontSize = 13
                },
                new TextBlock
                {
                    Text = "Created at: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    FontSize = 12
                }
            }
        };
    }
}
