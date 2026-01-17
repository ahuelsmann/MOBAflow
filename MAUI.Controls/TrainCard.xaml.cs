namespace Moba.MAUI.Controls;

/// <summary>
/// Represents a train control card displaying locomotive information.
/// </summary>
/// <remarks>
/// This .NET MAUI content view provides a visual representation of a train
/// with its current speed and direction. It can be used in the Android mobile
/// application to display train status information.
/// </remarks>
public partial class TrainCard : ContentView
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TrainCard"/> class.
    /// </summary>
    public TrainCard()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Identifies the <see cref="TrainName"/> bindable property.
    /// </summary>
    public static readonly BindableProperty TrainNameProperty =
        BindableProperty.Create(
            nameof(TrainName),
            typeof(string),
            typeof(TrainCard),
            string.Empty);

    /// <summary>
    /// Gets or sets the name of the train.
    /// </summary>
    /// <value>The train's display name. Default is an empty string.</value>
    public string TrainName
    {
        get => (string)GetValue(TrainNameProperty);
        set => SetValue(TrainNameProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="Speed"/> bindable property.
    /// </summary>
    public static readonly BindableProperty SpeedProperty =
        BindableProperty.Create(
            nameof(Speed),
            typeof(int),
            typeof(TrainCard),
            0);

    /// <summary>
    /// Gets or sets the current speed of the train.
    /// </summary>
    /// <value>The train's speed value. Default is 0.</value>
    public int Speed
    {
        get => (int)GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="IsForward"/> bindable property.
    /// </summary>
    public static readonly BindableProperty IsForwardProperty =
        BindableProperty.Create(
            nameof(IsForward),
            typeof(bool),
            typeof(TrainCard),
            true);

    /// <summary>
    /// Gets or sets a value indicating whether the train is moving forward.
    /// </summary>
    /// <value><c>true</c> if the train is moving forward; otherwise, <c>false</c>. Default is <c>true</c>.</value>
    public bool IsForward
    {
        get => (bool)GetValue(IsForwardProperty);
        set => SetValue(IsForwardProperty, value);
    }
}
