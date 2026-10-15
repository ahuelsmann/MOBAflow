// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Configuration;

/// <summary>Canonical app-local UI language preferences, independent of speech and solution data.</summary>
public static class UiLanguagePreference
{
    /// <summary>Follow the system UI language at startup.</summary>
    public const string System = "system";
    /// <summary>Use English UI text.</summary>
    public const string English = "en-US";
    /// <summary>Use German UI text.</summary>
    public const string German = "de-DE";

    /// <summary>Normalizes supported preferences and treats invalid values as System.</summary>
    public static string Normalize(string? value)
    {
        if (string.Equals(value, English, StringComparison.OrdinalIgnoreCase)) return English;
        if (string.Equals(value, German, StringComparison.OrdinalIgnoreCase)) return German;
        return System;
    }
}
