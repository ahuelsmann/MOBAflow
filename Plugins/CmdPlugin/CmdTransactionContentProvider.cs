// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Plugin.Cmd;

using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

/// <summary>
/// MOBAcmd Transaction Page - Command-style navigation interface.
/// Provides transaction codes for quick navigation to MOBAflow pages.
/// 
/// CRITICAL WinUI 3 Plugin Architecture:
/// - Plugins must NOT return types that inherit from Page
/// - WinUI cannot resolve Page types from dynamically loaded assemblies
/// - Instead, return a UIElement via CreateContent()
/// </summary>
public sealed class CmdTransactionContentProvider
{
    // MOBAcmd Corporate Colors (inspired by command-line interfaces)
    private static readonly Color CmdBlue = Color.FromArgb(255, 0, 102, 179);       // #0066B3 - Corporate Blue
    private static readonly Color CmdGold = Color.FromArgb(255, 255, 193, 7);       // #FFC107 - Accent Gold
    private static readonly Color CmdDarkBlue = Color.FromArgb(255, 0, 51, 102);    // #003366 - Dark Blue
    private static readonly Color CmdLightGray = Color.FromArgb(255, 240, 240, 240); // #F0F0F0 - Light Gray
    private static readonly Color CmdGreen = Color.FromArgb(255, 40, 167, 69);      // #28A745 - Success Green

    private readonly CmdPluginViewModel _viewModel;

    public CmdTransactionContentProvider(CmdPluginViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public UIElement CreateContent()
    {
        var content = BuildLayout();
        return content;
    }

    private Grid BuildLayout()
    {
        var rootGrid = new Grid();
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Row 1: Main Content
        var mainContent = BuildMainContent();
        Grid.SetRow(mainContent, 1);
        rootGrid.Children.Add(mainContent);

        return rootGrid;
    }

    private Grid BuildMainContent()
    {
        var grid = new Grid { Padding = new Thickness(16) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Left: CMD Menu TreeView
        var leftPanel = new Grid();
        leftPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        leftPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var menuTitle = new TextBlock
        {
            Text = "MOBAcmd Easy Access",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(CmdBlue),
            Margin = new Thickness(0, 0, 0, 16)
        };
        Grid.SetRow(menuTitle, 0);
        leftPanel.Children.Add(menuTitle);

        var treeView = BuildTreeView();
        Grid.SetRow(treeView, 1);
        leftPanel.Children.Add(treeView);

        Grid.SetColumn(leftPanel, 0);
        grid.Children.Add(leftPanel);

        // Divider
        var divider = new Border
        {
            Width = 1,
            Background = new SolidColorBrush(Colors.Gray),
            Margin = new Thickness(16, 0, 16, 0)
        };
        Grid.SetColumn(divider, 1);
        grid.Children.Add(divider);

        // Right: Content Area
        var rightPanel = BuildContentArea();
        Grid.SetColumn(rightPanel, 2);
        grid.Children.Add(rightPanel);

        return grid;
    }

    private TreeView BuildTreeView()
    {
        var treeView = new TreeView { SelectionMode = TreeViewSelectionMode.Single };
        treeView.ItemInvoked += CmdMenuTreeView_ItemInvoked;

        var rootNode = new TreeViewNode { Content = "Favorites", IsExpanded = true };
        rootNode.Children.Add(new TreeViewNode { Content = "TC - Train Control" });
        rootNode.Children.Add(new TreeViewNode { Content = "JR - Journeys" });
        treeView.RootNodes.Add(rootNode);

        var mobaMenuNode = new TreeViewNode { Content = "MOBAcmd Menu", IsExpanded = true };

        var operationsNode = new TreeViewNode { Content = "Train Operations", IsExpanded = true };
        operationsNode.Children.Add(CreateMenuNode("TC", "Train Control"));
        operationsNode.Children.Add(CreateMenuNode("TC01", "Train Control (Ramp)"));
        operationsNode.Children.Add(CreateMenuNode("TR", "Trains"));
        operationsNode.Children.Add(CreateMenuNode("TR01", "Train Details"));
        mobaMenuNode.Children.Add(operationsNode);

        var configNode = new TreeViewNode { Content = "Configuration" };
        configNode.Children.Add(CreateMenuNode("JR", "Journeys"));
        configNode.Children.Add(CreateMenuNode("JR01", "Journey Editor"));
        configNode.Children.Add(CreateMenuNode("WF", "Workflows"));
        configNode.Children.Add(CreateMenuNode("WF01", "Workflow Editor"));
        mobaMenuNode.Children.Add(configNode);

        var layoutNode = new TreeViewNode { Content = "Layout Design" };
        layoutNode.Children.Add(CreateMenuNode("TP", "Track Plan"));
        layoutNode.Children.Add(CreateMenuNode("TP01", "Import Track Plan"));
        layoutNode.Children.Add(CreateMenuNode("JM", "Journey Map"));
        layoutNode.Children.Add(CreateMenuNode("SB", "Signal Box"));
        mobaMenuNode.Children.Add(layoutNode);

        var systemNode = new TreeViewNode { Content = "System" };
        systemNode.Children.Add(CreateMenuNode("OV", "Overview"));
        systemNode.Children.Add(CreateMenuNode("SOL", "Solution"));
        systemNode.Children.Add(CreateMenuNode("SET", "Settings"));
        systemNode.Children.Add(CreateMenuNode("MON", "Monitor"));
        mobaMenuNode.Children.Add(systemNode);

        var helpNode = new TreeViewNode { Content = "Help" };
        helpNode.Children.Add(CreateMenuNode("H1", "Help (WebView2)"));
        helpNode.Children.Add(CreateMenuNode("H2", "Help (TreeView)"));
        helpNode.Children.Add(CreateMenuNode("H3", "Help (TabView)"));
        mobaMenuNode.Children.Add(helpNode);

        treeView.RootNodes.Add(mobaMenuNode);

        return treeView;
    }

    private static TreeViewNode CreateMenuNode(string code, string title)
    {
        return new TreeViewNode { Content = $"{code} - {title}" };
    }

    private Grid BuildContentArea()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // Title
        var titlePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        titlePanel.Children.Add(new TextBlock
        {
            Text = "MOBAcmd Easy Access",
            FontSize = 24,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(CmdBlue)
        });
        titlePanel.Children.Add(new TextBlock
        {
            Text = "Model Railway Command-Style Navigation Interface",
            Opacity = 0.7
        });
        Grid.SetRow(titlePanel, 0);
        grid.Children.Add(titlePanel);

        // Content Cards
        var scrollViewer = new ScrollViewer();
        var contentPanel = new StackPanel { Spacing = 16 };

        // Transaction Codes Card
        var tcodeCard = CreateTransactionCodesCard();
        contentPanel.Children.Add(tcodeCard);

        // Keyboard Shortcuts Card
        var shortcutsCard = CreateKeyboardShortcutsCard();
        contentPanel.Children.Add(shortcutsCard);

        scrollViewer.Content = contentPanel;
        Grid.SetRow(scrollViewer, 1);
        grid.Children.Add(scrollViewer);

        return grid;
    }

    private static Border CreateTransactionCodesCard()
    {
        var card = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(24),
            Background = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0))
        };

        var panel = new StackPanel { Spacing = 16 };

        panel.Children.Add(new TextBlock
        {
            Text = "Available Transaction Codes",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold
        });

        panel.Children.Add(new TextBlock
        {
            Text = "Enter a transaction code in the command field and press Enter:",
            Opacity = 0.7,
            Margin = new Thickness(0, 0, 0, 8)
        });

        // Transaction Code Grid
        var codeGrid = new Grid { ColumnSpacing = 24, RowSpacing = 8 };
        codeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        codeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        codeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        codeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var codes = TransactionCodeMapper.GetAllTransactionCodes();

        int row = 0;
        for (int i = 0; i < codes.Count; i += 2)
        {
            codeGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // First column
            var code1 = new TextBlock
            {
                Text = codes[i].Code,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(CmdBlue)
            };
            Grid.SetRow(code1, row);
            Grid.SetColumn(code1, 0);
            codeGrid.Children.Add(code1);

            var desc1 = new TextBlock { Text = codes[i].Description, TextWrapping = TextWrapping.Wrap };
            Grid.SetRow(desc1, row);
            Grid.SetColumn(desc1, 1);
            codeGrid.Children.Add(desc1);

            // Second column (if exists)
            if (i + 1 < codes.Count)
            {
                var code2 = new TextBlock
                {
                    Text = codes[i + 1].Code,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(CmdBlue)
                };
                Grid.SetRow(code2, row);
                Grid.SetColumn(code2, 2);
                codeGrid.Children.Add(code2);

                var desc2 = new TextBlock { Text = codes[i + 1].Description, TextWrapping = TextWrapping.Wrap };
                Grid.SetRow(desc2, row);
                Grid.SetColumn(desc2, 3);
                codeGrid.Children.Add(desc2);
            }

            row++;
        }

        panel.Children.Add(codeGrid);
        card.Child = panel;
        return card;
    }

    private static Border CreateKeyboardShortcutsCard()
    {
        var card = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(24),
            Background = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0))
        };

        var panel = new StackPanel { Spacing = 16 };

        panel.Children.Add(new TextBlock
        {
            Text = "Keyboard Shortcuts",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold
        });

        panel.Children.Add(new TextBlock
        {
            Text = "Global keyboard shortcuts for quick access to common functions:",
            Opacity = 0.7,
            Margin = new Thickness(0, 0, 0, 8)
        });

        // Shortcuts Grid
        var shortcutGrid = new Grid { ColumnSpacing = 24, RowSpacing = 8 };
        shortcutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        shortcutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        shortcutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        shortcutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var shortcuts = new[]
        {
            (Key: "F5", Description: "Connect to Z21"),
            (Key: "Space", Description: "Toggle Track Power"),
            (Key: "R", Description: "Reset Journey"),
            (Key: "Ctrl+N", Description: "New Solution"),
            (Key: "Ctrl+O", Description: "Open Solution"),
            (Key: "Ctrl+S", Description: "Save Solution"),
            (Key: "Enter", Description: "Execute Transaction Code"),
            (Key: "Esc", Description: "Clear Transaction Code")
        };

        int row = 0;
        for (int i = 0; i < shortcuts.Length; i += 2)
        {
            shortcutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // First column
            var key1 = new Border
            {
                Background = new SolidColorBrush(CmdBlue),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            var keyText1 = new TextBlock
            {
                Text = shortcuts[i].Key,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White)
            };
            key1.Child = keyText1;
            Grid.SetRow(key1, row);
            Grid.SetColumn(key1, 0);
            shortcutGrid.Children.Add(key1);

            var desc1 = new TextBlock
            {
                Text = shortcuts[i].Description,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(desc1, row);
            Grid.SetColumn(desc1, 1);
            shortcutGrid.Children.Add(desc1);

            // Second column (if exists)
            if (i + 1 < shortcuts.Length)
            {
                var key2 = new Border
                {
                    Background = new SolidColorBrush(CmdBlue),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 4, 8, 4),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                var keyText2 = new TextBlock
                {
                    Text = shortcuts[i + 1].Key,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.White)
                };
                key2.Child = keyText2;
                Grid.SetRow(key2, row);
                Grid.SetColumn(key2, 2);
                shortcutGrid.Children.Add(key2);

                var desc2 = new TextBlock
                {
                    Text = shortcuts[i + 1].Description,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetRow(desc2, row);
                Grid.SetColumn(desc2, 3);
                shortcutGrid.Children.Add(desc2);
            }

            row++;
        }

        panel.Children.Add(shortcutGrid);
        card.Child = panel;
        return card;
    }

    private void CommandHistoryListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        _ = sender;
        if (e.ClickedItem is CommandHistoryItem item)
        {
            _viewModel.SelectHistoryItemCommand.Execute(item);
        }
    }

    private void CmdMenuTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        _ = sender;
        if (args.InvokedItem is TreeViewNode node)
        {
            _viewModel.ExecuteFromTreeViewCommand.Execute(node.Content?.ToString());
        }
    }
}

/// <summary>
/// Converter to show "No history" text when command history is empty.
/// </summary>
public sealed partial class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        _ = targetType;
        _ = parameter;
        _ = language;
        return value is int count ? count == 0 ? Visibility.Visible : Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        _ = value;
        _ = targetType;
        _ = parameter;
        _ = language;
        throw new NotImplementedException();
    }
}

/// <summary>
/// Represents a command history entry for MOBAcmd.
/// </summary>
public sealed class CommandHistoryItem
{
    public string Timestamp { get; init; } = string.Empty;
    public string Command { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;

    public override string ToString() => $"[{Timestamp}] {Command} - {Status}";
}
