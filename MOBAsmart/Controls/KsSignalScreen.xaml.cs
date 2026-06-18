// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Controls;

using Microsoft.Maui.Controls.Shapes;

/// <summary>
/// Ks signal screen display for German railway signal aspects.
/// </summary>
public partial class KsSignalScreen
{
    private const double DesignWidth = 90;
    private const double BaseDesignHeight = 102;
    private const double SpeedRowDesignHeight = 43;

    private static readonly Color OffColor = Color.FromRgba(64, 64, 64, 60);
    private static readonly Color RedOn = Color.FromRgba(255, 0, 0, 255);
    private static readonly Color GreenOn = Color.FromRgba(0, 200, 0, 255);
    private static readonly Color YellowOn = Color.FromRgba(255, 200, 0, 255);
    private static readonly Color WhiteOn = Colors.White;

    private IDispatcherTimer? _blinkTimer;
    private bool _blinkState;
    private Ellipse? _blinkingLed;
    private Color _blinkingOnColor = Colors.Transparent;

    public static readonly BindableProperty AspectProperty = BindableProperty.Create(
        nameof(Aspect),
        typeof(string),
        typeof(KsSignalScreen),
        "Hp0",
        propertyChanged: OnSignalVisualPropertyChanged);

    public static readonly BindableProperty SignalArticleNumberProperty = BindableProperty.Create(
        nameof(SignalArticleNumber),
        typeof(string),
        typeof(KsSignalScreen),
        string.Empty,
        propertyChanged: OnSignalVisualPropertyChanged);

    public static readonly BindableProperty TopSpeedValueProperty = BindableProperty.Create(
        nameof(TopSpeedValue),
        typeof(string),
        typeof(KsSignalScreen),
        string.Empty,
        propertyChanged: OnSignalVisualPropertyChanged);

    public static readonly BindableProperty BottomSpeedValueProperty = BindableProperty.Create(
        nameof(BottomSpeedValue),
        typeof(string),
        typeof(KsSignalScreen),
        string.Empty,
        propertyChanged: OnSignalVisualPropertyChanged);

    public static readonly BindableProperty IsPreviewModeProperty = BindableProperty.Create(
        nameof(IsPreviewMode),
        typeof(bool),
        typeof(KsSignalScreen),
        false,
        propertyChanged: OnSignalVisualPropertyChanged);

    public string Aspect
    {
        get => (string)GetValue(AspectProperty);
        set => SetValue(AspectProperty, value);
    }

    public string SignalArticleNumber
    {
        get => (string)GetValue(SignalArticleNumberProperty);
        set => SetValue(SignalArticleNumberProperty, value);
    }

    public string TopSpeedValue
    {
        get => (string)GetValue(TopSpeedValueProperty);
        set => SetValue(TopSpeedValueProperty, value);
    }

    public string BottomSpeedValue
    {
        get => (string)GetValue(BottomSpeedValueProperty);
        set => SetValue(BottomSpeedValueProperty, value);
    }

    public bool IsPreviewMode
    {
        get => (bool)GetValue(IsPreviewModeProperty);
        set => SetValue(IsPreviewModeProperty, value);
    }

    public KsSignalScreen()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        UpdateScaleToFit();
        UpdateAspect();
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        UpdateScaleToFit();
    }

    private void UpdateScaleToFit()
    {
        if (SignalGrid == null)
        {
            return;
        }

        var availableWidth = Width;
        var availableHeight = Height;
        if (availableWidth <= 0 || availableHeight <= 0)
        {
            return;
        }

        var designHeight = GetDesignHeight();
        var scale = Math.Min(availableWidth / DesignWidth, availableHeight / designHeight);
        SignalGrid.Scale = scale;
    }

    private double GetDesignHeight()
    {
        var height = BaseDesignHeight;
        if (TopSpeedIndicator.IsVisible)
        {
            height += SpeedRowDesignHeight;
        }

        if (BottomSpeedIndicator.IsVisible)
        {
            height += SpeedRowDesignHeight;
        }

        return height;
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        StopBlinking();
    }

    private static void OnSignalVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is KsSignalScreen screen)
        {
            screen.UpdateAspect();
        }
    }

    private void UpdateAspect()
    {
        StopBlinking();

        W1.Fill = OffColor;
        Hp0.Fill = OffColor;
        Ks1.Fill = OffColor;
        Ks2.Fill = OffColor;
        W2.Fill = OffColor;
        Zs7Center.Fill = OffColor;
        Zs7Right.Fill = OffColor;
        W3.Fill = OffColor;
        Ra12Right.Fill = OffColor;
        TopSpeedIndicator.Text = string.Empty;
        TopSpeedIndicator.IsVisible = false;
        BottomSpeedIndicator.Text = string.Empty;
        BottomSpeedIndicator.IsVisible = false;

        if (string.Equals(SignalArticleNumber, "4046", StringComparison.Ordinal) && Render4046Aspect())
        {
            UpdateScaleToFit();
            return;
        }

        switch (Aspect)
        {
            case "Hp0":
                Hp0.Fill = RedOn;
                break;
            case "Ks1":
                Ks1.Fill = GreenOn;
                break;
            case "Ks2":
                Ks2.Fill = YellowOn;
                break;
            case "Ks1Blink":
                Ks1.Fill = GreenOn;
                StartBlinking(Ks1, GreenOn);
                break;
            case "Kennlicht":
                W1.Fill = WhiteOn;
                break;
            case "Dunkel":
                break;
            case "Ra12":
                W3.Fill = WhiteOn;
                Ra12Right.Fill = WhiteOn;
                break;
            case "Zs1":
                W1.Fill = WhiteOn;
                StartBlinking(W1, WhiteOn);
                break;
            case "Zs7":
                W2.Fill = YellowOn;
                Zs7Center.Fill = YellowOn;
                Zs7Right.Fill = YellowOn;
                break;
        }

        UpdateScaleToFit();
    }

    private bool Render4046Aspect()
    {
        switch (Aspect)
        {
            case "Hp0":
                Hp0.Fill = RedOn;
                return true;
            case "Ks1":
                Ks1.Fill = GreenOn;
                return true;
            case "Ra12":
                Hp0.Fill = RedOn;
                Zs7Center.Fill = WhiteOn;
                return true;
            case "Zs1":
                Ks1.Fill = GreenOn;
                ShowTopSpeedIndicator(FormatSpeedIndicatorValue(TopSpeedValue));
                return true;
            case "Ks2":
                Ks2.Fill = YellowOn;
                W1.Fill = WhiteOn;
                return true;
            case "Ks1Blink":
                Ks2.Fill = YellowOn;
                W1.Fill = WhiteOn;
                ShowTopSpeedIndicator(FormatSpeedIndicatorValue(TopSpeedValue));
                return true;
            case "Kennlicht":
                W1.Fill = WhiteOn;
                return true;
            case "Dunkel":
                W1.Fill = WhiteOn;
                Ks1.Fill = GreenOn;
                StartBlinking(Ks1, GreenOn);
                ShowTopSpeedIndicator(FormatSpeedIndicatorValue(TopSpeedValue));
                ShowBottomSpeedIndicator(FormatSpeedIndicatorValue(BottomSpeedValue));
                return true;
            case "Zs7":
                W2.Fill = YellowOn;
                Zs7Center.Fill = YellowOn;
                Zs7Right.Fill = YellowOn;
                return true;
            default:
                return false;
        }
    }

    private void ShowTopSpeedIndicator(string text)
    {
        TopSpeedIndicator.Text = text;
        TopSpeedIndicator.IsVisible = true;
    }

    private void ShowBottomSpeedIndicator(string text)
    {
        BottomSpeedIndicator.Text = text;
        BottomSpeedIndicator.IsVisible = true;
    }

    private static string FormatSpeedIndicatorValue(string? speedCode)
    {
        return string.IsNullOrWhiteSpace(speedCode) ? "--" : speedCode;
    }

    private void StartBlinking(Ellipse led, Color onColor)
    {
        if (IsPreviewMode)
        {
            led.Fill = onColor;
            return;
        }

        _blinkingLed = led;
        _blinkingOnColor = onColor;
        _blinkTimer = Dispatcher.CreateTimer();
        _blinkTimer.Interval = TimeSpan.FromMilliseconds(500);
        _blinkTimer.Tick += OnBlinkTick;
        _blinkTimer.Start();
    }

    private void OnBlinkTick(object? sender, EventArgs e)
    {
        if (_blinkingLed == null)
        {
            return;
        }

        _blinkState = !_blinkState;
        _blinkingLed.Fill = _blinkState ? _blinkingOnColor : OffColor;
    }

    private void StopBlinking()
    {
        if (_blinkTimer != null)
        {
            _blinkTimer.Tick -= OnBlinkTick;
            _blinkTimer.Stop();
            _blinkTimer = null;
        }

        _blinkState = false;
        _blinkingLed = null;
    }
}
