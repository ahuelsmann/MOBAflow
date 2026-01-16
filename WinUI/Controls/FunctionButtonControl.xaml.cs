// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls;

using System;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

/// <summary>
/// ESU-Style function button for DCC locomotive functions (F0-F28).
/// Features an icon, function number badge, and toggle state with visual feedback.
/// </summary>
public sealed partial class FunctionButtonControl : UserControl
{
    public static readonly DependencyProperty FunctionNumberProperty =
        DependencyProperty.Register(nameof(FunctionNumber), typeof(int), typeof(FunctionButtonControl),
            new PropertyMetadata(0, OnFunctionNumberChanged));

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(FunctionButtonControl),
            new PropertyMetadata(false, OnIsActiveChanged));

    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(FunctionButtonControl),
            new PropertyMetadata("\uE71E", OnGlyphChanged));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(FunctionButtonControl),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Occurs when the function button is clicked (toggled).
    /// </summary>
    public event EventHandler<FunctionButtonClickedEventArgs>? FunctionClicked;

    public FunctionButtonControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public int FunctionNumber
    {
        get => (int)GetValue(FunctionNumberProperty);
        set => SetValue(FunctionNumberProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateFunctionNumber();
        UpdateGlyph();
        UpdateActiveState();
    }

    private static void OnFunctionNumberChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FunctionButtonControl control && control.IsLoaded)
        {
            control.UpdateFunctionNumber();
        }
    }

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FunctionButtonControl control && control.IsLoaded)
        {
            control.UpdateActiveState();
        }
    }

    private static void OnGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FunctionButtonControl control && control.IsLoaded)
        {
            control.UpdateGlyph();
        }
    }

    private void UpdateFunctionNumber()
    {
        FunctionNumberText.Text = FunctionNumber.ToString(CultureInfo.InvariantCulture);
    }

    private void UpdateGlyph()
    {
        FunctionIcon.Glyph = Glyph;
    }

    private void UpdateActiveState()
    {
        ActiveOverlay.Visibility = IsActive ? Visibility.Visible : Visibility.Collapsed;

        // Change icon color when active
        FunctionIcon.Foreground = IsActive
            ? (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"]
            : (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
    }

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        IsActive = !IsActive;
        FunctionClicked?.Invoke(this, new FunctionButtonClickedEventArgs(FunctionNumber, IsActive));
    }
}

/// <summary>
/// Event arguments for function button click events.
/// </summary>
public class FunctionButtonClickedEventArgs : EventArgs
{
    public int FunctionNumber { get; }
    public bool IsActive { get; }

    public FunctionButtonClickedEventArgs(int functionNumber, bool isActive)
    {
        FunctionNumber = functionNumber;
        IsActive = isActive;
    }
}
