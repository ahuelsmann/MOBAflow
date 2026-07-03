// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls;

using Common.Display;
using Common.Multiplex;

using Domain;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Moba.WinUI.View;

internal sealed partial class SignalAspectPicker
{
    public static readonly DependencyProperty SelectedAspectProperty = DependencyProperty.Register(
        nameof(SelectedAspect),
        typeof(SignalAspect),
        typeof(SignalAspectPicker),
        new PropertyMetadata(SignalAspect.Hp0, OnPickerPropertyChanged));

    public static readonly DependencyProperty MultiplexerArticleNumberProperty = DependencyProperty.Register(
        nameof(MultiplexerArticleNumber),
        typeof(string),
        typeof(SignalAspectPicker),
        new PropertyMetadata("5229", OnPickerPropertyChanged));

    public static readonly DependencyProperty SignalArticleNumberProperty = DependencyProperty.Register(
        nameof(SignalArticleNumber),
        typeof(string),
        typeof(SignalAspectPicker),
        new PropertyMetadata("4046", OnPickerPropertyChanged));

    public static readonly DependencyProperty TopSpeedValueProperty = DependencyProperty.Register(
        nameof(TopSpeedValue),
        typeof(string),
        typeof(SignalAspectPicker),
        new PropertyMetadata(string.Empty, OnPickerPropertyChanged));

    public static readonly DependencyProperty BottomSpeedValueProperty = DependencyProperty.Register(
        nameof(BottomSpeedValue),
        typeof(string),
        typeof(SignalAspectPicker),
        new PropertyMetadata(string.Empty, OnPickerPropertyChanged));

    public SignalAspect SelectedAspect
    {
        get => (SignalAspect)GetValue(SelectedAspectProperty);
        set => SetValue(SelectedAspectProperty, value);
    }

    public string MultiplexerArticleNumber
    {
        get => (string)GetValue(MultiplexerArticleNumberProperty);
        set => SetValue(MultiplexerArticleNumberProperty, value);
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

    public SignalAspectPicker()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ActualThemeChanged -= OnActualThemeChanged;
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        UpdateSelectionVisuals();
    }

    private static void OnPickerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SignalAspectPicker picker)
        {
            picker.UpdatePicker();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ActualThemeChanged += OnActualThemeChanged;
        UpdatePicker();
    }

    private void OnAspectClicked(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border { Tag: string aspectName } && Enum.TryParse<SignalAspect>(aspectName, out var aspect))
        {
            SelectedAspect = aspect;
        }
    }

    private void UpdatePicker()
    {
        if (!IsLoaded)
        {
            return;
        }

        UpdateAspectPresentation();
        UpdateSupportedAspectVisibility();
        UpdateSelectionVisuals();
    }

    private void UpdateSelectionVisuals()
    {
        var accentBrush = ThemeResourceResolver.ResolveBrush(this, "AccentFillColorDefaultBrush", Microsoft.UI.Colors.Blue);
        var normalBrush = ThemeResourceResolver.ResolveBrush(this, "SubtleFillColorSecondaryBrush", Microsoft.UI.Colors.Gray);

        foreach (var (button, aspect) in EnumerateAspectButtons())
        {
            if (button == null)
            {
                continue;
            }

            button.Background = SelectedAspect == aspect ? accentBrush : normalBrush;
        }
    }

    private void UpdateSupportedAspectVisibility()
    {
        if (string.IsNullOrWhiteSpace(MultiplexerArticleNumber))
        {
            SetAllAspectButtonsVisibility(Visibility.Visible);
            return;
        }

        try
        {
            var supportedAspects = MultiplexerHelper.GetSupportedAspects(MultiplexerArticleNumber, SignalArticleNumber);
            foreach (var (button, aspect) in EnumerateAspectButtons())
            {
                button.Visibility = supportedAspects.Contains(aspect) ? Visibility.Visible : Visibility.Collapsed;
            }

            if (supportedAspects.Count == 0)
            {
                SetAllAspectButtonsVisibility(Visibility.Visible);
            }
        }
        catch (ArgumentException)
        {
            SetAllAspectButtonsVisibility(Visibility.Visible);
        }
    }

    private void UpdateAspectPresentation()
    {
        var is4046 = string.Equals(SignalArticleNumber, "4046", StringComparison.Ordinal);
        var signalArticleNumber = is4046 ? "4046" : string.Empty;

        foreach (var (screen, aspect) in EnumerateAspectSignals())
        {
            screen.ApplyVisualState(
                signalArticleNumber,
                TopSpeedValue,
                BottomSpeedValue,
                KsSignalAspectNames.ToAspectName(aspect));
        }

        AspectHp0Label.Text = KsSignalAspectNames.GetAspectLabel(SignalAspect.Hp0, is4046);
        AspectKs1Label.Text = KsSignalAspectNames.GetAspectLabel(SignalAspect.Ks1, is4046);
        AspectKs2Label.Text = KsSignalAspectNames.GetAspectLabel(SignalAspect.Ks2, is4046);
        AspectKs1BlinkLabel.Text = KsSignalAspectNames.GetAspectLabel(SignalAspect.Ks1Blink, is4046);
        AspectKennlichtLabel.Text = KsSignalAspectNames.GetAspectLabel(SignalAspect.Kennlicht, is4046);
        AspectDunkelLabel.Text = KsSignalAspectNames.GetAspectLabel(SignalAspect.Dunkel, is4046);
        AspectRa12Label.Text = KsSignalAspectNames.GetAspectLabel(SignalAspect.Ra12, is4046);
        AspectZs1Label.Text = KsSignalAspectNames.GetAspectLabel(SignalAspect.Zs1, is4046);
        AspectZs7Label.Text = KsSignalAspectNames.GetAspectLabel(SignalAspect.Zs7, is4046);

        ToolTipService.SetToolTip(AspectHp0Button, "Hp 0 - Stop");
        ToolTipService.SetToolTip(AspectKs1Button, "Ks 1 - Proceed");
        ToolTipService.SetToolTip(AspectKs2Button, is4046 ? "Ks 2 with white marker light at the top left" : "Ks 2 - Expect stop");
        ToolTipService.SetToolTip(AspectKs1BlinkButton, is4046 ? "Ks 2 with white marker light at the top left and top speed indicator" : "Ks 1 flashing - Proceed with speed pre-indicator");
        ToolTipService.SetToolTip(AspectKennlichtButton, is4046 ? "Only white marker light at the top left" : "Marker light - Signal disabled for operations");
        ToolTipService.SetToolTip(AspectDunkelButton, is4046 ? "Green flashing with white marker light at the top left and top/bottom speed indicators" : "Dark mode - Signal inactive");
        ToolTipService.SetToolTip(AspectRa12Button, is4046 ? "Hp0 with white marker light at the bottom for shunting movements" : "Sh 1/Ra 12 - Shunting allowed");
        ToolTipService.SetToolTip(AspectZs1Button, is4046 ? "Ks 1 with top speed indicator" : "Zs 1 - Substitute signal (white flashing)");
        ToolTipService.SetToolTip(AspectZs7Button, "Zs 7 - Caution signal (3x yellow)");
    }

    private void SetAllAspectButtonsVisibility(Visibility visibility)
    {
        foreach (var (button, _) in EnumerateAspectButtons())
        {
            button.Visibility = visibility;
        }
    }

    private IEnumerable<(Border Button, SignalAspect Aspect)> EnumerateAspectButtons()
    {
        yield return (AspectHp0Button, SignalAspect.Hp0);
        yield return (AspectKs1Button, SignalAspect.Ks1);
        yield return (AspectKs2Button, SignalAspect.Ks2);
        yield return (AspectKs1BlinkButton, SignalAspect.Ks1Blink);
        yield return (AspectKennlichtButton, SignalAspect.Kennlicht);
        yield return (AspectDunkelButton, SignalAspect.Dunkel);
        yield return (AspectRa12Button, SignalAspect.Ra12);
        yield return (AspectZs1Button, SignalAspect.Zs1);
        yield return (AspectZs7Button, SignalAspect.Zs7);
    }

    private IEnumerable<(KsSignalScreen Screen, SignalAspect Aspect)> EnumerateAspectSignals()
    {
        yield return (AspectHp0Signal, SignalAspect.Hp0);
        yield return (AspectKs1Signal, SignalAspect.Ks1);
        yield return (AspectKs2Signal, SignalAspect.Ks2);
        yield return (AspectKs1BlinkSignal, SignalAspect.Ks1Blink);
        yield return (AspectKennlichtSignal, SignalAspect.Kennlicht);
        yield return (AspectDunkelSignal, SignalAspect.Dunkel);
        yield return (AspectRa12Signal, SignalAspect.Ra12);
        yield return (AspectZs1Signal, SignalAspect.Zs1);
        yield return (AspectZs7Signal, SignalAspect.Zs7);
    }
}