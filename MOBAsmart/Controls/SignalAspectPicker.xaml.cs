// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Controls;

using Common.Display;
using Common.Multiplex;

using Domain;

using System.Windows.Input;

/// <summary>
/// Visual grid picker for railway signal aspects, matching the MOBAflow desktop experience.
/// </summary>
public partial class SignalAspectPicker
{
    private bool _isLoaded;

    public static readonly BindableProperty SelectedAspectProperty = BindableProperty.Create(
        nameof(SelectedAspect),
        typeof(SignalAspect?),
        typeof(SignalAspectPicker),
        null,
        BindingMode.TwoWay,
        propertyChanged: OnPickerPropertyChanged);

    public static readonly BindableProperty AspectSelectCommandProperty = BindableProperty.Create(
        nameof(AspectSelectCommand),
        typeof(ICommand),
        typeof(SignalAspectPicker));

    public static readonly BindableProperty MultiplexerArticleNumberProperty = BindableProperty.Create(
        nameof(MultiplexerArticleNumber),
        typeof(string),
        typeof(SignalAspectPicker),
        string.Empty,
        propertyChanged: OnPickerPropertyChanged);

    public static readonly BindableProperty SignalArticleNumberProperty = BindableProperty.Create(
        nameof(SignalArticleNumber),
        typeof(string),
        typeof(SignalAspectPicker),
        string.Empty,
        propertyChanged: OnSignalArticleNumberChanged);

    public static readonly BindableProperty PreviewSignalArticleNumberProperty = BindableProperty.Create(
        nameof(PreviewSignalArticleNumber),
        typeof(string),
        typeof(SignalAspectPicker),
        string.Empty);

    public static readonly BindableProperty TopSpeedValueProperty = BindableProperty.Create(
        nameof(TopSpeedValue),
        typeof(string),
        typeof(SignalAspectPicker),
        string.Empty,
        propertyChanged: OnPickerPropertyChanged);

    public static readonly BindableProperty BottomSpeedValueProperty = BindableProperty.Create(
        nameof(BottomSpeedValue),
        typeof(string),
        typeof(SignalAspectPicker),
        string.Empty,
        propertyChanged: OnPickerPropertyChanged);

    public SignalAspect? SelectedAspect
    {
        get => (SignalAspect?)GetValue(SelectedAspectProperty);
        set => SetValue(SelectedAspectProperty, value);
    }

    public ICommand? AspectSelectCommand
    {
        get => (ICommand?)GetValue(AspectSelectCommandProperty);
        set => SetValue(AspectSelectCommandProperty, value);
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

    public string PreviewSignalArticleNumber
    {
        get => (string)GetValue(PreviewSignalArticleNumberProperty);
        private set => SetValue(PreviewSignalArticleNumberProperty, value);
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

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        ScheduleUpdatePicker();
    }

    private static void OnPickerPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SignalAspectPicker picker)
        {
            picker.ScheduleUpdatePicker();
        }
    }

    private static void OnSignalArticleNumberChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not SignalAspectPicker picker)
        {
            return;
        }

        picker.PreviewSignalArticleNumber = KsSignalAspectNames.ResolvePreviewSignalArticleNumber(picker.SignalArticleNumber);
        picker.ScheduleUpdatePicker();
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        _isLoaded = true;
        PreviewSignalArticleNumber = KsSignalAspectNames.ResolvePreviewSignalArticleNumber(SignalArticleNumber);
        UpdatePicker();
    }

    private void ScheduleUpdatePicker()
    {
        if (!_isLoaded)
        {
            return;
        }

        Dispatcher.Dispatch(UpdatePicker);
    }

    private void OnAspectTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not string aspectName || !Enum.TryParse<SignalAspect>(aspectName, out var aspect))
        {
            return;
        }

        if (AspectSelectCommand != null)
        {
            AspectSelectCommand.Execute(aspect);
            return;
        }

        SelectedAspect = aspect;
    }

    private void UpdatePicker()
    {
        if (!_isLoaded)
        {
            return;
        }

        UpdateAspectLabels();
        UpdateSupportedAspectVisibility();
        UpdateSelectionVisuals();
    }

    private void UpdateSelectionVisuals()
    {
        var selected = SelectedAspect ?? SignalAspect.Hp0;
        var accentColor = ResolveColor("Primary", Color.FromArgb("#64B5F6"));
        var normalColor = ResolveColor("SurfaceVariant", Color.FromArgb("#2C2C2C"));
        var accentBorder = ResolveColor("Primary", Color.FromArgb("#64B5F6"));
        var selectedLabelColor = ResolveColor("TextOnPrimary", Colors.Black);
        var normalLabelColor = ResolveColor("TextPrimary", Colors.White);

        foreach (var (button, label, aspect) in EnumerateAspectButtonsWithLabels())
        {
            var isSelected = selected == aspect;
            button.BackgroundColor = isSelected ? accentColor : normalColor;
            button.Stroke = isSelected ? accentBorder : ResolveColor("BorderColor", Colors.Gray);
            button.StrokeThickness = isSelected ? 2 : 1;
            label.TextColor = isSelected ? selectedLabelColor : normalLabelColor;
        }
    }

    private void UpdateSupportedAspectVisibility()
    {
        if (string.IsNullOrWhiteSpace(MultiplexerArticleNumber))
        {
            SetAllAspectButtonsVisibility(true);
            return;
        }

        try
        {
            var supportedAspects = MultiplexerHelper.GetSupportedAspects(MultiplexerArticleNumber, SignalArticleNumber);
            foreach (var (button, aspect) in EnumerateAspectButtons())
            {
                button.IsVisible = supportedAspects.Contains(aspect);
            }

            if (supportedAspects.Count == 0)
            {
                SetAllAspectButtonsVisibility(true);
            }
        }
        catch (ArgumentException)
        {
            SetAllAspectButtonsVisibility(true);
        }
    }

    private void UpdateAspectLabels()
    {
        var is4046 = KsSignalAspectNames.Is4046Signal(SignalArticleNumber);

        AspectHp0Label.Text = KsSignalAspectNames.GetAspectLabel(SignalAspect.Hp0, is4046);
        AspectKs1Label.Text = KsSignalAspectNames.GetAspectLabel(SignalAspect.Ks1, is4046);
        AspectKs2Label.Text = KsSignalAspectNames.GetAspectLabel(SignalAspect.Ks2, is4046);
        AspectKs1BlinkLabel.Text = KsSignalAspectNames.GetAspectLabel(SignalAspect.Ks1Blink, is4046);
        AspectKennlichtLabel.Text = KsSignalAspectNames.GetAspectLabel(SignalAspect.Kennlicht, is4046);
        AspectDunkelLabel.Text = KsSignalAspectNames.GetAspectLabel(SignalAspect.Dunkel, is4046);
        AspectRa12Label.Text = KsSignalAspectNames.GetAspectLabel(SignalAspect.Ra12, is4046);
        AspectZs1Label.Text = KsSignalAspectNames.GetAspectLabel(SignalAspect.Zs1, is4046);
        AspectZs7Label.Text = KsSignalAspectNames.GetAspectLabel(SignalAspect.Zs7, is4046);
    }

    private void SetAllAspectButtonsVisibility(bool isVisible)
    {
        foreach (var (button, _) in EnumerateAspectButtons())
        {
            button.IsVisible = isVisible;
        }
    }

    private static Color ResolveColor(string key, Color fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var resource) == true && resource is Color color)
        {
            return color;
        }

        return fallback;
    }

    private IEnumerable<(Border Button, Label Label, SignalAspect Aspect)> EnumerateAspectButtonsWithLabels()
    {
        yield return (AspectHp0Button, AspectHp0Label, SignalAspect.Hp0);
        yield return (AspectKs1Button, AspectKs1Label, SignalAspect.Ks1);
        yield return (AspectKs2Button, AspectKs2Label, SignalAspect.Ks2);
        yield return (AspectKs1BlinkButton, AspectKs1BlinkLabel, SignalAspect.Ks1Blink);
        yield return (AspectKennlichtButton, AspectKennlichtLabel, SignalAspect.Kennlicht);
        yield return (AspectDunkelButton, AspectDunkelLabel, SignalAspect.Dunkel);
        yield return (AspectRa12Button, AspectRa12Label, SignalAspect.Ra12);
        yield return (AspectZs1Button, AspectZs1Label, SignalAspect.Zs1);
        yield return (AspectZs7Button, AspectZs7Label, SignalAspect.Zs7);
    }

    private IEnumerable<(Border Button, SignalAspect Aspect)> EnumerateAspectButtons()
    {
        foreach (var (button, _, aspect) in EnumerateAspectButtonsWithLabels())
        {
            yield return (button, aspect);
        }
    }
}
