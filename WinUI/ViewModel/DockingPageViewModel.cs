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
            Title = "DockingManager.xaml.cs",
            IconGlyph = "\uE943",
            IsModified = false,
            IsPinned = true,
            Content = CreateCodeContent(
                "DockingManager.xaml.cs",
                """
                namespace Moba.WinUI.Controls;

                using Microsoft.UI.Xaml;
                using Microsoft.UI.Xaml.Controls;
                using Microsoft.UI.Input;

                /// <summary>
                /// DockingManager Control mit Visual Studio-ähnlichem Layout.
                /// Cherry-Picked Features:
                ///   - Auto-Hide Sidebars (Qt-ADS 4.x)
                ///   - Draggable Proportional Splitters
                ///   - Focus Highlighting (Qt-ADS 3.5)
                ///   - Panel Close / Undock-to-Tab
                /// </summary>
                public sealed partial class DockingManager : UserControl
                {
                    private const string DockPanelDataKey = "DockPanel";
                    private bool _isOverlayVisible;

                    public DockingManager()
                    {
                        InitializeComponent();
                    }
                }
                """),
            Tag = "dockingmanager"
        };

        var doc2 = new DocumentTab
        {
            Title = "PropertyGrid.xaml",
            IconGlyph = "\uF158",
            IsModified = true,
            IsPinned = false,
            Content = CreateCodeContent(
                "PropertyGrid.xaml",
                """
                <UserControl
                    x:Class="Moba.WinUI.Controls.PropertyGrid"
                    xmlns="http://schemas.microsoft.com/.../presentation">

                    <Grid Background="{ThemeResource LayerFillColorDefaultBrush}">
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto" />
                            <RowDefinition Height="Auto" />
                            <RowDefinition Height="*" />
                            <RowDefinition Height="Auto" />
                        </Grid.RowDefinitions>

                        <!-- Object Selector -->
                        <ComboBox x:Name="ObjectSelector" ... />

                        <!-- Property ListView -->
                        <ListView x:Name="PropertyListView" ... />

                        <!-- Description Pane -->
                        <Border Grid.Row="3" ... />
                    </Grid>
                </UserControl>
                """),
            Tag = "propertygrid-xaml"
        };

        var doc3 = new DocumentTab
        {
            Title = "README.md",
            IconGlyph = "\uE736",
            IsModified = false,
            IsPinned = false,
            Content = CreateMarkdownContent(),
            Tag = "readme"
        };

        var doc4 = new DocumentTab
        {
            Title = "App.xaml",
            IconGlyph = "\uF158",
            IsModified = true,
            IsPinned = false,
            Content = CreateCodeContent(
                "App.xaml",
                """
                <Application x:Class="Moba.WinUI.App">
                    <Application.Resources>
                        <ResourceDictionary>
                            <ResourceDictionary.MergedDictionaries>
                                <XamlControlsResources />
                                <ResourceDictionary Source="/Resources/ControlStyles.xaml" />
                            </ResourceDictionary.MergedDictionaries>

                            <ResourceDictionary.ThemeDictionaries>
                                <ResourceDictionary x:Key="Light">
                                    <!-- All Fluent Design System native resources -->
                                </ResourceDictionary>
                                <ResourceDictionary x:Key="Dark">
                                    <!-- Automatic Dark theme support -->
                                </ResourceDictionary>
                            </ResourceDictionary.ThemeDictionaries>
                        </ResourceDictionary>
                    </Application.Resources>
                </Application>
                """),
            Tag = "app-xaml"
        };

        OpenDocuments.Add(doc1);
        OpenDocuments.Add(doc2);
        OpenDocuments.Add(doc3);
        OpenDocuments.Add(doc4);

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

    private static UIElement CreateCodeContent(string filename, string code)
    {
        return new Grid
        {
            Children =
            {
                new ScrollViewer
                {
                    Padding = new Thickness(16, 8, 16, 16),
                    Content = new StackPanel
                    {
                        Spacing = 0,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = filename,
                                FontSize = 11,
                                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                                Margin = new Thickness(0, 0, 0, 8),
                                Opacity = 0.5
                            },
                            new TextBlock
                            {
                                Text = code.Trim(),
                                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Cascadia Code,Cascadia Mono,Consolas"),
                                FontSize = 13,
                                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                                IsTextSelectionEnabled = true
                            }
                        }
                    }
                }
            }
        };
    }

    private static UIElement CreateMarkdownContent()
    {
        return new StackPanel
        {
            Padding = new Thickness(24, 16, 24, 16),
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "MOBAflow",
                    FontSize = 28,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold
                },
                new TextBlock
                {
                    Text = "Model Railway Automation & Control",
                    FontSize = 16,
                    Opacity = 0.7
                },
                new Border
                {
                    Height = 1,
                    Margin = new Thickness(0, 8, 0, 8),
                    Opacity = 0.2
                },
                new TextBlock
                {
                    Text = "Features",
                    FontSize = 18,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Thickness(0, 8, 0, 0)
                },
                new TextBlock
                {
                    Text = "• Visual Studio-style Docking Manager with Auto-Hide\n" +
                           "• PropertyGrid control with category/alphabetical view\n" +
                           "• Tab Grouping (Modified, Pinned, Open)\n" +
                           "• Draggable proportional splitters\n" +
                           "• Focus Highlighting (Qt-ADS inspired)\n" +
                           "• Full Fluent Design System compliance\n" +
                           "• Light/Dark theme support with system accent colors\n" +
                           "• Context menu: Pin to Left/Right/Bottom",
                    FontSize = 14,
                    TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                    LineHeight = 24
                }
            }
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
                    Text = $"Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    FontSize = 12,
                    Opacity = 0.6
                }
            }
        };
    }
}
