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

        // Update element info (type-based)
        ElementTypeText.Text = GetElementTypeName(SelectedElement);
        ElementPositionText.Text = $"({SelectedElement.X}, {SelectedElement.Y})";
        ElementIdText.Text = SelectedElement.Id.ToString()[..8];

        // Address (only for SbSwitch and SbSignal)
        if (SelectedElement is SbSwitchViewModel sw)
        {
            ElementAddressBox.Value = sw.Address;
            ElementAddressBox.Visibility = Visibility.Visible;
        }
        else if (SelectedElement is SbSignalViewModel sig)
        {
            ElementAddressBox.Value = sig.Address;
            ElementAddressBox.Visibility = Visibility.Visible;
        }
        else if (SelectedElement is SbDetectorViewModel det)
        {
            ElementAddressBox.Value = det.FeedbackAddress;
            ElementAddressBox.Visibility = Visibility.Visible;
        }
        else
        {
            ElementAddressBox.Visibility = Visibility.Collapsed;
        }

        // Signal panel visibility
        SignalAspectPanel.Visibility = SelectedElement is SbSignalViewModel ? Visibility.Visible : Visibility.Collapsed;

        // Update SignalPreview with current aspect
        if (SelectedElement is SbSignalViewModel signalVm)
        {
            SignalPreview.Aspect = signalVm.SignalAspect.ToString();
        }

        // Switch panel visibility
        SwitchPositionPanel.Visibility = SelectedElement is SbSwitchViewModel ? Visibility.Visible : Visibility.Collapsed;

        // Update aspect buttons
        UpdateAspectButtons();

        // Update switch buttons
        if (SelectedElement is SbSwitchViewModel)
        {
            UpdateSwitchButtons();
        }
    }

    private void UpdateAspectButtons()
    {
        if (SelectedElement is not SbSignalViewModel sig) return;

        var accentBrush = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        var normalBrush = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"];

        AspectHp0Button.Background = sig.SignalAspect == Domain.SignalAspect.Hp0 ? accentBrush : normalBrush;
        AspectKs1Button.Background = sig.SignalAspect == Domain.SignalAspect.Ks1 ? accentBrush : normalBrush;
        AspectKs2Button.Background = sig.SignalAspect == Domain.SignalAspect.Ks2 ? accentBrush : normalBrush;
        AspectKs1BlinkButton.Background = sig.SignalAspect == Domain.SignalAspect.Ks1Blink ? accentBrush : normalBrush;
        AspectKennlichtButton.Background = sig.SignalAspect == Domain.SignalAspect.Kennlicht ? accentBrush : normalBrush;
        AspectDunkelButton.Background = sig.SignalAspect == Domain.SignalAspect.Dunkel ? accentBrush : normalBrush;
        AspectRa12Button.Background = sig.SignalAspect == Domain.SignalAspect.Ra12 ? accentBrush : normalBrush;
        AspectZs1Button.Background = sig.SignalAspect == Domain.SignalAspect.Zs1 ? accentBrush : normalBrush;
        AspectZs7Button.Background = sig.SignalAspect == Domain.SignalAspect.Zs7 ? accentBrush : normalBrush;

        // Signal type is now fixed (SbSignal), ComboBox can be simplified
        SignalTypeComboBox.SelectedIndex = 0;
    }

    private void UpdateSwitchButtons()
    {
        if (SelectedElement is not SbSwitchViewModel sw) return;

        // All switches support all three positions now
        ThirdSwitchColumn.Width = new GridLength(1, GridUnitType.Star);
        SwitchRightButton.Visibility = Visibility.Visible;
        SwitchLeftButton.Visibility = Visibility.Visible;

        // Highlight active button
        var accentStyle = (Style)Application.Current.Resources["AccentButtonStyle"];
        var defaultStyle = (Style)Application.Current.Resources["DefaultButtonStyle"];

        SwitchStraightButton.Style = sw.SwitchPosition == Domain.SwitchPosition.Straight ? accentStyle : defaultStyle;
        SwitchLeftButton.Style = sw.SwitchPosition == Domain.SwitchPosition.DivergingLeft ? accentStyle : defaultStyle;
        SwitchRightButton.Style = sw.SwitchPosition == Domain.SwitchPosition.DivergingRight ? accentStyle : defaultStyle;
    }

    private static string GetElementTypeName(SbElementViewModel element) => element switch
    {
        SbTrackStraightViewModel => "Gerades Gleis",
        SbTrackCurveViewModel => "Kurve 90 Grad",
        SbSwitchViewModel => "Weiche",
        SbSignalViewModel => "Signal",
        SbDetectorViewModel => "Rückmelder",
        _ => "Unbekannt"
    };

    private void UpdateStatistics()
    {
        if (_planViewModel == null) return;
        TrackCountText.Text = _planViewModel.Elements.Count(e => e is SbTrackStraightViewModel or SbTrackCurveViewModel).ToString();
        SwitchCountText.Text = _planViewModel.Elements.OfType<SbSwitchViewModel>().Count().ToString();
        SignalCountText.Text = _planViewModel.Elements.OfType<SbSignalViewModel>().Count().ToString();
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
        if (SelectedElement is not SbSignalViewModel sig || sender is not Border { Tag: string aspectStr }) return;

        if (System.Enum.TryParse<Domain.SignalAspect>(aspectStr, out var aspect))
        {
            _blinkingLeds.Clear();
            sig.SignalAspect = aspect;

            // Update preview signal
            SignalPreview.Aspect = aspectStr;

            // Update canvas element
            RefreshElementVisual(sig);
            UpdateAspectButtons();
        }
    }

    private void OnSwitchPositionClicked(object sender, RoutedEventArgs e)
    {
        if (SelectedElement is not SbSwitchViewModel sw || sender is not Button { Tag: string positionStr }) return;

        sw.SwitchPosition = positionStr switch
        {
            "Straight" => Domain.SwitchPosition.Straight,
            "DivergingLeft" => Domain.SwitchPosition.DivergingLeft,
            "DivergingRight" => Domain.SwitchPosition.DivergingRight,
            _ => Domain.SwitchPosition.Straight
        };

        RefreshElementVisual(sw);
        UpdateSwitchButtons();
    }

    private void OnAddressChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (SelectedElement == null || double.IsNaN(args.NewValue)) return;

        if (SelectedElement is SbSwitchViewModel sw)
            sw.Address = (int)args.NewValue;
        else if (SelectedElement is SbSignalViewModel sig)
            sig.Address = (int)args.NewValue;
        else if (SelectedElement is SbDetectorViewModel det)
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

    #endregion
}
