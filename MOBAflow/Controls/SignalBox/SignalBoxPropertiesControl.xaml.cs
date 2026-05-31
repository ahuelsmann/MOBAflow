// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls.SignalBox;

using Common.Extension;
using Common.Multiplex;

using Domain;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Service;

using SharedUI.ViewModel;

using System;
using System.Linq;
using System.Threading.Tasks;

public sealed partial class SignalBoxPropertiesControl
{
    private ViessmannSignalService? _viessmannSignalService;
    private ILogger<SignalBoxPropertiesControl>? _logger;
    private bool _isUpdatingSpeedIndicatorControls;
    private bool _isUpdatingNameBox;

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(MainWindowViewModel),
        typeof(SignalBoxPropertiesControl),
        new PropertyMetadata(null));

    public MainWindowViewModel? ViewModel
    {
        get => (MainWindowViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty PlanViewModelProperty = DependencyProperty.Register(
        nameof(PlanViewModel),
        typeof(SignalBoxPlanViewModel),
        typeof(SignalBoxPropertiesControl),
        new PropertyMetadata(null));

    public SignalBoxPlanViewModel PlanViewModel
    {
        get => (SignalBoxPlanViewModel)GetValue(PlanViewModelProperty);
        set => SetValue(PlanViewModelProperty, value);
    }

    public static readonly DependencyProperty SelectedElementProperty = DependencyProperty.Register(
        nameof(SelectedElement),
        typeof(SbElement),
        typeof(SignalBoxPropertiesControl),
        new PropertyMetadata(null, OnSelectedElementChanged));

    public SbElement? SelectedElement
    {
        get => (SbElement?)GetValue(SelectedElementProperty);
        set => SetValue(SelectedElementProperty, value);
    }

    public event EventHandler<SbElement>? RequestVisualRefresh;
    public event EventHandler<SbElement>? RequestElementDeletion;

    public SignalBoxPropertiesControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Wires runtime services from the hosting page. Required because WinUI instantiates this control from XAML
    /// before constructor injection can supply dependencies.
    /// </summary>
    /// <param name="viessmannSignalService">Viessmann signal catalog and aspect helpers.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    internal void AttachRuntimeServices(ViessmannSignalService viessmannSignalService, ILogger<SignalBoxPropertiesControl>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(viessmannSignalService);
        _viessmannSignalService = viessmannSignalService;
        _logger = logger;
    }

    private static void OnSelectedElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SignalBoxPropertiesControl control)
        {
            control.UpdatePropertiesPanel();
        }
    }

    private void UpdatePropertiesPanel()
    {
        if (SelectedElement == null)
        {
            NoSelectionInfo.Visibility = Visibility.Visible;
            ElementPropertiesPanel.Visibility = Visibility.Collapsed;
            return;
        }

        NoSelectionInfo.Visibility = Visibility.Collapsed;
        ElementPropertiesPanel.Visibility = Visibility.Visible;

        _isUpdatingNameBox = true;
        try
        {
            ElementNameBox.Text = SelectedElement.Name;
        }
        finally
        {
            _isUpdatingNameBox = false;
        }

        if (SelectedElement is SbSwitch sw)
        {
            ElementAddressBox.Header = "DCC address (switch)";
            ElementAddressBox.Value = sw.Address;
            ElementAddressBox.Visibility = Visibility.Visible;
            AddressPanel.Visibility = Visibility.Visible;
        }
        else if (SelectedElement is SbDetector det)
        {
            ElementAddressBox.Header = "Feedback address";
            ElementAddressBox.Value = det.FeedbackAddress;
            ElementAddressBox.Visibility = Visibility.Visible;
            AddressPanel.Visibility = Visibility.Visible;
        }
        else if (SelectedElement is SbSignal)
        {
            AddressPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            AddressPanel.Visibility = Visibility.Collapsed;
        }

        SignalAspectPanel.Visibility = SelectedElement is SbSignal ? Visibility.Visible : Visibility.Collapsed;
        UpdateMultiplexConfigPanel();
        SwitchPositionPanel.Visibility = SelectedElement is SbSwitch ? Visibility.Visible : Visibility.Collapsed;
        UpdateAspectButtons();
        UpdateAspectPresentation(SelectedElement as SbSignal);

        if (SelectedElement is SbSwitch)
        {
            UpdateSwitchButtons();
        }
    }

    private void UpdateMultiplexConfigPanel()
    {
        if (SelectedElement is not SbSignal sig)
        {
            MultiplexConfigPanel.Visibility = Visibility.Collapsed;
            return;
        }

        MultiplexConfigPanel.Visibility = Visibility.Visible;
        EnsureMultiplexerComboBoxItems();
        RestoreMultiplexerSelection(sig);
        ApplySignalConfigToMultiplexPanel(sig);
    }

    private void EnsureMultiplexerComboBoxItems()
    {
        if (MultiplexerComboBox.Items.Count > 0)
        {
            return;
        }

        MultiplexerComboBox.SelectionChanged -= OnMultiplexerSelected;
        foreach (var def in MultiplexerHelper.GetAllDefinitions())
        {
            MultiplexerComboBox.Items.Add(new ComboBoxItem
            {
                Content = def.DisplayName,
                Tag = def.ArticleNumber
            });
        }
        MultiplexerComboBox.SelectionChanged += OnMultiplexerSelected;
    }

    private void RestoreMultiplexerSelection(SbSignal sig)
    {
        if (string.IsNullOrEmpty(sig.MultiplexerArticleNumber))
        {
            return;
        }

        var selectedItem = MultiplexerComboBox.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(x => x.Tag?.ToString() == sig.MultiplexerArticleNumber);
        if (selectedItem != null)
        {
            MultiplexerComboBox.SelectedItem = selectedItem;
        }
    }

    private void ApplySignalConfigToMultiplexPanel(SbSignal sig)
    {
        UpdateSignalArticleComboBoxes(sig);
        BaseAddressBox.Value = sig.BaseAddress > 0 ? sig.BaseAddress : 1;
        UpdateSpeedIndicatorConfig(sig);
        UpdateAvailableSignalAspects(sig);
    }

    private void UpdateSpeedIndicatorConfig(SbSignal sig)
    {
        var is4046 = string.Equals(sig.MainSignalArticleNumber, "4046", StringComparison.Ordinal);
        SpeedIndicatorConfigPanel.Visibility = is4046 ? Visibility.Visible : Visibility.Collapsed;

        _isUpdatingSpeedIndicatorControls = true;
        try
        {
            TopSpeedIndicatorBox.Value = ParseNullableSpeedIndicator(sig.TopSpeedIndicator) ?? double.NaN;
            BottomSpeedIndicatorBox.Value = ParseNullableSpeedIndicator(sig.BottomSpeedIndicator) ?? double.NaN;
        }
        finally
        {
            _isUpdatingSpeedIndicatorControls = false;
        }
    }

    private void UpdateAvailableSignalAspects(SbSignal sig)
    {
        UpdateAspectPresentation(sig);

        if (string.IsNullOrEmpty(sig.MultiplexerArticleNumber))
        {
            SetAllAspectButtonsVisibility(Visibility.Visible);
            return;
        }

        if (!TryGetSupportedAspects(sig, out var supportedAspects))
        {
            SetAllAspectButtonsVisibility(Visibility.Visible);
            return;
        }

        ApplySupportedAspectVisibility(supportedAspects);
        if (supportedAspects.Count == 0)
        {
            SetAllAspectButtonsVisibility(Visibility.Visible);
        }
    }

    private static bool TryGetSupportedAspects(SbSignal sig, out IReadOnlyCollection<SignalAspect> supportedAspects)
    {
        try
        {
            supportedAspects = MultiplexerHelper.GetSupportedAspects(
                sig.MultiplexerArticleNumber!,
                sig.MainSignalArticleNumber);
            return true;
        }
        catch (ArgumentException)
        {
            supportedAspects = Array.Empty<SignalAspect>();
            return false;
        }
    }

    private void ApplySupportedAspectVisibility(IReadOnlyCollection<SignalAspect> supportedAspects)
    {
        AspectHp0Button.Visibility = ToVisibility(supportedAspects.Contains(SignalAspect.Hp0));
        AspectKs1Button.Visibility = ToVisibility(supportedAspects.Contains(SignalAspect.Ks1));
        AspectKs2Button.Visibility = ToVisibility(supportedAspects.Contains(SignalAspect.Ks2));
        AspectKs1BlinkButton.Visibility = ToVisibility(supportedAspects.Contains(SignalAspect.Ks1Blink));
        AspectKennlichtButton.Visibility = ToVisibility(supportedAspects.Contains(SignalAspect.Kennlicht));
        AspectDunkelButton.Visibility = ToVisibility(supportedAspects.Contains(SignalAspect.Dunkel));
        AspectRa12Button.Visibility = ToVisibility(supportedAspects.Contains(SignalAspect.Ra12));
        AspectZs1Button.Visibility = ToVisibility(supportedAspects.Contains(SignalAspect.Zs1));
        AspectZs7Button.Visibility = ToVisibility(supportedAspects.Contains(SignalAspect.Zs7));
    }

    private static Visibility ToVisibility(bool isVisible)
    {
        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetAllAspectButtonsVisibility(Visibility visibility)
    {
        AspectHp0Button.Visibility = visibility;
        AspectKs1Button.Visibility = visibility;
        AspectKs2Button.Visibility = visibility;
        AspectKs1BlinkButton.Visibility = visibility;
        AspectKennlichtButton.Visibility = visibility;
        AspectDunkelButton.Visibility = visibility;
        AspectRa12Button.Visibility = visibility;
        AspectZs1Button.Visibility = visibility;
        AspectZs7Button.Visibility = visibility;
    }

    private void UpdateSignalArticleComboBoxes(SbSignal sig)
    {
        var viessmann = _viessmannSignalService;
        if (viessmann == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(sig.MultiplexerArticleNumber))
        {
            MainSignalComboBox.Items.Clear();
            DistantSignalComboBox.Items.Clear();
            return;
        }

        try
        {
            var def = MultiplexerHelper.GetDefinition(sig.MultiplexerArticleNumber);
            UpdateMainSignalComboBox(sig, def.MainSignalArticleNumber, viessmann);
            UpdateDistantSignalComboBox(sig, viessmann);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error updating signal article ComboBoxes");
        }
    }

    private void UpdateMainSignalComboBox(SbSignal sig, string defaultMainSignalArticleNumber, ViessmannSignalService viessmann)
    {
        MainSignalComboBox.SelectionChanged -= OnMainSignalSelected;
        MainSignalComboBox.Items.Clear();
        foreach (var (articleNumber, displayName) in viessmann.GetMainSignalOptions(sig.MultiplexerArticleNumber!))
        {
            MainSignalComboBox.Items.Add(new ComboBoxItem { Content = displayName, Tag = articleNumber });
        }
        MainSignalComboBox.SelectionChanged += OnMainSignalSelected;

        var mainSelected = MainSignalComboBox.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(x => x.Tag?.ToString() == sig.MainSignalArticleNumber);
        if (mainSelected != null)
        {
            MainSignalComboBox.SelectedItem = mainSelected;
            return;
        }

        if (MainSignalComboBox.Items.Count > 0)
        {
            MainSignalComboBox.SelectedIndex = 0;
            sig.MainSignalArticleNumber = (MainSignalComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? defaultMainSignalArticleNumber;
        }
    }

    private void UpdateDistantSignalComboBox(SbSignal sig, ViessmannSignalService viessmann)
    {
        DistantSignalComboBox.SelectionChanged -= OnDistantSignalSelected;
        DistantSignalComboBox.Items.Clear();
        foreach (var (articleNumber, displayName) in viessmann.GetDistantSignalOptions(sig.MultiplexerArticleNumber!))
        {
            DistantSignalComboBox.Items.Add(new ComboBoxItem { Content = displayName, Tag = articleNumber });
        }
        DistantSignalComboBox.SelectionChanged += OnDistantSignalSelected;

        if (DistantSignalComboBox.Items.Count == 0)
        {
            return;
        }

        var distantSelected = DistantSignalComboBox.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(x => x.Tag?.ToString() == sig.DistantSignalArticleNumber);
        if (distantSelected != null)
        {
            DistantSignalComboBox.SelectedItem = distantSelected;
            return;
        }

        DistantSignalComboBox.SelectedIndex = 0;
        sig.DistantSignalArticleNumber = (DistantSignalComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
    }

    private void OnMainSignalSelected(object sender, SelectionChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        TryApplySignalArticleSelection(MainSignalComboBox, static (sig, articleNumber) => sig.MainSignalArticleNumber = articleNumber);
    }

    private void OnDistantSignalSelected(object sender, SelectionChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        TryApplySignalArticleSelection(DistantSignalComboBox, static (sig, articleNumber) => sig.DistantSignalArticleNumber = articleNumber);
    }

    private void TryApplySignalArticleSelection(ComboBox comboBox, Action<SbSignal, string> applySelection)
    {
        if (SelectedElement is not SbSignal sig || comboBox.SelectedItem is not ComboBoxItem { Tag: string articleNumber })
        {
            return;
        }

        applySelection(sig, articleNumber);
        UpdateAvailableSignalAspects(sig);
        QueueSolutionSave();
    }

    private void UpdateAspectButtons()
    {
        if (SelectedElement is not SbSignal sig) return;

        var accentBrush = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        var normalBrush = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"];

        foreach (var (button, aspect) in EnumerateAspectButtons())
        {
            button.Background = sig.SignalAspect == aspect ? accentBrush : normalBrush;
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

    private void UpdateAspectPresentation(SbSignal? sig)
    {
        var is4046 = sig is not null && string.Equals(sig.MainSignalArticleNumber, "4046", StringComparison.Ordinal);
        var signalArticleNumber = is4046 ? "4046" : string.Empty;

        ApplyAspectPreviewSignals(signalArticleNumber, sig);
        ApplyAspectLabels(is4046);
        ApplyAspectTooltips(is4046);
    }

    private void ApplyAspectPreviewSignals(string signalArticleNumber, SbSignal? selectedSignal)
    {
        var topSpeedIndicator = selectedSignal?.TopSpeedIndicator ?? string.Empty;
        var bottomSpeedIndicator = selectedSignal?.BottomSpeedIndicator ?? string.Empty;

        AspectHp0Signal.SignalArticleNumber = signalArticleNumber;
        AspectKs1Signal.SignalArticleNumber = signalArticleNumber;
        AspectKs2Signal.SignalArticleNumber = signalArticleNumber;
        AspectKs1BlinkSignal.SignalArticleNumber = signalArticleNumber;
        AspectKennlichtSignal.SignalArticleNumber = signalArticleNumber;
        AspectDunkelSignal.SignalArticleNumber = signalArticleNumber;
        AspectRa12Signal.SignalArticleNumber = signalArticleNumber;
        AspectZs1Signal.SignalArticleNumber = signalArticleNumber;
        AspectZs7Signal.SignalArticleNumber = signalArticleNumber;

        AspectHp0Signal.TopSpeedValue = topSpeedIndicator;
        AspectKs1Signal.TopSpeedValue = topSpeedIndicator;
        AspectKs2Signal.TopSpeedValue = topSpeedIndicator;
        AspectKs1BlinkSignal.TopSpeedValue = topSpeedIndicator;
        AspectKennlichtSignal.TopSpeedValue = topSpeedIndicator;
        AspectDunkelSignal.TopSpeedValue = topSpeedIndicator;
        AspectRa12Signal.TopSpeedValue = topSpeedIndicator;
        AspectZs1Signal.TopSpeedValue = topSpeedIndicator;
        AspectZs7Signal.TopSpeedValue = topSpeedIndicator;

        AspectHp0Signal.BottomSpeedValue = bottomSpeedIndicator;
        AspectKs1Signal.BottomSpeedValue = bottomSpeedIndicator;
        AspectKs2Signal.BottomSpeedValue = bottomSpeedIndicator;
        AspectKs1BlinkSignal.BottomSpeedValue = bottomSpeedIndicator;
        AspectKennlichtSignal.BottomSpeedValue = bottomSpeedIndicator;
        AspectDunkelSignal.BottomSpeedValue = bottomSpeedIndicator;
        AspectRa12Signal.BottomSpeedValue = bottomSpeedIndicator;
        AspectZs1Signal.BottomSpeedValue = bottomSpeedIndicator;
        AspectZs7Signal.BottomSpeedValue = bottomSpeedIndicator;

        AspectHp0Signal.Aspect = nameof(SignalAspect.Hp0);
        AspectKs1Signal.Aspect = nameof(SignalAspect.Ks1);
        AspectKs2Signal.Aspect = nameof(SignalAspect.Ks2);
        AspectKs1BlinkSignal.Aspect = nameof(SignalAspect.Ks1Blink);
        AspectKennlichtSignal.Aspect = nameof(SignalAspect.Kennlicht);
        AspectDunkelSignal.Aspect = nameof(SignalAspect.Dunkel);
        AspectRa12Signal.Aspect = nameof(SignalAspect.Ra12);
        AspectZs1Signal.Aspect = nameof(SignalAspect.Zs1);
        AspectZs7Signal.Aspect = nameof(SignalAspect.Zs7);
    }

    private void ApplyAspectLabels(bool is4046)
    {
        AspectHp0Label.Text = "Hp0";
        AspectKs1Label.Text = "Ks1";
        AspectKs2Label.Text = is4046 ? "Ks2+K" : "Ks2";
        AspectKs1BlinkLabel.Text = is4046 ? "Ks2+K+G" : "Ks1 Bl";
        AspectKennlichtLabel.Text = is4046 ? "K links" : "Kennl.";
        AspectDunkelLabel.Text = is4046 ? "GrBl+K+G" : "Dunkel";
        AspectRa12Label.Text = is4046 ? "Hp0+Rg" : "Ra12";
        AspectZs1Label.Text = is4046 ? "Ks1+G" : "Zs1";
        AspectZs7Label.Text = "Zs7";
    }

    private void ApplyAspectTooltips(bool is4046)
    {
        ToolTipService.SetToolTip(AspectHp0Button, "Hp 0 - Halt");
        ToolTipService.SetToolTip(AspectKs1Button, "Ks 1 - Proceed");
        ToolTipService.SetToolTip(AspectKs2Button, is4046 ? "Ks 2 with white marker light at the top left" : "Ks 2 - Expect stop");
        ToolTipService.SetToolTip(AspectKs1BlinkButton, is4046 ? "Ks 2 with white marker light at the top left and top speed indicator" : "Ks 1 flashing - Proceed with speed pre-indicator");
        ToolTipService.SetToolTip(AspectKennlichtButton, is4046 ? "Only white marker light at the top left" : "Marker light - Signal disabled for operations");
        ToolTipService.SetToolTip(AspectDunkelButton, is4046 ? "Green flashing with white marker light at the top left and top/bottom speed indicators" : "Dark mode - Signal inactive");
        ToolTipService.SetToolTip(AspectRa12Button, is4046 ? "Hp0 with white marker light at the bottom for shunting movements" : "Sh 1/Ra 12 - Shunting allowed");
        ToolTipService.SetToolTip(AspectZs1Button, is4046 ? "Ks 1 with top speed indicator" : "Zs 1 - Substitute signal (white flashing)");
        ToolTipService.SetToolTip(AspectZs7Button, "Zs 7 - Caution signal (3x yellow)");
    }

    private void UpdateSwitchButtons()
    {
        if (SelectedElement is not SbSwitch sw) return;

        ThirdSwitchColumn.Width = new GridLength(1, GridUnitType.Star);
        SwitchRightButton.Visibility = Visibility.Visible;
        SwitchLeftButton.Visibility = Visibility.Visible;

        var accentStyle = (Style)Application.Current.Resources["AccentButtonStyle"];
        var defaultStyle = (Style)Application.Current.Resources["DefaultButtonStyle"];
        ApplySwitchButtonStyle(SwitchStraightButton, sw.SwitchPosition == SwitchPosition.Straight, accentStyle, defaultStyle);
        ApplySwitchButtonStyle(SwitchLeftButton, sw.SwitchPosition == SwitchPosition.DivergingLeft, accentStyle, defaultStyle);
        ApplySwitchButtonStyle(SwitchRightButton, sw.SwitchPosition == SwitchPosition.DivergingRight, accentStyle, defaultStyle);
    }

    private static void ApplySwitchButtonStyle(Button button, bool isActive, Style accentStyle, Style defaultStyle)
    {
        button.Style = isActive ? accentStyle : defaultStyle;
    }

    private void OnElementNameChanged(object sender, TextChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        if (_isUpdatingNameBox || SelectedElement == null)
        {
            return;
        }

        SelectedElement.Name = ElementNameBox.Text;
        QueueSolutionSave();
    }

    private void OnRotateClicked(object sender, RoutedEventArgs e)
    {
        _ = e;
        if (SelectedElement == null || !TryGetButtonTag(sender, out var rotationTag) || !int.TryParse(rotationTag, out var rotation))
        {
            return;
        }

        SelectedElement.Rotation = rotation;
        RequestVisualRefresh?.Invoke(this, SelectedElement);
    }

    private void OnAspectClicked(object sender, PointerRoutedEventArgs e)
    {
        if (SelectedElement is not SbSignal sig || sender is not Border { Tag: string aspectStr }) return;

        if (Enum.TryParse<SignalAspect>(aspectStr, out var aspect))
        {
            sig.SignalAspect = aspect;
            RequestVisualRefresh?.Invoke(this, sig);
            UpdateAspectButtons();
            SetSignalAspectAutomaticallyAsync(sig).Observe(
                ex => _logger?.LogWarning(ex, "Set signal aspect failed"));
        }
    }

    private async Task SetSignalAspectAutomaticallyAsync(SbSignal sig)
    {
        if (!TryValidateAspectSetRequest(sig))
        {
            return;
        }

        if (!TryApplyTurnoutCommand(sig))
        {
            return;
        }

        if (ViewModel == null)
        {
            return;
        }

        if (!ViewModel.IsConnected)
        {
            return;
        }

        await ViewModel.SetSignalAspectAsync(sig).ConfigureAwait(false);
    }

    private static bool TryValidateAspectSetRequest(SbSignal sig)
    {
        if (!sig.IsMultiplexed)
        {
            return false;
        }

        if (string.IsNullOrEmpty(sig.MultiplexerArticleNumber))
        {
            return false;
        }

        if (sig.BaseAddress <= 0 || sig.BaseAddress > 2044)
        {
            return false;
        }

        if (sig.BaseAddress % 2 == 0)
        {
            return false;
        }

        if (!MultiplexerHelper.TryGetMaxAddressOffset(
                sig.MultiplexerArticleNumber,
                sig.MainSignalArticleNumber,
                out var maxOffset))
        {
            return false;
        }

        return sig.BaseAddress + maxOffset <= 2044;
    }

    private static bool TryApplyTurnoutCommand(SbSignal sig)
    {
        try
        {
            if (!MultiplexerHelper.TryGetTurnoutCommand(
                    sig.MultiplexerArticleNumber!,
                    sig.MainSignalArticleNumber,
                    sig.SignalAspect,
                    out var resolvedTurnoutCommand))
            {
                return false;
            }

            sig.ExtendedAccessoryValue = resolvedTurnoutCommand.Activate ? 1 : 0;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private void OnSwitchPositionClicked(object sender, RoutedEventArgs e)
    {
        _ = e;
        if (SelectedElement is not SbSwitch sw || !TryGetButtonTag(sender, out var positionTag))
        {
            return;
        }

        sw.SwitchPosition = ParseSwitchPosition(positionTag);

        RequestVisualRefresh?.Invoke(this, sw);
        UpdateSwitchButtons();
    }

    private static bool TryGetButtonTag(object sender, out string tag)
    {
        if (sender is Button { Tag: string buttonTag })
        {
            tag = buttonTag;
            return true;
        }

        tag = string.Empty;
        return false;
    }

    private static SwitchPosition ParseSwitchPosition(string positionTag)
    {
        return positionTag switch
        {
            "Straight" => SwitchPosition.Straight,
            "DivergingLeft" => SwitchPosition.DivergingLeft,
            "DivergingRight" => SwitchPosition.DivergingRight,
            _ => SwitchPosition.Straight
        };
    }

    private void OnAddressChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        _ = sender;
        if (!TryGetNumberBoxIntValue(args, out var value) || SelectedElement == null)
        {
            return;
        }

        ApplyAddressValue(SelectedElement, value);
    }

    private void OnDeleteElementClicked(object sender, RoutedEventArgs e)
    {
        if (SelectedElement != null)
        {
            RequestElementDeletion?.Invoke(this, SelectedElement);
        }
    }

    private void OnMultiplexerSelected(object sender, SelectionChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        if (SelectedElement is not SbSignal sig) return;

        if (TryGetSelectedMultiplexerArticle(out var articleNumber))
        {
            ApplyMultiplexerSelection(sig, articleNumber);
        }
        else
        {
            ResetMultiplexerSelection(sig);
        }
    }

    private bool TryGetSelectedMultiplexerArticle(out string articleNumber)
    {
        if (MultiplexerComboBox.SelectedItem is ComboBoxItem { Tag: string selectedArticleNumber })
        {
            articleNumber = selectedArticleNumber;
            return true;
        }

        articleNumber = string.Empty;
        return false;
    }

    private void ApplyMultiplexerSelection(SbSignal sig, string articleNumber)
    {
        sig.MultiplexerArticleNumber = articleNumber;
        sig.IsMultiplexed = true;
        UpdateSignalArticleComboBoxes(sig);
        UpdateAvailableSignalAspects(sig);
    }

    private void ResetMultiplexerSelection(SbSignal sig)
    {
        sig.MultiplexerArticleNumber = string.Empty;
        sig.IsMultiplexed = false;
        MainSignalComboBox.Items.Clear();
        DistantSignalComboBox.Items.Clear();
        SetAllAspectButtonsVisibility(Visibility.Visible);
    }

    private void OnBaseAddressChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        _ = sender;
        if (!TryGetNumberBoxIntValue(args, out var value) || SelectedElement is not SbSignal sig)
        {
            return;
        }

        sig.BaseAddress = value;
        QueueSolutionSave();
    }

    private void OnTopSpeedIndicatorChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        _ = sender;
        if (_isUpdatingSpeedIndicatorControls || SelectedElement is not SbSignal sig)
        {
            return;
        }

        sig.TopSpeedIndicator = ParseSpeedIndicatorInput(args.NewValue);
        RequestVisualRefresh?.Invoke(this, sig);
        UpdateAspectPresentation(sig);
        QueueSolutionSave();
    }

    private void OnBottomSpeedIndicatorChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        _ = sender;
        if (_isUpdatingSpeedIndicatorControls || SelectedElement is not SbSignal sig)
        {
            return;
        }

        sig.BottomSpeedIndicator = ParseSpeedIndicatorInput(args.NewValue);
        RequestVisualRefresh?.Invoke(this, sig);
        UpdateAspectPresentation(sig);
        QueueSolutionSave();
    }

    private static string? ParseSpeedIndicatorInput(double newValue)
    {
        return double.IsNaN(newValue) ? null : ((int)newValue).ToString();
    }

    private static double? ParseNullableSpeedIndicator(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        return int.TryParse(rawValue, out var parsedValue)
            ? parsedValue
            : null;
    }

    private void QueueSolutionSave()
    {
        ViewModel?.SaveSolutionInternalAsync().Observe(
            ex => _logger?.LogWarning(ex, "Failed to persist signal box properties"));
    }

    private static bool TryGetNumberBoxIntValue(NumberBoxValueChangedEventArgs args, out int value)
    {
        if (double.IsNaN(args.NewValue))
        {
            value = 0;
            return false;
        }

        value = (int)args.NewValue;
        return true;
    }

    private static void ApplyAddressValue(SbElement element, int value)
    {
        switch (element)
        {
            case SbSwitch sw:
                sw.Address = value;
                break;
            case SbDetector det:
                det.FeedbackAddress = value;
                break;
        }
    }

}
