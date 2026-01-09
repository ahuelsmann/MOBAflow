// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using Moba.SharedUI.ViewModel;
using Moba.WinUI.Service;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Windows.System;
using Windows.UI;

/// <summary>
/// MOBAerp - ERP-style transaction code navigation interface.
/// Provides alphanumeric transaction codes for quick access to MOBAflow pages.
/// Inspired by enterprise systems like SAP.
/// </summary>
public sealed class ErpPage : Page
{
    private static readonly Color ErpBlue = Color.FromArgb(255, 0, 102, 179);
    private static readonly Color ErpGold = Color.FromArgb(255, 255, 193, 7);
    private static readonly Color ErpDarkBlue = Color.FromArgb(255, 0, 51, 102);
    private static readonly Color ErpGreen = Color.FromArgb(255, 40, 167, 69);

    private readonly MainWindowViewModel _viewModel;
    private readonly NavigationService _navigationService;
    private readonly ObservableCollection<TransactionHistoryItem> _history = [];
    private readonly Dictionary<string, TransactionDefinition> _transactions = [];

    private TextBox _commandInput = null!;
    private TextBlock _statusText = null!;
    private ListView _historyListView = null!;

    public ErpPage(MainWindowViewModel viewModel, NavigationService navigationService)
    {
        _viewModel = viewModel;
        _navigationService = navigationService;

        InitializeTransactions();
        Content = BuildLayout();
        Loaded += (s, e) => _commandInput?.Focus(FocusState.Programmatic);
    }

    private void InitializeTransactions()
    {
        // Navigation Transactions
        _transactions["OV"] = new("OV", "Overview", "overview", "Dashboard and overview page");
        _transactions["TC"] = new("TC", "Train Control", "traincontrol", "Control locomotives");
        _transactions["JR"] = new("JR", "Journeys", "journeys", "Journey management");
        _transactions["WF"] = new("WF", "Workflows", "workflows", "Workflow automation");
        _transactions["TR"] = new("TR", "Trains", "trains", "Train/locomotive list");
        _transactions["TP"] = new("TP", "Track Plan", "trackplaneditor", "Track plan editor");
        _transactions["JM"] = new("JM", "Journey Map", "journeymap", "Journey visualization");
        _transactions["SB"] = new("SB", "Signal Box", "signalbox", "MOBAixl Interlocking");
        _transactions["IXL"] = new("IXL", "Interlocking", "signalbox", "MOBAixl Interlocking");
        _transactions["MON"] = new("MON", "Monitor", "monitor", "System monitor");
        _transactions["SET"] = new("SET", "Settings", "settings", "Application settings");
        _transactions["HELP"] = new("HELP", "Help", "help", "Help and documentation");
        _transactions["SOL"] = new("SOL", "Solution", "solution", "Solution management");

        // Aliases
        _transactions["?"] = new("?", "Help", "help", "Help and documentation");
        _transactions["H"] = new("H", "Help", "help", "Help and documentation");
    }

    private Grid BuildLayout()
    {
        var grid = new Grid { Background = new SolidColorBrush(ErpDarkBlue) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Header
        var header = BuildHeader();
        Grid.SetRow(header, 0);
        grid.Children.Add(header);

        // Command Input
        var commandPanel = BuildCommandPanel();
        Grid.SetRow(commandPanel, 1);
        grid.Children.Add(commandPanel);

        // Main Content (Transaction Grid + History)
        var mainContent = BuildMainContent();
        Grid.SetRow(mainContent, 2);
        grid.Children.Add(mainContent);

        // Status Bar
        var statusBar = BuildStatusBar();
        Grid.SetRow(statusBar, 3);
        grid.Children.Add(statusBar);

        return grid;
    }

    private Border BuildHeader()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16, Padding = new Thickness(24, 16, 24, 16) };

        // Logo
        var logo = new FontIcon
        {
            Glyph = "\uE756",
            FontSize = 32,
            Foreground = new SolidColorBrush(ErpGold)
        };
        panel.Children.Add(logo);

        // Title
        var titlePanel = new StackPanel();
        titlePanel.Children.Add(new TextBlock
        {
            Text = "MOBAerp",
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Colors.White)
        });
        titlePanel.Children.Add(new TextBlock
        {
            Text = "Transaction Navigation System",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255))
        });
        panel.Children.Add(titlePanel);

        return new Border
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 0),
                GradientStops = {
                    new GradientStop { Color = ErpDarkBlue, Offset = 0 },
                    new GradientStop { Color = ErpBlue, Offset = 1 }
                }
            },
            Child = panel
        };
    }

    private Border BuildCommandPanel()
    {
        var panel = new Grid { Padding = new Thickness(24, 16, 24, 16) };
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Label
        var label = new TextBlock
        {
            Text = "Transaction:",
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Colors.White),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0)
        };
        Grid.SetColumn(label, 0);
        panel.Children.Add(label);

        // Input
        _commandInput = new TextBox
        {
            PlaceholderText = "Enter transaction code (e.g., TC, JR, SB)",
            FontSize = 16,
            FontFamily = new FontFamily("Consolas"),
            CharacterCasing = CharacterCasing.Upper,
            MaxLength = 10
        };
        _commandInput.KeyDown += OnCommandInputKeyDown;
        Grid.SetColumn(_commandInput, 1);
        panel.Children.Add(_commandInput);

        // Execute Button
        var executeBtn = new Button
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children = {
                    new FontIcon { Glyph = "\uE768", FontSize = 16 },
                    new TextBlock { Text = "Execute", VerticalAlignment = VerticalAlignment.Center }
                }
            },
            Background = new SolidColorBrush(ErpGreen),
            Foreground = new SolidColorBrush(Colors.White),
            Padding = new Thickness(16, 8, 16, 8),
            Margin = new Thickness(12, 0, 0, 0)
        };
        executeBtn.Click += (s, e) => ExecuteTransaction(_commandInput.Text);
        Grid.SetColumn(executeBtn, 2);
        panel.Children.Add(executeBtn);

        return new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 0, 40, 80)),
            Child = panel
        };
    }

    private Grid BuildMainContent()
    {
        var grid = new Grid { Padding = new Thickness(24) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Transaction Grid
        var transactionPanel = BuildTransactionGrid();
        Grid.SetColumn(transactionPanel, 0);
        grid.Children.Add(transactionPanel);

        // History Panel
        var historyPanel = BuildHistoryPanel();
        Grid.SetColumn(historyPanel, 1);
        grid.Children.Add(historyPanel);

        return grid;
    }

    private Border BuildTransactionGrid()
    {
        var scroll = new ScrollViewer { Padding = new Thickness(0, 0, 16, 0) };
        var panel = new StackPanel { Spacing = 16 };

        // Categories
        panel.Children.Add(CreateTransactionCategory("Navigation", [
            ("OV", "Overview", "\uE80F"),
            ("TC", "Train Control", "\uE7C0"),
            ("JR", "Journeys", "\uE7C1"),
            ("TR", "Trains", "\uE7C0")
        ]));

        panel.Children.Add(CreateTransactionCategory("Operations", [
            ("SB", "Signal Box (IXL)", "\uE806"),
            ("WF", "Workflows", "\uE945"),
            ("MON", "Monitor", "\uE7F4")
        ]));

        panel.Children.Add(CreateTransactionCategory("Planning", [
            ("TP", "Track Plan", "\uE809"),
            ("JM", "Journey Map", "\uE81D"),
            ("SOL", "Solution", "\uE8B7")
        ]));

        panel.Children.Add(CreateTransactionCategory("System", [
            ("SET", "Settings", "\uE713"),
            ("HELP", "Help / Wiki", "\uE897")
        ]));

        scroll.Content = panel;

        return new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Child = scroll
        };
    }

    private StackPanel CreateTransactionCategory(string title, (string code, string name, string glyph)[] items)
    {
        var panel = new StackPanel { Spacing = 8 };

        // Header
        panel.Children.Add(new TextBlock
        {
            Text = title.ToUpperInvariant(),
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(ErpGold),
            CharacterSpacing = 100,
            Margin = new Thickness(0, 0, 0, 4)
        });

        // Items Grid
        var itemsGrid = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
        itemsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        itemsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        int row = 0;
        for (int i = 0; i < items.Length; i++)
        {
            if (i % 2 == 0)
                itemsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var (code, name, glyph) = items[i];
            var button = CreateTransactionButton(code, name, glyph);
            Grid.SetRow(button, row);
            Grid.SetColumn(button, i % 2);
            itemsGrid.Children.Add(button);

            if (i % 2 == 1) row++;
        }

        panel.Children.Add(itemsGrid);
        return panel;
    }

    private Border CreateTransactionButton(string code, string name, string glyph)
    {
        var button = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 0, 60, 120)),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 10, 12, 10)
        };

        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Icon
        var icon = new FontIcon
        {
            Glyph = glyph,
            FontSize = 20,
            Foreground = new SolidColorBrush(ErpGold),
            Margin = new Thickness(0, 0, 12, 0)
        };
        Grid.SetColumn(icon, 0);
        content.Children.Add(icon);

        // Text
        var textPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        textPanel.Children.Add(new TextBlock
        {
            Text = code,
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            FontFamily = new FontFamily("Consolas"),
            Foreground = new SolidColorBrush(Colors.White)
        });
        textPanel.Children.Add(new TextBlock
        {
            Text = name,
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255))
        });
        Grid.SetColumn(textPanel, 1);
        content.Children.Add(textPanel);

        button.Child = content;

        // Hover
        button.PointerEntered += (s, e) => button.Background = new SolidColorBrush(Color.FromArgb(255, 0, 90, 160));
        button.PointerExited += (s, e) => button.Background = new SolidColorBrush(Color.FromArgb(255, 0, 60, 120));

        // Click
        button.PointerPressed += (s, e) => ExecuteTransaction(code);

        return button;
    }

    private Border BuildHistoryPanel()
    {
        var panel = new StackPanel { Spacing = 8, Margin = new Thickness(16, 0, 0, 0) };

        panel.Children.Add(new TextBlock
        {
            Text = "RECENT TRANSACTIONS",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
            CharacterSpacing = 100
        });

        _historyListView = new ListView
        {
            ItemsSource = _history,
            SelectionMode = ListViewSelectionMode.None,
            Background = new SolidColorBrush(Colors.Transparent)
        };
        _historyListView.ItemTemplate = CreateHistoryItemTemplate();
        panel.Children.Add(_historyListView);

        return new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Child = panel
        };
    }

    private static DataTemplate CreateHistoryItemTemplate()
    {
        // Simple text display for history items
        return null!; // WinUI will use default ToString()
    }

    private Border BuildStatusBar()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Padding = new Thickness(24, 8, 24, 8), Spacing = 16 };

        // Status LED
        var led = new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = new SolidColorBrush(ErpGreen)
        };
        panel.Children.Add(led);

        _statusText = new TextBlock
        {
            Text = "Ready - Enter transaction code or click a button",
            FontSize = 12,
            Foreground = new SolidColorBrush(Colors.White)
        };
        panel.Children.Add(_statusText);

        return new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 0, 30, 60)),
            Child = panel
        };
    }

    private void OnCommandInputKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            ExecuteTransaction(_commandInput.Text);
            e.Handled = true;
        }
    }

    private async void ExecuteTransaction(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            _statusText.Text = "Please enter a transaction code";
            return;
        }

        code = code.Trim().ToUpperInvariant();

        if (!_transactions.TryGetValue(code, out var transaction))
        {
            _statusText.Text = $"Unknown transaction: {code}";
            return;
        }

        // Add to history
        _history.Insert(0, new TransactionHistoryItem(code, transaction.Name, DateTime.Now));
        while (_history.Count > 10) _history.RemoveAt(_history.Count - 1);

        // Navigate
        _statusText.Text = $"Executing: {transaction.Name}...";
        _commandInput.Text = string.Empty;

        try
        {
            await _navigationService.NavigateToPageAsync(transaction.NavigationTag);
            _statusText.Text = $"Navigated to {transaction.Name}";
        }
        catch (Exception ex)
        {
            _statusText.Text = $"Error: {ex.Message}";
        }
    }

    private sealed record TransactionDefinition(string Code, string Name, string NavigationTag, string Description);
    private sealed record TransactionHistoryItem(string Code, string Name, DateTime Timestamp)
    {
        public override string ToString() => $"[{Timestamp:HH:mm:ss}] {Code} - {Name}";
    }
}
