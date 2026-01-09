// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Moba.WinUI.Service;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Windows.UI;

/// <summary>
/// SAP-style transaction page with command field for quick navigation.
/// Provides a familiar SAP GUI experience for users accustomed to ERP systems.
/// Built programmatically in C# without XAML for reliability.
/// </summary>
public sealed class SapTransactionPage : Page
{
    // SAP Corporate Colors
    private static readonly Color SapBlue = Color.FromArgb(255, 10, 110, 209);      // #0A6ED1 - SAP Blue
    private static readonly Color SapGold = Color.FromArgb(255, 240, 171, 0);       // #F0AB00 - SAP Gold
    private static readonly Color SapDarkBlue = Color.FromArgb(255, 0, 50, 100);    // #003264 - SAP Dark Blue
    private static readonly Color SapLightGray = Color.FromArgb(255, 237, 237, 237); // #EDEDED - SAP Light Gray

    private readonly NavigationService _navigationService;
    private readonly ObservableCollection<CommandHistoryItem> _commandHistory = [];
    private readonly TextBox _transactionCodeTextBox;
    private readonly TextBlock _statusMessageTextBlock;
    private readonly TextBlock _noHistoryTextBlock;
    private readonly ListView _commandHistoryListView;
    private readonly TreeView _sapMenuTreeView;

    public SapTransactionPage(NavigationService navigationService)
    {
        _navigationService = navigationService;

        // Create UI elements
        _transactionCodeTextBox = new TextBox
        {
            PlaceholderText = "/n[TCODE]...",
            FontFamily = new FontFamily("Consolas"),
            FontSize = 14,
            Width = 300
        };
        _transactionCodeTextBox.KeyDown += TransactionCodeTextBox_KeyDown;

        _statusMessageTextBlock = new TextBlock { Text = "Ready" };
        _noHistoryTextBlock = new TextBlock
        {
            Text = "No commands executed yet.",
            FontStyle = Windows.UI.Text.FontStyle.Italic,
            Opacity = 0.7
        };

        _commandHistoryListView = new ListView
        {
            MaxHeight = 200,
            SelectionMode = ListViewSelectionMode.Single,
            IsItemClickEnabled = true
        };
        _commandHistoryListView.ItemClick += CommandHistoryListView_ItemClick;
        _commandHistoryListView.ItemsSource = _commandHistory;

        _sapMenuTreeView = new TreeView { SelectionMode = TreeViewSelectionMode.Single };
        _sapMenuTreeView.ItemInvoked += SapMenuTreeView_ItemInvoked;

        Content = BuildLayout();
        InitializeTreeView();
    }

    private Grid BuildLayout()
    {
        var rootGrid = new Grid();
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Row 0: Command Bar
        var commandBar = BuildCommandBar();
        Grid.SetRow(commandBar, 0);
        rootGrid.Children.Add(commandBar);

        // Row 1: Menu Bar
        var menuBar = BuildMenuBar();
        Grid.SetRow(menuBar, 1);
        rootGrid.Children.Add(menuBar);

        // Row 2: Main Content
        var mainContent = BuildMainContent();
        Grid.SetRow(mainContent, 2);
        rootGrid.Children.Add(mainContent);

        // Row 3: Status Bar
        var statusBar = BuildStatusBar();
        Grid.SetRow(statusBar, 3);
        rootGrid.Children.Add(statusBar);

        return rootGrid;
    }

    private Grid BuildCommandBar()
    {
        var grid = new Grid
        {
            Padding = new Thickness(8),
            Background = new SolidColorBrush(SapBlue)
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var label = new TextBlock
        {
            Text = "Transaction:",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 8, 0),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Colors.White)
        };
        Grid.SetColumn(label, 0);
        grid.Children.Add(label);

        Grid.SetColumn(_transactionCodeTextBox, 1);
        grid.Children.Add(_transactionCodeTextBox);

        var executeButton = new Button
        {
            Margin = new Thickness(8, 0, 0, 0),
            Background = new SolidColorBrush(SapGold),
            Foreground = new SolidColorBrush(SapDarkBlue)
        };
        var executeContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        executeContent.Children.Add(new FontIcon { Glyph = "\uE768", FontSize = 14, Foreground = new SolidColorBrush(SapDarkBlue) });
        executeContent.Children.Add(new TextBlock { Text = "Execute", Foreground = new SolidColorBrush(SapDarkBlue), FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        executeButton.Content = executeContent;
        executeButton.Click += ExecuteButton_Click;
        Grid.SetColumn(executeButton, 2);
        grid.Children.Add(executeButton);

        var toolbarPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        toolbarPanel.Children.Add(CreateToolbarButton("\uE72B", BackButton_Click));
        toolbarPanel.Children.Add(CreateToolbarButton("\uE711", ExitButton_Click));
        toolbarPanel.Children.Add(CreateToolbarButton("\uE8BB", CancelButton_Click));
        toolbarPanel.Children.Add(new Border { Width = 1, Margin = new Thickness(8, 4, 8, 4), Background = new SolidColorBrush(Colors.White), Opacity = 0.5 });
        toolbarPanel.Children.Add(CreateToolbarButton("\uE74E", SaveButton_Click));
        toolbarPanel.Children.Add(CreateToolbarButton("\uE749", PrintButton_Click));
        toolbarPanel.Children.Add(CreateToolbarButton("\uE721", FindButton_Click));
        Grid.SetColumn(toolbarPanel, 4);
        grid.Children.Add(toolbarPanel);

            return grid;
        }

        private static Button CreateToolbarButton(string glyph, RoutedEventHandler handler)
        {
            var button = new Button
            {
                Background = new SolidColorBrush(Colors.Transparent),
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                Content = new FontIcon
                {
                    Glyph = glyph,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Colors.White)
                }
            };
            button.Click += handler;
            return button;
        }

    private static MenuBar BuildMenuBar()
    {
        var menuBar = new MenuBar();

        var systemMenu = new MenuBarItem { Title = "System" };
        systemMenu.Items.Add(new MenuFlyoutItem { Text = "User Profile" });
        systemMenu.Items.Add(new MenuFlyoutItem { Text = "Log Off" });
        menuBar.Items.Add(systemMenu);

        var editMenu = new MenuBarItem { Title = "Edit" };
        editMenu.Items.Add(new MenuFlyoutItem { Text = "Copy" });
        editMenu.Items.Add(new MenuFlyoutItem { Text = "Paste" });
        menuBar.Items.Add(editMenu);

        var favoritesMenu = new MenuBarItem { Title = "Favorites" };
        favoritesMenu.Items.Add(new MenuFlyoutItem { Text = "Add to Favorites" });
        menuBar.Items.Add(favoritesMenu);

        var helpMenu = new MenuBarItem { Title = "Help" };
        helpMenu.Items.Add(new MenuFlyoutItem { Text = "Application Help" });
        menuBar.Items.Add(helpMenu);

        return menuBar;
    }

    private Grid BuildMainContent()
    {
        var grid = new Grid { Padding = new Thickness(16) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Left: SAP Menu TreeView
        var leftPanel = new Grid();
        leftPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        leftPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var menuTitle = new TextBlock
        {
            Text = "SAP Easy Access",
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 16)
        };
        Grid.SetRow(menuTitle, 0);
        leftPanel.Children.Add(menuTitle);

        Grid.SetRow(_sapMenuTreeView, 1);
        leftPanel.Children.Add(_sapMenuTreeView);

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

    private Grid BuildContentArea()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // Title
        var titlePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        titlePanel.Children.Add(new TextBlock
        {
            Text = "SAP Easy Access",
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(SapBlue)
        });
        titlePanel.Children.Add(new TextBlock
        {
            Text = "MOBAflow SAP-Style Interface",
            Opacity = 0.7
        });
        Grid.SetRow(titlePanel, 0);
        grid.Children.Add(titlePanel);

        // Content Cards
        var scrollViewer = new ScrollViewer();
        var contentPanel = new StackPanel { Spacing = 16 };

        // TCODE Card
        var tcodeCard = CreateCard("Available Transaction Codes",
            "Enter a transaction code and press Enter:\n\n" +
            "/nOVERVIEW - Open Overview Page\n" +
            "/nSOLUTION - Solution Management\n" +
            "/nJOURNEYS - Journey Configuration\n" +
            "/nWORKFLOWS - Workflow Automation\n" +
            "/nTRAINS - Train Management\n" +
            "/nTRAINCONTROL - Train Control Panel\n" +
            "/nSETTINGS - Application Settings\n" +
            "/nHELP - Wiki / Help Center");
        contentPanel.Children.Add(tcodeCard);

        // History Card
        var historyCard = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(24)
        };
        var historyPanel = new StackPanel { Spacing = 12 };
        historyPanel.Children.Add(new TextBlock
        {
            Text = "Command History",
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        historyPanel.Children.Add(_commandHistoryListView);
        historyPanel.Children.Add(_noHistoryTextBlock);
        historyCard.Child = historyPanel;
        contentPanel.Children.Add(historyCard);

        scrollViewer.Content = contentPanel;
        Grid.SetRow(scrollViewer, 1);
        grid.Children.Add(scrollViewer);

        return grid;
    }

    private static Border CreateCard(string title, string content)
    {
        var card = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(24)
        };
        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        panel.Children.Add(new TextBlock { Text = content, TextWrapping = TextWrapping.Wrap });
        card.Child = panel;
        return card;
    }

    private Grid BuildStatusBar()
    {
        var grid = new Grid
        {
            Padding = new Thickness(8, 4, 8, 4),
            Background = new SolidColorBrush(SapLightGray)
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Status message with SAP Gold accent for visibility
        _statusMessageTextBlock.Foreground = new SolidColorBrush(SapDarkBlue);
        Grid.SetColumn(_statusMessageTextBlock, 0);
        grid.Children.Add(_statusMessageTextBlock);

        var clientText = new TextBlock
        {
            Text = "Client: 100",
            Margin = new Thickness(16, 0, 16, 0),
            Foreground = new SolidColorBrush(SapDarkBlue)
        };
        Grid.SetColumn(clientText, 1);
        grid.Children.Add(clientText);

        var userText = new TextBlock
        {
            Text = "MOBAUSER",
            Foreground = new SolidColorBrush(SapDarkBlue)
        };
        Grid.SetColumn(userText, 2);
        grid.Children.Add(userText);

        return grid;
    }

    private void InitializeTreeView()
    {
        var rootNode = new TreeViewNode { Content = "Favorites", IsExpanded = true };
        rootNode.Children.Add(new TreeViewNode { Content = "[No favorites defined]" });
        _sapMenuTreeView.RootNodes.Add(rootNode);

        var sapMenuNode = new TreeViewNode { Content = "SAP Menu", IsExpanded = true };

        var logisticsNode = new TreeViewNode { Content = "Logistics" };
        logisticsNode.Children.Add(new TreeViewNode { Content = "Materials Management" });
        logisticsNode.Children.Add(new TreeViewNode { Content = "Sales and Distribution" });
        sapMenuNode.Children.Add(logisticsNode);

        var accountingNode = new TreeViewNode { Content = "Accounting" };
        accountingNode.Children.Add(new TreeViewNode { Content = "Financial Accounting" });
        accountingNode.Children.Add(new TreeViewNode { Content = "Controlling" });
        sapMenuNode.Children.Add(accountingNode);

        var toolsNode = new TreeViewNode { Content = "Tools" };
        toolsNode.Children.Add(new TreeViewNode { Content = "ABAP Workbench" });
        toolsNode.Children.Add(new TreeViewNode { Content = "Administration" });
        sapMenuNode.Children.Add(toolsNode);

        _sapMenuTreeView.RootNodes.Add(sapMenuNode);
    }

    private void TransactionCodeTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ExecuteTransaction();
            e.Handled = true;
        }
    }

    private void ExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        ExecuteTransaction();
    }

    private void ExecuteTransaction()
    {
        var command = _transactionCodeTextBox.Text.Trim();
        if (string.IsNullOrEmpty(command))
        {
            SetStatus("Please enter a transaction code");
            return;
        }

        var normalizedCommand = command.ToUpperInvariant();
        var navigationTag = MapTransactionToNavigationTag(normalizedCommand);

        if (navigationTag != null)
        {
            AddToHistory(command, "OK");
            SetStatus($"Navigating to {navigationTag}...");
            _ = _navigationService.NavigateToPageAsync(navigationTag);
            _transactionCodeTextBox.Text = string.Empty;
        }
        else
        {
            AddToHistory(command, "Error");
            SetStatus($"Unknown transaction code: {command}");
        }
    }

    private static string? MapTransactionToNavigationTag(string command)
    {
        var cleanCommand = command.StartsWith("/N", StringComparison.OrdinalIgnoreCase)
            ? command[2..]
            : command;

        return TransactionMappings.TryGetValue(cleanCommand, out var tag) ? tag : null;
    }

    private static readonly Dictionary<string, string> TransactionMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "OVERVIEW", "overview" },
        { "OV", "overview" },
        { "SOLUTION", "solution" },
        { "SOL", "solution" },
        { "JOURNEYS", "journeys" },
        { "JRN", "journeys" },
        { "WORKFLOWS", "workflows" },
        { "WF", "workflows" },
        { "TRAINS", "trains" },
        { "TR", "trains" },
        { "TRAINCONTROL", "traincontrol" },
        { "TC", "traincontrol" },
        { "TRACKPLAN", "trackplaneditor" },
        { "TP", "trackplaneditor" },
        { "JOURNEYMAP", "journeymap" },
        { "JM", "journeymap" },
        { "SETTINGS", "settings" },
        { "SET", "settings" },
        { "HELP", "help" },
        { "WIKI", "help" },
        { "SAP", "sap" },
        { "SE00", "sap" },
    };

    private void AddToHistory(string command, string status)
    {
        _commandHistory.Insert(0, new CommandHistoryItem
        {
            Timestamp = DateTime.Now.ToString("HH:mm:ss"),
            Command = command,
            Status = status
        });

        if (_commandHistory.Count > 50)
        {
            _commandHistory.RemoveAt(_commandHistory.Count - 1);
        }

        _noHistoryTextBlock.Visibility = _commandHistory.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetStatus(string message)
    {
        _statusMessageTextBlock.Text = message;
    }

    private void CommandHistoryListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        _ = sender;

        if (e.ClickedItem is CommandHistoryItem item)
        {
            _transactionCodeTextBox.Text = item.Command;
            _transactionCodeTextBox.Focus(FocusState.Programmatic);
        }
    }

    private void SapMenuTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        _ = sender;

        if (args.InvokedItem is TreeViewNode node)
        {
            var content = node.Content?.ToString();
            SetStatus($"Selected: {content}");
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        SetStatus("Back navigation not available");
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        _ = _navigationService.NavigateToPageAsync("overview");
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        _transactionCodeTextBox.Text = string.Empty;
        SetStatus("Command cancelled");
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        SetStatus("Save not applicable");
    }

    private void PrintButton_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        SetStatus("Print not implemented");
    }

    private void FindButton_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        _transactionCodeTextBox.Focus(FocusState.Programmatic);
        _transactionCodeTextBox.SelectAll();
    }
}

/// <summary>
/// Represents a command history entry.
/// </summary>
public sealed class CommandHistoryItem
{
    public string Timestamp { get; init; } = string.Empty;
    public string Command { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}
