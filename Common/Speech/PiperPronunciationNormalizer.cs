// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Speech;

using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// Prepares German announcement text for Piper TTS by expanding abbreviations,
/// converting contextual numbers to words, and applying custom replacements.
/// </summary>
public static partial class PiperPronunciationNormalizer
{
    private static readonly (string Pattern, string Replacement)[] BuiltInAbbreviationRules =
    [
        (@"\bHbf\.?\b", "Hauptbahnhof"),
        (@"\bBf\.?\b", "Bahnhof"),
        (@"\bHp\.?\b", "Haltepunkt"),
        (@"\bBstg\.?\b", "Bahnsteig"),
        (@"\bGl\.?\b", "Gleis"),
        (@"\bStr\.?\b", "Straße"),
        (@"\bSt\.?\b", "Sankt"),
        (@"\bNr\.?\b", "Nummer"),
        (@"\bca\.?\b", "circa"),
        (@"\bkm/h\b", "Kilometer pro Stunde"),
        (@"\bkm\b", "Kilometer"),
        (@"&", "und"),
        (@"\+", "plus"),
        (@"/", " Schrägstrich "),
    ];

    /// <summary>
    /// Normalizes announcement text to improve Piper pronunciation quality.
    /// </summary>
    /// <param name="text">Raw announcement text.</param>
    /// <param name="customReplacements">Optional phrase replacements (longest keys first).</param>
    /// <param name="enabled">When false, returns the input unchanged.</param>
    public static string Normalize(
        string? text,
        IReadOnlyDictionary<string, string>? customReplacements = null,
        bool enabled = true)
    {
        if (!enabled || string.IsNullOrWhiteSpace(text))
        {
            return text ?? string.Empty;
        }

        var normalized = text.Trim();
        normalized = ApplyCustomReplacements(normalized, customReplacements);
        normalized = ApplyBuiltInAbbreviations(normalized);
        normalized = ExpandContextualNumbers(normalized);
        normalized = NormalizePunctuation(normalized);
        normalized = CollapseWhitespace(normalized);

        return normalized;
    }

    private static string ApplyCustomReplacements(string text, IReadOnlyDictionary<string, string>? customReplacements)
    {
        if (customReplacements is not { Count: > 0 })
        {
            return text;
        }

        foreach (var (source, target) in customReplacements
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
            .OrderByDescending(pair => pair.Key.Length))
        {
            text = Regex.Replace(
                text,
                Regex.Escape(source.Trim()),
                target.Trim(),
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return text;
    }

    private static string ApplyBuiltInAbbreviations(string text)
    {
        foreach (var (pattern, replacement) in BuiltInAbbreviationRules)
        {
            text = Regex.Replace(text, pattern, replacement, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return text;
    }

    private static string ExpandContextualNumbers(string text)
    {
        return ContextualNumberRegex().Replace(text, match =>
        {
            if (!int.TryParse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            {
                return match.Value;
            }

            return $"{match.Groups[1].Value} {ToGermanWords(number)}";
        });
    }

    private static string NormalizePunctuation(string text)
    {
        text = text.Replace(';', ',');
        text = Regex.Replace(text, @"\.{2,}", ".");
        text = Regex.Replace(text, @"(?<=\w):(?=\S)", ": ");
        text = Regex.Replace(text, @"(?<=\w),(?=\S)", ", ");
        return text;
    }

    private static string CollapseWhitespace(string text) =>
        WhitespaceRegex().Replace(text, " ").Trim();

    /// <summary>
    /// Converts numbers from 0 to 999 into German words for clearer TTS output.
    /// </summary>
    public static string ToGermanWords(int number)
    {
        if (number is < 0 or > 999)
        {
            return number.ToString(CultureInfo.InvariantCulture);
        }

        if (number == 0)
        {
            return "null";
        }

        if (number < 20)
        {
            return GetOnesWord(number, standalone: true);
        }

        if (number < 100)
        {
            var ones = number % 10;
            var tens = number / 10;
            return ones == 0
                ? GetTensWord(tens)
                : $"{GetOnesWord(ones, standalone: false)}und{GetTensWord(tens)}";
        }

        var hundreds = number / 100;
        var remainder = number % 100;
        var hundredPart = hundreds == 1
            ? "einhundert"
            : $"{GetOnesWord(hundreds, standalone: false)}hundert";

        return remainder == 0
            ? hundredPart
            : $"{hundredPart}{ToGermanWords(remainder)}";
    }

    private static string GetOnesWord(int number, bool standalone) =>
        number switch
        {
            1 when !standalone => "ein",
            1 => "eins",
            2 => "zwei",
            3 => "drei",
            4 => "vier",
            5 => "fünf",
            6 => "sechs",
            7 => "sieben",
            8 => "acht",
            9 => "neun",
            10 => "zehn",
            11 => "elf",
            12 => "zwölf",
            13 => "dreizehn",
            14 => "vierzehn",
            15 => "fünfzehn",
            16 => "sechzehn",
            17 => "siebzehn",
            18 => "achtzehn",
            19 => "neunzehn",
            _ => number.ToString(CultureInfo.InvariantCulture)
        };

    private static string GetTensWord(int tens) =>
        tens switch
        {
            2 => "zwanzig",
            3 => "dreißig",
            4 => "vierzig",
            5 => "fünfzig",
            6 => "sechzig",
            7 => "siebzig",
            8 => "achtzig",
            9 => "neunzig",
            _ => tens.ToString(CultureInfo.InvariantCulture)
        };

    [GeneratedRegex(@"\b(Gleis|Bahnsteig|Plattform|Gleisnummer|Spur)\s*(\d{1,3})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ContextualNumberRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}
