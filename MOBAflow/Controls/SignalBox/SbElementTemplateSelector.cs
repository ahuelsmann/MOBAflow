namespace Moba.WinUI.Controls.SignalBox;

using Domain;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

public class SbElementTemplateSelector : DataTemplateSelector
{
    public DataTemplate? StraightTrackTemplate { get; set; }
    public DataTemplate? CurveTrackTemplate { get; set; }
    public DataTemplate? SwitchTemplate { get; set; }
    public DataTemplate? SignalTemplate { get; set; }
    public DataTemplate? DetectorTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            SbTrackStraight => StraightTrackTemplate ?? base.SelectTemplateCore(item, container),
            SbTrackCurve => CurveTrackTemplate ?? base.SelectTemplateCore(item, container),
            SbSwitch => SwitchTemplate ?? base.SelectTemplateCore(item, container),
            SbSignal => SignalTemplate ?? base.SelectTemplateCore(item, container),
            SbDetector => DetectorTemplate ?? base.SelectTemplateCore(item, container),
            _ => base.SelectTemplateCore(item, container)
        };
    }
}
