// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Unit;

using Moba.TrackLibrary.PikoA;
using Moba.TrackLibrary.PikoA.Import;
using Moba.Vision;

[TestFixture]
internal class PikoACodeExtractorTest
{
    private static VisionReadLine Line(string text, int x, int y, params (string Text, int X, int Y)[] words)
    {
        var poly = Square(x, y, 40, 20);
        var wordList = words.Length == 0
            ? new List<VisionReadWord> { new(text, 0.95f, poly) }
            : words.Select(w => new VisionReadWord(w.Text, 0.95f, Square(w.X, w.Y, 40, 20))).ToList();
        return new VisionReadLine(text, wordList, poly);
    }

    private static IReadOnlyList<VisionPoint> Square(int cx, int cy, int w, int h) =>
    [
        new(cx - w / 2, cy - h / 2),
        new(cx + w / 2, cy - h / 2),
        new(cx + w / 2, cy + h / 2),
        new(cx - w / 2, cy + h / 2),
    ];

    private static VisionReadResult Result(params VisionReadLine[] lines) =>
        new(lines, 0, 2034, 862);

    [Test]
    public void Extract_ReturnsEmptyForEmptyResult()
    {
        var outcome = PikoACodeExtractor.Extract(VisionReadResult.Empty);

        Assert.That(outcome.Matches, Is.Empty);
        Assert.That(outcome.Unresolved, Is.Empty);
        Assert.That(outcome.MatchCountsByCode, Is.Empty);
    }

    [Test]
    public void Extract_MatchesAllExactCatalogCodes()
    {
        var result = Result(
            Line("G231", 659, 40),
            Line("G239", 1188, 40),
            Line("G119", 1324, 181),
            Line("G62", 1619, 182),
            Line("R1", 320, 143),
            Line("R2", 307, 98),
            Line("R3", 295, 54),
            Line("WL", 1539, 48),
            Line("WR", 483, 47),
            Line("W3", 655, 88),
            Line("DKW", 1013, 134));

        var outcome = PikoACodeExtractor.Extract(result);

        Assert.That(outcome.Matches.Count, Is.EqualTo(11));
        Assert.That(outcome.Unresolved, Is.Empty);
        Assert.That(outcome.MatchCountsByCode["G231"], Is.EqualTo(1));
        Assert.That(outcome.MatchCountsByCode["DKW"], Is.EqualTo(1));
        Assert.That(outcome.Matches.First(m => m.RawText == "G231").CatalogEntry.Code, Is.EqualTo("G231"));
    }

    [Test]
    public void Extract_SplitsCompoundLineIntoSeparateCandidatesUsingWordPolygons()
    {
        var result = Result(
            Line("G62 G119", 840, 181,
                ("G62", 808, 181),
                ("G119", 872, 181)));

        var outcome = PikoACodeExtractor.Extract(result);

        Assert.That(outcome.Matches.Count, Is.EqualTo(2));
        Assert.That(outcome.Matches[0].CatalogEntry.Code, Is.EqualTo("G62"));
        Assert.That(outcome.Matches[0].CenterX, Is.EqualTo(808d));
        Assert.That(outcome.Matches[1].CatalogEntry.Code, Is.EqualTo("G119"));
        Assert.That(outcome.Matches[1].CenterX, Is.EqualTo(872d));
        Assert.That(outcome.Unresolved, Is.Empty);
    }

    [Test]
    public void Extract_TrimsTrailingPunctuation_R3DotBecomesR3()
    {
        var result = Result(Line("R3.", 1731, 807));

        var outcome = PikoACodeExtractor.Extract(result);

        Assert.That(outcome.Matches.Count, Is.EqualTo(1));
        Assert.That(outcome.Matches[0].CatalogEntry.Code, Is.EqualTo("R3"));
        Assert.That(outcome.Matches[0].RawText, Is.EqualTo("R3."));
        Assert.That(outcome.Unresolved, Is.Empty);
    }

    [Test]
    public void Extract_DoesNotAcceptFuzzyTokens_UnderStrictPolicy()
    {
        // These were observed in the real OCR output (anyrail2.png). Under the strict policy
        // they MUST all land in Unresolved, not be auto-mapped.
        var result = Result(
            Line("Ri", 1826, 214),
            Line("R'", 200, 214),
            Line("RZ", 1942, 326),
            Line("R", 1942, 490),
            Line("G23", 1188, 134),
            Line("G6", 2001, 429));

        var outcome = PikoACodeExtractor.Extract(result);

        Assert.That(outcome.Matches, Is.Empty);
        Assert.That(outcome.Unresolved.Count, Is.EqualTo(6));
        Assert.That(outcome.Unresolved.Select(u => u.RawText),
            Is.EquivalentTo(new[] { "Ri", "R'", "RZ", "R", "G23", "G6" }));
    }

    [Test]
    public void Extract_IsCaseSensitive_LowerCaseIsUnresolved()
    {
        // Strict policy: 'g231' is not accepted as G231. This is intentional; OCR output is
        // uppercase for PIKO labels so lowercase is a signal of a different font/context.
        var result = Result(Line("g231", 659, 40));

        var outcome = PikoACodeExtractor.Extract(result);

        Assert.That(outcome.Matches, Is.Empty);
        Assert.That(outcome.Unresolved.Count, Is.EqualTo(1));
        Assert.That(outcome.Unresolved[0].RawText, Is.EqualTo("g231"));
    }

    [Test]
    public void Extract_CountsProduceExpectedBillOfMaterialsForAnyrail2Screenshot()
    {
        // Re-create the exact list of recognized lines from mobaflow-20260419.log (82 lines).
        // Expected outcome: every exact PIKO code is matched; every OCR artifact stays in
        // the unresolved bucket for manual review.
        var result = Result(
            Line("G231", 659, 40), Line("G231", 834, 40), Line("WL", 1539, 48),
            Line("R3", 295, 54), Line("WR", 483, 47), Line("G231", 1009, 41),
            Line("G239", 1188, 40), Line("G231", 1366, 40), Line("R3", 1726, 53),
            Line("R2", 307, 98), Line("G231", 482, 88), Line("W3", 655, 88),
            Line("WR", 838, 94), Line("G231", 1014, 88), Line("G231", 1190, 88),
            Line("W3", 1366, 87), Line("G231", 1547, 89), Line("R2", 1715, 98),
            Line("WL", 480, 128), Line("DKW", 1013, 134), Line("G23", 1188, 134),
            Line("G231", 1367, 134), Line("WR", 1542, 128), Line("R3", 134, 148),
            Line("R1", 320, 143), Line("G23", 658, 134), Line("G231", 834, 134),
            Line("R1", 1704, 143), Line("R3", 1893, 148), Line("R2", 169, 182),
            Line("G62", 404, 182), Line("G23", 518, 182), Line("G231", 694, 182),
            Line("G62 G119", 840, 181, ("G62", 808, 181), ("G119", 872, 181)),
            Line("G239", 1006, 181), Line("WR", 1188, 175), Line("G119", 1324, 181),
            Line("G62", 1394, 180), Line("G231", 1503, 181), Line("G62", 1619, 182),
            Line("R2", 1858, 180), Line("Ri", 1826, 214), Line("R'", 200, 214),
            Line("R3", 1989, 313), Line("RZ", 1942, 326), Line("R3", 41, 314),
            Line("G6", 2001, 429), Line("G62", 29, 433), Line("R", 1942, 490),
            Line("R1", 1828, 600), Line("R1", 200, 601), Line("R2", 1860, 632),
            Line("R2", 166, 631), Line("R1", 321, 670), Line("G231", 479, 679),
            Line("G239", 656, 680), Line("G231", 835, 679), Line("G239", 1013, 680),
            Line("G231", 1190, 680), Line("G239", 1368, 680), Line("G231", 1547, 680),
            Line("R1", 1704, 670), Line("R3", 132, 713), Line("R2", 310, 716),
            Line("G239", 659, 727), Line("G239", 1012, 727), Line("G231", 1191, 727),
            Line("G239", 1369, 727), Line("G231", 1545, 727), Line("R2", 1714, 715),
            Line("R3", 1894, 712), Line("G231", 482, 727), Line("G23", 832, 727),
            Line("R3", 296, 808), Line("G231", 480, 820), Line("G239", 659, 820),
            Line("G231", 836, 820), Line("G239", 1014, 819), Line("G231", 1193, 819),
            Line("G239", 1371, 820), Line("G231", 1548, 820), Line("R3.", 1731, 807));

        var outcome = PikoACodeExtractor.Extract(result);

        // Sanity: every unresolved token is one of the known OCR artifacts.
        Assert.That(outcome.Unresolved.Select(u => u.RawText).Distinct(),
            Is.SubsetOf(new[] { "Ri", "R'", "RZ", "R", "G23", "G6" }));

        // Bill of materials: every expected catalog code is present, no unexpected one leaks in.
        Assert.That(outcome.MatchCountsByCode.Keys,
            Is.EquivalentTo(new[] { "G231", "G239", "G119", "G62", "R1", "R2", "R3", "WL", "WR", "W3", "DKW" }));

        // Sanity per-code: the four "big" groups should clearly dominate.
        Assert.That(outcome.MatchCountsByCode["G231"], Is.GreaterThan(outcome.MatchCountsByCode["G239"]));
        Assert.That(outcome.MatchCountsByCode["G239"], Is.GreaterThan(outcome.MatchCountsByCode["G62"]));
        Assert.That(outcome.MatchCountsByCode["DKW"], Is.EqualTo(1));
        Assert.That(outcome.MatchCountsByCode["W3"], Is.EqualTo(2));

        // 82 OCR lines. The compound line "G62 G119" contributes 2 word-level tokens, so the
        // extractor sees 83 tokens in total. Every token is either matched or unresolved.
        Assert.That(outcome.Matches.Count + outcome.Unresolved.Count, Is.EqualTo(83));

        // No pseudo-codes leak through as matches.
        Assert.That(outcome.Matches.Select(m => m.RawText),
            Has.None.EqualTo("G23").And.None.EqualTo("Ri").And.None.EqualTo("R'").And.None.EqualTo("RZ").And.None.EqualTo("R").And.None.EqualTo("G6"));
    }
}
