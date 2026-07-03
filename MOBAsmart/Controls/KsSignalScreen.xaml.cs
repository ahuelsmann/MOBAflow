// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Controls;

using Common.Display;

using Microsoft.Maui.Controls.Shapes;

/// <summary>
/// Ks signal screen: fixed grid, aspect changes only lamp colors and speed indicators.
/// </summary>
public partial class KsSignalScreen
{
    private const double DesignWidth = KsSignalScreenLayout.DesignWidth;

    private static readonly Color RedOn = Color.FromRgba(255, 0, 0, 255);
    private static readonly Color GreenOn = Color.FromRgba(0, 200, 0, 255);
    private static readonly Color YellowOn = Color.FromRgba(255, 200, 0, 255);
    private static readonly Color WhiteOn = Colors.White;

    private Color OffColor => ResolveThemeColor("SignalLampOff");

    private IDispatcherTimer? _blinkTimer;
    private bool _blinkState;
    private bool _isApplyingVisualState;
    private bool _updateScheduled;
    private Ellipse? _blinkingLed;
    private Color _blinkingOnColor = Colors.Transparent;

    public static readonly BindableProperty AspectProperty = BindableProperty.Create(
        nameof(Aspect),
        typeof(string),
        typeof(KsSignalScreen),
        KsSignalAspectNames.Hp0,
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

    public static readonly BindableProperty IsStaticPreviewProperty = BindableProperty.Create(
        nameof(IsStaticPreview),
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
        SizeChanged += OnSizeChanged;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeChanged += OnRequestedThemeChanged;
        }

        ScheduleUpdateAspect();
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        ScheduleUpdateAspect();
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        UpdateScaleToFit();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeChanged -= OnRequestedThemeChanged;
        }

        StopBlinking();
    }

    private static Color ResolveThemeColor(string key)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var resource) == true && resource is Color color)
        {
            return color;
        }

        return Application.Current?.RequestedTheme == AppTheme.Light
            ? Color.FromRgba(64, 64, 64, 96)
            : Color.FromRgba(64, 64, 64, 60);
    }

    private static void OnSignalVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is KsSignalScreen screen && !screen._isApplyingVisualState)
        {
            screen.ScheduleUpdateAspect();
        }
    }

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

        ScheduleUpdateAspect();
    }

    private void ScheduleUpdateAspect()
    {
        if (_updateScheduled)
        {
            return;
        }

        _updateScheduled = true;
        Dispatcher?.Dispatch(() =>
        {
            _updateScheduled = false;
            UpdateAspect();
        });
    }

    private bool CanUpdateVisuals() =>
        W1 != null
        && Hp0 != null
        && Ks1 != null
        && Ks2 != null
        && W2 != null
        && Zs7Center != null
        && Zs7Right != null
        && W3 != null
        && Ra12Right != null
        && TopSpeedBox != null
        && BottomSpeedBox != null
        && TopSpeedIndicator != null
        && BottomSpeedIndicator != null
        && SignalGrid != null;

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
        UpdateScaleToFit();
    }

    private void ApplySpeedIndicators(KsSignalScreenVisualState state)
    {
        SignalGrid.RowDefinitions[0].Height = state.ShowTopSpeed ? KsSignalScreenLayout.SpeedRowHeight : 0;
        SignalGrid.RowDefinitions[6].Height = state.ShowBottomSpeed ? KsSignalScreenLayout.SpeedRowHeight : 0;

        TopSpeedBox.IsVisible = state.ShowTopSpeed;
        TopSpeedIndicator.Text = state.ShowTopSpeed ? state.TopSpeedText : string.Empty;

        BottomSpeedBox.IsVisible = state.ShowBottomSpeed;
        BottomSpeedIndicator.Text = state.ShowBottomSpeed ? state.BottomSpeedText : string.Empty;
    }

    private void UpdateScaleToFit()
    {
        if (SignalGrid == null)
        {
            return;
        }

        // Picker previews use natural grid size so 4046 speed digits stay readable on phones.
        if (IsStaticPreview)
        {
            SignalGrid.Scale = 1;
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
        if (TopSpeedBox == null || BottomSpeedBox == null)
        {
            return KsSignalScreenLayout.GetDesignHeight(false, false);
        }

        return KsSignalScreenLayout.GetDesignHeight(TopSpeedBox.IsVisible, BottomSpeedBox.IsVisible);
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

        if (Dispatcher == null)
        {
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
