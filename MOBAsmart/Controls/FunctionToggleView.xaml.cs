// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Controls;

using Moba.Common.Display;
using Moba.MAUI.Converters;
using Moba.SharedUI.ViewModel;
using System.ComponentModel;
using System.Windows.Input;

/// <summary>
/// Lightweight function key tile for the Control page. Updates visuals in code to avoid per-cell MultiBinding cost.
/// </summary>
public partial class FunctionToggleView
{
    private static readonly FunctionIconSourceConverter IconConverter = new();
    private FunctionButtonViewModel? _observedFunction;
    private string? _appliedLabel;
    private string? _appliedDescription;
    private string? _appliedIconSource;
    private bool? _appliedIsOn;
    private string? _appliedBacklightColorHex;
    private FunctionBacklightColor.AppearanceTheme? _appliedTheme;

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

    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(
        nameof(Description),
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

    public FunctionToggleView()
    {
        InitializeComponent();
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeChanged += OnRequestedThemeChanged;
        }
    }

    protected override void OnBindingContextChanged()
    {
        DetachObservedFunction();
        base.OnBindingContextChanged();

        if (BindingContext is FunctionButtonViewModel function)
        {
            AttachObservedFunction(function);
        }

        InvalidateAppliedVisualState();
        ApplyVisualState();
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        InvalidateAppliedVisualState();
        ApplyVisualState();
    }

    private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is FunctionToggleView view)
        {
            view.ApplyVisualState();
        }
    }

    private void AttachObservedFunction(FunctionButtonViewModel function)
    {
        _observedFunction = function;
        function.PropertyChanged += OnObservedFunctionPropertyChanged;
    }

    private void DetachObservedFunction()
    {
        if (_observedFunction == null)
        {
            return;
        }

        _observedFunction.PropertyChanged -= OnObservedFunctionPropertyChanged;
        _observedFunction = null;
    }

    private void OnObservedFunctionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FunctionButtonViewModel.IsOn)
            or nameof(FunctionButtonViewModel.BacklightColorHex)
            or nameof(FunctionButtonViewModel.IconAsset)
            or nameof(FunctionButtonViewModel.Label)
            or nameof(FunctionButtonViewModel.Description))
        {
            ApplyVisualState();
        }
    }

    private void InvalidateAppliedVisualState()
    {
        _appliedTheme = null;
        _appliedIsOn = null;
        _appliedBacklightColorHex = null;
        _appliedLabel = null;
        _appliedDescription = null;
        _appliedIconSource = null;
    }

    private int GetEffectiveFunctionIndex() => _observedFunction?.Index ?? FunctionIndex;

    private bool GetEffectiveIsOn() => _observedFunction?.IsOn ?? IsOn;

    private string GetEffectiveBacklightColorHex() => _observedFunction?.BacklightColorHex ?? BacklightColorHex;

    private string GetEffectiveIconAsset() => _observedFunction?.IconAsset ?? IconAsset;

    private string GetEffectiveLabel() => _observedFunction?.Label ?? Label;

    private string GetEffectiveDescription() => _observedFunction?.Description ?? Description;

    private void ApplyVisualState()
    {
        var label = GetEffectiveLabel();
        if (_appliedLabel != label)
        {
            FunctionKeyLabel.Text = label;
            var description = GetEffectiveDescription();
            var accessibility = string.IsNullOrWhiteSpace(description) ? label : $"{label}, {description}";
            SemanticProperties.SetDescription(this, $"Toggle function {accessibility}");
            _appliedLabel = label;
        }

        var effectiveDescription = GetEffectiveDescription();
        if (_appliedDescription != effectiveDescription)
        {
            DescriptionLabel.Text = effectiveDescription;
            DescriptionLabel.IsVisible = !string.IsNullOrWhiteSpace(effectiveDescription);
            _appliedDescription = effectiveDescription;
        }

        var isOn = GetEffectiveIsOn();
        var backlightColorHex = GetEffectiveBacklightColorHex();
        var theme = GetAppearanceTheme();
        if (_appliedIsOn != isOn || _appliedBacklightColorHex != backlightColorHex || _appliedTheme != theme)
        {
            var appearance = FunctionBacklightColor.Resolve(isOn, backlightColorHex, theme);
            ToggleBorder.BackgroundColor = ToColor(appearance.BackgroundArgb);
            FunctionKeyLabel.TextColor = ToColor(appearance.PrimaryTextArgb);
            DescriptionLabel.TextColor = ToColor(appearance.SecondaryTextArgb);
            if (Application.Current?.Resources.TryGetValue(isOn ? "RailwayAccent" : "BorderColor", out var strokeResource) == true
                && strokeResource is Color strokeColor)
            {
                ToggleBorder.Stroke = strokeColor;
                ToggleBorder.StrokeThickness = isOn ? 2 : 1;
            }

            _appliedIsOn = isOn;
            _appliedBacklightColorHex = backlightColorHex;
            _appliedTheme = theme;
        }

        var effectiveFunctionIndex = GetEffectiveFunctionIndex();
        if (FunctionIndex != effectiveFunctionIndex)
        {
            FunctionIndex = effectiveFunctionIndex;
        }

        var iconSource = IconConverter.Convert(
            GetEffectiveIconAsset(),
            typeof(ImageSource),
            null,
            System.Globalization.CultureInfo.CurrentCulture) as string;
        if (string.IsNullOrWhiteSpace(iconSource))
        {
            if (_appliedIconSource != null)
            {
                IconImage.IsVisible = false;
                IconImage.Source = null;
                _appliedIconSource = null;
            }

            return;
        }

        if (string.Equals(_appliedIconSource, iconSource, StringComparison.Ordinal))
        {
            return;
        }

        IconImage.Source = iconSource;
        IconImage.IsVisible = true;
        _appliedIconSource = iconSource;
    }

    private static FunctionBacklightColor.AppearanceTheme GetAppearanceTheme()
    {
        return Application.Current?.RequestedTheme == AppTheme.Light
            ? FunctionBacklightColor.AppearanceTheme.Light
            : FunctionBacklightColor.AppearanceTheme.Dark;
    }

    private static Color ToColor(uint argb)
    {
        return Color.FromRgba(
            ((argb >> 16) & 0xFF) / 255f,
            ((argb >> 8) & 0xFF) / 255f,
            (argb & 0xFF) / 255f,
            ((argb >> 24) & 0xFF) / 255f);
    }
}
