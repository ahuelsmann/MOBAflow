// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls.Docking;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Model;
using System.ComponentModel;

internal sealed class DockSplitControl : UserControl
{
    private DockSplitNode? _observedSplitNode;
    private readonly Grid _layoutGrid;
    private readonly DockNodePresenter _firstPresenter;
    private readonly Border _splitter;
    private readonly DockNodePresenter _secondPresenter;
    private readonly Grid _verticalSplitterLine;
    private readonly Grid _horizontalSplitterLine;

    public event EventHandler<DockPanelExpansionChangedEventArgs>? PanelExpansionChanged;

    public static readonly DependencyProperty SplitNodeProperty =
        DependencyProperty.Register(
            nameof(SplitNode),
            typeof(DockSplitNode),
            typeof(DockSplitControl),
            new PropertyMetadata(null, OnSplitNodeChanged));

    public DockSplitControl()
    {
        _layoutGrid = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        _firstPresenter = new DockNodePresenter();
        _firstPresenter.PanelExpansionChanged += OnChildPanelExpansionChanged;
        _verticalSplitterLine = new Grid
        {
            Width = 1,
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
            Visibility = Visibility.Collapsed
        };
        _horizontalSplitterLine = new Grid
        {
            Height = 1,
            VerticalAlignment = VerticalAlignment.Center,
            Background = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
            Visibility = Visibility.Collapsed
        };
        _splitter = new Border
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            Child = new Grid
            {
                Children =
                {
                    _verticalSplitterLine,
                    _horizontalSplitterLine
                }
            }
        };
        _secondPresenter = new DockNodePresenter();
        _secondPresenter.PanelExpansionChanged += OnChildPanelExpansionChanged;

        _layoutGrid.Children.Add(_firstPresenter);
        _layoutGrid.Children.Add(_splitter);
        _layoutGrid.Children.Add(_secondPresenter);
        Content = _layoutGrid;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public DockSplitNode? SplitNode
    {
        get => (DockSplitNode?)GetValue(SplitNodeProperty);
        set => SetValue(SplitNodeProperty, value);
    }

    private static void OnSplitNodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockSplitControl control)
        {
            control.ObserveSplitNode(e.OldValue as DockSplitNode, e.NewValue as DockSplitNode);
            control.UpdateLayoutGrid();
            control.UpdateNodeBindings();
        }
    }

    private void ObserveSplitNode(DockSplitNode? previousNode, DockSplitNode? nextNode)
    {
        if (previousNode is not null)
        {
            previousNode.PropertyChanged -= OnSplitNodePropertyChanged;
        }

        _observedSplitNode = nextNode;
        if (nextNode is not null)
        {
            nextNode.PropertyChanged += OnSplitNodePropertyChanged;
        }
    }

    private void OnSplitNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (SplitNode is null)
        {
            return;
        }

        if (e.PropertyName is nameof(DockSplitNode.Orientation) or nameof(DockSplitNode.SplitRatio))
        {
            UpdateLayoutGrid();
        }

        if (e.PropertyName is nameof(DockSplitNode.FirstNode) or nameof(DockSplitNode.SecondNode))
        {
            UpdateNodeBindings();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateLayoutGrid();
        UpdateNodeBindings();
    }

    private void OnChildPanelExpansionChanged(object? sender, DockPanelExpansionChangedEventArgs e)
    {
        PanelExpansionChanged?.Invoke(this, e);
    }

    private void UpdateNodeBindings()
    {
        if (SplitNode is null)
        {
            _firstPresenter.Node = null;
            _secondPresenter.Node = null;
            return;
        }

        _firstPresenter.Node = SplitNode.FirstNode;
        _secondPresenter.Node = SplitNode.SecondNode;
    }

    private void UpdateLayoutGrid()
    {
        if (SplitNode is null)
        {
            return;
        }

        _layoutGrid.RowDefinitions.Clear();
        _layoutGrid.ColumnDefinitions.Clear();
        var splitRatio = Math.Clamp(SplitNode.SplitRatio, 0.1, 0.9);

        if (SplitNode.Orientation == Orientation.Horizontal)
        {
            // First (Left) | Splitter | Second (Right)
            _layoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(splitRatio, GridUnitType.Star) });
            _layoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _layoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1 - splitRatio, GridUnitType.Star) });

            Grid.SetColumn(_firstPresenter, 0);
            Grid.SetColumn(_splitter, 1);
            Grid.SetColumn(_secondPresenter, 2);

            Grid.SetRow(_firstPresenter, 0);
            Grid.SetRow(_splitter, 0);
            Grid.SetRow(_secondPresenter, 0);

            _splitter.Width = 5;
            _splitter.Height = double.NaN;
            _splitter.HorizontalAlignment = HorizontalAlignment.Center;
            _splitter.VerticalAlignment = VerticalAlignment.Stretch;

            _verticalSplitterLine.Visibility = Visibility.Visible;
            _horizontalSplitterLine.Visibility = Visibility.Collapsed;
        }
        else
        {
            // First (Top)
            // Splitter
            // Second (Bottom)
            _layoutGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(splitRatio, GridUnitType.Star) });
            _layoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _layoutGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1 - splitRatio, GridUnitType.Star) });

            Grid.SetRow(_firstPresenter, 0);
            Grid.SetRow(_splitter, 1);
            Grid.SetRow(_secondPresenter, 2);

            Grid.SetColumn(_firstPresenter, 0);
            Grid.SetColumn(_splitter, 0);
            Grid.SetColumn(_secondPresenter, 0);

            _splitter.Height = 5;
            _splitter.Width = double.NaN;
            _splitter.VerticalAlignment = VerticalAlignment.Center;
            _splitter.HorizontalAlignment = HorizontalAlignment.Stretch;

            _horizontalSplitterLine.Visibility = Visibility.Visible;
            _verticalSplitterLine.Visibility = Visibility.Collapsed;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ObserveSplitNode(_observedSplitNode, null);
    }
}
