// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Localization;

using Common.Configuration;
using System.Globalization;
using System.Text.Json;

/// <summary>Resolves startup UI culture without changing technical or speech formatting.</summary>
public static class UiLanguagePolicy
{
    /// <summary>Resolves a local preference against the system's UI culture.</summary>
    public static CultureInfo Resolve(string? preference, CultureInfo systemUiCulture)
    {
        ArgumentNullException.ThrowIfNull(systemUiCulture);
        var language = UiLanguagePreference.Normalize(preference);
        if (language == UiLanguagePreference.System)
        {
            language = systemUiCulture.TwoLetterISOLanguageName == "de"
                ? UiLanguagePreference.German : UiLanguagePreference.English;
        }
        return CultureInfo.GetCultureInfo(language);
    }

    /// <summary>Reads only the language preference before host UI construction; malformed/unreadable settings use System.</summary>
    public static string ReadSavedPreference(string settingsPath)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(settingsPath));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return UiLanguagePreference.System;
            if (!(root.TryGetProperty("Application", out var application) || root.TryGetProperty("application", out application))
                || application.ValueKind != JsonValueKind.Object) return UiLanguagePreference.System;
            if (!(application.TryGetProperty("UiLanguage", out var language) || application.TryGetProperty("uiLanguage", out language))
                || language.ValueKind != JsonValueKind.String) return UiLanguagePreference.System;
            return UiLanguagePreference.Normalize(language.GetString());
        }
        catch (IOException) { return UiLanguagePreference.System; }
        catch (UnauthorizedAccessException) { return UiLanguagePreference.System; }
        catch (JsonException) { return UiLanguagePreference.System; }
    }

    /// <summary>Applies resource culture only; callers retain their existing CurrentCulture.</summary>
    public static void ApplyResourceCulture(CultureInfo culture)
    {
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}
