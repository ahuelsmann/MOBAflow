// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls;

using Common.Display;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using Moba.WinUI.View;

using Windows.UI;

/// <summary>
/// Ks signal screen: fixed grid, aspect changes only lamp colors and speed indicators.
/// </summary>
internal sealed partial class KsSignalScreen
{
    private static readonly SolidColorBrush RedOn = new(Color.FromArgb(255, 255, 0, 0));
    private static readonly SolidColorBrush GreenOn = new(Color.FromArgb(255, 0, 200, 0));
    private static readonly SolidColorBrush YellowOn = new(Color.FromArgb(255, 255, 200, 0));
    private static readonly SolidColorBrush WhiteOn = new(Colors.White);

    private SolidColorBrush OffColor => _offColor ??= CreateOffBrush();
    private SolidColorBrush? _offColor;

    private DispatcherTimer? _blinkTimer;
    private bool _blinkState;
    private bool _isApplyingVisualState;
    private Ellipse? _blinkingLed;
    private SolidColorBrush? _blinkingOnColor;

    public static readonly DependencyProperty AspectProperty = DependencyProperty.Register(
        nameof(Aspect),
        typeof(string),
        typeof(KsSignalScreen),
        new PropertyMetadata(KsSignalAspectNames.Hp0, OnSignalVisualPropertyChanged));

    public static readonly DependencyProperty SignalArticleNumberProperty = DependencyProperty.Register(
        nameof(SignalArticleNumber),
        typeof(string),
        typeof(KsSignalScreen),
        new PropertyMetadata(string.Empty, OnSignalVisualPropertyChanged));

    public static readonly DependencyProperty TopSpeedValueProperty = DependencyProperty.Register(
        nameof(TopSpeedValue),
        typeof(string),
        typeof(KsSignalScreen),
        new PropertyMetadata(string.Empty, OnSignalVisualPropertyChanged));

    public static readonly DependencyProperty BottomSpeedValueProperty = DependencyProperty.Register(
        nameof(BottomSpeedValue),
        typeof(string),
        typeof(KsSignalScreen),
        new PropertyMetadata(string.Empty, OnSignalVisualPropertyChanged));

    public static readonly DependencyProperty IsStaticPreviewProperty = DependencyProperty.Register(
        nameof(IsStaticPreview),
        typeof(bool),
        typeof(KsSignalScreen),
        new PropertyMetadata(false, OnSignalVisualPropertyChanged));

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

    public bool IsStaticPreview
    {
        get => (bool)GetValue(IsStaticPreviewProperty);
        set => SetValue(IsStaticPreviewProperty, value);
    }

    public KsSignalScreen()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ActualThemeChanged += OnActualThemeChanged;
        UpdateAspect();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ActualThemeChanged -= OnActualThemeChanged;
        StopBlinking();
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        _offColor = null;
        UpdateAspect();
    }

    private SolidColorBrush CreateOffBrush()
    {
        var color = ThemeResourceResolver.ResolveColor(this, "SignalLampOffBrush", Color.FromArgb(60, 64, 64, 64));
        return new SolidColorBrush(color);
    }

    private static void OnSignalVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KsSignalScreen screen && !screen._isApplyingVisualState)
        {
            screen.UpdateAspect();
        }
    }

    /// <summary>
    /// Applies all visual inputs atomically and always re-renders, even when individual values are unchanged.
    /// </summary>
    internal void ApplyVisualState(string signalArticleNumber, string topSpeedValue, string bottomSpeedValue, string aspect)
    {
        _isApplyingVisualState = true;
        try
        {
            SignalArticleNumber = signalArticleNumber;
            TopSpeedValue = topSpeedValue;
            BottomSpeedValue = bottomSpeedValue;
            Aspect = aspect;
        }
        finally
        {
            _isApplyingVisualState = false;
        }

        UpdateAspect();
    }

    private bool CanUpdateVisuals() =>
        W1 != null
        && Hp0 != null
        && Ks1 != null
        && Ks2 != null
        && TopSpeedBox != null
        && BottomSpeedBox != null
        && TopSpeedIndicator != null
        && BottomSpeedIndicator != null
        && TopSpeedRow != null
        && BottomSpeedRow != null;

    private void UpdateAspect()
    {
        if (!CanUpdateVisuals())
        {
            return;
        }

        StopBlinking();

        var state = KsSignalScreenVisualState.Create(
            SignalArticleNumber,
            Aspect,
            TopSpeedValue,
            BottomSpeedValue);

        ApplyLamp(W1, state.W1);
        ApplyLamp(Hp0, state.Hp0);
        ApplyLamp(Ks1, state.Ks1);
        ApplyLamp(Ks2, state.Ks2);
        ApplyLamp(W2, state.W2);
        ApplyLamp(Zs7Center, state.Zs7Center);
        ApplyLamp(Zs7Right, state.Zs7Right);
        ApplyLamp(W3, state.W3);
        ApplyLamp(Ra12Right, state.Ra12Right);

        ApplySpeedIndicators(state);

        StartBlinkingIfNeeded(state);
    }

    private void ApplySpeedIndicators(KsSignalScreenVisualState state)
    {
        TopSpeedRow.Height = new GridLength(state.ShowTopSpeed ? KsSignalScreenLayout.SpeedRowHeight : 0);
        BottomSpeedRow.Height = new GridLength(state.ShowBottomSpeed ? KsSignalScreenLayout.SpeedRowHeight : 0);

        TopSpeedBox.Visibility = state.ShowTopSpeed ? Visibility.Visible : Visibility.Collapsed;
        TopSpeedIndicator.Text = state.ShowTopSpeed ? state.TopSpeedText : string.Empty;

        BottomSpeedBox.Visibility = state.ShowBottomSpeed ? Visibility.Visible : Visibility.Collapsed;
        BottomSpeedIndicator.Text = state.ShowBottomSpeed ? state.BottomSpeedText : string.Empty;
    }

    private void ApplyLamp(Ellipse led, KsSignalLampColor color)
    {
        led.Fill = color switch
        {
            KsSignalLampColor.Red => RedOn,
            KsSignalLampColor.Green => GreenOn,
            KsSignalLampColor.Yellow => YellowOn,
            KsSignalLampColor.White => WhiteOn,
            _ => OffColor
        };
    }

    private void StartBlinkingIfNeeded(KsSignalScreenVisualState state)
    {
        if (state.BlinkLamp == KsSignalBlinkLamp.None)
        {
            return;
        }

        var (led, onColor) = state.BlinkLamp switch
        {
            KsSignalBlinkLamp.Ks1 => (Ks1, GreenOn),
            KsSignalBlinkLamp.W1 => (W1, WhiteOn),
            _ => (null, null)
        };

        if (led == null || onColor == null)
        {
            return;
        }

        if (IsStaticPreview)
        {
            led.Fill = onColor;
            return;
        }

        _blinkingLed = led;
        _blinkingOnColor = onColor;
        _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _blinkTimer.Tick += OnBlinkTick;
        _blinkTimer.Start();
    }

    private void OnBlinkTick(object? sender, object e)
    {
        if (_blinkingLed == null || _blinkingOnColor == null)
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
        _blinkingOnColor = null;
    }
}
