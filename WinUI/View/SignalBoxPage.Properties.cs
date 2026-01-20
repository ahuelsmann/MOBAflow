namespace Moba.WinUI.View;

using Domain;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using SharedUI.ViewModel;

using System.Linq;

/// <summary>
/// Partial class containing properties panel logic and event handlers.
/// </summary>
public sealed partial class SignalBoxPage
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

        // Update element info
        ElementTypeText.Text = GetElementTypeName(SelectedElement.Symbol);
        ElementPositionText.Text = $"({SelectedElement.X}, {SelectedElement.Y})";
        ElementIdText.Text = SelectedElement.Id.ToString()[..8];

        // Address
        ElementAddressBox.Value = SelectedElement.Address;

        // Signal panel visibility
        SignalAspectPanel.Visibility = SignalBoxPlanViewModel.IsSignalSymbol(SelectedElement.Symbol) ? Visibility.Visible : Visibility.Collapsed;

        // Update SignalPreview with current aspect
        if (SignalBoxPlanViewModel.IsSignalSymbol(SelectedElement.Symbol))
        {
            SignalPreview.Aspect = SelectedElement.SignalAspect.ToString();
        }

        // Switch panel visibility
        SwitchPositionPanel.Visibility = SignalBoxPlanViewModel.IsSwitchSymbol(SelectedElement.Symbol) ? Visibility.Visible : Visibility.Collapsed;

        // Update aspect buttons
        UpdateAspectButtons();

        // Update switch buttons
        if (SignalBoxPlanViewModel.IsSwitchSymbol(SelectedElement.Symbol))
        {
            UpdateSwitchButtons();
        }
    }

    private void UpdateAspectButtons()
    {
        if (SelectedElement == null) return;

        var accentBrush = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        var normalBrush = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"];

        AspectHp0Button.Background = SelectedElement.SignalAspect == Domain.SignalAspect.Hp0 ? accentBrush : normalBrush;
        AspectKs1Button.Background = SelectedElement.SignalAspect == Domain.SignalAspect.Ks1 ? accentBrush : normalBrush;
        AspectKs2Button.Background = SelectedElement.SignalAspect == Domain.SignalAspect.Ks2 ? accentBrush : normalBrush;
        AspectKs1BlinkButton.Background = SelectedElement.SignalAspect == Domain.SignalAspect.Ks1Blink ? accentBrush : normalBrush;
        AspectKennlichtButton.Background = SelectedElement.SignalAspect == Domain.SignalAspect.Kennlicht ? accentBrush : normalBrush;
        AspectDunkelButton.Background = SelectedElement.SignalAspect == Domain.SignalAspect.Dunkel ? accentBrush : normalBrush;
        AspectRa12Button.Background = SelectedElement.SignalAspect == Domain.SignalAspect.Ra12 ? accentBrush : normalBrush;
        AspectZs1Button.Background = SelectedElement.SignalAspect == Domain.SignalAspect.Zs1 ? accentBrush : normalBrush;
        AspectZs7Button.Background = SelectedElement.SignalAspect == Domain.SignalAspect.Zs7 ? accentBrush : normalBrush;

        // Update SignalType ComboBox
        SignalTypeComboBox.SelectedIndex = SelectedElement.Symbol switch
        {
            SignalBoxSymbol.SignalKsMain => 0,
            SignalBoxSymbol.SignalKsDistant => 1,
            SignalBoxSymbol.SignalKsCombined => 2,
            SignalBoxSymbol.SignalSh or SignalBoxSymbol.SignalDwarf => 3,
            SignalBoxSymbol.SignalBlock => 4,
            _ => 0
        };
    }

    private void UpdateSwitchButtons()
    {
        if (SelectedElement == null || !SignalBoxPlanViewModel.IsSwitchSymbol(SelectedElement.Symbol)) return;

        var isThreeWay = SelectedElement.Symbol == SignalBoxSymbol.SwitchThreeWay;
        var isLeftSwitch = SelectedElement.Symbol == SignalBoxSymbol.SwitchSimpleLeft;

        // Third column only for three-way switch or left/right switch
        ThirdSwitchColumn.Width = isThreeWay ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        SwitchRightButton.Visibility = isThreeWay || !isLeftSwitch ? Visibility.Visible : Visibility.Collapsed;
        SwitchLeftButton.Visibility = isThreeWay || isLeftSwitch ? Visibility.Visible : Visibility.Collapsed;

        // Highlight active button
        var accentStyle = (Style)Application.Current.Resources["AccentButtonStyle"];
        var defaultStyle = (Style)Application.Current.Resources["DefaultButtonStyle"];

        SwitchStraightButton.Style = SelectedElement.SwitchPosition == Domain.SwitchPosition.Straight ? accentStyle : defaultStyle;
        SwitchLeftButton.Style = SelectedElement.SwitchPosition == Domain.SwitchPosition.DivergingLeft ? accentStyle : defaultStyle;
        SwitchRightButton.Style = SelectedElement.SwitchPosition == Domain.SwitchPosition.DivergingRight ? accentStyle : defaultStyle;
    }

    private static string GetElementTypeName(SignalBoxSymbol symbol) => symbol switch
    {
        SignalBoxSymbol.TrackStraight => "Gerades Gleis",
        SignalBoxSymbol.TrackCurve45 => "Kurve 45 Grad",
        SignalBoxSymbol.TrackCurve90 => "Kurve 90 Grad",
        SignalBoxSymbol.TrackEnd => "Prellbock",
        SignalBoxSymbol.SwitchSimpleLeft => "Weiche Links",
        SignalBoxSymbol.SwitchSimpleRight => "Weiche Rechts",
        SignalBoxSymbol.SwitchThreeWay => "Dreiwege-Weiche",
        SignalBoxSymbol.SwitchDoubleSlip => "Doppelkreuzungsweiche",
        SignalBoxSymbol.TrackCrossing => "Kreuzung",
        SignalBoxSymbol.SignalKsMain => "Ks-Hauptsignal",
        SignalBoxSymbol.SignalKsDistant => "Ks-Vorsignal",
        SignalBoxSymbol.SignalSh or SignalBoxSymbol.SignalDwarf => "Rangiersignal",
        SignalBoxSymbol.SignalBlock => "Gleissperrsignal",
        SignalBoxSymbol.SignalKsCombined => "Ks-Signalschirm",
        SignalBoxSymbol.Platform => "Bahnsteig",
        SignalBoxSymbol.Detector or SignalBoxSymbol.AxleCounter => "Rueckmelder",
        _ => "Unbekannt"
    };

    private void UpdateStatistics()
    {
        if (_planViewModel == null) return;
        TrackCountText.Text = _planViewModel.Elements.Count(e => IsTrackSymbol(e.Symbol)).ToString();
        SwitchCountText.Text = _planViewModel.Elements.Count(e => SignalBoxPlanViewModel.IsSwitchSymbol(e.Symbol)).ToString();
        SignalCountText.Text = _planViewModel.Elements.Count(e => SignalBoxPlanViewModel.IsSignalSymbol(e.Symbol)).ToString();
    }

    private static bool IsTrackSymbol(SignalBoxSymbol symbol) =>
        symbol is SignalBoxSymbol.TrackStraight or SignalBoxSymbol.TrackCurve45 or SignalBoxSymbol.TrackCurve90 or SignalBoxSymbol.TrackEnd or SignalBoxSymbol.TrackCrossing;

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
        if (SelectedElement == null || sender is not Border { Tag: string aspectStr }) return;

        if (System.Enum.TryParse<Domain.SignalAspect>(aspectStr, out var aspect))
        {
            _blinkingLeds.Clear();
            SelectedElement.SignalAspect = aspect;

            // Update preview signal
            SignalPreview.Aspect = aspectStr;

            // Update canvas element
            RefreshElementVisual(SelectedElement);
            UpdateAspectButtons();
        }
    }

    private void OnSwitchPositionClicked(object sender, RoutedEventArgs e)
    {
        if (SelectedElement == null || sender is not Button { Tag: string positionStr }) return;

            SelectedElement.SwitchPosition = positionStr switch
            {
                "Straight" => Domain.SwitchPosition.Straight,
                "DivergingLeft" => Domain.SwitchPosition.DivergingLeft,
                "DivergingRight" => Domain.SwitchPosition.DivergingRight,
                _ => Domain.SwitchPosition.Straight
            };

            RefreshElementVisual(SelectedElement);
            UpdateSwitchButtons();
        }

        private void OnAddressChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (SelectedElement == null || double.IsNaN(args.NewValue)) return;
            SelectedElement.Address = (int)args.NewValue;
        }

        private void OnDeleteElementClicked(object sender, RoutedEventArgs e)
        {
            DeleteSelectedElement();
        }

        private void OnSignalTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedElement == null || SignalTypeComboBox.SelectedItem is not ComboBoxItem { Tag: string typeTag })
                return;

            var newSymbol = typeTag switch
            {
                "KsMain" => SignalBoxSymbol.SignalKsMain,
                "KsDistant" => SignalBoxSymbol.SignalKsDistant,
                "KsMulti" => SignalBoxSymbol.SignalKsCombined,
                "Shunt" => SignalBoxSymbol.SignalSh,
                "Block" => SignalBoxSymbol.SignalBlock,
                _ => SelectedElement.Symbol
            };

            if (newSymbol != SelectedElement.Symbol)
            {
                SelectedElement.Symbol = newSymbol;
                ElementTypeText.Text = GetElementTypeName(newSymbol);
                RefreshElementVisual(SelectedElement);
                UpdateStatistics();
            }
        }

        #endregion
    }
