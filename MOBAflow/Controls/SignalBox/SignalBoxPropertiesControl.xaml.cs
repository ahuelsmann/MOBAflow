namespace Moba.WinUI.Controls.SignalBox;

using Common.Multiplex;

using Domain;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Moba.SharedUI.ViewModel;

using Service;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

public sealed partial class SignalBoxPropertiesControl
{
    private readonly ViessmannSignalService _viessmannSignalService;

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(MainWindowViewModel),
        typeof(SignalBoxPropertiesControl),
        new PropertyMetadata(null));

    public MainWindowViewModel ViewModel
    {
        get => (MainWindowViewModel)GetValue(ViewModelProperty);
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
        _viessmannSignalService = App.Current.Services.GetRequiredService<ViessmannSignalService>();
    }

    private static void OnSelectedElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SignalBoxPropertiesControl control)
        {
            control.UpdatePropertiesPanel();
        }
    }

    public void UpdateStatistics()
    {
        TrackCountText.Text = PlanViewModel.Elements.Count(e => e is SbTrackStraight or SbTrackCurve).ToString();
        SwitchCountText.Text = PlanViewModel.Elements.OfType<SbSwitch>().Count().ToString();
        SignalCountText.Text = PlanViewModel.Elements.OfType<SbSignal>().Count().ToString();
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

        ElementTypeText.Text = GetElementTypeName(SelectedElement);
        ElementPositionText.Text = $"({SelectedElement.X}, {SelectedElement.Y})";
        ElementIdText.Text = SelectedElement.Id.ToString()[..8];

        if (SelectedElement is SbSwitch sw)
        {
            ElementAddressBox.Header = "DCC-Adresse (Weiche)";
            ElementAddressBox.Value = sw.Address;
            ElementAddressBox.Visibility = Visibility.Visible;
            AddressPanel.Visibility = Visibility.Visible;
        }
        else if (SelectedElement is SbDetector det)
        {
            ElementAddressBox.Header = "Feedback-Adresse";
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

        if (MultiplexerComboBox.Items.Count == 0)
        {
            MultiplexerComboBox.SelectionChanged -= OnMultiplexerSelected;
            foreach (var def in MultiplexerHelper.GetAllDefinitions())
            {
                var item = new ComboBoxItem
                {
                    Content = def.DisplayName,
                    Tag = def.ArticleNumber
                };
                MultiplexerComboBox.Items.Add(item);
            }
            MultiplexerComboBox.SelectionChanged += OnMultiplexerSelected;
        }

        if (!string.IsNullOrEmpty(sig.MultiplexerArticleNumber))
        {
            var multiplexerItem = MultiplexerComboBox.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(x => x.Tag?.ToString() == sig.MultiplexerArticleNumber);

            if (multiplexerItem != null)
            {
                MultiplexerComboBox.SelectedItem = multiplexerItem;
            }
        }

        UpdateSignalArticleComboBoxes(sig);
        BaseAddressBox.Value = sig.BaseAddress > 0 ? sig.BaseAddress : 1;
        UpdateAvailableSignalAspects(sig);
    }

    private void UpdateAvailableSignalAspects(SbSignal sig)
    {
        UpdateAspectPresentation(sig);

        if (string.IsNullOrEmpty(sig.MultiplexerArticleNumber))
        {
            SetAllAspectButtonsVisibility(Visibility.Visible);
            return;
        }

        try
        {
            var supportedAspects = MultiplexerHelper.GetSupportedAspects(
                sig.MultiplexerArticleNumber,
                sig.MainSignalArticleNumber);

            AspectHp0Button.Visibility = supportedAspects.Contains(SignalAspect.Hp0) ? Visibility.Visible : Visibility.Collapsed;
            AspectKs1Button.Visibility = supportedAspects.Contains(SignalAspect.Ks1) ? Visibility.Visible : Visibility.Collapsed;
            AspectKs2Button.Visibility = supportedAspects.Contains(SignalAspect.Ks2) ? Visibility.Visible : Visibility.Collapsed;
            AspectKs1BlinkButton.Visibility = supportedAspects.Contains(SignalAspect.Ks1Blink) ? Visibility.Visible : Visibility.Collapsed;
            AspectKennlichtButton.Visibility = supportedAspects.Contains(SignalAspect.Kennlicht) ? Visibility.Visible : Visibility.Collapsed;
            AspectDunkelButton.Visibility = supportedAspects.Contains(SignalAspect.Dunkel) ? Visibility.Visible : Visibility.Collapsed;
            AspectRa12Button.Visibility = supportedAspects.Contains(SignalAspect.Ra12) ? Visibility.Visible : Visibility.Collapsed;
            AspectZs1Button.Visibility = supportedAspects.Contains(SignalAspect.Zs1) ? Visibility.Visible : Visibility.Collapsed;
            AspectZs7Button.Visibility = supportedAspects.Contains(SignalAspect.Zs7) ? Visibility.Visible : Visibility.Collapsed;

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
        if (string.IsNullOrEmpty(sig.MultiplexerArticleNumber))
        {
            MainSignalComboBox.Items.Clear();
            DistantSignalComboBox.Items.Clear();
            return;
        }

        try
        {
            var def = MultiplexerHelper.GetDefinition(sig.MultiplexerArticleNumber);

            MainSignalComboBox.SelectionChanged -= OnMainSignalSelected;
            MainSignalComboBox.Items.Clear();
            foreach (var (articleNumber, displayName) in _viessmannSignalService.GetMainSignalOptions(sig.MultiplexerArticleNumber))
            {
                var item = new ComboBoxItem { Content = displayName, Tag = articleNumber };
                MainSignalComboBox.Items.Add(item);
            }
            MainSignalComboBox.SelectionChanged += OnMainSignalSelected;

            var mainSelected = MainSignalComboBox.Items.OfType<ComboBoxItem>()
                .FirstOrDefault(x => x.Tag?.ToString() == sig.MainSignalArticleNumber);
            if (mainSelected != null)
                MainSignalComboBox.SelectedItem = mainSelected;
            else if (MainSignalComboBox.Items.Count > 0)
            {
                MainSignalComboBox.SelectedIndex = 0;
                sig.MainSignalArticleNumber = (MainSignalComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? def.MainSignalArticleNumber;
            }

            DistantSignalComboBox.SelectionChanged -= OnDistantSignalSelected;
            DistantSignalComboBox.Items.Clear();
            foreach (var (articleNumber, displayName) in _viessmannSignalService.GetDistantSignalOptions(sig.MultiplexerArticleNumber))
            {
                var item = new ComboBoxItem { Content = displayName, Tag = articleNumber };
                DistantSignalComboBox.Items.Add(item);
            }
            DistantSignalComboBox.SelectionChanged += OnDistantSignalSelected;

            if (DistantSignalComboBox.Items.Count > 0)
            {
                var distantSelected = DistantSignalComboBox.Items.OfType<ComboBoxItem>()
                    .FirstOrDefault(x => x.Tag?.ToString() == sig.DistantSignalArticleNumber);
                if (distantSelected != null)
                    DistantSignalComboBox.SelectedItem = distantSelected;
                else
                {
                    DistantSignalComboBox.SelectedIndex = 0;
                    sig.DistantSignalArticleNumber = (DistantSignalComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating signal article ComboBoxes: {ex.Message}");
        }
    }

    private void OnMainSignalSelected(object sender, SelectionChangedEventArgs e)
    {
        if (SelectedElement is not SbSignal sig) return;
        if (MainSignalComboBox.SelectedItem is ComboBoxItem { Tag: string articleNumber })
        {
            sig.MainSignalArticleNumber = articleNumber;
            UpdateAvailableSignalAspects(sig);
        }
    }

    private void OnDistantSignalSelected(object sender, SelectionChangedEventArgs e)
    {
        if (SelectedElement is not SbSignal sig) return;
        if (DistantSignalComboBox.SelectedItem is ComboBoxItem { Tag: string articleNumber })
        {
            sig.DistantSignalArticleNumber = articleNumber;
            UpdateAvailableSignalAspects(sig);
        }
    }

    private void UpdateAspectButtons()
    {
        if (SelectedElement is not SbSignal sig) return;

        var accentBrush = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        var normalBrush = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"];

        AspectHp0Button.Background = sig.SignalAspect == SignalAspect.Hp0 ? accentBrush : normalBrush;
        AspectKs1Button.Background = sig.SignalAspect == SignalAspect.Ks1 ? accentBrush : normalBrush;
        AspectKs2Button.Background = sig.SignalAspect == SignalAspect.Ks2 ? accentBrush : normalBrush;
        AspectKs1BlinkButton.Background = sig.SignalAspect == SignalAspect.Ks1Blink ? accentBrush : normalBrush;
        AspectKennlichtButton.Background = sig.SignalAspect == SignalAspect.Kennlicht ? accentBrush : normalBrush;
        AspectDunkelButton.Background = sig.SignalAspect == SignalAspect.Dunkel ? accentBrush : normalBrush;
        AspectRa12Button.Background = sig.SignalAspect == SignalAspect.Ra12 ? accentBrush : normalBrush;
        AspectZs1Button.Background = sig.SignalAspect == SignalAspect.Zs1 ? accentBrush : normalBrush;
        AspectZs7Button.Background = sig.SignalAspect == SignalAspect.Zs7 ? accentBrush : normalBrush;
    }

    private void UpdateAspectPresentation(SbSignal? sig)
    {
        var is4046 = sig is not null && string.Equals(sig.MainSignalArticleNumber, "4046", StringComparison.Ordinal);
        var signalArticleNumber = is4046 ? "4046" : string.Empty;

        AspectHp0Signal.SignalArticleNumber = signalArticleNumber;
        AspectKs1Signal.SignalArticleNumber = signalArticleNumber;
        AspectKs2Signal.SignalArticleNumber = signalArticleNumber;
        AspectKs1BlinkSignal.SignalArticleNumber = signalArticleNumber;
        AspectKennlichtSignal.SignalArticleNumber = signalArticleNumber;
        AspectDunkelSignal.SignalArticleNumber = signalArticleNumber;
        AspectRa12Signal.SignalArticleNumber = signalArticleNumber;
        AspectZs1Signal.SignalArticleNumber = signalArticleNumber;
        AspectZs7Signal.SignalArticleNumber = signalArticleNumber;

        AspectHp0Signal.Aspect = nameof(SignalAspect.Hp0);
        AspectKs1Signal.Aspect = nameof(SignalAspect.Ks1);
        AspectKs2Signal.Aspect = nameof(SignalAspect.Ks2);
        AspectKs1BlinkSignal.Aspect = nameof(SignalAspect.Ks1Blink);
        AspectKennlichtSignal.Aspect = nameof(SignalAspect.Kennlicht);
        AspectDunkelSignal.Aspect = nameof(SignalAspect.Dunkel);
        AspectRa12Signal.Aspect = nameof(SignalAspect.Ra12);
        AspectZs1Signal.Aspect = nameof(SignalAspect.Zs1);
        AspectZs7Signal.Aspect = nameof(SignalAspect.Zs7);

        AspectHp0Label.Text = "Hp0";
        AspectKs1Label.Text = "Ks1";
        AspectKs2Label.Text = is4046 ? "Ks2+K" : "Ks2";
        AspectKs1BlinkLabel.Text = is4046 ? "Ks2+K+G" : "Ks1 Bl";
        AspectKennlichtLabel.Text = is4046 ? "K links" : "Kennl.";
        AspectDunkelLabel.Text = is4046 ? "GrBl+K+G" : "Dunkel";
        AspectRa12Label.Text = is4046 ? "Hp0+Rg" : "Ra12";
        AspectZs1Label.Text = is4046 ? "Ks1+G" : "Zs1";
        AspectZs7Label.Text = "Zs7";

        ToolTipService.SetToolTip(AspectHp0Button, "Hp 0 - Halt");
        ToolTipService.SetToolTip(AspectKs1Button, "Ks 1 - Fahrt");
        ToolTipService.SetToolTip(AspectKs2Button, is4046 ? "Ks 2 mit weißem Kennlicht oben links" : "Ks 2 - Halt erwarten");
        ToolTipService.SetToolTip(AspectKs1BlinkButton, is4046 ? "Ks 2 mit weißem Kennlicht oben links und Geschwindigkeitsanzeiger oben" : "Ks 1 blinkend - Fahrt mit Geschwindigkeitsvoranzeiger");
        ToolTipService.SetToolTip(AspectKennlichtButton, is4046 ? "Nur weißes Kennlicht oben links" : "Kennlicht - Signal betrieblich abgeschaltet");
        ToolTipService.SetToolTip(AspectDunkelButton, is4046 ? "Grün blinkend mit weißem Kennlicht oben links sowie Geschwindigkeitsanzeigern oben und unten" : "Dunkelschaltung - Signal nicht aktiv");
        ToolTipService.SetToolTip(AspectRa12Button, is4046 ? "Hp0 mit weißem Kennlicht unten für Rangierfahrten" : "Sh 1/Ra 12 - Rangierfahrt erlaubt");
        ToolTipService.SetToolTip(AspectZs1Button, is4046 ? "Ks 1 mit Geschwindigkeitsanzeiger oben" : "Zs 1 - Ersatzsignal (weiß blinkend)");
        ToolTipService.SetToolTip(AspectZs7Button, "Zs 7 - Vorsichtsignal (3x gelb)");
    }

    private void UpdateSwitchButtons()
    {
        if (SelectedElement is not SbSwitch sw) return;

        ThirdSwitchColumn.Width = new GridLength(1, GridUnitType.Star);
        SwitchRightButton.Visibility = Visibility.Visible;
        SwitchLeftButton.Visibility = Visibility.Visible;

        var accentStyle = (Style)Application.Current.Resources["AccentButtonStyle"];
        var defaultStyle = (Style)Application.Current.Resources["DefaultButtonStyle"];

        SwitchStraightButton.Style = sw.SwitchPosition == SwitchPosition.Straight ? accentStyle : defaultStyle;
        SwitchLeftButton.Style = sw.SwitchPosition == SwitchPosition.DivergingLeft ? accentStyle : defaultStyle;
        SwitchRightButton.Style = sw.SwitchPosition == SwitchPosition.DivergingRight ? accentStyle : defaultStyle;
    }

    private static string GetElementTypeName(SbElement element) => element switch
    {
        SbTrackStraight => "Gerades Gleis",
        SbTrackCurve => "Kurve 90 Grad",
        SbSwitch => "Weiche",
        SbSignal => "Signal",
        SbDetector => "Rückmelder",
        _ => "Unbekannt"
    };

    private void OnRotateClicked(object sender, RoutedEventArgs e)
    {
        if (SelectedElement == null || sender is not Button { Tag: string rotationStr }) return;

        if (int.TryParse(rotationStr, out var rotation))
        {
            SelectedElement.Rotation = rotation;
            RequestVisualRefresh?.Invoke(this, SelectedElement);
        }
    }

    private void OnAspectClicked(object sender, PointerRoutedEventArgs e)
    {
        if (SelectedElement is not SbSignal sig || sender is not Border { Tag: string aspectStr }) return;

        if (Enum.TryParse<SignalAspect>(aspectStr, out var aspect))
        {
            sig.SignalAspect = aspect;
            RequestVisualRefresh?.Invoke(this, sig);
            UpdateAspectButtons();
            _ = SetSignalAspectAutomaticallyAsync(sig);
        }
    }

    private async Task SetSignalAspectAutomaticallyAsync(SbSignal sig)
    {
        try
        {
            if (!sig.IsMultiplexed)
            {
                SetSignalStatusText.Visibility = Visibility.Collapsed;
                return;
            }

            if (string.IsNullOrEmpty(sig.MultiplexerArticleNumber))
            {
                SetSignalStatusText.Text = "⚠️ Multiplexer-Nummer nicht konfiguriert.";
                SetSignalStatusText.Visibility = Visibility.Visible;
                return;
            }

            if (sig.BaseAddress <= 0 || sig.BaseAddress > 2044)
            {
                SetSignalStatusText.Text = "⚠️ Basis-DCC-Adresse ungültig (1-2044).";
                SetSignalStatusText.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                if (!MultiplexerHelper.TryGetTurnoutCommand(
                        sig.MultiplexerArticleNumber,
                        sig.MainSignalArticleNumber,
                        sig.SignalAspect,
                        out var turnoutCommand))
                {
                    SetSignalStatusText.Text = "⚠️ Signalaspekt nicht unterstützt.";
                    SetSignalStatusText.Visibility = Visibility.Visible;
                    return;
                }

                sig.Address = sig.BaseAddress + turnoutCommand.AddressOffset + 1;
                sig.ExtendedAccessoryValue = turnoutCommand.Activate ? 1 : 0;
            }
            catch (ArgumentException ex)
            {
                SetSignalStatusText.Text = $"⚠️ Signalaspekt nicht unterstützt: {ex.Message}";
                SetSignalStatusText.Visibility = Visibility.Visible;
                return;
            }

            if (ViewModel == null)
            {
                SetSignalStatusText.Text = "❌ ViewModel nicht verfügbar.";
                SetSignalStatusText.Visibility = Visibility.Visible;
                return;
            }

            if (!ViewModel.IsConnected)
            {
                SetSignalStatusText.Text = ViewModel.StatusText;
                SetSignalStatusText.Visibility = Visibility.Visible;
                return;
            }

            SetSignalStatusText.Text = "⏳ Signal wird gestellt...";
            SetSignalStatusText.Visibility = Visibility.Visible;

            await ViewModel.SetSignalAspectAsync(sig).ConfigureAwait(false);

            DispatcherQueue.TryEnqueue(() =>
            {
                if (MultiplexerHelper.TryGetTurnoutCommand(
                        sig.MultiplexerArticleNumber!,
                        sig.MainSignalArticleNumber,
                        sig.SignalAspect,
                        out var cmd))
                {
                    SetSignalStatusText.Text =
                        $"Signal: {sig.SignalAspect}\n" +
                        $"DCC-Adresse: {sig.Address}, Ausgang: {cmd.Output}, Activate: {(cmd.Activate ? "Ja" : "Nein")}";
                }
                else
                {
                    SetSignalStatusText.Text = $"Signal gesetzt: {sig.SignalAspect}";
                }
            });
        }
        catch (Exception ex)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                SetSignalStatusText.Text = $"❌ Fehler: {ex.Message}";
                SetSignalStatusText.Visibility = Visibility.Visible;
            });
        }
    }

    private void OnSwitchPositionClicked(object sender, RoutedEventArgs e)
    {
        if (SelectedElement is not SbSwitch sw || sender is not Button { Tag: string positionStr }) return;

        sw.SwitchPosition = positionStr switch
        {
            "Straight" => SwitchPosition.Straight,
            "DivergingLeft" => SwitchPosition.DivergingLeft,
            "DivergingRight" => SwitchPosition.DivergingRight,
            _ => SwitchPosition.Straight
        };

        RequestVisualRefresh?.Invoke(this, sw);
        UpdateSwitchButtons();
    }

    private void OnAddressChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (SelectedElement == null || double.IsNaN(args.NewValue)) return;

        if (SelectedElement is SbSwitch sw)
            sw.Address = (int)args.NewValue;
        else if (SelectedElement is SbSignal sig)
            sig.Address = (int)args.NewValue;
        else if (SelectedElement is SbDetector det)
            det.FeedbackAddress = (int)args.NewValue;
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
        if (SelectedElement is not SbSignal sig) return;

        var selectedItem = MultiplexerComboBox.SelectedItem as ComboBoxItem;
        if (selectedItem?.Tag is string articleNumber)
        {
            sig.MultiplexerArticleNumber = articleNumber;
            sig.IsMultiplexed = true;
            UpdateSignalArticleComboBoxes(sig);
            UpdateAvailableSignalAspects(sig);
        }
        else
        {
            sig.MultiplexerArticleNumber = string.Empty;
            sig.IsMultiplexed = false;
            MainSignalComboBox.Items.Clear();
            DistantSignalComboBox.Items.Clear();
            SetAllAspectButtonsVisibility(Visibility.Visible);
        }
    }

    private void OnBaseAddressChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (SelectedElement is not SbSignal sig || double.IsNaN(args.NewValue)) return;
        sig.BaseAddress = (int)args.NewValue;
    }
}
