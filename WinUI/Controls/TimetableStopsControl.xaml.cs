// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

/// <summary>
/// Displays timetable stops for current journey (Previous/Current/Next station).
/// Used in TrainControlPage to show journey progress information.
/// </summary>
public sealed partial class TimetableStopsControl : UserControl
{
    private const string StationPlaceholder = "\u2014";

    /// <summary>
    /// Previous station name.
    /// </summary>
    public static readonly DependencyProperty PreviousStationNameValueProperty =
        DependencyProperty.Register("PreviousStationNameValue", typeof(string), typeof(TimetableStopsControl),
            new PropertyMetadata(StationPlaceholder));

    /// <summary>
    /// Previous station arrival time (formatted string).
    /// </summary>
    public static readonly DependencyProperty PreviousStationArrivalValueProperty =
        DependencyProperty.Register("PreviousStationArrivalValue", typeof(string), typeof(TimetableStopsControl),
            new PropertyMetadata(StationPlaceholder));

    /// <summary>
    /// Previous station departure time (formatted string).
    /// </summary>
    public static readonly DependencyProperty PreviousStationDepartureValueProperty =
        DependencyProperty.Register("PreviousStationDepartureValue", typeof(string), typeof(TimetableStopsControl),
            new PropertyMetadata(StationPlaceholder));

    /// <summary>
    /// Previous station track/platform number.
    /// </summary>
    public static readonly DependencyProperty PreviousStationTrackValueProperty =
        DependencyProperty.Register("PreviousStationTrackValue", typeof(string), typeof(TimetableStopsControl),
            new PropertyMetadata(StationPlaceholder));

    /// <summary>
    /// Used to hide exit direction icons when there is no previous station.
    /// </summary>
    public static readonly DependencyProperty PreviousStationHasValueProperty =
        DependencyProperty.Register("PreviousStationHasValue", typeof(bool), typeof(TimetableStopsControl),
            new PropertyMetadata(false));

    /// <summary>
    /// Used to select the exit direction icon for the previous station.
    /// </summary>
    public static readonly DependencyProperty PreviousStationIsExitOnLeftProperty =
        DependencyProperty.Register("PreviousStationIsExitOnLeft", typeof(bool), typeof(TimetableStopsControl),
            new PropertyMetadata(false));

    /// <summary>
    /// Current station name.
    /// </summary>
    public static readonly DependencyProperty CurrentStationNameValueProperty =
        DependencyProperty.Register("CurrentStationNameValue", typeof(string), typeof(TimetableStopsControl),
            new PropertyMetadata(StationPlaceholder));

    /// <summary>
    /// Current station arrival time (formatted string).
    /// </summary>
    public static readonly DependencyProperty CurrentStationArrivalValueProperty =
        DependencyProperty.Register("CurrentStationArrivalValue", typeof(string), typeof(TimetableStopsControl),
            new PropertyMetadata(StationPlaceholder));

    /// <summary>
    /// Current station departure time (formatted string).
    /// </summary>
    public static readonly DependencyProperty CurrentStationDepartureValueProperty =
        DependencyProperty.Register("CurrentStationDepartureValue", typeof(string), typeof(TimetableStopsControl),
            new PropertyMetadata(StationPlaceholder));

    /// <summary>
    /// Current station track/platform number.
    /// </summary>
    public static readonly DependencyProperty CurrentStationTrackValueProperty =
        DependencyProperty.Register("CurrentStationTrackValue", typeof(string), typeof(TimetableStopsControl),
            new PropertyMetadata(StationPlaceholder));

    /// <summary>
    /// Used to hide exit direction icons when there is no current station.
    /// </summary>
    public static readonly DependencyProperty CurrentStationHasValueProperty =
        DependencyProperty.Register("CurrentStationHasValue", typeof(bool), typeof(TimetableStopsControl),
            new PropertyMetadata(false));

    /// <summary>
    /// Used to select the exit direction icon for the current station.
    /// </summary>
    public static readonly DependencyProperty CurrentStationIsExitOnLeftProperty =
        DependencyProperty.Register("CurrentStationIsExitOnLeft", typeof(bool), typeof(TimetableStopsControl),
            new PropertyMetadata(false));

    /// <summary>
    /// Next station name.
    /// </summary>
    public static readonly DependencyProperty NextStationNameValueProperty =
        DependencyProperty.Register("NextStationNameValue", typeof(string), typeof(TimetableStopsControl),
            new PropertyMetadata(StationPlaceholder));

    /// <summary>
    /// Next station arrival time (formatted string).
    /// </summary>
    public static readonly DependencyProperty NextStationArrivalValueProperty =
        DependencyProperty.Register("NextStationArrivalValue", typeof(string), typeof(TimetableStopsControl),
            new PropertyMetadata(StationPlaceholder));

    /// <summary>
    /// Next station departure time (formatted string).
    /// </summary>
    public static readonly DependencyProperty NextStationDepartureValueProperty =
        DependencyProperty.Register("NextStationDepartureValue", typeof(string), typeof(TimetableStopsControl),
            new PropertyMetadata(StationPlaceholder));

    /// <summary>
    /// Next station track/platform number.
    /// </summary>
    public static readonly DependencyProperty NextStationTrackValueProperty =
        DependencyProperty.Register("NextStationTrackValue", typeof(string), typeof(TimetableStopsControl),
            new PropertyMetadata(StationPlaceholder));

    /// <summary>
    /// Used to hide exit direction icons when there is no next station.
    /// </summary>
    public static readonly DependencyProperty NextStationHasValueProperty =
        DependencyProperty.Register("NextStationHasValue", typeof(bool), typeof(TimetableStopsControl),
            new PropertyMetadata(false));

    /// <summary>
    /// Used to select the exit direction icon for the next station.
    /// </summary>
    public static readonly DependencyProperty NextStationIsExitOnLeftProperty =
        DependencyProperty.Register("NextStationIsExitOnLeft", typeof(bool), typeof(TimetableStopsControl),
            new PropertyMetadata(false));

    public TimetableStopsControl()
    {
        InitializeComponent();
    }

    // === Previous Station Properties ===

    public string PreviousStationNameValue
    {
        get => (string)GetValue(PreviousStationNameValueProperty);
        set => SetValue(PreviousStationNameValueProperty, value);
    }

    public string PreviousStationArrivalValue
    {
        get => (string)GetValue(PreviousStationArrivalValueProperty);
        set => SetValue(PreviousStationArrivalValueProperty, value);
    }

    public string PreviousStationDepartureValue
    {
        get => (string)GetValue(PreviousStationDepartureValueProperty);
        set => SetValue(PreviousStationDepartureValueProperty, value);
    }

    public string PreviousStationTrackValue
    {
        get => (string)GetValue(PreviousStationTrackValueProperty);
        set => SetValue(PreviousStationTrackValueProperty, value);
    }

    public bool PreviousStationHasValue
    {
        get => (bool)GetValue(PreviousStationHasValueProperty);
        set => SetValue(PreviousStationHasValueProperty, value);
    }

    public bool PreviousStationIsExitOnLeft
    {
        get => (bool)GetValue(PreviousStationIsExitOnLeftProperty);
        set => SetValue(PreviousStationIsExitOnLeftProperty, value);
    }

    // === Current Station Properties ===

    public string CurrentStationNameValue
    {
        get => (string)GetValue(CurrentStationNameValueProperty);
        set => SetValue(CurrentStationNameValueProperty, value);
    }

    public string CurrentStationArrivalValue
    {
        get => (string)GetValue(CurrentStationArrivalValueProperty);
        set => SetValue(CurrentStationArrivalValueProperty, value);
    }

    public string CurrentStationDepartureValue
    {
        get => (string)GetValue(CurrentStationDepartureValueProperty);
        set => SetValue(CurrentStationDepartureValueProperty, value);
    }

    public string CurrentStationTrackValue
    {
        get => (string)GetValue(CurrentStationTrackValueProperty);
        set => SetValue(CurrentStationTrackValueProperty, value);
    }

    public bool CurrentStationHasValue
    {
        get => (bool)GetValue(CurrentStationHasValueProperty);
        set => SetValue(CurrentStationHasValueProperty, value);
    }

    public bool CurrentStationIsExitOnLeft
    {
        get => (bool)GetValue(CurrentStationIsExitOnLeftProperty);
        set => SetValue(CurrentStationIsExitOnLeftProperty, value);
    }

    // === Next Station Properties ===

    public string NextStationNameValue
    {
        get => (string)GetValue(NextStationNameValueProperty);
        set => SetValue(NextStationNameValueProperty, value);
    }

    public string NextStationArrivalValue
    {
        get => (string)GetValue(NextStationArrivalValueProperty);
        set => SetValue(NextStationArrivalValueProperty, value);
    }

    public string NextStationDepartureValue
    {
        get => (string)GetValue(NextStationDepartureValueProperty);
        set => SetValue(NextStationDepartureValueProperty, value);
    }

    public string NextStationTrackValue
    {
        get => (string)GetValue(NextStationTrackValueProperty);
        set => SetValue(NextStationTrackValueProperty, value);
    }

    public bool NextStationHasValue
    {
        get => (bool)GetValue(NextStationHasValueProperty);
        set => SetValue(NextStationHasValueProperty, value);
    }

    public bool NextStationIsExitOnLeft
    {
        get => (bool)GetValue(NextStationIsExitOnLeftProperty);
        set => SetValue(NextStationIsExitOnLeftProperty, value);
    }
}
