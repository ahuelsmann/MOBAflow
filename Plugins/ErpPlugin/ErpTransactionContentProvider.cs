// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Moba.SharedUI.ViewModel;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Windows.UI;

namespace Moba.Plugin.Erp;

/// <summary>
/// MOBAerp Transaction Page - ERP-style navigation interface.
/// Provides transaction codes for quick navigation to MOBAflow pages.
/// 
/// CRITICAL WinUI 3 Plugin Architecture:
/// - Plugins must NOT return types that inherit from Page
/// - WinUI cannot resolve Page types from dynamically loaded assemblies
/// - Instead, return a UIElement via CreateContent()
/// </summary>
public sealed class ErpTransactionContentProvider
{
    // MOBAerp Corporate Colors (inspired by enterprise systems)
    private static readonly Color ErpBlue = Color.FromArgb(255, 0, 102, 179);       // #0066B3 - Corporate Blue
    private static readonly Color ErpGold = Color.FromArgb(255, 255, 193, 7);       // #FFC107 - Accent Gold
    private static readonly Color ErpDarkBlue = Color.FromArgb(255, 0, 51, 102);    // #003366 - Dark Blue
    private static readonly Color ErpLightGray = Color.FromArgb(255, 240, 240, 240); // #F0F0F0 - Light Gray
    private static readonly Color ErpGreen = Color.FromArgb(255, 40, 167, 69);      // #28A745 - Success Green

    private readonly MainWindowViewModel _mainViewModel;
    private readonly ObservableCollection<CommandHistoryItem> _commandHistory = [];
    private TextBox? _transactionCodeTextBox;
    private TextBlock? _statusMessageTextBlock;
    private TextBlock? _noHistoryTextBlock;
    private ListView? _commandHistoryListView;
    private TreeView? _erpMenuTreeView;

    public ErpTransactionContentProvider(MainWindowViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    public UIElement CreateContent()
    {
        // Create UI elements
        _transactionCodeTextBox = new TextBox
        {
            PlaceholderText = "/n[TCODE]...",
            FontFamily = new FontFamily("Consolas"),
            FontSize = 14,
            Width = 300
        };
        _transactionCodeTextBox.KeyDown += TransactionCodeTextBox_KeyDown;

        _statusMessageTextBlock = new TextBlock { Text = "MOBAerp System Ready" };
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

        _erpMenuTreeView = new TreeView { SelectionMode = TreeViewSelectionMode.Single };
        _erpMenuTreeView.ItemInvoked += ErpMenuTreeView_ItemInvoked;

        var content = BuildLayout();
        InitializeTreeView();
        return content;
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
            Background = new SolidColorBrush(ErpBlue)
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
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Colors.White)
        };
        Grid.SetColumn(label, 0);
        grid.Children.Add(label);

        Grid.SetColumn(_transactionCodeTextBox!, 1);
        grid.Children.Add(_transactionCodeTextBox!);

        var executeButton = new Button
        {
            Margin = new Thickness(8, 0, 0, 0),
            Background = new SolidColorBrush(ErpGold),
            Foreground = new SolidColorBrush(ErpDarkBlue)
        };
        var executeContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        executeContent.Children.Add(new FontIcon { Glyph = "\uE768", FontSize = 14, Foreground = new SolidColorBrush(ErpDarkBlue) });
        executeContent.Children.Add(new TextBlock { Text = "Execute", Foreground = new SolidColorBrush(ErpDarkBlue), FontWeight = FontWeights.SemiBold });
        executeButton.Content = executeContent;
        executeButton.Click += ExecuteButton_Click;
        Grid.SetColumn(executeButton, 2);
        grid.Children.Add(executeButton);

        // Title
        var titlePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 8
        };
        titlePanel.Children.Add(new FontIcon { Glyph = "\uE7C0", FontSize = 20, Foreground = new SolidColorBrush(Colors.White) });
        titlePanel.Children.Add(new TextBlock
        {
            Text = "MOBAerp System",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Colors.White),
            VerticalAlignment = VerticalAlignment.Center
        });
        Grid.SetColumn(titlePanel, 3);
        grid.Children.Add(titlePanel);

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
        helpMenu.Items.Add(new MenuFlyoutItem { Text = "Transaction Codes" });
        helpMenu.Items.Add(new MenuFlyoutItem { Text = "Application Help" });
        menuBar.Items.Add(helpMenu);

        return menuBar;
    }

    private Grid BuildMainContent()
    {
        var grid = new Grid { Padding = new Thickness(16) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Left: ERP Menu TreeView
        var leftPanel = new Grid();
        leftPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        leftPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var menuTitle = new TextBlock
        {
            Text = "MOBAerp Easy Access",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(ErpBlue),
            Margin = new Thickness(0, 0, 0, 16)
        };
        Grid.SetRow(menuTitle, 0);
        leftPanel.Children.Add(menuTitle);

        Grid.SetRow(_erpMenuTreeView!, 1);
        leftPanel.Children.Add(_erpMenuTreeView!);

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
            Text = "MOBAerp Easy Access",
            FontSize = 24,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(ErpBlue)
        });
        titlePanel.Children.Add(new TextBlock
        {
            Text = "Model Railway ERP-Style Navigation Interface",
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

        // History Card
        var historyCard = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(24),
            Background = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0))
        };
        var historyPanel = new StackPanel { Spacing = 12 };
        historyPanel.Children.Add(new TextBlock
        {
            Text = "Command History",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold
        });
        historyPanel.Children.Add(_commandHistoryListView!);
        historyPanel.Children.Add(_noHistoryTextBlock!);
        historyCard.Child = historyPanel;
        contentPanel.Children.Add(historyCard);

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

        var codes = new[]
        {
            ("TC", "Train Control - Locomotive driving interface"),
            ("TC01", "Train Control with Speed Ramp"),
            ("JR", "Journeys - Configure train journeys"),
            ("JR01", "Journey Editor"),
            ("WF", "Workflows - Automation workflows"),
            ("WF01", "Workflow Editor"),
            ("TR", "Trains - Locomotive management"),
            ("TR01", "Train Details"),
            ("TP", "Track Plan - Layout editor"),
            ("TP01", "Track Plan Import"),
            ("JM", "Journey Map - Visual journey display"),
            ("JM01", "Map Settings"),
            ("SB", "Signal Box - Electronic interlocking"),
            ("SB01", "Track Diagram"),
            ("OV", "Overview - Dashboard"),
            ("OV01", "Quick Stats"),
            ("SOL", "Solution - Project management"),
            ("SOL01", "Solution Settings"),
            ("SET", "Settings - Application configuration"),
            ("SET01", "Connection Settings"),
            ("MON", "Monitor - System log viewer"),
            ("MON01", "Debug Log"),
            ("H1", "Help 1 - WebView2 Wiki"),
            ("H2", "Help 2 - TreeView Help"),
            ("H3", "Help 3 - TabView Help"),
        };

        int row = 0;
        for (int i = 0; i < codes.Length; i += 2)
        {
            codeGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // First column
            var code1 = new TextBlock
            {
                Text = codes[i].Item1,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(ErpBlue)
            };
            Grid.SetRow(code1, row);
            Grid.SetColumn(code1, 0);
            codeGrid.Children.Add(code1);

            var desc1 = new TextBlock { Text = codes[i].Item2, TextWrapping = TextWrapping.Wrap };
            Grid.SetRow(desc1, row);
            Grid.SetColumn(desc1, 1);
            codeGrid.Children.Add(desc1);

            // Second column (if exists)
            if (i + 1 < codes.Length)
            {
                var code2 = new TextBlock
                {
                    Text = codes[i + 1].Item1,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(ErpBlue)
                };
                Grid.SetRow(code2, row);
                Grid.SetColumn(code2, 2);
                codeGrid.Children.Add(code2);

                var desc2 = new TextBlock { Text = codes[i + 1].Item2, TextWrapping = TextWrapping.Wrap };
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

    private Grid BuildStatusBar()
    {
        var grid = new Grid
        {
            Padding = new Thickness(8, 4, 8, 4),
            Background = new SolidColorBrush(ErpLightGray)
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _statusMessageTextBlock!.Foreground = new SolidColorBrush(ErpDarkBlue);
        Grid.SetColumn(_statusMessageTextBlock, 0);
        grid.Children.Add(_statusMessageTextBlock);

        // Connection Status
        var connectionIndicator = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Margin = new Thickness(16, 0, 16, 0) };
        var indicator = new Border
        {
            Width = 10,
            Height = 10,
            CornerRadius = new CornerRadius(5),
            Background = new SolidColorBrush(_mainViewModel.IsZ21Connected ? ErpGreen : Colors.Red)
        };
        connectionIndicator.Children.Add(indicator);
        connectionIndicator.Children.Add(new TextBlock
        {
            Text = _mainViewModel.IsZ21Connected ? "Z21 Online" : "Z21 Offline",
            Foreground = new SolidColorBrush(ErpDarkBlue),
            FontSize = 12
        });
        Grid.SetColumn(connectionIndicator, 1);
        grid.Children.Add(connectionIndicator);

        var clientText = new TextBlock
        {
            Text = "Client: MOBA",
            Margin = new Thickness(16, 0, 16, 0),
            Foreground = new SolidColorBrush(ErpDarkBlue)
        };
        Grid.SetColumn(clientText, 2);
        grid.Children.Add(clientText);

        var userText = new TextBlock
        {
            Text = "MOBAUSER",
            Foreground = new SolidColorBrush(ErpDarkBlue)
        };
        Grid.SetColumn(userText, 3);
        grid.Children.Add(userText);

        return grid;
    }

    private void InitializeTreeView()
    {
        if (_erpMenuTreeView == null) return;

        var rootNode = new TreeViewNode { Content = "Favorites", IsExpanded = true };
        rootNode.Children.Add(new TreeViewNode { Content = "TC - Train Control" });
        rootNode.Children.Add(new TreeViewNode { Content = "JR - Journeys" });
        _erpMenuTreeView.RootNodes.Add(rootNode);

        var mobaMenuNode = new TreeViewNode { Content = "MOBAerp Menu", IsExpanded = true };

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

        _erpMenuTreeView.RootNodes.Add(mobaMenuNode);
    }

    private static TreeViewNode CreateMenuNode(string code, string title)
    {
        return new TreeViewNode { Content = $"{code} - {title}" };
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
        if (_transactionCodeTextBox == null) return;

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
            SetStatus($"Transaction {command} executed successfully");
            // Note: In plugin context, navigation is limited - show success message
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
        // Train Operations
        { "TC", "traincontrol" },
        { "TC01", "traincontrol" },
        { "TRAINCONTROL", "traincontrol" },
        { "TR", "trains" },
        { "TR01", "trains" },
        { "TRAINS", "trains" },

        // Configuration
        { "JR", "journeys" },
        { "JR01", "journeys" },
        { "JOURNEYS", "journeys" },
        { "WF", "workflows" },
        { "WF01", "workflows" },
        { "WORKFLOWS", "workflows" },

        // Layout Design
        { "TP", "trackplaneditor" },
        { "TP01", "trackplaneditor" },
        { "TRACKPLAN", "trackplaneditor" },
        { "JM", "journeymap" },
        { "JM01", "journeymap" },
        { "JOURNEYMAP", "journeymap" },
        { "SB", "signalbox" },
        { "SB01", "signalbox" },
        { "SIGNALBOX", "signalbox" },

        // System
        { "OV", "overview" },
        { "OV01", "overview" },
        { "OVERVIEW", "overview" },
        { "SOL", "solution" },
        { "SOL01", "solution" },
        { "SOLUTION", "solution" },
        { "SET", "settings" },
        { "SET01", "settings" },
        { "SETTINGS", "settings" },
        { "MON", "monitor" },
        { "MON01", "monitor" },
        { "MONITOR", "monitor" },

        // Help
        { "H1", "help1" },
        { "HELP1", "help1" },
        { "H2", "help2" },
        { "HELP2", "help2" },
        { "H3", "help3" },
        { "HELP3", "help3" },

        // ERP
        { "ERP", "erp" },
        { "MOBAERP", "erp" },
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

        if (_noHistoryTextBlock != null)
        {
            _noHistoryTextBlock.Visibility = _commandHistory.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void SetStatus(string message)
    {
        if (_statusMessageTextBlock != null)
        {
            _statusMessageTextBlock.Text = message;
        }
    }

    private void CommandHistoryListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        _ = sender;

        if (e.ClickedItem is CommandHistoryItem item && _transactionCodeTextBox != null)
        {
            _transactionCodeTextBox.Text = item.Command;
            _transactionCodeTextBox.Focus(FocusState.Programmatic);
        }
    }

    private void ErpMenuTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        _ = sender;

        if (args.InvokedItem is TreeViewNode node)
        {
            var content = node.Content?.ToString();
            if (content != null && content.Contains(" - "))
            {
                var code = content.Split(" - ")[0];
                if (_transactionCodeTextBox != null)
                {
                    _transactionCodeTextBox.Text = code;
                    ExecuteTransaction();
                }
            }
            else
            {
                SetStatus($"Selected: {content}");
            }
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        SetStatus("Back navigation not available in plugin context");
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        SetStatus("Use main navigation to exit");
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        if (_transactionCodeTextBox != null)
        {
            _transactionCodeTextBox.Text = string.Empty;
        }
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
        _transactionCodeTextBox?.Focus(FocusState.Programmatic);
        _transactionCodeTextBox?.SelectAll();
    }
}

/// <summary>
/// Represents a command history entry for MOBAerp.
/// </summary>
public sealed class CommandHistoryItem
{
    public string Timestamp { get; init; } = string.Empty;
    public string Command { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;

    public override string ToString() => $"[{Timestamp}] {Command} - {Status}";
}
