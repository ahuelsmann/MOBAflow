// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Display;

using Domain;

/// <summary>
/// Resolves function button symbols and colors for a locomotive (shared between WinUI and MAUI).
/// </summary>
public static class LocomotiveFunctionAppearanceResolver
{
    public const string SignalGrayHex = "#808080";

    public static readonly string[] DefaultFunctionAssets =
    [
        "headlight.png",
        "f1__driving_sound.png"
    ];

    public static readonly string[] DefaultBacklightColors =
    [
        "#FFD700", "#0078D4", "#FF8C00", "#E81123", "#107C10", "#00B7C3", "#FFB900", "#767676",
        "#E81B23", "#7B68EE", "#8764B8", "#038387", "#C239B3", "#FF1493", "#7A7574", "#567C73",
        "#8E562E", "#847545", "#525E54", "#4A5459", "#69797E", "#69797E", "#69797E", "#69797E",
        "#69797E", "#69797E", "#69797E", "#69797E", "#69797E", "#69797E", "#69797E", "#69797E"
    ];

    public static string GetGlyph(Locomotive? locomotive, int functionIndex)
    {
        if (functionIndex < 0 || functionIndex > 31)
        {
            return string.Empty;
        }

        if (locomotive?.FunctionSymbols != null && functionIndex < locomotive.FunctionSymbols.Count)
        {
            var stored = locomotive.FunctionSymbols[functionIndex];
            if (stored == "none")
            {
                return string.Empty;
            }

            if (IsValidAssetReference(stored))
            {
                return stored.Trim();
            }
        }

        return functionIndex < DefaultFunctionAssets.Length ? DefaultFunctionAssets[functionIndex] : string.Empty;
    }

    public static string GetColor(Locomotive? locomotive, int functionIndex)
    {
        if (functionIndex < 0 || functionIndex > 31)
        {
            return SignalGrayHex;
        }

        if (locomotive?.FunctionColors != null && functionIndex < locomotive.FunctionColors.Count)
        {
            var stored = locomotive.FunctionColors[functionIndex];
            if (stored == "none")
            {
                return SignalGrayHex;
            }

            if (IsValidHexColor(stored))
            {
                return stored;
            }
        }

        return functionIndex < DefaultBacklightColors.Length ? DefaultBacklightColors[functionIndex] : SignalGrayHex;
    }

    public static bool IsValidAssetReference(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.EndsWith(".png", StringComparison.OrdinalIgnoreCase);

    public static bool IsValidHexColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 7 || value[0] != '#')
        {
            return false;
        }

        for (var i = 1; i < value.Length; i++)
        {
            if (!char.IsAsciiHexDigit(value[i]))
            {
                return false;
            }
        }

        return true;
    }
}
