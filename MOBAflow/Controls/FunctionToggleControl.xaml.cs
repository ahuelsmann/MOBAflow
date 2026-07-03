// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls;

using Converter;

using Microsoft.UI.Xaml;

using Microsoft.UI.Xaml.Controls;

using Microsoft.UI.Xaml.Input;

using Microsoft.UI.Xaml.Media;

using System.Windows.Input;

/// <summary>
/// WinUI function key tile for Train Control. Forwards tap and right-tap to ViewModel commands.
/// </summary>

public sealed partial class FunctionToggleControl : UserControl

{

    public static readonly DependencyProperty FunctionIndexProperty =

        DependencyProperty.Register(nameof(FunctionIndex), typeof(int), typeof(FunctionToggleControl), new PropertyMetadata(0));

    public static readonly DependencyProperty IsOnProperty =

        DependencyProperty.Register(nameof(IsOn), typeof(bool), typeof(FunctionToggleControl), new PropertyMetadata(false, OnAppearancePropertyChanged));

    public static readonly DependencyProperty BacklightColorHexProperty =

        DependencyProperty.Register(nameof(BacklightColorHex), typeof(string), typeof(FunctionToggleControl), new PropertyMetadata(string.Empty, OnAppearancePropertyChanged));

    public static readonly DependencyProperty IconAssetProperty =

        DependencyProperty.Register(nameof(IconAsset), typeof(string), typeof(FunctionToggleControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty LabelProperty =

        DependencyProperty.Register(nameof(Label), typeof(string), typeof(FunctionToggleControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty =

        DependencyProperty.Register(nameof(Description), typeof(string), typeof(FunctionToggleControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ToggleCommandProperty =

        DependencyProperty.Register(nameof(ToggleCommand), typeof(ICommand), typeof(FunctionToggleControl), new PropertyMetadata(null));

    public static readonly DependencyProperty EditAppearanceCommandProperty =

        DependencyProperty.Register(nameof(EditAppearanceCommand), typeof(ICommand), typeof(FunctionToggleControl), new PropertyMetadata(null));

    public int FunctionIndex

    {

        get => (int)GetValue(FunctionIndexProperty);

        set => SetValue(FunctionIndexProperty, value);

    }

    public bool IsOn

    {

        get => (bool)GetValue(IsOnProperty);

        set => SetValue(IsOnProperty, value);

    }

    public string BacklightColorHex

    {

        get => (string)GetValue(BacklightColorHexProperty);

        set => SetValue(BacklightColorHexProperty, value);

    }

    public string IconAsset

    {

        get => (string)GetValue(IconAssetProperty);

        set => SetValue(IconAssetProperty, value);

    }

    public string Label

    {

        get => (string)GetValue(LabelProperty);

        set => SetValue(LabelProperty, value);

    }

    public string Description

    {

        get => (string)GetValue(DescriptionProperty);

        set => SetValue(DescriptionProperty, value);

    }

    public ICommand? ToggleCommand

    {

        get => (ICommand?)GetValue(ToggleCommandProperty);

        set => SetValue(ToggleCommandProperty, value);

    }

    public ICommand? EditAppearanceCommand

    {

        get => (ICommand?)GetValue(EditAppearanceCommandProperty);

        set => SetValue(EditAppearanceCommandProperty, value);

    }

    public FunctionToggleControl()

    {

        InitializeComponent();

        Loaded += OnLoaded;

        Unloaded += OnUnloaded;

        ApplyAppearance();

    }

    private void OnLoaded(object sender, RoutedEventArgs e)

    {

        ActualThemeChanged += OnActualThemeChanged;

    }

    private void OnUnloaded(object sender, RoutedEventArgs e)

    {

        ActualThemeChanged -= OnActualThemeChanged;

    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)

    {

        ApplyAppearance();

    }

    private static void OnAppearancePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)

    {

        if (d is FunctionToggleControl control)

        {

            control.ApplyAppearance();

        }
    }

    private void ApplyAppearance()

    {

        FunctionToggleButton.Background = BoolToBacklightBrushConverter.CreateBrush(IsOn, BacklightColorHex, this);

        FunctionLabelText.Foreground = new SolidColorBrush(
            BoolToBacklightBrushConverter.CreatePrimaryTextColor(IsOn, BacklightColorHex, this));

        DescriptionText.Foreground = new SolidColorBrush(
            BoolToBacklightBrushConverter.CreateSecondaryTextColor(IsOn, BacklightColorHex, this));

    }

    private void FunctionToggleButton_RightTapped(object sender, RightTappedRoutedEventArgs e)

    {

        e.Handled = true;

        if (EditAppearanceCommand?.CanExecute(FunctionIndex) != false)

        {

            EditAppearanceCommand?.Execute(FunctionIndex);

        }
    }
}

