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

        base.OnBindingContextChanged();

        ApplyVisualState();

    }



    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)

    {

        _appliedTheme = null;

        _appliedIsOn = null;

        _appliedBacklightColorHex = null;

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

        if (_appliedLabel != Label)

        {

            FunctionKeyLabel.Text = Label;

            var accessibility = string.IsNullOrWhiteSpace(Description) ? Label : $"{Label}, {Description}";

            SemanticProperties.SetDescription(this, $"Toggle function {accessibility}");

            _appliedLabel = Label;

        }



        if (_appliedDescription != Description)

        {

            DescriptionLabel.Text = Description;

            DescriptionLabel.IsVisible = !string.IsNullOrWhiteSpace(Description);

            _appliedDescription = Description;

        }



        var theme = GetAppearanceTheme();

        if (_appliedIsOn != IsOn || _appliedBacklightColorHex != BacklightColorHex || _appliedTheme != theme)

        {

            var appearance = FunctionBacklightColor.Resolve(IsOn, BacklightColorHex, theme);

            ToggleBorder.BackgroundColor = ToColor(appearance.BackgroundArgb);

            FunctionKeyLabel.TextColor = ToColor(appearance.PrimaryTextArgb);

            DescriptionLabel.TextColor = ToColor(appearance.SecondaryTextArgb);



            if (Application.Current?.Resources.TryGetValue(IsOn ? "RailwayAccent" : "BorderColor", out var strokeResource) == true

                && strokeResource is Color strokeColor)

            {

                ToggleBorder.Stroke = strokeColor;

                ToggleBorder.StrokeThickness = IsOn ? 2 : 1;

            }



            _appliedIsOn = IsOn;

            _appliedBacklightColorHex = BacklightColorHex;

            _appliedTheme = theme;

        }



        var iconSource = IconConverter.Convert(IconAsset, typeof(ImageSource), null, System.Globalization.CultureInfo.CurrentCulture) as string;

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


