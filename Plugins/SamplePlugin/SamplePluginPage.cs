// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using Moba.SharedUI.ViewModel;

using Windows.UI;

namespace Moba.Plugin;

/// <summary>
/// Sample Plugin Page with professional dashboard-style UI.
/// Demonstrates C#-based (code-only) plugin UI development for WinUI plugins.
/// </summary>
public sealed class SamplePluginPage : Page
{
    public SamplePluginViewModel ViewModel { get; }

    public SamplePluginPage(SamplePluginViewModel viewModel, MainWindowViewModel mainWindowViewModel)
    {
        _ = mainWindowViewModel;
        ViewModel = viewModel;
        DataContext = ViewModel;
        BuildUI();
    }

    private void BuildUI()
    {
        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(24, 16, 24, 16)
        };

        var rootStack = new StackPanel { Spacing = 20 };

        // Compact Header
        rootStack.Children.Add(CreateCompactHeader());

        // Connection Status Section (Z21 + REST API)
        rootStack.Children.Add(CreateConnectionSection());

        // Main Dashboard Grid (2x2 layout)
        rootStack.Children.Add(CreateDashboardGrid());

        scrollViewer.Content = rootStack;
        Content = scrollViewer;
    }

    #region Header

    private UIElement CreateCompactHeader()
    {
        var stack = new StackPanel { Spacing = 2 };

        var titleBlock = new TextBlock
        {
            Text = "📊 Project Dashboard",
            Style = Application.Current.Resources["TitleTextBlockStyle"] as Style,
            FontWeight = FontWeights.SemiBold
        };

        var descBlock = new TextBlock
        {
            Text = "Real-time statistics and configuration overview",
            Style = Application.Current.Resources["CaptionTextBlockStyle"] as Style,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush
        };

        stack.Children.Add(titleBlock);
        stack.Children.Add(descBlock);

        return stack;
    }

    #endregion

    #region Connection Section

    private UIElement CreateConnectionSection()
    {
        var grid = new Grid { ColumnSpacing = 16 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Z21 Card
        var z21Card = CreateConnectionCard("🚂 Z21 Command Station", new[]
        {
            ("IP Address", "Z21IpAddress"),
            ("Port", "Z21Port"),
            ("Status", "Z21Status"),
            ("Track Power", "TrackPowerStatus")
        });
        Grid.SetColumn(z21Card, 0);
        grid.Children.Add(z21Card);

        // REST API Card
        var restCard = CreateConnectionCard("🌐 REST API Server", new[]
        {
            ("Port", "RestApiPort"),
            ("URL", "RestApiUrl"),
            ("Status", "RestApiStatus"),
            ("Auto-Start", "IsRestApiEnabled")
        });
        Grid.SetColumn(restCard, 1);
        grid.Children.Add(restCard);

        return grid;
    }

    private Border CreateConnectionCard(string title, (string Label, string Binding)[] items)
    {
        var border = new Border
        {
            Background = Application.Current.Resources["CardBackgroundFillColorDefaultBrush"] as Brush,
            BorderBrush = Application.Current.Resources["CardStrokeColorDefaultBrush"] as Brush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12)
        };

        var stack = new StackPanel { Spacing = 10 };

        // Title
        var titleBlock = new TextBlock
        {
            Text = title,
            Style = Application.Current.Resources["BodyStrongTextBlockStyle"] as Style,
            Margin = new Thickness(0, 0, 0, 4)
        };
        stack.Children.Add(titleBlock);

        // Items Grid (2x2)
        var itemsGrid = new Grid { ColumnSpacing = 16, RowSpacing = 8 };
        itemsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        itemsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        itemsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        itemsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (int i = 0; i < items.Length && i < 4; i++)
        {
            var itemStack = new StackPanel { Spacing = 2 };

            var labelBlock = new TextBlock
            {
                Text = items[i].Label,
                Style = Application.Current.Resources["CaptionTextBlockStyle"] as Style,
                Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush
            };

            var valueBlock = new TextBlock
            {
                Style = Application.Current.Resources["BodyTextBlockStyle"] as Style,
                FontWeight = FontWeights.SemiBold
            };

            // Special handling for boolean bindings
            if (items[i].Binding == "IsRestApiEnabled")
            {
                valueBlock.SetBinding(TextBlock.TextProperty, new Binding
                {
                    Path = new PropertyPath(items[i].Binding),
                    Mode = BindingMode.OneWay,
                    Converter = new BooleanToOnOffConverter()
                });
            }
            else
            {
                valueBlock.SetBinding(TextBlock.TextProperty, new Binding
                {
                    Path = new PropertyPath(items[i].Binding),
                    Mode = BindingMode.OneWay
                });
            }

            itemStack.Children.Add(labelBlock);
            itemStack.Children.Add(valueBlock);

            Grid.SetRow(itemStack, i / 2);
            Grid.SetColumn(itemStack, i % 2);
            itemsGrid.Children.Add(itemStack);
        }

        stack.Children.Add(itemsGrid);
        border.Child = stack;
        return border;
    }

    #endregion

    #region Dashboard Grid

    private UIElement CreateDashboardGrid()
    {
        var grid = new Grid { ColumnSpacing = 16, RowSpacing = 16 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Top Left: Project Stats
        var projectCard = CreateMetricCard("📦 Project Statistics", new[]
        {
            ("Projects", "ProjectCount"),
            ("Journeys", "TotalJourneys"),
            ("Stations", "TotalStations")
        });
        Grid.SetRow(projectCard, 0);
        Grid.SetColumn(projectCard, 0);
        grid.Children.Add(projectCard);

        // Top Right: Workflow Stats
        var workflowCard = CreateMetricCard("⚙️ Workflow Statistics", new[]
        {
            ("Workflows", "TotalWorkflows"),
            ("Actions", "TotalActions"),
            ("Voices", "TotalVoices")
        });
        Grid.SetRow(workflowCard, 0);
        Grid.SetColumn(workflowCard, 1);
        grid.Children.Add(workflowCard);

        // Bottom Left: Train Stats
        var trainCard = CreateMetricCard("🚃 Rolling Stock", new[]
        {
            ("Trains", "TotalTrains"),
            ("Locos", "TotalLocomotives"),
            ("Wagons", "TotalWagons")
        });
        Grid.SetRow(trainCard, 1);
        Grid.SetColumn(trainCard, 0);
        grid.Children.Add(trainCard);

        // Bottom Right: Settings
        var settingsCard = CreateSettingsCard();
        Grid.SetRow(settingsCard, 1);
        Grid.SetColumn(settingsCard, 1);
        grid.Children.Add(settingsCard);

        return grid;
    }

    private Border CreateMetricCard(string title, (string Label, string Binding)[] metrics)
    {
        var border = new Border
        {
            Background = Application.Current.Resources["CardBackgroundFillColorDefaultBrush"] as Brush,
            BorderBrush = Application.Current.Resources["CardStrokeColorDefaultBrush"] as Brush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12)
        };

        var stack = new StackPanel { Spacing = 12 };

        // Title
        var titleBlock = new TextBlock
        {
            Text = title,
            Style = Application.Current.Resources["BodyStrongTextBlockStyle"] as Style
        };
        stack.Children.Add(titleBlock);

        // Metrics in horizontal layout
        var metricsGrid = new Grid { ColumnSpacing = 12 };
        for (int i = 0; i < metrics.Length; i++)
            metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        for (int i = 0; i < metrics.Length; i++)
        {
            var metricStack = new StackPanel { Spacing = 2 };

            var valueBlock = new TextBlock
            {
                FontSize = 28,
                FontWeight = FontWeights.SemiBold,
                Foreground = Application.Current.Resources["AccentTextFillColorPrimaryBrush"] as Brush ?? new SolidColorBrush(Colors.DodgerBlue)
            };
            valueBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(metrics[i].Binding), Mode = BindingMode.OneWay });

            var labelBlock = new TextBlock
            {
                Text = metrics[i].Label,
                Style = Application.Current.Resources["CaptionTextBlockStyle"] as Style,
                Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush
            };

            metricStack.Children.Add(valueBlock);
            metricStack.Children.Add(labelBlock);

            Grid.SetColumn(metricStack, i);
            metricsGrid.Children.Add(metricStack);
        }

        stack.Children.Add(metricsGrid);
        border.Child = stack;
        return border;
    }

    private Border CreateSettingsCard()
    {
        var border = new Border
        {
            Background = Application.Current.Resources["CardBackgroundFillColorDefaultBrush"] as Brush,
            BorderBrush = Application.Current.Resources["CardStrokeColorDefaultBrush"] as Brush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12)
        };

        var stack = new StackPanel { Spacing = 8 };

        var titleBlock = new TextBlock
        {
            Text = "🔧 Application Settings",
            Style = Application.Current.Resources["BodyStrongTextBlockStyle"] as Style,
            Margin = new Thickness(0, 0, 0, 4)
        };
        stack.Children.Add(titleBlock);

        AddCompactSettingRow(stack, "Speech Engine", "SpeechEngine");
        AddCompactSettingRow(stack, "Auto-Load", "AutoLoadLastSolution", isBoolean: true);
        AddCompactSettingRow(stack, "Health Check", "HealthCheckEnabled", isBoolean: true);
        AddCompactSettingRow(stack, "Feedback Pts", "FeedbackPointCount");

        border.Child = stack;
        return border;
    }

    private void AddCompactSettingRow(StackPanel parent, string label, string bindingPath, bool isBoolean = false)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var labelBlock = new TextBlock
        {
            Text = label,
            Style = Application.Current.Resources["CaptionTextBlockStyle"] as Style,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush,
            VerticalAlignment = VerticalAlignment.Center
        };

        var valueBlock = new TextBlock
        {
            Style = Application.Current.Resources["CaptionTextBlockStyle"] as Style,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        if (isBoolean)
        {
            valueBlock.SetBinding(TextBlock.TextProperty, new Binding
            {
                Path = new PropertyPath(bindingPath),
                Mode = BindingMode.OneWay,
                Converter = new BooleanToOnOffConverter()
            });
        }
        else
        {
            valueBlock.SetBinding(TextBlock.TextProperty, new Binding
            {
                Path = new PropertyPath(bindingPath),
                Mode = BindingMode.OneWay
            });
        }

        Grid.SetColumn(labelBlock, 0);
        Grid.SetColumn(valueBlock, 1);
        grid.Children.Add(labelBlock);
        grid.Children.Add(valueBlock);
        parent.Children.Add(grid);
    }

    #endregion
}

/// <summary>
/// Converter for boolean to On/Off display
/// </summary>
internal class BooleanToOnOffConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        return value is bool boolValue && boolValue ? "✓ On" : "○ Off";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for boolean to Yes/No display
/// </summary>
internal class BooleanToYesNoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        return value is bool boolValue && boolValue ? "✅ Yes" : "❌ No";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
