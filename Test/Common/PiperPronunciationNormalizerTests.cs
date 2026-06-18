// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Speech;

/// <summary>
/// Tests for Piper announcement text normalization.
/// </summary>
[TestFixture]
internal sealed class PiperPronunciationNormalizerTests
{
    [Test]
    public void Normalize_Should_ReturnInput_WhenDisabled()
    {
        var text = "Nächster Halt Berlin Hbf, Gleis 3.";

        Assert.That(
            PiperPronunciationNormalizer.Normalize(text, enabled: false),
            Is.EqualTo(text));
    }

    [Test]
    public void Normalize_Should_ExpandRailwayAbbreviations()
    {
        var result = PiperPronunciationNormalizer.Normalize("Nächster Halt Berlin Hbf.");

        Assert.That(result, Is.EqualTo("Nächster Halt Berlin Hauptbahnhof."));
    }

    [Test]
    public void Normalize_Should_ConvertContextualNumbersToGermanWords()
    {
        var result = PiperPronunciationNormalizer.Normalize("Bitte einsteigen, Gleis 3, Bahnsteig 12.");

        Assert.That(result, Is.EqualTo("Bitte einsteigen, Gleis drei, Bahnsteig zwölf."));
    }

    [Test]
    public void Normalize_Should_ApplyCustomReplacements_BeforeBuiltInRules()
    {
        var replacements = new Dictionary<string, string>
        {
            ["Wuppertal Hbf"] = "Wuppertal Hauptbahnhof"
        };

        var result = PiperPronunciationNormalizer.Normalize(
            "Nächster Halt Wuppertal Hbf.",
            replacements);

        Assert.That(result, Is.EqualTo("Nächster Halt Wuppertal Hauptbahnhof."));
    }

    [Test]
    public void Normalize_Should_NormalizePunctuationSpacing()
    {
        var result = PiperPronunciationNormalizer.Normalize("Nächster Halt:Hauptbahnhof,Ausstieg links.");

        Assert.That(result, Is.EqualTo("Nächster Halt: Hauptbahnhof, Ausstieg links."));
    }

    [TestCase(0, "null")]
    [TestCase(1, "eins")]
    [TestCase(21, "einundzwanzig")]
    [TestCase(101, "einhunderteins")]
    [TestCase(123, "einhundertdreiundzwanzig")]
    public void ToGermanWords_Should_ConvertExpectedValues(int number, string expected)
    {
        Assert.That(PiperPronunciationNormalizer.ToGermanWords(number), Is.EqualTo(expected));
    }
}