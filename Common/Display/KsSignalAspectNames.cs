// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Display;

using Domain;

/// <summary>
/// Shared signal-aspect naming and 4046 preview helpers used by WinUI and MAUI signal controls.
/// </summary>
public static class KsSignalAspectNames
{
    public const string Hp0 = nameof(SignalAspect.Hp0);
    public const string Ks1 = nameof(SignalAspect.Ks1);
    public const string Ks2 = nameof(SignalAspect.Ks2);
    public const string Ks1Blink = nameof(SignalAspect.Ks1Blink);
    public const string Kennlicht = nameof(SignalAspect.Kennlicht);
    public const string Dunkel = nameof(SignalAspect.Dunkel);
    public const string Ra12 = nameof(SignalAspect.Ra12);
    public const string Zs1 = nameof(SignalAspect.Zs1);
    public const string Zs7 = nameof(SignalAspect.Zs7);

    public static string ToAspectName(SignalAspect aspect) => aspect.ToString();

    public static string ResolvePreviewSignalArticleNumber(string? mainSignalArticleNumber)
    {
        return string.Equals(mainSignalArticleNumber, "4046", StringComparison.Ordinal) ? "4046" : string.Empty;
    }

    public static bool Is4046Signal(string? mainSignalArticleNumber)
    {
        return string.Equals(mainSignalArticleNumber, "4046", StringComparison.Ordinal);
    }

    public static string GetAspectLabel(SignalAspect aspect, bool is4046)
    {
        return aspect switch
        {
            SignalAspect.Hp0 => "Hp0",
            SignalAspect.Ks1 => "Ks1",
            SignalAspect.Ks2 => is4046 ? "Ks2+K" : "Ks2",
            SignalAspect.Ks1Blink => is4046 ? "Ks2+K+G" : "Ks1 blink",
            SignalAspect.Kennlicht => is4046 ? "Marker left" : "Marker",
            SignalAspect.Dunkel => is4046 ? "GrBl+K+G" : "Dark",
            SignalAspect.Ra12 => is4046 ? "Hp0+Rg" : "Ra12",
            SignalAspect.Zs1 => is4046 ? "Ks1+G" : "Zs1",
            SignalAspect.Zs7 => "Zs7",
            _ => aspect.ToString()
        };
    }
}
