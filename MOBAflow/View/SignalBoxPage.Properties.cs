namespace Moba.WinUI.View;

using Common.Multiplex;

using Domain;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using System.Diagnostics;

/// <summary>
/// Partial class containing properties panel logic and event handlers.
/// </summary>
sealed partial class SignalBoxPage
{
    #region Properties Panel

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

        // Update element info (type-based)
        ElementTypeText.Text = GetElementTypeName(SelectedElement);
        ElementPositionText.Text = $"({SelectedElement.X}, {SelectedElement.Y})";
        ElementIdText.Text = SelectedElement.Id.ToString()[..8];

        // Address field - only show for switches, detectors (NOT for signals with multiplex)
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
            // Signals use BaseAddress in multiplex panel - hide this address field
            AddressPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            AddressPanel.Visibility = Visibility.Collapsed;
        }

        // Signal panel visibility
        SignalAspectPanel.Visibility = SelectedElement is SbSignal ? Visibility.Visible : Visibility.Collapsed;

        // Multiplex configuration panel (only for signals)
        UpdateMultiplexConfigPanel();

        // Switch panel visibility
        SwitchPositionPanel.Visibility = SelectedElement is SbSwitch ? Visibility.Visible : Visibility.Collapsed;

        // Update aspect buttons
        UpdateAspectButtons();

        // Update switch buttons
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

        // Show multiplex panel for all signals
        MultiplexConfigPanel.Visibility = Visibility.Visible;

        // Populate multiplexer ComboBox with available options
        if (MultiplexerComboBox.Items.Count == 0)
        {
            // Temporarily detach handler to prevent event-loop during initialization
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

            // Re-attach handler after population
            MultiplexerComboBox.SelectionChanged += OnMultiplexerSelected;
        }

        // Select current multiplexer
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

        // Populate main signal and distant signal ComboBoxes
        UpdateSignalArticleComboBoxes(sig);

        // Update base address
        BaseAddressBox.Value = sig.BaseAddress > 0 ? sig.BaseAddress : 1;

        // Update available signal aspects based on multiplexer
        UpdateAvailableSignalAspects(sig);
    }

    /// <summary>
    /// Shows/hides signal aspect buttons based on what the selected multiplexer supports.
    /// </summary>
    private void UpdateAvailableSignalAspects(SbSignal sig)
    {
        if (string.IsNullOrEmpty(sig.MultiplexerArticleNumber))
        {
            // No multiplexer selected - show all aspects (default behavior)
            SetAllAspectButtonsVisibility(Visibility.Visible);
            return;
        }

        try
        {
            var supportedAspects = MultiplexerHelper.GetSupportedAspects(
                sig.MultiplexerArticleNumber,
                sig.MainSignalArticleNumber);

            // Show/hide aspect buttons based on multiplexer support
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
            // Unknown multiplexer - show all aspects
            SetAllAspectButtonsVisibility(Visibility.Visible);
        }
    }

    /// <summary>
    /// Helper method to set visibility for all aspect buttons.
    /// </summary>
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

            // Hauptsignal: aus Stammdaten (data.json) oder MultiplexerHelper-Fallback
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

            // Vorsignal: aus Stammdaten (data.json) oder MultiplexerHelper-Fallback
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

    private void UpdateSwitchButtons()
    {
        if (SelectedElement is not SbSwitch sw) return;

        // All switches support all three positions now
        ThirdSwitchColumn.Width = new GridLength(1, GridUnitType.Star);
        SwitchRightButton.Visibility = Visibility.Visible;
        SwitchLeftButton.Visibility = Visibility.Visible;

        // Highlight active button
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

    private void UpdateStatistics()
    {
        if (_planViewModel == null) return;
        TrackCountText.Text = _planViewModel.Elements.Count(e => e is SbTrackStraight or SbTrackCurve).ToString();
        SwitchCountText.Text = _planViewModel.Elements.OfType<SbSwitch>().Count().ToString();
        SignalCountText.Text = _planViewModel.Elements.OfType<SbSignal>().Count().ToString();
    }

    #endregion

    #region Property Panel Event Handlers

    private void OnRotateClicked(object sender, RoutedEventArgs e)
    {
        if (SelectedElement == null || sender is not Button { Tag: string rotationStr }) return;

        if (int.TryParse(rotationStr, out var rotation))
        {
            SelectedElement.Rotation = rotation;
            RefreshElementVisual(SelectedElement);
        }
    }

    private void OnAspectClicked(object sender, PointerRoutedEventArgs e)
    {
        if (SelectedElement is not SbSignal sig || sender is not Border { Tag: string aspectStr }) return;

        if (Enum.TryParse<SignalAspect>(aspectStr, out var aspect))
        {
            _blinkingLeds.Clear();
            sig.SignalAspect = aspect;

            // Update canvas element
            RefreshElementVisual(sig);
            UpdateAspectButtons();

            // Automatically send signal command to Z21 if multiplex is configured
            _ = SetSignalAspectAutomaticallyAsync(sig);
        }
    }

    /// <summary>
    /// Automatically sends signal command to Z21 after aspect selection.
    /// Same logic as OnSetSignalAspectClicked but called automatically.
    /// </summary>
    private async Task SetSignalAspectAutomaticallyAsync(SbSignal sig)
    {
        try
        {
            Debug.WriteLine($"[AUTO-SIGNAL] Attempting to set signal aspect: {sig.SignalAspect}");

            // Validate configuration
            if (!sig.IsMultiplexed)
            {
                Debug.WriteLine("[AUTO-SIGNAL] Signal is not multiplexed - skipping auto-set");
                SetSignalStatusText.Visibility = Visibility.Collapsed;
                return;
            }

            if (string.IsNullOrEmpty(sig.MultiplexerArticleNumber))
            {
                Debug.WriteLine("[AUTO-SIGNAL] ERROR: No multiplexer configured");
                SetSignalStatusText.Text = "⚠️ Multiplexer-Nummer nicht konfiguriert.";
                SetSignalStatusText.Visibility = Visibility.Visible;
                return;
            }

            if (sig.BaseAddress <= 0 || sig.BaseAddress > 2044)
            {
                Debug.WriteLine($"[AUTO-SIGNAL] ERROR: Invalid base address: {sig.BaseAddress}");
                SetSignalStatusText.Text = "⚠️ Basis-DCC-Adresse ungültig (1-2044).";
                SetSignalStatusText.Visibility = Visibility.Visible;
                return;
            }

            Debug.WriteLine($"[AUTO-SIGNAL] Configuration valid: Multiplexer={sig.MultiplexerArticleNumber}, BaseAddress={sig.BaseAddress}");

            // Calculate DCC address and activation from base address and current aspect
            try
            {
                if (!MultiplexerHelper.TryGetTurnoutCommand(
                        sig.MultiplexerArticleNumber,
                        sig.MainSignalArticleNumber,
                        sig.SignalAspect,
                        out var turnoutCommand))
                {
                    Debug.WriteLine("[AUTO-SIGNAL] ERROR: Aspect not supported by multiplexer mapping");
                    SetSignalStatusText.Text = "⚠️ Signalaspekt nicht unterstützt.";
                    SetSignalStatusText.Visibility = Visibility.Visible;
                    return;
                }

                sig.Address = sig.BaseAddress + turnoutCommand.AddressOffset;
                sig.ExtendedAccessoryValue = turnoutCommand.Activate ? 1 : 0;

                Debug.WriteLine($"[AUTO-SIGNAL] Calculated: DCC_Address={sig.Address}, Output={turnoutCommand.Output}, Activate={turnoutCommand.Activate}");
            }
            catch (ArgumentException ex)
            {
                Debug.WriteLine($"[AUTO-SIGNAL] ERROR: Aspect calculation failed: {ex.Message}");
                SetSignalStatusText.Text = $"⚠️ Signalaspekt nicht unterstützt: {ex.Message}";
                SetSignalStatusText.Visibility = Visibility.Visible;
                return;
            }

            // Get MainWindowViewModel from the page's DataContext
            if (ViewModel is not { } mainVm)
            {
                Debug.WriteLine("[AUTO-SIGNAL] ERROR: ViewModel is null or wrong type");
                SetSignalStatusText.Text = "❌ ViewModel nicht verfügbar.";
                SetSignalStatusText.Visibility = Visibility.Visible;
                return;
            }

            if (!mainVm.IsConnected)
            {
                Debug.WriteLine("[AUTO-SIGNAL] ERROR: Z21 not connected");
                SetSignalStatusText.Text = mainVm.StatusText;
                SetSignalStatusText.Visibility = Visibility.Visible;
                return;
            }

            Debug.WriteLine($"[AUTO-SIGNAL] Calling MainWindowViewModel.SetSignalAspectAsync...");
            SetSignalStatusText.Text = $"⏳ Signal wird gestellt...";
            SetSignalStatusText.Visibility = Visibility.Visible;

            // Call the ViewModel method to send the signal command
            await mainVm.SetSignalAspectAsync(sig).ConfigureAwait(false);

            Debug.WriteLine("[AUTO-SIGNAL] SUCCESS: Signal set");
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
            Debug.WriteLine($"[AUTO-SIGNAL] EXCEPTION: {ex.Message}");
            Debug.WriteLine($"[AUTO-SIGNAL] StackTrace: {ex.StackTrace}");
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

        RefreshElementVisual(sw);
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
        DeleteSelectedElement();
    }

    private void OnSignalTypeChanged(object sender, SelectionChangedEventArgs e)
    {
        // Signal type changes are no longer supported - SbSignal is a single type
        // This method is kept for XAML binding compatibility
    }

    private void OnMultiplexerSelected(object sender, SelectionChangedEventArgs e)
    {
        if (SelectedElement is not SbSignal sig) return;

        var selectedItem = MultiplexerComboBox.SelectedItem as ComboBoxItem;
        if (selectedItem?.Tag is string articleNumber)
        {
            // Multiplexer selected → automatically enable multiplex mode
            sig.MultiplexerArticleNumber = articleNumber;
            sig.IsMultiplexed = true;

            // Update signal article ComboBoxes with compatible signals
            UpdateSignalArticleComboBoxes(sig);

            // Update available signal aspects based on multiplexer capabilities
            UpdateAvailableSignalAspects(sig);
        }
        else
        {
            // No multiplexer selected → disable multiplex mode
            sig.MultiplexerArticleNumber = string.Empty;
            sig.IsMultiplexed = false;

            // Clear signal article ComboBoxes
            MainSignalComboBox.Items.Clear();
            DistantSignalComboBox.Items.Clear();

            // Show all aspects when no multiplexer is selected
            SetAllAspectButtonsVisibility(Visibility.Visible);
        }
    }

    private void OnBaseAddressChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (SelectedElement is not SbSignal sig || double.IsNaN(args.NewValue)) return;
        sig.BaseAddress = (int)args.NewValue;
    }

    #endregion
}
