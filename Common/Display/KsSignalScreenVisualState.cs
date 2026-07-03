// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Display;

/// <summary>
/// Lamp color for a single LED on the shared Ks signal screen grid.
/// </summary>
public enum KsSignalLampColor
{
    Off,
    Red,
    Green,
    Yellow,
    White
}

/// <summary>
/// Which lamp blinks for animated aspects (Ks1Blink, Zs1, 4046 Dunkel).
/// </summary>
public enum KsSignalBlinkLamp
{
    None,
    Ks1,
    W1
}

/// <summary>
/// Platform-neutral visual state for the fixed Ks signal screen grid.
/// All aspects share the same layout; only lamp colors and speed indicators differ.
/// </summary>
public sealed class KsSignalScreenVisualState
{
    public KsSignalLampColor W1 { get; init; } = KsSignalLampColor.Off;

    public KsSignalLampColor Hp0 { get; init; } = KsSignalLampColor.Off;

    public KsSignalLampColor Ks1 { get; init; } = KsSignalLampColor.Off;

    public KsSignalLampColor Ks2 { get; init; } = KsSignalLampColor.Off;

    public KsSignalLampColor W2 { get; init; } = KsSignalLampColor.Off;

    public KsSignalLampColor Zs7Center { get; init; } = KsSignalLampColor.Off;

    public KsSignalLampColor Zs7Right { get; init; } = KsSignalLampColor.Off;

    public KsSignalLampColor W3 { get; init; } = KsSignalLampColor.Off;

    public KsSignalLampColor Ra12Right { get; init; } = KsSignalLampColor.Off;

    public bool ShowTopSpeed { get; init; }

    public string TopSpeedText { get; init; } = string.Empty;

    public bool ShowBottomSpeed { get; init; }

    public string BottomSpeedText { get; init; } = string.Empty;

    public KsSignalBlinkLamp BlinkLamp { get; init; } = KsSignalBlinkLamp.None;

    /// <summary>
    /// Resolves lamp colors and speed indicators for the given signal article and aspect.
    /// </summary>
    public static KsSignalScreenVisualState Create(
        string? signalArticleNumber,
        string aspect,
        string? topSpeedValue = null,
        string? bottomSpeedValue = null)
    {
        return KsSignalAspectNames.Is4046Signal(signalArticleNumber)
            ? Create4046(aspect, topSpeedValue, bottomSpeedValue)
            : CreateStandard(aspect);
    }

    private static KsSignalScreenVisualState CreateStandard(string aspect)
    {
        return aspect switch
        {
            KsSignalAspectNames.Hp0 => new KsSignalScreenVisualState { Hp0 = KsSignalLampColor.Red },
            KsSignalAspectNames.Ks1 => new KsSignalScreenVisualState { Ks1 = KsSignalLampColor.Green },
            KsSignalAspectNames.Ks2 => new KsSignalScreenVisualState { Ks2 = KsSignalLampColor.Yellow },
            KsSignalAspectNames.Ks1Blink => new KsSignalScreenVisualState
            {
                Ks1 = KsSignalLampColor.Green,
                BlinkLamp = KsSignalBlinkLamp.Ks1
            },
            KsSignalAspectNames.Kennlicht => new KsSignalScreenVisualState { W1 = KsSignalLampColor.White },
            KsSignalAspectNames.Dunkel => new KsSignalScreenVisualState(),
            KsSignalAspectNames.Ra12 => new KsSignalScreenVisualState
            {
                W3 = KsSignalLampColor.White,
                Ra12Right = KsSignalLampColor.White
            },
            KsSignalAspectNames.Zs1 => new KsSignalScreenVisualState
            {
                W1 = KsSignalLampColor.White,
                BlinkLamp = KsSignalBlinkLamp.W1
            },
            KsSignalAspectNames.Zs7 => new KsSignalScreenVisualState
            {
                W2 = KsSignalLampColor.Yellow,
                Zs7Center = KsSignalLampColor.Yellow,
                Zs7Right = KsSignalLampColor.Yellow
            },
            _ => new KsSignalScreenVisualState()
        };
    }

    private static KsSignalScreenVisualState Create4046(
        string aspect,
        string? topSpeedValue,
        string? bottomSpeedValue)
    {
        return aspect switch
        {
            KsSignalAspectNames.Hp0 => new KsSignalScreenVisualState { Hp0 = KsSignalLampColor.Red },
            KsSignalAspectNames.Ks1 => new KsSignalScreenVisualState { Ks1 = KsSignalLampColor.Green },
            KsSignalAspectNames.Ra12 => new KsSignalScreenVisualState
            {
                Hp0 = KsSignalLampColor.Red,
                Zs7Center = KsSignalLampColor.White
            },
            KsSignalAspectNames.Zs1 => WithTopSpeed(new KsSignalScreenVisualState { Ks1 = KsSignalLampColor.Green }, topSpeedValue),
            KsSignalAspectNames.Ks2 => new KsSignalScreenVisualState
            {
                Ks2 = KsSignalLampColor.Yellow,
                W1 = KsSignalLampColor.White
            },
            KsSignalAspectNames.Ks1Blink => WithTopSpeed(new KsSignalScreenVisualState
            {
                Ks2 = KsSignalLampColor.Yellow,
                W1 = KsSignalLampColor.White
            }, topSpeedValue),
            KsSignalAspectNames.Kennlicht => new KsSignalScreenVisualState { W1 = KsSignalLampColor.White },
            KsSignalAspectNames.Dunkel => WithBottomSpeed(
                WithTopSpeed(new KsSignalScreenVisualState
                {
                    W1 = KsSignalLampColor.White,
                    Ks1 = KsSignalLampColor.Green,
                    BlinkLamp = KsSignalBlinkLamp.Ks1
                }, topSpeedValue),
                bottomSpeedValue),
            KsSignalAspectNames.Zs7 => new KsSignalScreenVisualState
            {
                W2 = KsSignalLampColor.Yellow,
                Zs7Center = KsSignalLampColor.Yellow,
                Zs7Right = KsSignalLampColor.Yellow
            },
            _ => new KsSignalScreenVisualState()
        };
    }

    private static KsSignalScreenVisualState WithTopSpeed(KsSignalScreenVisualState state, string? topSpeedValue)
    {
        return new KsSignalScreenVisualState
        {
            W1 = state.W1,
            Hp0 = state.Hp0,
            Ks1 = state.Ks1,
            Ks2 = state.Ks2,
            W2 = state.W2,
            Zs7Center = state.Zs7Center,
            Zs7Right = state.Zs7Right,
            W3 = state.W3,
            Ra12Right = state.Ra12Right,
            BlinkLamp = state.BlinkLamp,
            ShowTopSpeed = true,
            TopSpeedText = FormatSpeedIndicatorValue(topSpeedValue),
            ShowBottomSpeed = state.ShowBottomSpeed,
            BottomSpeedText = state.BottomSpeedText
        };
    }

    private static KsSignalScreenVisualState WithBottomSpeed(KsSignalScreenVisualState state, string? bottomSpeedValue)
    {
        return new KsSignalScreenVisualState
        {
            W1 = state.W1,
            Hp0 = state.Hp0,
            Ks1 = state.Ks1,
            Ks2 = state.Ks2,
            W2 = state.W2,
            Zs7Center = state.Zs7Center,
            Zs7Right = state.Zs7Right,
            W3 = state.W3,
            Ra12Right = state.Ra12Right,
            BlinkLamp = state.BlinkLamp,
            ShowTopSpeed = state.ShowTopSpeed,
            TopSpeedText = state.TopSpeedText,
            ShowBottomSpeed = true,
            BottomSpeedText = FormatSpeedIndicatorValue(bottomSpeedValue)
        };
    }

    private static string FormatSpeedIndicatorValue(string? speedCode)
    {
        return string.IsNullOrWhiteSpace(speedCode) ? "--" : speedCode;
    }
}
