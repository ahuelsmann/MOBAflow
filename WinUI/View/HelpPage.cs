// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

using System.Collections.Generic;

using Windows.UI;

/// <summary>
/// Help Page with TreeView navigation and RichTextBlock content.
/// Uses 100% native WinUI controls for best performance and accessibility.
/// </summary>
public sealed partial class HelpPage : Page
{
    private readonly TreeView _navigationTreeView;
    private readonly RichTextBlock _contentRichTextBlock;
    private readonly TextBlock _headerTextBlock;
    private readonly Dictionary<string, HelpArticle> _articles = [];
    private readonly Dictionary<TreeViewNode, string> _nodeToArticleMap = [];

    public HelpPage()
    {
        _navigationTreeView = new TreeView { SelectionMode = TreeViewSelectionMode.Single };
        _navigationTreeView.ItemInvoked += NavigationTreeView_ItemInvoked;

        _contentRichTextBlock = new RichTextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            IsTextSelectionEnabled = true
        };

        _headerTextBlock = new TextBlock
        {
            FontSize = 28,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 16)
        };

        InitializeArticles();
        InitializeTreeView();
        Content = BuildLayout();

        LoadArticle("welcome");
    }

    private Grid BuildLayout()
    {
        var rootGrid = new Grid();
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var leftPanel = BuildLeftPanel();
        Grid.SetColumn(leftPanel, 0);
        rootGrid.Children.Add(leftPanel);

        var divider = new Border { Width = 1, Background = new SolidColorBrush(Colors.Gray) };
        Grid.SetColumn(divider, 1);
        rootGrid.Children.Add(divider);

        var rightPanel = BuildRightPanel();
        Grid.SetColumn(rightPanel, 2);
        rootGrid.Children.Add(rightPanel);

        return rootGrid;
    }

    private Grid BuildLeftPanel()
    {
        var grid = new Grid { Padding = new Thickness(16) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new TextBlock
        {
            Text = "MOBAflow Wiki",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 16)
        };
        Grid.SetRow(header, 0);
        grid.Children.Add(header);

        Grid.SetRow(_navigationTreeView, 1);
        grid.Children.Add(_navigationTreeView);

        return grid;
    }

    private ScrollViewer BuildRightPanel()
    {
        var scrollViewer = new ScrollViewer
        {
            Padding = new Thickness(24),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var contentPanel = new StackPanel { Spacing = 16 };
        contentPanel.Children.Add(_headerTextBlock);
        contentPanel.Children.Add(_contentRichTextBlock);

        scrollViewer.Content = contentPanel;
        return scrollViewer;
    }

    private void InitializeArticles()
    {
        _articles["welcome"] = new HelpArticle
        {
            Title = "Welcome to MOBAflow",
            Sections =
            [
                new HelpSection
                {
                    Heading = "About MOBAflow",
                    Content = "MOBAflow is a comprehensive model railway control platform that integrates with Z21 digital command stations. It provides journey management, workflow automation, and real-time feedback monitoring."
                },
                new HelpSection
                {
                    Heading = "Platform Components",
                    IsList = true,
                    ListItems = [
                        "MOBAflow (WinUI) - Full-featured Windows desktop application",
                        "MOBAsmart (MAUI) - Mobile app for Android and iOS",
                        "MOBAdash (Blazor) - Browser-based dashboard",
                        "Plugin System - Extensible architecture for custom features"
                    ]
                },
                new HelpSection
                {
                    Heading = "Getting Help",
                    Content = "Select a topic from the navigation tree on the left to learn more about MOBAflow's features."
                }
            ]
        };

        _articles["getting-started"] = new HelpArticle
        {
            Title = "Getting Started",
            Sections =
            [
                new HelpSection { Heading = "Prerequisites", IsList = true, ListItems = [
                    "Roco/Fleischmann Z21 or z21 command station",
                    "Windows 10/11 for MOBAflow desktop",
                    "Network connection between PC and Z21"
                ]},
                new HelpSection { Heading = "First Steps", IsList = true, ListItems = [
                    "1. Connect to Z21 via Settings page",
                    "2. Create a new Solution or open an existing one",
                    "3. Add your locomotives in the Trains section",
                    "4. Configure feedback points for your layout",
                    "5. Create journeys to automate train movements"
                ]}
            ]
        };

        _articles["train-control"] = new HelpArticle
        {
            Title = "Train Control",
            Sections =
            [
                new HelpSection { Heading = "Overview", Content = "The Train Control page provides a comprehensive interface for controlling locomotives. It features a modern speedometer, function buttons (F0-F20), and configurable speed presets." },
                new HelpSection { Heading = "Features", IsList = true, ListItems = [
                    "Animated tachometer with speed display",
                    "Speed Ramp (software momentum) for smooth acceleration",
                    "Function buttons F0-F20 with custom icons",
                    "Speed category presets (Stop, Slow, Normal, Fast)",
                    "Direction control (Forward/Reverse)",
                    "Emergency stop button"
                ]},
                new HelpSection { Heading = "Transaction Code", Content = "Quick access: Enter 'TC' in MOBAerp to open Train Control." }
            ]
        };

        _articles["journeys"] = new HelpArticle
        {
            Title = "Journey Configuration",
            Sections =
            [
                new HelpSection { Heading = "What are Journeys?", Content = "Journeys define the path a train takes through your layout, including stops at stations, speed changes, and automated actions triggered by feedback points." },
                new HelpSection { Heading = "Creating a Journey", IsList = true, ListItems = [
                    "1. Go to the Journeys page (or enter 'JR' in MOBAerp)",
                    "2. Click 'Add Journey' to create a new journey",
                    "3. Select the locomotive for this journey",
                    "4. Add stops by selecting feedback points",
                    "5. Configure actions for each stop (announcements, sounds)",
                    "6. Save the journey to your solution"
                ]},
                new HelpSection { Heading = "Automated Actions", IsList = true, ListItems = [
                    "Station announcements (Azure Speech)",
                    "Sound effects (WAV files)",
                    "Speed changes",
                    "Function toggles (lights, horn)",
                    "Delays and wait times"
                ]}
            ]
        };

        _articles["signal-box"] = new HelpArticle
        {
            Title = "Signal Box (Stellwerk)",
            Sections =
            [
                new HelpSection { Heading = "Overview", Content = "The Signal Box page provides an electronic interlocking (ESTW) interface similar to real railway control centers. It displays a track diagram showing the occupancy status of each section in real-time." },
                new HelpSection { Heading = "Track Diagram Colors", IsList = true, ListItems = [
                    "Gray: Track section is free (unoccupied)",
                    "Red: Track section is occupied (train detected)",
                    "Green: Route is set",
                    "Yellow: Route is clearing",
                    "Blue: Track is blocked"
                ]},
                new HelpSection { Heading = "Signal Systems", IsList = true, ListItems = [
                    "Ks (Kombinationssignal) - Modern unified system",
                    "H/V (Haupt-/Vorsignal) - Classic light signals",
                    "Formsignale - Historical semaphore signals"
                ]},
                new HelpSection { Heading = "Controls", IsList = true, ListItems = [
                    "NOTHALT: Emergency stop - immediately stops all trains",
                    "Track Power: Toggle track power on/off",
                    "Drag & Drop: Place elements from toolbox",
                    "Mouse Wheel: Rotate selected element"
                ]}
            ]
        };

        _articles["workflows"] = new HelpArticle
        {
            Title = "Workflow Automation",
            Sections =
            [
                new HelpSection { Heading = "Overview", Content = "Workflows allow you to automate complex sequences of actions triggered by events on your layout." },
                new HelpSection { Heading = "Trigger Types", IsList = true, ListItems = [
                    "Feedback point activation",
                    "Manual button press",
                    "Timer/schedule",
                    "Journey events"
                ]},
                new HelpSection { Heading = "Available Actions", IsList = true, ListItems = [
                    "Play audio file",
                    "Text-to-speech announcement",
                    "Locomotive command (speed, function)",
                    "Switch turnout",
                    "Set signal aspect",
                    "Delay/wait"
                ]}
            ]
        };

        _articles["erp"] = new HelpArticle
        {
            Title = "MOBAerp System",
            Sections =
            [
                new HelpSection { Heading = "Overview", Content = "MOBAerp provides ERP-style transaction code navigation for quick access to any page in MOBAflow." },
                new HelpSection { Heading = "Common Transaction Codes", IsList = true, ListItems = [
                    "TC - Train Control",
                    "JR - Journeys",
                    "WF - Workflows",
                    "TR - Trains",
                    "TP - Track Plan",
                    "SB - Signal Box",
                    "SET - Settings",
                    "OV - Overview"
                ]},
                new HelpSection { Heading = "Usage", Content = "Enter a transaction code in the command field and press Enter to navigate directly to that page." }
            ]
        };
    }

    private void InitializeTreeView()
    {
        var rootNode = new TreeViewNode { Content = "MOBAflow Help", IsExpanded = true };

        var welcomeNode = new TreeViewNode { Content = "Welcome" };
        _nodeToArticleMap[welcomeNode] = "welcome";
        rootNode.Children.Add(welcomeNode);

        var gettingStartedNode = new TreeViewNode { Content = "Getting Started" };
        _nodeToArticleMap[gettingStartedNode] = "getting-started";
        rootNode.Children.Add(gettingStartedNode);

        var featuresNode = new TreeViewNode { Content = "Features", IsExpanded = true };

        var trainControlNode = new TreeViewNode { Content = "Train Control" };
        _nodeToArticleMap[trainControlNode] = "train-control";
        featuresNode.Children.Add(trainControlNode);

        var journeysNode = new TreeViewNode { Content = "Journeys" };
        _nodeToArticleMap[journeysNode] = "journeys";
        featuresNode.Children.Add(journeysNode);

        var workflowsNode = new TreeViewNode { Content = "Workflows" };
        _nodeToArticleMap[workflowsNode] = "workflows";
        featuresNode.Children.Add(workflowsNode);

        var signalBoxNode = new TreeViewNode { Content = "Signal Box" };
        _nodeToArticleMap[signalBoxNode] = "signal-box";
        featuresNode.Children.Add(signalBoxNode);

        var erpNode = new TreeViewNode { Content = "MOBAerp System" };
        _nodeToArticleMap[erpNode] = "erp";
        featuresNode.Children.Add(erpNode);

        rootNode.Children.Add(featuresNode);

        _navigationTreeView.RootNodes.Add(rootNode);
    }

    private void NavigationTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        _ = sender;

        if (args.InvokedItem is TreeViewNode node && _nodeToArticleMap.TryGetValue(node, out var articleId))
        {
            LoadArticle(articleId);
        }
    }

    private void LoadArticle(string articleId)
    {
        if (!_articles.TryGetValue(articleId, out var article)) return;

        _headerTextBlock.Text = article.Title;
        _contentRichTextBlock.Blocks.Clear();

        foreach (var section in article.Sections)
        {
            var headingParagraph = new Paragraph { Margin = new Thickness(0, 16, 0, 8) };
            headingParagraph.Inlines.Add(new Run
            {
                Text = section.Heading,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 120, 212))
            });
            _contentRichTextBlock.Blocks.Add(headingParagraph);

            if (section.IsList && section.ListItems != null)
            {
                foreach (var item in section.ListItems)
                {
                    var listParagraph = new Paragraph { Margin = new Thickness(16, 4, 0, 4) };
                    listParagraph.Inlines.Add(new Run { Text = "• " + item });
                    _contentRichTextBlock.Blocks.Add(listParagraph);
                }
            }
            else if (!string.IsNullOrEmpty(section.Content))
            {
                var contentParagraph = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };
                contentParagraph.Inlines.Add(new Run { Text = section.Content });
                _contentRichTextBlock.Blocks.Add(contentParagraph);
            }
        }
    }

    private sealed class HelpArticle
    {
        public string Title { get; init; } = string.Empty;
        public List<HelpSection> Sections { get; init; } = [];
    }

    private sealed class HelpSection
    {
        public string Heading { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public bool IsList { get; init; }
        public List<string> ListItems { get; init; } = [];
    }
}
