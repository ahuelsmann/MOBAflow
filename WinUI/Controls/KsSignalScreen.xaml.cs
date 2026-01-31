namespace Moba.WinUI.Controls;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

/// <summary>
/// KsSignalScreen - German Ks signal system display control.
/// Shows signal aspect with realistic LED arrangement per German railway standards.
/// Supports all Ks signal aspects: Hp0, Ks1, Ks2, Ks1Blink, Kennlicht, Dunkel, Ra12, Zs1, Zs7.
/// Automatically blinks for Ks1Blink and Zs1 aspects.
/// </summary>
public sealed partial class KsSignalScreen : UserControl
{
    private static readonly SolidColorBrush OffColor = new(Color.FromArgb(60, 64, 64, 64));
    private static readonly SolidColorBrush RedOn = new(Color.FromArgb(255, 255, 0, 0));
    private static readonly SolidColorBrush GreenOn = new(Color.FromArgb(255, 0, 200, 0));
    private static readonly SolidColorBrush YellowOn = new(Color.FromArgb(255, 255, 200, 0));
    private static readonly SolidColorBrush WhiteOn = new(Colors.White);

    private DispatcherTimer? _blinkTimer;
    private bool _blinkState;

    public static readonly DependencyProperty AspectProperty = DependencyProperty.Register(
        nameof(Aspect),
        typeof(string),
        typeof(KsSignalScreen),
        new PropertyMetadata("Hp0", OnAspectChanged));

    public string Aspect
    {
        get => (string)GetValue(AspectProperty);
        set => SetValue(AspectProperty, value);
    }

    public KsSignalScreen()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateAspect();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        StopBlinking();
    }

    private static void OnAspectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KsSignalScreen screen)
        {
            screen.UpdateAspect();
        }
    }

    private void UpdateAspect()
    {
        StopBlinking();

        // Default: all LEDs off
        TopWhiteLight.Fill = OffColor;
        RedLight.Fill = OffColor;
        GreenLight.Fill = OffColor;
        YellowLight.Fill = OffColor;
        Zs7Left.Fill = OffColor;
        Zs7Center.Fill = OffColor;
        Zs7Right.Fill = OffColor;
        Ra12Left.Fill = OffColor;
        Ra12Right.Fill = OffColor;
        BottomWhiteLight.Fill = OffColor;

        switch (Aspect)
        {
            case "Hp0":
                RedLight.Fill = RedOn;
                break;
            case "Ks1":
                GreenLight.Fill = GreenOn;
                break;
            case "Ks2":
                YellowLight.Fill = YellowOn;
                break;
            case "Ks1Blink":
                GreenLight.Fill = GreenOn;
                BottomWhiteLight.Fill = WhiteOn;
                StartBlinking(GreenLight, GreenOn);
                break;
            case "Kennlicht":
                TopWhiteLight.Fill = WhiteOn;
                break;
            case "Dunkel":
                // All off - already set above
                break;
            case "Ra12":
                Ra12Left.Fill = WhiteOn;
                Ra12Right.Fill = WhiteOn;
                break;
            case "Zs1":
                TopWhiteLight.Fill = WhiteOn;
                StartBlinking(TopWhiteLight, WhiteOn);
                break;
            case "Zs7":
                Zs7Left.Fill = YellowOn;
                Zs7Center.Fill = YellowOn;
                Zs7Right.Fill = YellowOn;
                break;
        }
    }

    private void StartBlinking(Ellipse led, SolidColorBrush onColor)
    {
        _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _blinkTimer.Tick += (s, e) =>
        {
            _blinkState = !_blinkState;
            led.Fill = _blinkState ? onColor : OffColor;
        };
        _blinkTimer.Start();
    }

    private void StopBlinking()
    {
        _blinkTimer?.Stop();
        _blinkTimer = null;
        _blinkState = false;
    }
}
