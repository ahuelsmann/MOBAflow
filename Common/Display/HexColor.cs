// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Display;

using System.Globalization;

/// <summary>
/// Parses the hex colours stored for locomotive function buttons.
/// </summary>
public static class HexColor
{
    /// <summary>
    /// Parses <c>RRGGBB</c> or <c>AARRGGBB</c>, each with optional leading <c>#</c>; the alpha part must be hex
    /// but is ignored. Whitespace and any other character make the value invalid.
    /// </summary>
    public static bool TryParseRgb(string? value, out byte red, out byte green, out byte blue)
    {
        red = green = blue = 0;
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var hex = value.AsSpan().TrimStart('#');
        var offset = hex.Length switch
        {
            6 => 0,
            8 => 2,
            _ => -1,
        };

        return offset >= 0
               && (offset == 0 || TryParseByte(hex[..2], out _))
               && TryParseByte(hex.Slice(offset, 2), out red)
               && TryParseByte(hex.Slice(offset + 2, 2), out green)
               && TryParseByte(hex.Slice(offset + 4, 2), out blue);
    }

    /// <summary>Whether <paramref name="value"/> has the stored form <c>#RRGGBB</c>.</summary>
    public static bool IsStoredForm(string? value) =>
        value is { Length: 7 } && value[0] == '#' && TryParseRgb(value, out _, out _, out _);

    private static bool TryParseByte(ReadOnlySpan<char> digits, out byte value) =>
        byte.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
}
