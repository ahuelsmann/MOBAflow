// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Common.Configuration;
using Moba.SharedUI.Localization;
using System.Globalization;

[TestFixture]
internal sealed class UiLocalizerTests
{
    [TestCase("system", "de-AT", "de-DE")]
    [TestCase("system", "de-CH", "de-DE")]
    [TestCase("system", "fr-FR", "en-US")]
    [TestCase("en-US", "de-DE", "en-US")]
    [TestCase("de-DE", "en-US", "de-DE")]
    [TestCase("invalid", "de-DE", "de-DE")]
    [TestCase(null, "en-US", "en-US")]
    public void StartupPolicyResolvesPreferenceAndSystem(string? preference, string system, string expected)
    {
        Assert.That(UiLanguagePolicy.Resolve(preference, CultureInfo.GetCultureInfo(system)).Name, Is.EqualTo(expected));
    }

    [TestCase("en-US", "UI language")]
    [TestCase("de-DE", "Sprache der Benutzeroberfläche")]
    [TestCase("fr-FR", "UI language")]
    public void ResourceLookupUsesTranslationOrCompleteEnglishFallback(string culture, string expected)
    {
        var text = new UiLocalizer(CultureInfo.GetCultureInfo(culture));
        Assert.That(text.Get("LanguageTitle"), Is.EqualTo(expected));
    }

    [TestCase("en-US", "12.5", "10/14/2026")]
    [TestCase("de-DE", "12,5", "14.10.2026")]
    public void TemplatesUseDisplayCultureAndPreserveArgumentMeaning(string culture, string number, string date)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var text = new UiLocalizer(CultureInfo.GetCultureInfo(culture));
        var result = text.Format("Version", 12.5m);
        var dateResult = text.Format("Version", new DateTime(2026, 10, 14));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Does.Contain(number));
            Assert.That(dateResult, Does.Contain(date));
            Assert.That(CultureInfo.CurrentCulture, Is.SameAs(originalCulture));
        }
    }

    [Test]
    public void UnknownKeyIsDetectedBeforeAVisibleKeyCanEscape()
    {
        var text = new UiLocalizer(CultureInfo.GetCultureInfo("en-US"));
        Assert.Throws<InvalidOperationException>(() => text.Get("UnregisteredKey"));
    }

    [TestCase("{\"Application\":{\"UiLanguage\":\"de-DE\"}}", "de-DE")]
    [TestCase("{\"application\":{\"uiLanguage\":\"en-US\"}}", "en-US")]
    [TestCase("{\"Application\":{\"UiLanguage\":null}}", "system")]
    [TestCase("{\"Application\":{\"UiLanguage\":123}}", "system")]
    [TestCase("{\"Application\":{\"UiLanguage\":\"fr-FR\"}}", "system")]
    [TestCase("[]", "system")]
    [TestCase("{malformed", "system")]
    public void StartupReadHandlesHostCasingAndMalformedValues(string json, string expected)
    {
        var path = Path.Combine(Path.GetTempPath(), "mobaflow-ui-language-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, json);
            Assert.That(UiLanguagePolicy.ReadSavedPreference(path), Is.EqualTo(expected));
        }
        finally { File.Delete(path); }
    }

    [Test]
    public void MissingStartupSettingsUseSystem()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.json");
        Assert.That(UiLanguagePolicy.ReadSavedPreference(path), Is.EqualTo(UiLanguagePreference.System));
    }
}
