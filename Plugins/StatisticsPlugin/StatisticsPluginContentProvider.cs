// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Moba.SharedUI.ViewModel;

using System.ComponentModel;

using Windows.UI;

namespace Moba.Plugin.Statistics;

/// <summary>
/// Statistics Plugin Dashboard - Fluent Design System Implementation.
/// Shows comprehensive project statistics, connection status, and settings overview.
/// 
/// CRITICAL WinUI 3 Plugin Architecture:
/// - Plugins must NOT return types that inherit from Page
/// - WinUI cannot resolve Page types from dynamically loaded assemblies
/// - Instead, return a UIElement via CreateContent()
/// </summary>
public sealed class StatisticsPluginContentProvider
{
    private readonly StatisticsPluginViewModel _vm;
    private readonly Dictionary<string, TextBlock> _bindings = new();

    // Fluent Design Colors
    private static readonly Color AccentBlue = Color.FromArgb(255, 0, 120, 212);
    private static readonly Color SuccessGreen = Color.FromArgb(255, 16, 124, 16);
    private static readonly Color WarningOrange = Color.FromArgb(255, 255, 140, 0);
    private static readonly Color CardBackground = Color.FromArgb(20, 255, 255, 255);
    private static readonly Color CardBorder = Color.FromArgb(40, 255, 255, 255);

    public StatisticsPluginContentProvider(StatisticsPluginViewModel viewModel, MainWindowViewModel mainWindowViewModel)
    {
        _ = mainWindowViewModel;
        _vm = viewModel;
    }

    public UIElement CreateContent()
    {
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var root = new StackPanel
        {
            Padding = new Thickness(24, 20, 24, 24),
            Spacing = 20
        };

        // Header
        root.Children.Add(CreateHeader());

        // Connection Status Section
        root.Children.Add(CreateSectionHeader("Connection Status", "\uE8CE"));
        root.Children.Add(CreateConnectionStatusGrid());

        // Project Statistics Section
        root.Children.Add(CreateSectionHeader("Project Statistics", "\uE8A5"));
        root.Children.Add(CreateProjectStatsGrid());

        // Entity Counts Section
        root.Children.Add(CreateSectionHeader("Entity Overview", "\uE71D"));
        root.Children.Add(CreateEntityCountsGrid());

        // Settings Overview Section
        root.Children.Add(CreateSectionHeader("Settings Overview", "\uE713"));
        root.Children.Add(CreateSettingsGrid());

        // Actions Section
        root.Children.Add(CreateActionsSection());

        scroll.Content = root;

        // Subscribe to changes
        _vm.PropertyChanged += OnPropertyChanged;
        RefreshAllBindings();

        return scroll;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not null && _bindings.TryGetValue(e.PropertyName, out var tb))
        {
            tb.Text = GetValue(e.PropertyName);
        }
    }

    private string GetValue(string prop)
    {
        var p = typeof(StatisticsPluginViewModel).GetProperty(prop);
        return p?.GetValue(_vm)?.ToString() ?? "-";
    }

    private void RefreshAllBindings()
    {
        foreach (var kvp in _bindings)
            kvp.Value.Text = GetValue(kvp.Key);
    }

    #region Header

    private UIElement CreateHeader()
    {
        var stack = new StackPanel { Spacing = 4 };

        var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        headerRow.Children.Add(new FontIcon
        {
            Glyph = "\uE9D2",
            FontSize = 32,
            Foreground = new SolidColorBrush(AccentBlue)
        });
        headerRow.Children.Add(new TextBlock
        {
            Text = "MOBAflow Statistics",
            FontSize = 32,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        stack.Children.Add(headerRow);

        stack.Children.Add(new TextBlock
        {
            Text = "Real-time project statistics, connection status, and configuration overview",
            FontSize = 14,
            Opacity = 0.6
        });

        return stack;
    }

    private UIElement CreateSectionHeader(string title, string glyph)
    {
        var stack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Margin = new Thickness(0, 8, 0, 0)
        };

        stack.Children.Add(new FontIcon
        {
            Glyph = glyph,
            FontSize = 18,
            Foreground = new SolidColorBrush(AccentBlue)
        });

        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold
        });

        return stack;
    }

    #endregion

    #region Connection Status

    private UIElement CreateConnectionStatusGrid()
    {
        var grid = new Grid { ColumnSpacing = 12, RowSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Z21 Connection Card
        var z21Card = CreateConnectionCard(
            "Z21 Digital Command Station",
            "IsZ21Connected",
            [
                ("IP Address", "Z21IpAddress"),
                ("Port", "Z21Port"),
                ("Track Power", "TrackPowerText"),
                ("Current", "Z21MainCurrent"),
                ("Temperature", "Z21Temperature"),
                ("Voltage", "Z21SupplyVoltage")
            ],
            true);
        Grid.SetColumn(z21Card, 0);
        grid.Children.Add(z21Card);

        // REST API Card
        var restCard = CreateConnectionCard(
            "REST API Service",
            "IsRestApiRunning",
            [
                ("Status", "RestApiStatusText"),
                ("Port", "RestApiPort"),
                ("Local IP", "LocalIpAddress"),
                ("URL", "RestApiUrl")
            ],
            false);
        Grid.SetColumn(restCard, 1);
        grid.Children.Add(restCard);

        return grid;
    }

    private Border CreateConnectionCard(string title, string statusProp, (string Label, string Prop)[] items, bool isZ21)
    {
        _ = statusProp;

        var border = new Border
        {
            Background = new SolidColorBrush(CardBackground),
            BorderBrush = new SolidColorBrush(CardBorder),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16)
        };

        var stack = new StackPanel { Spacing = 12 };

        // Header with status indicator
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        header.Children.Add(new TextBlock
        {
            Text = title,
            FontWeight = FontWeights.SemiBold,
            FontSize = 16
        });

        var statusIndicator = CreateBoundTextBlock(isZ21 ? "Z21StatusIcon" : "RestApiStatusIcon");
        statusIndicator.FontSize = 20;
        Grid.SetColumn(statusIndicator, 1);
        header.Children.Add(statusIndicator);

        stack.Children.Add(header);

        // Separator
        stack.Children.Add(new Border
        {
            Height = 1,
            Background = new SolidColorBrush(CardBorder),
            Margin = new Thickness(0, 4, 0, 4)
        });

        // Items
        foreach (var (label, prop) in items)
        {
            stack.Children.Add(CreateLabelValueRow(label, prop));
        }

        border.Child = stack;
        return border;
    }

    #endregion

    #region Project Statistics

    private UIElement CreateProjectStatsGrid()
    {
        var border = new Border
        {
            Background = new SolidColorBrush(CardBackground),
            BorderBrush = new SolidColorBrush(CardBorder),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16)
        };

        var grid = new Grid { ColumnSpacing = 24, RowSpacing = 8 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var items = new[]
        {
            ("Solution", "SolutionName", 0, 0),
            ("Projects", "ProjectCount", 0, 1),
            ("Selected Project", "SelectedProjectName", 0, 2)
        };

        foreach (var (label, prop, row, col) in items)
        {
            var item = CreateLabelValueRow(label, prop);
            Grid.SetRow(item, row);
            Grid.SetColumn(item, col);
            grid.Children.Add(item);
        }

        border.Child = grid;
        return border;
    }

    #endregion

    #region Entity Counts

    private UIElement CreateEntityCountsGrid()
    {
        var grid = new Grid { ColumnSpacing = 12, RowSpacing = 12 };

        // 3 columns
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // 2 rows
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var cards = new[]
        {
            ("\uE7C1", "Journeys", "TotalJourneys", AccentBlue, 0, 0),
            ("\uE945", "Workflows", "TotalWorkflows", WarningOrange, 0, 1),
            ("\uE7C0", "Trains", "TotalTrains", SuccessGreen, 0, 2),
            ("\uEC49", "Locomotives", "TotalLocomotives", Color.FromArgb(255, 0, 99, 177), 1, 0),
            ("\uE806", "Passenger Wagons", "TotalPassengerWagons", Color.FromArgb(255, 136, 23, 152), 1, 1),
            ("\uE7B8", "Goods Wagons", "TotalGoodsWagons", Color.FromArgb(255, 107, 107, 107), 1, 2)
        };

        foreach (var (icon, label, prop, color, row, col) in cards)
        {
            var card = CreateStatCard(icon, label, prop, color);
            Grid.SetRow(card, row);
            Grid.SetColumn(card, col);
            grid.Children.Add(card);
        }

        return grid;
    }

    private Border CreateStatCard(string iconGlyph, string label, string prop, Color accent)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(25, accent.R, accent.G, accent.B)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(50, accent.R, accent.G, accent.B)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            MinHeight = 90
        };

        var stack = new StackPanel { Spacing = 8 };

        var headerStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        headerStack.Children.Add(new FontIcon
        {
            Glyph = iconGlyph,
            FontSize = 18,
            Foreground = new SolidColorBrush(accent)
        });
        headerStack.Children.Add(new TextBlock
        {
            Text = label,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.8
        });

        var valueBlock = CreateBoundTextBlock(prop);
        valueBlock.FontSize = 28;
        valueBlock.FontWeight = FontWeights.Bold;

        stack.Children.Add(headerStack);
        stack.Children.Add(valueBlock);

        border.Child = stack;
        return border;
    }

    #endregion

    #region Settings Overview

    private UIElement CreateSettingsGrid()
    {
        var border = new Border
        {
            Background = new SolidColorBrush(CardBackground),
            BorderBrush = new SolidColorBrush(CardBorder),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16)
        };

        var grid = new Grid { ColumnSpacing = 24, RowSpacing = 8 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        for (int i = 0; i < 3; i++)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var items = new[]
        {
            ("Auto-Load Solution", "AutoLoadLastSolution", 0, 0),
            ("Auto-Start REST API", "AutoStartWebApp", 0, 1),
            ("Health Check", "HealthCheckEnabled", 0, 2),
            ("Speech Engine", "SpeechEngine", 1, 0),
            ("Speech Rate", "SpeechRate", 1, 1),
            ("Health Check Interval", "HealthCheckInterval", 1, 2),
            ("Speech Volume", "SpeechVolume", 2, 0)
        };

        foreach (var (label, prop, row, col) in items)
        {
            var item = CreateLabelValueRow(label, prop);
            Grid.SetRow(item, row);
            Grid.SetColumn(item, col);
            grid.Children.Add(item);
        }

        border.Child = grid;
        return border;
    }

    #endregion

    #region Actions

    private UIElement CreateActionsSection()
    {
        var border = new Border
        {
            Background = new SolidColorBrush(CardBackground),
            BorderBrush = new SolidColorBrush(CardBorder),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16)
        };

        var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };

        var refreshBtn = new Button
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    new FontIcon { Glyph = "\uE72C", FontSize = 14 },
                    new TextBlock { Text = "Refresh All", VerticalAlignment = VerticalAlignment.Center }
                }
            }
        };
        refreshBtn.Click += (s, e) =>
        {
            _vm.RefreshAllCommand.Execute(null);
            RefreshAllBindings();
        };

        var infoText = new TextBlock
        {
            Text = "Data refreshes automatically when changes occur",
            Opacity = 0.6,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0)
        };

        stack.Children.Add(refreshBtn);
        stack.Children.Add(infoText);

        border.Child = stack;
        return border;
    }

    #endregion

    #region Helpers

    private StackPanel CreateLabelValueRow(string label, string prop)
    {
        var stack = new StackPanel { Spacing = 2 };

        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 12,
            Opacity = 0.6
        });

        var value = CreateBoundTextBlock(prop);
        value.FontWeight = FontWeights.SemiBold;
        stack.Children.Add(value);

        return stack;
    }

    private TextBlock CreateBoundTextBlock(string prop)
    {
        var tb = new TextBlock { Text = "-" };
        _bindings[prop] = tb;
        return tb;
    }

    #endregion
}
