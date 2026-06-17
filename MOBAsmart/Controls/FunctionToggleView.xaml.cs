// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Controls;

using Moba.Common.Display;
using Moba.MAUI.Converters;

using System.Windows.Input;

/// <summary>
/// Lightweight function key tile for the Control page. Updates visuals in code to avoid per-cell MultiBinding cost.
/// </summary>
public partial class FunctionToggleView
{
    private static readonly FunctionIconSourceConverter IconConverter = new();

    public static readonly BindableProperty FunctionIndexProperty = BindableProperty.Create(
        nameof(FunctionIndex),
        typeof(int),
        typeof(FunctionToggleView),
        0);

    public static readonly BindableProperty IsOnProperty = BindableProperty.Create(
        nameof(IsOn),
        typeof(bool),
        typeof(FunctionToggleView),
        false,
        propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty BacklightColorHexProperty = BindableProperty.Create(
        nameof(BacklightColorHex),
        typeof(string),
        typeof(FunctionToggleView),
        string.Empty,
        propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty IconAssetProperty = BindableProperty.Create(
        nameof(IconAsset),
        typeof(string),
        typeof(FunctionToggleView),
        string.Empty,
        propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty LabelProperty = BindableProperty.Create(
        nameof(Label),
        typeof(string),
        typeof(FunctionToggleView),
        string.Empty,
        propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty ToggleCommandProperty = BindableProperty.Create(
        nameof(ToggleCommand),
        typeof(ICommand),
        typeof(FunctionToggleView));

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

    public ICommand? ToggleCommand
    {
        get => (ICommand?)GetValue(ToggleCommandProperty);
        set => SetValue(ToggleCommandProperty, value);
    }

    public FunctionToggleView()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        ApplyVisualState();
    }

    private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is FunctionToggleView view)
        {
            view.ApplyVisualState();
        }
    }

    private void ApplyVisualState()
    {
        FunctionLabel.Text = Label;
        SemanticProperties.SetDescription(this, $"Toggle function {Label}");

        var argb = FunctionBacklightColor.ToArgb(IsOn, BacklightColorHex);
        ToggleBorder.BackgroundColor = Color.FromRgba(
            ((argb >> 16) & 0xFF) / 255f,
            ((argb >> 8) & 0xFF) / 255f,
            (argb & 0xFF) / 255f,
            ((argb >> 24) & 0xFF) / 255f);

        if (Application.Current?.Resources.TryGetValue(IsOn ? "RailwayAccent" : "BorderColor", out var strokeResource) == true
            && strokeResource is Color strokeColor)
        {
            ToggleBorder.Stroke = strokeColor;
        }

        var iconSource = IconConverter.Convert(IconAsset, typeof(ImageSource), null, System.Globalization.CultureInfo.CurrentCulture) as string;
        if (string.IsNullOrWhiteSpace(iconSource))
        {
            IconImage.IsVisible = false;
            IconImage.Source = null;
            return;
        }

        IconImage.Source = iconSource;
        IconImage.IsVisible = true;
    }
}
