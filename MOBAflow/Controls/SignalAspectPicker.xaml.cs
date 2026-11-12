// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls;

using SharedUI.Appearance;
using Common.Multiplex;

using Domain;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Grid of signal aspects. Each tile is a <see cref="ToggleButton"/>, so Tab reaches it, Enter or Space selects it
/// and a screen reader announces the aspect description and whether it is the selected one.
/// </summary>
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
        UpdatePicker();
    }

    private void OnAspectClicked(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton { Tag: string aspectName } && Enum.TryParse<SignalAspect>(aspectName, out var aspect))
        {
            SelectedAspect = aspect;
        }

        // A ToggleButton toggles itself on click; the selection decides which tile is checked.
        UpdateSelectionVisuals();
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
        foreach (var (button, aspect) in EnumerateAspectButtons(this))
        {
            button.IsChecked = SelectedAspect == aspect;
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
            foreach (var (button, aspect) in EnumerateAspectButtons(this))
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

        foreach (var (screen, aspect) in EnumerateAspectSignals(this))
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

        // The tooltip text is also the accessible name: the tile's own text is only the short aspect label.
        foreach (var (button, aspect) in EnumerateAspectButtons(this))
        {
            var description = KsSignalAspectNames.GetAspectDescription(aspect, is4046);
            ToolTipService.SetToolTip(button, description);
            AutomationProperties.SetName(button, description);
        }
    }

    private void SetAllAspectButtonsVisibility(Visibility visibility)
    {
        foreach (var (button, _) in EnumerateAspectButtons(this))
        {
            button.Visibility = visibility;
        }
    }

    private static IEnumerable<(ToggleButton Button, SignalAspect Aspect)> EnumerateAspectButtons(SignalAspectPicker picker)
    {
        yield return (picker.AspectHp0Button, SignalAspect.Hp0);
        yield return (picker.AspectKs1Button, SignalAspect.Ks1);
        yield return (picker.AspectKs2Button, SignalAspect.Ks2);
        yield return (picker.AspectKs1BlinkButton, SignalAspect.Ks1Blink);
        yield return (picker.AspectKennlichtButton, SignalAspect.Kennlicht);
        yield return (picker.AspectDunkelButton, SignalAspect.Dunkel);
        yield return (picker.AspectRa12Button, SignalAspect.Ra12);
        yield return (picker.AspectZs1Button, SignalAspect.Zs1);
        yield return (picker.AspectZs7Button, SignalAspect.Zs7);
    }

    private static IEnumerable<(KsSignalScreen Screen, SignalAspect Aspect)> EnumerateAspectSignals(SignalAspectPicker picker)
    {
        yield return (picker.AspectHp0Signal, SignalAspect.Hp0);
        yield return (picker.AspectKs1Signal, SignalAspect.Ks1);
        yield return (picker.AspectKs2Signal, SignalAspect.Ks2);
        yield return (picker.AspectKs1BlinkSignal, SignalAspect.Ks1Blink);
        yield return (picker.AspectKennlichtSignal, SignalAspect.Kennlicht);
        yield return (picker.AspectDunkelSignal, SignalAspect.Dunkel);
        yield return (picker.AspectRa12Signal, SignalAspect.Ra12);
        yield return (picker.AspectZs1Signal, SignalAspect.Zs1);
        yield return (picker.AspectZs7Signal, SignalAspect.Zs7);
    }
}